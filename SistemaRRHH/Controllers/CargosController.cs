using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SistemaRRHH.Models;

namespace SistemaRRHH.Controllers
{
    public class CargosController : Controller
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
                var query = db.Cargos.Include(c => c.Departamentos).AsQueryable();

                if (!mostrarInactivos)
                {
                    query = query.Where(c => c.Activo);
                }

                if (!string.IsNullOrEmpty(busqueda))
                {
                    query = query.Where(c => c.NombreCargo.Contains(busqueda));
                }

                var lista = await query
                    .Select(c => new
                    {
                        c.CargoID,
                        c.NombreCargo,
                        Descripcion = c.Descripcion ?? "Sin descripción",
                        DepartamentoNombre = c.Departamentos.Nombre,
                        c.Activo
                    })
                    .ToListAsync();

                var estadisticas = new
                {
                    Total = await db.Cargos.CountAsync(),
                    Activos = await db.Cargos.CountAsync(c => c.Activo)
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
            ViewBag.DepartamentoID = new SelectList(db.Departamentos.Where(d => d.Activo == true), "DepartamentoID", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(FormCollection form)
        {
            Cargos cargo = new Cargos();

            if (TryUpdateModel(cargo, "", new string[] { "NombreCargo", "Descripcion", "DepartamentoID" }))
            {
                try
                {
                    cargo.Activo = true;
                    cargo.FechaCreacion = DateTime.Now;
                    db.Cargos.Add(cargo);
                    await db.SaveChangesAsync();
                    TempData["Success"] = "Cargo corporativo creado exitosamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al guardar el cargo: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "No se pudieron procesar los campos del formulario de cargos.";
            }

            ViewBag.DepartamentoID = new SelectList(db.Departamentos.Where(d => d.Activo == true), "DepartamentoID", "Nombre", cargo.DepartamentoID);
            return View(cargo);
        }

        public async Task<ActionResult> Edit(int id)
        {
            var cargo = await db.Cargos.FindAsync(id);
            if (cargo == null)
            {
                return HttpNotFound();
            }
            ViewBag.DepartamentoID = new SelectList(db.Departamentos.Where(d => d.Activo == true), "DepartamentoID", "Nombre", cargo.DepartamentoID);
            return View(cargo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, FormCollection form)
        {
            var cargoExistente = await db.Cargos.FindAsync(id);
            if (cargoExistente == null)
            {
                return HttpNotFound();
            }

            if (TryUpdateModel(cargoExistente, "", new string[] { "NombreCargo", "Descripcion", "DepartamentoID" }))
            {
                try
                {
                    cargoExistente.FechaModificacion = DateTime.Now;
                    db.Entry(cargoExistente).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    TempData["Success"] = "Cargo actualizado de manera correcta.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al modificar en la base de datos: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "No se pudieron validar los campos enviados.";
            }

            ViewBag.DepartamentoID = new SelectList(db.Departamentos.Where(d => d.Activo == true), "DepartamentoID", "Nombre", cargoExistente.DepartamentoID);
            return View(cargoExistente);
        }

        [HttpPost]
        public async Task<JsonResult> ToggleActivo(int id)
        {
            try
            {
                var cargo = await db.Cargos.FindAsync(id);
                if (cargo == null)
                {
                    return Json(new { success = false, message = "Cargo no localizado." });
                }

                cargo.Activo = !cargo.Activo;
                cargo.FechaModificacion = DateTime.Now;
                db.Entry(cargo).State = EntityState.Modified;
                await db.SaveChangesAsync();

                string estadoFinal = cargo.Activo ? "habilitado" : "deshabilitado";
                return Json(new { success = true, message = $"El cargo fue {estadoFinal} correctamente." });
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