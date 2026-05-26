using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SistemaRRHH.Models;

namespace SistemaRRHH.Controllers
{
    public class DepartamentosController : Controller
    {
        private RecursosHumanosDBEntities db = new RecursosHumanosDBEntities();

        // GET: Departamentos
        public ActionResult Index()
        {
            return View();
        }

        // GET: Departamentos/GetAll
        [HttpGet]
        public async Task<JsonResult> GetAll(string busqueda, bool mostrarInactivos = false)
        {
            try
            {
                var query = db.Departamentos.AsQueryable();

                if (!mostrarInactivos)
                {
                    query = query.Where(d => d.Activo == true);
                }

                if (!string.IsNullOrEmpty(busqueda))
                {
                    query = query.Where(d => d.Nombre.Contains(busqueda));
                }

                var lista = await query
                    .Select(d => new
                    {
                        d.DepartamentoID,
                        d.Nombre,
                        Descripcion = d.Descripcion ?? "Sin descripcion",
                        d.Activo,
                        TotalCargos = d.Cargos.Count(c => c.Activo == true)
                    })
                    .OrderBy(d => d.Nombre)
                    .ToListAsync();

                var estadisticas = new
                {
                    Total = await db.Departamentos.CountAsync(),
                    Activos = await db.Departamentos.CountAsync(d => d.Activo == true)
                };

                return Json(new { success = true, data = lista, estadisticas = estadisticas }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Departamentos/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Departamentos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(FormCollection form)
        {
            Departamentos departamento = new Departamentos();

            if (TryUpdateModel(departamento, "", new string[] { "Nombre", "Descripcion" }))
            {
                try
                {
                    departamento.Activo = true;
                    departamento.FechaCreacion = DateTime.Now;
                    db.Departamentos.Add(departamento);
                    await db.SaveChangesAsync();
                    TempData["Success"] = "Departamento creado exitosamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al guardar el departamento: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "No se pudieron procesar los campos del formulario.";
            }

            return View(departamento);
        }

        // GET: Departamentos/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var departamento = await db.Departamentos.FindAsync(id);
            if (departamento == null)
            {
                return HttpNotFound();
            }
            return View(departamento);
        }

        // POST: Departamentos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, FormCollection form)
        {
            var departamentoExistente = await db.Departamentos.FindAsync(id);
            if (departamentoExistente == null)
            {
                return HttpNotFound();
            }

            if (TryUpdateModel(departamentoExistente, "", new string[] { "Nombre", "Descripcion" }))
            {
                try
                {
                    departamentoExistente.FechaModificacion = DateTime.Now;
                    db.Entry(departamentoExistente).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    TempData["Success"] = "Departamento actualizado correctamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al modificar el departamento: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "No se pudieron validar los campos enviados.";
            }

            return View(departamentoExistente);
        }

        // POST: Departamentos/ToggleActivo
        [HttpPost]
        public async Task<JsonResult> ToggleActivo(int id)
        {
            try
            {
                var departamento = await db.Departamentos.FindAsync(id);
                if (departamento == null)
                {
                    return Json(new { success = false, message = "Departamento no encontrado." });
                }

                departamento.Activo = !departamento.Activo;
                departamento.FechaModificacion = DateTime.Now;
                db.Entry(departamento).State = EntityState.Modified;
                await db.SaveChangesAsync();

                string estadoFinal = departamento.Activo == true ? "habilitado" : "deshabilitado";
                return Json(new { success = true, message = $"El departamento fue {estadoFinal} correctamente." });
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
