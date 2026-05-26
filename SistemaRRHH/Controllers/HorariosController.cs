using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SistemaRRHH.Models;

namespace SistemaRRHH.Controllers
{
    public class HorariosController : Controller
    {
        private RecursosHumanosDBEntities db = new RecursosHumanosDBEntities();

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetAll(string busqueda, bool mostrarInactivos = false)
        {
            try
            {
                var query = db.Horarios.AsQueryable();

                if (!mostrarInactivos)
                {
                    query = query.Where(h => h.Activo);
                }

                if (!string.IsNullOrEmpty(busqueda))
                {
                    query = query.Where(h => h.Nombre.Contains(busqueda));
                }

                var lista = await query
                    .Select(h => new
                    {
                        h.HorarioID,
                        h.Nombre,
                        HoraEntrada = h.HoraEntrada.ToString(),
                        HoraSalida = h.HoraSalida.ToString(),
                        h.Activo
                    })
                    .ToListAsync();

                var estadisticas = new
                {
                    Total = await db.Horarios.CountAsync(),
                    Activos = await db.Horarios.CountAsync(h => h.Activo)
                };

                return Json(new { success = true, data = lista, estadisticas = estadisticas }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(FormCollection form)
        {
            Horarios nuevoHorario = new Horarios();

            string txtEntrada = form["HoraEntrada"];
            string txtSalida = form["HoraSalida"];

            if (TimeSpan.TryParse(txtEntrada, out TimeSpan entrada) && TimeSpan.TryParse(txtSalida, out TimeSpan salida))
            {
                nuevoHorario.Nombre = form["Nombre"];
                nuevoHorario.HoraEntrada = entrada;
                nuevoHorario.HoraSalida = salida;
                nuevoHorario.Activo = true;
                nuevoHorario.FechaCreacion = DateTime.Now;

                try
                {
                    db.Horarios.Add(nuevoHorario);
                    await db.SaveChangesAsync();
                    TempData["Success"] = "Horario registrado correctamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al guardar en la base de datos: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "El formato de las horas de entrada o salida no es valido.";
            }

            return View(nuevoHorario);
        }

        public async Task<ActionResult> Edit(int id)
        {
            var horario = await db.Horarios.FindAsync(id);
            if (horario == null)
            {
                return HttpNotFound();
            }
            return View(horario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, FormCollection form)
        {
            var horarioExistente = await db.Horarios.FindAsync(id);
            if (horarioExistente == null)
            {
                return HttpNotFound();
            }

            string txtEntrada = form["HoraEntrada"];
            string txtSalida = form["HoraSalida"];

            if (TimeSpan.TryParse(txtEntrada, out TimeSpan entrada) && TimeSpan.TryParse(txtSalida, out TimeSpan salida))
            {
                horarioExistente.Nombre = form["Nombre"];
                horarioExistente.HoraEntrada = entrada;
                horarioExistente.HoraSalida = salida;
                horarioExistente.FechaModificacion = DateTime.Now;

                try
                {
                    db.Entry(horarioExistente).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    TempData["Success"] = "Horario actualizado correctamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al actualizar en la base de datos: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "El formato de las horas ingresadas es invalido.";
            }

            return View(horarioExistente);
        }

        [HttpPost]
        public async Task<JsonResult> ToggleActivo(int id)
        {
            try
            {
                var horario = await db.Horarios.FindAsync(id);
                if (horario == null)
                {
                    return Json(new { success = false, message = "Horario no encontrado." });
                }

                horario.Activo = !horario.Activo;
                horario.FechaModificacion = DateTime.Now;
                db.Entry(horario).State = EntityState.Modified;
                await db.SaveChangesAsync();

                string accion = horario.Activo ? "activado" : "desactivado";
                return Json(new { success = true, message = $"El horario fue {accion} con exito." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}