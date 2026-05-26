using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SistemaRRHH.Models;

namespace SistemaRRHH.Controllers
{
    public class PlanillaController : Controller
    {
        private RecursosHumanosDBEntities db = new RecursosHumanosDBEntities();

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetAll(int? mes, int? anio)
        {
            try
            {
                int filtroMes = mes ?? DateTime.Now.Month;
                int filtroAnio = anio ?? DateTime.Now.Year;

                var planillas = await db.Planilla
                    .Include(p => p.Empleados)
                    .Where(p => p.Mes == filtroMes && p.Año == filtroAnio && p.Activo)
                    .Select(p => new
                    {
                        p.PlanillaID,
                        p.EmpleadoID,
                        NombreCompleto = p.Empleados.Nombre + " " + p.Empleados.Apellido,
                        p.Mes,
                        p.Año,
                        p.MontoTotal,
                        p.AFP,
                        p.ISSS,
                        p.Renta,
                        p.OtrasDeducciones,
                        p.Estado
                    })
                    .ToListAsync();

                decimal totalPlanilla = planillas.Sum(p => p.MontoTotal ?? 0);
                decimal totalDeducciones = planillas.Sum(p => (p.AFP ?? 0) + (p.ISSS ?? 0) + (p.Renta ?? 0) + (p.OtrasDeducciones ?? 0));
                decimal netoPagar = totalPlanilla - totalDeducciones;

                var estadisticas = new
                {
                    TotalBruto = totalPlanilla,
                    TotalDeducciones = totalDeducciones,
                    TotalNeto = netoPagar,
                    CantidadRegistros = planillas.Count
                };

                return Json(new { success = true, data = planillas, estadisticas = estadisticas }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public async Task<JsonResult> GenerarPlanilla(int mes, int anio)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    bool existe = await db.Planilla.AnyAsync(p => p.Mes == mes && p.Año == anio && p.Activo);
                    if (existe)
                    {
                        return Json(new { success = false, message = "La planilla para el mes y año seleccionados ya fue generada." });
                    }

                    var empleadosActivos = await db.Empleados
                        .Where(e => e.Activo)
                        .ToListAsync();

                    if (!empleadosActivos.Any())
                    {
                        return Json(new { success = false, message = "No se encontraron empleados activos para generar la planilla." });
                    }

                    foreach (var emp in empleadosActivos)
                    {
                        var salarioVigente = await db.Salarios
                            .Where(s => s.EmpleadoID == emp.EmpleadoID && s.Activo)
                            .OrderByDescending(s => s.FechaInicio)
                            .FirstOrDefaultAsync();

                        decimal sueldoBase = salarioVigente?.Monto ?? 0;

                        decimal afp = Math.Round(sueldoBase * 0.0725m, 2);
                        decimal isss = Math.Round(sueldoBase * 0.03m, 2);
                        if (isss > 30.00m) isss = 30.00m;

                        decimal sueldoPreRenta = sueldoBase - afp - isss;
                        decimal renta = 0;

                        if (sueldoPreRenta > 472.00m && sueldoPreRenta <= 895.24m)
                        {
                            renta = ((sueldoPreRenta - 472.00m) * 0.10m) + 17.67m;
                        }
                        else if (sueldoPreRenta > 895.24m && sueldoPreRenta <= 2038.10m)
                        {
                            renta = ((sueldoPreRenta - 895.24m) * 0.20m) + 60.00m;
                        }
                        else if (sueldoPreRenta > 2038.10m)
                        {
                            renta = ((sueldoPreRenta - 2038.10m) * 0.30m) + 288.57m;
                        }
                        renta = Math.Round(renta, 2);

                        Planilla nuevaPlanilla = new Planilla
                        {
                            EmpleadoID = emp.EmpleadoID,
                            Mes = mes,
                            Año = anio,
                            MontoTotal = sueldoBase,
                            AFP = afp,
                            ISSS = isss,
                            Renta = renta,
                            OtrasDeducciones = 0.00m,
                            Estado = "PENDIENTE",
                            Activo = true,
                            FechaCreacion = DateTime.Now
                        };

                        db.Planilla.Add(nuevaPlanilla);
                    }

                    await db.SaveChangesAsync();
                    transaction.Commit();

                    return Json(new { success = true, message = "Planilla mensual pre-calculada y generada con éxito de forma global." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        [HttpPost]
        public async Task<JsonResult> CambiarEstadoPlanilla(int id, string nuevoEstado)
        {
            try
            {
                var planilla = await db.Planilla.FindAsync(id);
                if (planilla == null)
                {
                    return Json(new { success = false, message = "Registro de planilla no encontrado." });
                }

                planilla.Estado = nuevoEstado;
                planilla.FechaModificacion = DateTime.Now;

                db.Entry(planilla).State = EntityState.Modified;
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "El estado de la planilla ha sido actualizado a: " + nuevoEstado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}