using System;
using System.Linq;
using System.Web.Mvc;
using SistemaRRHH.Models;
using System.Data.Entity;
using System.Collections.Generic;

namespace SistemaRRHH.Controllers
{
	public class PermisosController : Controller
	{
		private RecursosHumanosDBEntities db = new RecursosHumanosDBEntities();

		// GET: Permisos
		public ActionResult Index(string estado, int? empleadoId, DateTime? desde, DateTime? hasta)
		{
			var todosPermisos = db.Permisos.ToList();

			var totalPendientes = todosPermisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "PENDIENTE");
			var totalAprobados = todosPermisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "APROBADO");
			var totalRechazados = todosPermisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "RECHAZADO");

			ViewBag.Pendientes = totalPendientes;
			ViewBag.Aprobados = totalAprobados;
			ViewBag.Rechazados = totalRechazados;

			var permisosQuery = db.Permisos
				.Include(p => p.Empleados)
				.Include(p => p.CatalogoPermisos)
				.AsQueryable();

			if (!string.IsNullOrEmpty(estado) && estado != "TODOS")
			{
				permisosQuery = permisosQuery.Where(p => p.Estado != null && p.Estado.ToUpper() == estado);
			}
			if (empleadoId.HasValue && empleadoId.Value > 0)
			{
				permisosQuery = permisosQuery.Where(p => p.EmpleadoID == empleadoId.Value);
			}
			if (desde.HasValue)
			{
				permisosQuery = permisosQuery.Where(p => p.FechaInicio >= desde.Value);
			}
			if (hasta.HasValue)
			{
				permisosQuery = permisosQuery.Where(p => p.FechaFin <= hasta.Value);
			}

			var permisosLista = permisosQuery.OrderByDescending(p => p.FechaInicio).ToList();

			var permisosList = permisosLista.Select(p => new PermisoViewModel
			{
				SolicitudPermisoID = p.SolicitudPermisoID,
				EmpleadoID = p.EmpleadoID,
				EmpleadoNombre = p.Empleados.Nombre + " " + p.Empleados.Apellido,
				TipoPermiso = p.CatalogoPermisos.NombrePermiso,
				FechaInicio = p.FechaInicio,
				FechaFin = p.FechaFin,
				Estado = p.Estado != null ? p.Estado.ToUpper() : "PENDIENTE",
				DiasSolicitados = p.FechaInicio.HasValue && p.FechaFin.HasValue ?
								 (p.FechaFin.Value - p.FechaInicio.Value).Days + 1 : 0,
				Observaciones = p.Observaciones
			}).ToList();

			ViewBag.EmpleadosList = new SelectList(db.Empleados, "EmpleadoID", "Nombre");
			ViewBag.EstadosList = new SelectList(new[] { "TODOS", "PENDIENTE", "APROBADO", "RECHAZADO" });
			ViewBag.EstadoSeleccionado = estado;
			ViewBag.EmpleadoIdSeleccionado = empleadoId;
			ViewBag.DesdeSeleccionado = desde?.ToString("yyyy-MM-dd");
			ViewBag.HastaSeleccionado = hasta?.ToString("yyyy-MM-dd");

			return View(permisosList);
		}

		// GET: Permisos/Create
		public ActionResult Create()
		{
			ViewBag.EmpleadoID = new SelectList(db.Empleados, "EmpleadoID", "Nombre");
			ViewBag.TipoPermisoID = new SelectList(db.CatalogoPermisos, "TipoPermisoID", "NombrePermiso");
			return View();
		}

		// POST: Permisos/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create(Permisos permiso)
		{
			if (ModelState.IsValid)
			{
				// No asignar ID manualmente - la BD lo genera automáticamente
				var catalogo = db.CatalogoPermisos.Find(permiso.TipoPermisoID);
				if (catalogo != null)
				{
					if (string.IsNullOrEmpty(permiso.Observaciones))
					{
						permiso.Observaciones = catalogo.Descripcion;
					}
					else
					{
						permiso.Observaciones = catalogo.Descripcion + " - " + permiso.Observaciones;
					}
				}

				permiso.Estado = "PENDIENTE";
				db.Permisos.Add(permiso);
				db.SaveChanges();
				return RedirectToAction("Index");
			}

			ViewBag.EmpleadoID = new SelectList(db.Empleados, "EmpleadoID", "Nombre", permiso.EmpleadoID);
			ViewBag.TipoPermisoID = new SelectList(db.CatalogoPermisos, "TipoPermisoID", "NombrePermiso", permiso.TipoPermisoID);
			return View(permiso);
		}

		// GET: Permisos/Edit/5
		public ActionResult Edit(int id)
		{
			var permiso = db.Permisos.Find(id);
			if (permiso == null) return HttpNotFound();

			ViewBag.EmpleadoID = new SelectList(db.Empleados, "EmpleadoID", "Nombre", permiso.EmpleadoID);
			ViewBag.TipoPermisoID = new SelectList(db.CatalogoPermisos, "TipoPermisoID", "NombrePermiso", permiso.TipoPermisoID);
			return View(permiso);
		}

		// POST: Permisos/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(Permisos permiso)
		{
			if (ModelState.IsValid)
			{
				db.Entry(permiso).State = EntityState.Modified;
				db.SaveChanges();
				return RedirectToAction("Index");
			}
			ViewBag.EmpleadoID = new SelectList(db.Empleados, "EmpleadoID", "Nombre", permiso.EmpleadoID);
			ViewBag.TipoPermisoID = new SelectList(db.CatalogoPermisos, "TipoPermisoID", "NombrePermiso", permiso.TipoPermisoID);
			return View(permiso);
		}

		// GET: Permisos/Details/5
		public ActionResult Details(int id)
		{
			var permiso = db.Permisos
				.Include(p => p.Empleados)
				.Include(p => p.CatalogoPermisos)
				.FirstOrDefault(p => p.SolicitudPermisoID == id);

			if (permiso == null) return HttpNotFound();
			return View(permiso);
		}

		// GET: Permisos/Delete/5
		public ActionResult Delete(int id)
		{
			var permiso = db.Permisos.Find(id);
			if (permiso == null) return HttpNotFound();
			return View(permiso);
		}

		// POST: Permisos/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public ActionResult DeleteConfirmed(int id)
		{
			var permiso = db.Permisos.Find(id);
			db.Permisos.Remove(permiso);
			db.SaveChanges();
			return RedirectToAction("Index");
		}

		// AJAX: Aprobar permiso
		[HttpPost]
		public JsonResult AprobarPermiso(int id, string comentario)
		{
			var permiso = db.Permisos.Find(id);
			if (permiso == null)
			{
				return Json(new { success = false, message = "Permiso no encontrado" });
			}

			permiso.Estado = "APROBADO";
			permiso.Observaciones = (permiso.Observaciones ?? "") +
				$"\nAprobado: {comentario} - {DateTime.Now}";
			db.SaveChanges();

			var estadisticas = new
			{
				Pendientes = db.Permisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "PENDIENTE"),
				Aprobados = db.Permisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "APROBADO"),
				Rechazados = db.Permisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "RECHAZADO")
			};

			return Json(new
			{
				success = true,
				message = "Permiso aprobado exitosamente",
				nuevoEstado = "APROBADO",
				estadisticas = estadisticas
			});
		}

		// AJAX: Rechazar permiso
		[HttpPost]
		public JsonResult RechazarPermiso(int id, string motivo)
		{
			var permiso = db.Permisos.Find(id);
			if (permiso == null)
			{
				return Json(new { success = false, message = "Permiso no encontrado" });
			}

			permiso.Estado = "RECHAZADO";
			permiso.Observaciones = (permiso.Observaciones ?? "") +
				$"\nRechazado: {motivo} - {DateTime.Now}";
			db.SaveChanges();

			var estadisticas = new
			{
				Pendientes = db.Permisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "PENDIENTE"),
				Aprobados = db.Permisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "APROBADO"),
				Rechazados = db.Permisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "RECHAZADO")
			};

			return Json(new
			{
				success = true,
				message = "Permiso rechazado",
				estadisticas = estadisticas
			});
		}

		// AJAX: Verificar disponibilidad de días
		[HttpGet]
		public JsonResult VerificarDiasDisponibles(int empleadoId, int tipoPermisoId, DateTime inicio, DateTime fin)
		{
			var diasSolicitados = (fin - inicio).Days + 1;

			var permisosAprobados = db.Permisos
				.Where(p => p.EmpleadoID == empleadoId &&
							p.Estado != null && p.Estado.ToUpper() == "APROBADO" &&
							p.FechaInicio.HasValue && p.FechaFin.HasValue)
				.ToList();

			int permisosExistentes = permisosAprobados
				.Where(p => (p.FechaInicio >= inicio && p.FechaInicio <= fin) ||
							(p.FechaFin >= inicio && p.FechaFin <= fin) ||
							(p.FechaInicio <= inicio && p.FechaFin >= fin))
				.Sum(p => (p.FechaFin.Value - p.FechaInicio.Value).Days + 1);

			int maxDiasPorTipo;
			switch (tipoPermisoId)
			{
				case 1:
					maxDiasPorTipo = 30;
					break;
				case 2:
					maxDiasPorTipo = 5;
					break;
				case 3:
					maxDiasPorTipo = 3;
					break;
				case 4:
					maxDiasPorTipo = 5;
					break;
				default:
					maxDiasPorTipo = 10;
					break;
			}

			int diasRestantes = maxDiasPorTipo - permisosExistentes;

			return Json(new
			{
				disponible = diasSolicitados <= diasRestantes,
				diasSolicitados = diasSolicitados,
				diasUsados = permisosExistentes,
				diasDisponibles = diasRestantes > 0 ? diasRestantes : 0,
				maximoPermitido = maxDiasPorTipo
			}, JsonRequestBehavior.AllowGet);
		}

		// AJAX: Obtener historial
		[HttpGet]
		public JsonResult GetHistorialPermisos(int empleadoId, int year = 0)
		{
			var query = db.Permisos
				.Where(p => p.EmpleadoID == empleadoId)
				.ToList();

			var resultado = query
				.Where(p => year == 0 || (p.FechaInicio.HasValue && p.FechaInicio.Value.Year == year))
				.Select(p => new
				{
					p.SolicitudPermisoID,
					Tipo = p.CatalogoPermisos?.NombrePermiso ?? "N/A",
					p.FechaInicio,
					p.FechaFin,
					Estado = p.Estado != null ? p.Estado.ToUpper() : "PENDIENTE",
					Dias = p.FechaInicio.HasValue && p.FechaFin.HasValue ?
						   (p.FechaFin.Value - p.FechaInicio.Value).Days + 1 : 0
				})
				.OrderByDescending(p => p.FechaInicio)
				.ToList();

			return Json(resultado, JsonRequestBehavior.AllowGet);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) db.Dispose();
			base.Dispose(disposing);
		}
	}

	public class PermisoViewModel
	{
		public int SolicitudPermisoID { get; set; }
		public int EmpleadoID { get; set; }
		public string EmpleadoNombre { get; set; }
		public string TipoPermiso { get; set; }
		public DateTime? FechaInicio { get; set; }
		public DateTime? FechaFin { get; set; }
		public string Estado { get; set; }
		public int DiasSolicitados { get; set; }
		public string Observaciones { get; set; }
	}
}