using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SistemaRRHH.Models;

namespace SistemaRRHH.Controllers
{
    public class CatalogoPermisosController : Controller
    {
        private RecursosHumanosDBEntities db = new RecursosHumanosDBEntities();

        // GET: CatalogoPermisos
        public ActionResult Index()
        {
            return View();
        }

        // GET: CatalogoPermisos/GetAll
        [HttpGet]
        public async Task<JsonResult> GetAll(string busqueda, bool mostrarInactivos = false)
        {
            try
            {
                var query = db.CatalogoPermisos.AsQueryable();

                if (!mostrarInactivos)
                {
                    query = query.Where(p => p.Activo == true);
                }

                if (!string.IsNullOrEmpty(busqueda))
                {
                    query = query.Where(p => p.NombrePermiso.Contains(busqueda));
                }

                var lista = await query
                    .Select(p => new
                    {
                        p.TipoPermisoID,
                        p.NombrePermiso,
                        Descripcion = p.Descripcion ?? "Sin descripcion",
                        DiasMaximos = p.DiasMaximos.HasValue ? p.DiasMaximos.Value : 0,
                        p.Activo,
                        TotalSolicitudes = p.Permisos.Count()
                    })
                    .OrderBy(p => p.NombrePermiso)
                    .ToListAsync();

                var estadisticas = new
                {
                    Total = await db.CatalogoPermisos.CountAsync(),
                    Activos = await db.CatalogoPermisos.CountAsync(p => p.Activo == true)
                };

                return Json(new { success = true, data = lista, estadisticas = estadisticas }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: CatalogoPermisos/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CatalogoPermisos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(FormCollection form)
        {
            CatalogoPermisos catalogo = new CatalogoPermisos();

            if (TryUpdateModel(catalogo, "", new string[] { "NombrePermiso", "Descripcion", "DiasMaximos" }))
            {
                try
                {
                    catalogo.Activo = true;
                    catalogo.FechaCreacion = DateTime.Now;
                    db.CatalogoPermisos.Add(catalogo);
                    await db.SaveChangesAsync();
                    TempData["Success"] = "Tipo de permiso creado exitosamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al guardar el tipo de permiso: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "No se pudieron procesar los campos del formulario.";
            }

            return View(catalogo);
        }

        // GET: CatalogoPermisos/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var catalogo = await db.CatalogoPermisos.FindAsync(id);
            if (catalogo == null)
            {
                return HttpNotFound();
            }
            return View(catalogo);
        }

        // POST: CatalogoPermisos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, FormCollection form)
        {
            var catalogoExistente = await db.CatalogoPermisos.FindAsync(id);
            if (catalogoExistente == null)
            {
                return HttpNotFound();
            }

            if (TryUpdateModel(catalogoExistente, "", new string[] { "NombrePermiso", "Descripcion", "DiasMaximos" }))
            {
                try
                {
                    catalogoExistente.FechaModificacion = DateTime.Now;
                    db.Entry(catalogoExistente).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    TempData["Success"] = "Tipo de permiso actualizado correctamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al modificar el tipo de permiso: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "No se pudieron validar los campos enviados.";
            }

            return View(catalogoExistente);
        }

        // POST: CatalogoPermisos/ToggleActivo
        [HttpPost]
        public async Task<JsonResult> ToggleActivo(int id)
        {
            try
            {
                var catalogo = await db.CatalogoPermisos.FindAsync(id);
                if (catalogo == null)
                {
                    return Json(new { success = false, message = "Tipo de permiso no encontrado." });
                }

                catalogo.Activo = !catalogo.Activo;
                catalogo.FechaModificacion = DateTime.Now;
                db.Entry(catalogo).State = EntityState.Modified;
                await db.SaveChangesAsync();

                string estadoFinal = catalogo.Activo == true ? "habilitado" : "deshabilitado";
                return Json(new { success = true, message = $"El tipo de permiso fue {estadoFinal} correctamente." });
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
