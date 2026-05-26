using System;
using System.Linq;
using System.Web.Mvc;
using SistemaRRHH.Models;
using System.Data.Entity;
using System.Collections.Generic;

namespace SistemaRRHH.Controllers
{
	/// <summary>
	/// Controlador para la gestion de permisos de empleados
	/// Maneja operaciones CRUD, aprobacion/rechazo, activacion/desactivacion y verificacion de disponibilidad
	/// </summary>
	public class PermisosController : Controller
	{
		// Contexto de base de datos (Entity Framework)
		private RecursosHumanosDBEntities db = new RecursosHumanosDBEntities();

		// ==================== VISTAS PRINCIPALES ====================

		/// <summary>
		/// GET: Permisos
		/// Vista principal que carga el listado de permisos via AJAX
		/// </summary>
		public ActionResult Index()
		{
			return View();
		}

		/// <summary>
		/// GET: Permisos/Create
		/// Muestra el formulario para crear una nueva solicitud de permiso
		/// Solo carga empleados activos y tipos de permiso activos
		/// </summary>
		public ActionResult Create()
		{
			// Carga solo empleados ACTIVOS en el dropdown
			ViewBag.EmpleadoID = new SelectList(db.Empleados.Where(e => e.Activo == true), "EmpleadoID", "Nombre");
			// Carga solo tipos de permiso ACTIVOS en el dropdown
			ViewBag.TipoPermisoID = new SelectList(db.CatalogoPermisos.Where(c => c.Activo == true), "TipoPermisoID", "NombrePermiso");
			return View();
		}

		// ==================== CREATE (POST) ====================

		/// <summary>
		/// POST: Permisos/Create
		/// Guarda una nueva solicitud de permiso en la base de datos
		/// Estado inicial: "PENDIENTE", Activo: true
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create([Bind(Include = "EmpleadoID,TipoPermisoID,FechaInicio,FechaFin,Observaciones")] Permisos permiso)
		{
			// ========== VALIDACIONES MANUALES DE FECHAS ==========
			if (!permiso.FechaInicio.HasValue)
			{
				ModelState.AddModelError("FechaInicio", "La fecha de inicio es obligatoria");
			}
			if (!permiso.FechaFin.HasValue)
			{
				ModelState.AddModelError("FechaFin", "La fecha de fin es obligatoria");
			}
			if (permiso.FechaInicio.HasValue && permiso.FechaFin.HasValue && permiso.FechaInicio > permiso.FechaFin)
			{
				ModelState.AddModelError("", "La fecha de inicio no puede ser mayor a la fecha de fin");
			}

			if (ModelState.IsValid)
			{
				try
				{
					// Obtiene el catalogo del permiso para agregar descripcion por defecto
					var catalogo = db.CatalogoPermisos.Find(permiso.TipoPermisoID);
					if (catalogo != null)
					{
						// Si no hay observaciones, usa la descripcion del catalogo
						permiso.Observaciones = string.IsNullOrEmpty(permiso.Observaciones)
							? catalogo.Descripcion
							: catalogo.Descripcion + " - " + permiso.Observaciones;
					}

					// Valores por defecto para un nuevo permiso
					permiso.Estado = "PENDIENTE";    // Estado inicial
					permiso.Activo = true;            // Activo por defecto
					permiso.FechaCreacion = DateTime.Now;  // Marca fecha de creacion

					db.Permisos.Add(permiso);
					db.SaveChanges();

					TempData["Success"] = "Permiso creado exitosamente";
					return RedirectToAction("Index");
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", "Error al guardar: " + ex.Message);
				}
			}

			// Si hay error, recarga los dropdowns
			ViewBag.EmpleadoID = new SelectList(db.Empleados.Where(e => e.Activo == true), "EmpleadoID", "Nombre", permiso.EmpleadoID);
			ViewBag.TipoPermisoID = new SelectList(db.CatalogoPermisos.Where(c => c.Activo == true), "TipoPermisoID", "NombrePermiso", permiso.TipoPermisoID);
			return View(permiso);
		}

		// ==================== EDIT ====================

		/// <summary>
		/// GET: Permisos/Edit/5
		/// Muestra el formulario para editar un permiso existente
		/// Solo permite editar si el permiso esta PENDIENTE
		/// </summary>
		public ActionResult Edit(int id)
		{
			var permiso = db.Permisos.Find(id);
			if (permiso == null)
			{
				return HttpNotFound();
			}

			// Solo permisos en estado PENDIENTE pueden editarse
			if (permiso.Estado != "PENDIENTE")
			{
				TempData["Error"] = "No se pueden editar permisos aprobados o rechazados";
				return RedirectToAction("Index");
			}

			ViewBag.EmpleadoID = new SelectList(db.Empleados.Where(e => e.Activo == true), "EmpleadoID", "Nombre", permiso.EmpleadoID);
			ViewBag.TipoPermisoID = new SelectList(db.CatalogoPermisos.Where(c => c.Activo == true), "TipoPermisoID", "NombrePermiso", permiso.TipoPermisoID);
			return View(permiso);
		}

		/// <summary>
		/// POST: Permisos/Edit/5
		/// Actualiza un permiso existente
		/// Solo permite actualizar si el permiso esta PENDIENTE
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(int id, FormCollection form)
		{
			var permisoExistente = db.Permisos.Find(id);
			if (permisoExistente == null)
			{
				return HttpNotFound();
			}

			// Solo permisos en estado PENDIENTE pueden editarse
			if (permisoExistente.Estado != "PENDIENTE")
			{
				TempData["Error"] = "No se pueden editar permisos aprobados o rechazados";
				return RedirectToAction("Index");
			}

			// Actualiza solo los campos permitidos
			if (TryUpdateModel(permisoExistente, "", new string[] { "EmpleadoID", "TipoPermisoID", "FechaInicio", "FechaFin", "Observaciones" }))
			{
				try
				{
					// Marca la fecha de modificacion
					permisoExistente.FechaModificacion = DateTime.Now;
					db.SaveChanges();
					TempData["Success"] = "Permiso actualizado exitosamente";
					return RedirectToAction("Index");
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
				}
			}

			ViewBag.EmpleadoID = new SelectList(db.Empleados.Where(e => e.Activo == true), "EmpleadoID", "Nombre", permisoExistente.EmpleadoID);
			ViewBag.TipoPermisoID = new SelectList(db.CatalogoPermisos.Where(c => c.Activo == true), "TipoPermisoID", "NombrePermiso", permisoExistente.TipoPermisoID);
			return View(permisoExistente);
		}

		// ==================== ACTIVAR/DESACTIVAR PERMISO ====================

		/// <summary>
		/// POST: Permisos/ToggleActivo
		/// Activa o desactiva un permiso (Soft Delete)
		/// No elimina el registro fisicamente, solo cambia el estado Activo
		/// </summary>
		[HttpPost]
		public JsonResult ToggleActivo(int id)
		{
			try
			{
				var permiso = db.Permisos.Find(id);
				if (permiso == null)
				{
					return Json(new { success = false, message = "Permiso no encontrado" });
				}

				if (permiso.Activo == true)
				{
					// ========== DESACTIVAR PERMISO ==========
					permiso.Activo = false;
					permiso.FechaModificacion = DateTime.Now;
					db.SaveChanges();
					return Json(new { success = true, message = "Permiso desactivado exitosamente", nuevoEstado = "Inactivo" });
				}
				else
				{
					// ========== ACTIVAR PERMISO ==========
					permiso.Activo = true;
					permiso.FechaModificacion = DateTime.Now;
					db.SaveChanges();
					return Json(new { success = true, message = "Permiso activado exitosamente", nuevoEstado = "Activo" });
				}
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Error al procesar: " + ex.Message });
			}
		}

		// ==================== ELIMINACION FISICA (OPCIONAL) ====================

		/// <summary>
		/// POST: Permisos/Delete
		/// Elimina fisicamente un permiso de la base de datos
		/// USAR CON PRECAUCION - Eliminacion permanente
		/// </summary>
		[HttpPost]
		public JsonResult Delete(int id)
		{
			try
			{
				var permiso = db.Permisos.Find(id);
				if (permiso == null)
				{
					return Json(new { success = false, message = "Permiso no encontrado" });
				}

				db.Permisos.Remove(permiso);
				db.SaveChanges();

				return Json(new { success = true, message = "Permiso eliminado exitosamente" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
			}
		}

		// ==================== GETALL (AJAX) - CONSULTA PRINCIPAL ====================

		/// <summary>
		/// GET: Permisos/GetAll (AJAX)
		/// Obtiene lista de permisos con multiples filtros
		/// Parametros:
		///   - estado: PENDIENTE, APROBADO, RECHAZADO, TODOS
		///   - empleadoId: ID del empleado
		///   - desde/hasta: rango de fechas
		///   - mostrarInactivos: si muestra tambien permisos desactivados
		/// </summary>
		[HttpGet]
		public JsonResult GetAll(string estado, int? empleadoId, DateTime? desde, DateTime? hasta, bool mostrarInactivos = false)
		{
			var permisosQuery = db.Permisos
				.Include(p => p.Empleados)          // Incluye datos del empleado
				.Include(p => p.CatalogoPermisos)   // Incluye datos del catalogo
				.AsQueryable();

			// ========== FILTRO POR ACTIVO/INACTIVO ==========
			if (mostrarInactivos)
			{
				permisosQuery = permisosQuery.Where(p => p.Activo == false);  // Solo inactivos
			}
			else
			{
				permisosQuery = permisosQuery.Where(p => p.Activo == true);   // Solo activos
			}

			// ========== FILTRO POR ESTADO ==========
			if (!string.IsNullOrEmpty(estado) && estado != "TODOS")
			{
				permisosQuery = permisosQuery.Where(p => p.Estado != null && p.Estado.ToUpper() == estado);
			}

			// ========== FILTRO POR EMPLEADO ==========
			if (empleadoId.HasValue && empleadoId.Value > 0)
			{
				permisosQuery = permisosQuery.Where(p => p.EmpleadoID == empleadoId.Value);
			}

			// ========== FILTRO POR RANGO DE FECHAS ==========
			if (desde.HasValue)
			{
				permisosQuery = permisosQuery.Where(p => p.FechaInicio >= desde.Value);
			}
			if (hasta.HasValue)
			{
				permisosQuery = permisosQuery.Where(p => p.FechaFin <= hasta.Value);
			}

			// ========== PROYECCION DE DATOS ==========
			var permisosTemp = permisosQuery
				.OrderBy(p => p.FechaInicio)  // Orden ascendente por fecha
				.Select(p => new
				{
					p.SolicitudPermisoID,
					p.EmpleadoID,
					EmpleadoNombre = (p.Empleados.Nombre + " " + p.Empleados.Apellido).Trim(),
					TipoPermiso = p.CatalogoPermisos != null ? p.CatalogoPermisos.NombrePermiso : "N/A",
					FechaInicio = p.FechaInicio,
					FechaFin = p.FechaFin,
					Estado = p.Estado != null ? p.Estado.ToUpper() : "PENDIENTE",
					p.Observaciones,
					p.Activo
				})
				.ToList();

			// ========== CALCULO DE DIAS SOLICITADOS ==========
			var permisosList = permisosTemp.Select(p => new
			{
				p.SolicitudPermisoID,
				p.EmpleadoID,
				p.EmpleadoNombre,
				p.TipoPermiso,
				FechaInicio = p.FechaInicio,
				FechaFin = p.FechaFin,
				p.Estado,
				DiasSolicitados = p.FechaInicio.HasValue && p.FechaFin.HasValue ?
								 (p.FechaFin.Value - p.FechaInicio.Value).Days + 1 : 0,
				p.Observaciones,
				p.Activo
			}).ToList();

			// ========== ESTADISTICAS PARA TARJETAS ==========
			var estadisticas = new
			{
				Pendientes = db.Permisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "PENDIENTE" && p.Activo == true),
				Aprobados = db.Permisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "APROBADO" && p.Activo == true),
				Rechazados = db.Permisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "RECHAZADO" && p.Activo == true),
				Inactivos = db.Permisos.Count(p => p.Activo == false)
			};

			return Json(new { data = permisosList, estadisticas = estadisticas }, JsonRequestBehavior.AllowGet);
		}

		// ==================== GETEMPLEADOS (AJAX) ====================

		/// <summary>
		/// GET: Permisos/GetEmpleados
		/// Obtiene lista de empleados ACTIVOS para llenar dropdowns
		/// </summary>
		[HttpGet]
		public JsonResult GetEmpleados()
		{
			var empleados = db.Empleados
				.Where(e => e.Activo == true)
				.Select(e => new { e.EmpleadoID, e.Nombre })
				.OrderBy(e => e.Nombre)
				.ToList();

			return Json(empleados, JsonRequestBehavior.AllowGet);
		}

		// ==================== GETTIPOSPERMISO (AJAX) ====================

		/// <summary>
		/// GET: Permisos/GetTiposPermiso
		/// Obtiene lista de tipos de permiso ACTIVOS para llenar dropdowns
		/// </summary>
		[HttpGet]
		public JsonResult GetTiposPermiso()
		{
			var tipos = db.CatalogoPermisos
				.Where(c => c.Activo == true)
				.Select(c => new { c.TipoPermisoID, c.NombrePermiso })
				.OrderBy(c => c.NombrePermiso)
				.ToList();

			return Json(tipos, JsonRequestBehavior.AllowGet);
		}

		// ==================== APROBAR PERMISO ====================

		/// <summary>
		/// POST: Permisos/Aprobar
		/// Aprueba una solicitud de permiso
		/// Cambia el estado a "APROBADO" y registra comentario con fecha/hora
		/// </summary>
		[HttpPost]
		public JsonResult Aprobar(int id, string comentario)
		{
			var permiso = db.Permisos.Find(id);
			if (permiso == null)
			{
				return Json(new { success = false, message = "Permiso no encontrado" });
			}

			// Cambia estado y agrega comentario de aprobacion
			permiso.Estado = "APROBADO";
			permiso.Observaciones = (permiso.Observaciones ?? "") +
				$"\n--- Aprobado el {DateTime.Now:dd/MM/yyyy HH:mm}: {comentario} ---";
			permiso.FechaModificacion = DateTime.Now;
			db.SaveChanges();

			var estadisticas = ObtenerEstadisticas();

			return Json(new
			{
				success = true,
				message = "Permiso aprobado exitosamente",
				nuevoEstado = "APROBADO",
				estadisticas = estadisticas
			});
		}

		// ==================== RECHAZAR PERMISO ====================

		/// <summary>
		/// POST: Permisos/Rechazar
		/// Rechaza una solicitud de permiso
		/// Cambia el estado a "RECHAZADO" y registra motivo con fecha/hora
		/// </summary>
		[HttpPost]
		public JsonResult Rechazar(int id, string motivo)
		{
			var permiso = db.Permisos.Find(id);
			if (permiso == null)
			{
				return Json(new { success = false, message = "Permiso no encontrado" });
			}

			// Cambia estado y agrega motivo de rechazo
			permiso.Estado = "RECHAZADO";
			permiso.Observaciones = (permiso.Observaciones ?? "") +
				$"\n--- Rechazado el {DateTime.Now:dd/MM/yyyy HH:mm}: {motivo} ---";
			permiso.FechaModificacion = DateTime.Now;
			db.SaveChanges();

			var estadisticas = ObtenerEstadisticas();

			return Json(new
			{
				success = true,
				message = "Permiso rechazado",
				estadisticas = estadisticas
			});
		}

		// ==================== HISTORIAL DE PERMISOS ====================

		/// <summary>
		/// GET: Permisos/GetHistorial
		/// Obtiene el historial de permisos de un empleado especifico
		/// Solo muestra permisos ACTIVOS
		/// </summary>
		[HttpGet]
		public JsonResult GetHistorial(int empleadoId, int year = 0)
		{
			// Obtiene permisos del empleado (solo activos)
			var query = db.Permisos
				.Include(p => p.CatalogoPermisos)
				.Where(p => p.EmpleadoID == empleadoId && p.Activo == true)
				.ToList();

			// Filtra por ano si se especifica
			var resultado = query
				.Where(p => year == 0 || (p.FechaInicio.HasValue && p.FechaInicio.Value.Year == year))
				.Select(p => new
				{
					p.SolicitudPermisoID,
					Tipo = p.CatalogoPermisos != null ? p.CatalogoPermisos.NombrePermiso : "N/A",
					FechaInicio = p.FechaInicio,
					FechaFin = p.FechaFin,
					Estado = p.Estado != null ? p.Estado.ToUpper() : "PENDIENTE",
					Dias = p.FechaInicio.HasValue && p.FechaFin.HasValue ?
						   (p.FechaFin.Value - p.FechaInicio.Value).Days + 1 : 0
				})
				.OrderByDescending(p => p.FechaInicio)  // Mas recientes primero
				.ToList();

			return Json(resultado, JsonRequestBehavior.AllowGet);
		}

		// ==================== VERIFICAR DISPONIBILIDAD ====================

		/// <summary>
		/// GET: Permisos/VerificarDisponibilidad
		/// Verifica si un empleado tiene dias disponibles para un tipo de permiso
		/// Calcula dias usados vs dias maximos permitidos
		/// </summary>
		[HttpGet]
		public JsonResult VerificarDisponibilidad(int empleadoId, int tipoPermisoId, DateTime inicio, DateTime fin)
		{
			try
			{
				// Calcula dias solicitados
				var diasSolicitados = (fin - inicio).Days + 1;

				if (diasSolicitados <= 0)
				{
					return Json(new
					{
						disponible = false,
						diasSolicitados = 0,
						diasUsados = 0,
						diasDisponibles = 0,
						maximoPermitido = 0,
						message = "Las fechas son invalidas"
					}, JsonRequestBehavior.AllowGet);
				}

				// Obtiene el limite maximo del catalogo de permisos
				var catalogo = db.CatalogoPermisos.Find(tipoPermisoId);
				int maxDiasPorTipo = catalogo?.DiasMaximos ?? 10;
				string nombrePermiso = catalogo?.NombrePermiso ?? "General";

				// Obtiene permisos aprobados y activos del empleado para este tipo
				var permisosAprobados = db.Permisos
					.Where(p => p.EmpleadoID == empleadoId &&
								p.TipoPermisoID == tipoPermisoId &&
								p.Estado != null &&
								p.Estado.ToUpper() == "APROBADO" &&
								p.Activo == true &&
								p.FechaInicio.HasValue &&
								p.FechaFin.HasValue)
					.ToList();

				// Calcula dias ya usados
				int diasUsados = 0;
				foreach (var permiso in permisosAprobados)
				{
					if (permiso.FechaInicio.HasValue && permiso.FechaFin.HasValue)
					{
						diasUsados += (permiso.FechaFin.Value - permiso.FechaInicio.Value).Days + 1;
					}
				}

				// Calcula dias disponibles
				int diasDisponibles = maxDiasPorTipo - diasUsados;
				bool disponible = diasSolicitados <= diasDisponibles && diasDisponibles > 0;

				return Json(new
				{
					disponible = disponible,
					diasSolicitados = diasSolicitados,
					diasUsados = diasUsados,
					diasDisponibles = diasDisponibles > 0 ? diasDisponibles : 0,
					maximoPermitido = maxDiasPorTipo,
					nombrePermiso = nombrePermiso,
					message = disponible ? "Dias disponibles" : "No hay suficientes dias disponibles"
				}, JsonRequestBehavior.AllowGet);
			}
			catch (Exception ex)
			{
				return Json(new
				{
					disponible = false,
					diasSolicitados = 0,
					diasUsados = 0,
					diasDisponibles = 0,
					maximoPermitido = 0,
					error = ex.Message,
					message = "Error al verificar disponibilidad: " + ex.Message
				}, JsonRequestBehavior.AllowGet);
			}
		}

		// ==================== METODO AUXILIAR ====================

		/// <summary>
		/// Obtiene estadisticas actualizadas de permisos ACTIVOS
		/// Usado despues de aprobar/rechazar para actualizar tarjetas
		/// </summary>
		private object ObtenerEstadisticas()
		{
			var todosPermisos = db.Permisos.Where(p => p.Activo == true).ToList();
			return new
			{
				Pendientes = todosPermisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "PENDIENTE"),
				Aprobados = todosPermisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "APROBADO"),
				Rechazados = todosPermisos.Count(p => p.Estado != null && p.Estado.ToUpper() == "RECHAZADO")
			};
		}

		/// <summary>
		/// Libera los recursos del contexto de base de datos
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing) db.Dispose();
			base.Dispose(disposing);
		}
	}
}