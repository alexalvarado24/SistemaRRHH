using System;
using System.Linq;
using System.Web.Mvc;
using SistemaRRHH.Models;
using System.Data.Entity;
using System.Collections.Generic;

namespace SistemaRRHH.Controllers
{
	/// <summary>
	/// Controlador para la gestión de empleados
	/// Maneja operaciones CRUD, activación/desactivación, asignación de horarios y salarios
	/// </summary>
	public class EmpleadosController : Controller
	{
		// Contexto de base de datos para acceso a datos
		private RecursosHumanosDBEntities db = new RecursosHumanosDBEntities();

		/// <summary>
		/// GET: Empleados
		/// Vista principal que carga el listado de empleados vía AJAX
		/// </summary>
		/// <returns>Vista Index.cshtml</returns>
		public ActionResult Index()
		{
			return View();
		}

		/// <summary>
		/// GET: Empleados/Create
		/// Muestra el formulario para crear un nuevo empleado
		/// </summary>
		/// <returns>Vista Create.cshtml con dropdown de cargos</returns>
		public ActionResult Create()
		{
			// Carga los cargos activos en un dropdown para selección
			ViewBag.CargoID = new SelectList(db.Cargos.Where(c => c.Activo == true), "CargoID", "NombreCargo");
			return View();
		}

		/// <summary>
		/// POST: Empleados/Create
		/// Guarda un nuevo empleado en la base de datos
		/// </summary>
		/// <param name="form">Formulario con los datos del empleado</param>
		/// <returns>Redirección al Index si es exitoso, o vuelve al formulario si hay error</returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create(FormCollection form)
		{
			try
			{
				var empleado = new Empleados();

				// Asignación manual de campos del formulario
				empleado.Nombre = form["Nombre"];
				empleado.Apellido = form["Apellido"];
				empleado.Email = form["Email"];
				empleado.Telefono = form["Telefono"];
				empleado.Direccion = form["Direccion"];
				empleado.FechaNacimiento = !string.IsNullOrEmpty(form["FechaNacimiento"]) ? DateTime.Parse(form["FechaNacimiento"]) : (DateTime?)null;
				empleado.FechaIngreso = !string.IsNullOrEmpty(form["FechaIngreso"]) ? DateTime.Parse(form["FechaIngreso"]) : DateTime.Now;
				empleado.FechaSalida = !string.IsNullOrEmpty(form["FechaSalida"]) ? DateTime.Parse(form["FechaSalida"]) : (DateTime?)null;
				empleado.CargoID = int.Parse(form["CargoID"]);
				empleado.Observaciones = form["Observaciones"];
				empleado.HistorialLaboral = form["HistorialLaboral"];
				empleado.Activo = true; // Nuevo empleado activo por defecto
				empleado.FechaCreacion = DateTime.Now;

				// Guardar empleado en BD
				db.Empleados.Add(empleado);
				db.SaveChanges();

				// Asignar salario automáticamente según su cargo
				AsignarSalarioDesdeTablaSalarios(empleado.EmpleadoID, empleado.CargoID);

				// Asignar horario si se seleccionó uno
				int horarioId = !string.IsNullOrEmpty(form["HorarioID"]) ? int.Parse(form["HorarioID"]) : 0;
				if (horarioId > 0)
				{
					AsignarHorarioAEmpleado(empleado.EmpleadoID, horarioId);
				}

				TempData["Success"] = "Empleado creado exitosamente";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", "Error al guardar: " + ex.Message);
			}

			// Si hay error, recargar dropdown y devolver vista
			ViewBag.CargoID = new SelectList(db.Cargos.Where(c => c.Activo == true), "CargoID", "NombreCargo");
			return View();
		}

		/// <summary>
		/// GET: Empleados/Edit/5
		/// Muestra el formulario para editar un empleado existente
		/// </summary>
		/// <param name="id">ID del empleado a editar</param>
		/// <returns>Vista Edit.cshtml con datos del empleado</returns>
		public ActionResult Edit(int id)
		{
			var empleado = db.Empleados.Find(id);
			if (empleado == null)
			{
				return HttpNotFound();
			}

			// Obtener el horario actual del empleado para preseleccionarlo en el dropdown
			var horarioActual = db.AsignacionHorarios
				.Where(a => a.EmpleadoID == id && a.Activo == true)
				.Select(a => a.HorarioID)
				.FirstOrDefault();

			ViewBag.HorarioActual = horarioActual;
			ViewBag.CargoID = new SelectList(db.Cargos.Where(c => c.Activo == true), "CargoID", "NombreCargo", empleado.CargoID);
			return View(empleado);
		}

		/// <summary>
		/// POST: Empleados/Edit/5
		/// Actualiza los datos de un empleado existente
		/// </summary>
		/// <param name="id">ID del empleado a actualizar</param>
		/// <param name="form">Formulario con los datos actualizados</param>
		/// <returns>Redirección al Index si es exitoso</returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(int id, FormCollection form)
		{
			var empleadoExistente = db.Empleados.Find(id);
			if (empleadoExistente == null)
			{
				return HttpNotFound();
			}

			// Guardar cargo anterior para detectar cambios
			int cargoAnterior = empleadoExistente.CargoID;

			// Actualizar campos del empleado
			empleadoExistente.Nombre = form["Nombre"];
			empleadoExistente.Apellido = form["Apellido"];
			empleadoExistente.Email = form["Email"];
			empleadoExistente.Telefono = form["Telefono"];
			empleadoExistente.Direccion = form["Direccion"];
			empleadoExistente.FechaNacimiento = !string.IsNullOrEmpty(form["FechaNacimiento"]) ? DateTime.Parse(form["FechaNacimiento"]) : (DateTime?)null;
			empleadoExistente.FechaIngreso = !string.IsNullOrEmpty(form["FechaIngreso"]) ? DateTime.Parse(form["FechaIngreso"]) : (DateTime?)null;
			empleadoExistente.FechaSalida = !string.IsNullOrEmpty(form["FechaSalida"]) ? DateTime.Parse(form["FechaSalida"]) : (DateTime?)null;
			empleadoExistente.CargoID = int.Parse(form["CargoID"]);
			empleadoExistente.Observaciones = form["Observaciones"];
			empleadoExistente.HistorialLaboral = form["HistorialLaboral"];

			// Actualizar estado activo según fecha de salida
			if (empleadoExistente.FechaSalida.HasValue)
			{
				empleadoExistente.Activo = false;
			}
			else
			{
				empleadoExistente.Activo = true;
			}

			empleadoExistente.FechaModificacion = DateTime.Now;

			// Guardar cambios del empleado
			db.Entry(empleadoExistente).State = EntityState.Modified;
			db.SaveChanges();

			// ========== ACTUALIZAR HORARIO (SIN CREAR NUEVO REGISTRO) ==========
			int nuevoHorarioId = !string.IsNullOrEmpty(form["HorarioID"]) ? int.Parse(form["HorarioID"]) : 0;

			if (nuevoHorarioId > 0)
			{
				// Buscar la asignación de horario activa actual
				var asignacionActual = db.AsignacionHorarios
					.FirstOrDefault(a => a.EmpleadoID == id && a.Activo == true);

				if (asignacionActual != null)
				{
					if (asignacionActual.HorarioID != nuevoHorarioId)
					{
						// Si cambió el horario, actualizar el registro existente (NO crear uno nuevo)
						asignacionActual.HorarioID = nuevoHorarioId;
						asignacionActual.FechaModificacion = DateTime.Now;
						db.Entry(asignacionActual).State = EntityState.Modified;
						db.SaveChanges();
					}
				}
				else
				{
					// Si no tiene asignación, crear una nueva
					var nuevaAsignacion = new AsignacionHorarios
					{
						EmpleadoID = id,
						HorarioID = nuevoHorarioId,
						Activo = true,
						FechaAsignacion = DateTime.Now,
						FechaCreacion = DateTime.Now
					};
					db.AsignacionHorarios.Add(nuevaAsignacion);
					db.SaveChanges();
				}
			}

			// Actualizar salario si cambió de cargo
			bool cargoCambiado = cargoAnterior != empleadoExistente.CargoID;
			if (cargoCambiado)
			{
				// Desactivar salario anterior
				var salarioAnterior = db.Salarios.FirstOrDefault(s => s.EmpleadoID == id && s.Activo == true);
				if (salarioAnterior != null)
				{
					salarioAnterior.Activo = false;
					salarioAnterior.FechaFin = DateTime.Now;
					db.Entry(salarioAnterior).State = EntityState.Modified;
					db.SaveChanges();
				}
				// Asignar nuevo salario según nuevo cargo
				AsignarSalarioDesdeTablaSalarios(id, empleadoExistente.CargoID);
			}

			TempData["Success"] = "Empleado actualizado exitosamente";
			return RedirectToAction("Index");
		}

		/// <summary>
		/// POST: Empleados/ToggleActivo
		/// Activa o desactiva un empleado (Soft Delete)
		/// No elimina el registro, solo cambia el estado Activo
		/// </summary>
		/// <param name="id">ID del empleado</param>
		/// <returns>JSON con resultado de la operación</returns>
		[HttpPost]
		public JsonResult ToggleActivo(int id)
		{
			try
			{
				var empleado = db.Empleados.Find(id);
				if (empleado == null)
				{
					return Json(new { success = false, message = "Empleado no encontrado" });
				}

				if (empleado.Activo == true)
				{
					// ========== DESACTIVAR EMPLEADO ==========
					empleado.Activo = false;
					empleado.FechaSalida = DateTime.Now;
					empleado.FechaModificacion = DateTime.Now;

					// Desactivar salario activo
					var salarioActivo = db.Salarios.FirstOrDefault(s => s.EmpleadoID == id && s.Activo == true);
					if (salarioActivo != null)
					{
						salarioActivo.Activo = false;
						salarioActivo.FechaFin = DateTime.Now;
					}

					db.SaveChanges();
					return Json(new { success = true, message = "Empleado desactivado exitosamente", nuevoEstado = "Inactivo" });
				}
				else
				{
					// ========== ACTIVAR EMPLEADO ==========
					empleado.Activo = true;
					empleado.FechaSalida = null;
					empleado.FechaModificacion = DateTime.Now;

					// Reactivar salario existente (NO crear uno nuevo)
					var salarioExistente = db.Salarios
						.Where(s => s.EmpleadoID == id)
						.OrderByDescending(s => s.SalarioID)
						.FirstOrDefault();

					if (salarioExistente != null)
					{
						salarioExistente.Activo = true;
						salarioExistente.FechaFin = null;
						salarioExistente.FechaInicio = DateTime.Now;
					}
					else
					{
						// Si no existe salario, crear uno nuevo
						AsignarSalarioDesdeTablaSalarios(id, empleado.CargoID);
					}

					db.SaveChanges();
					return Json(new { success = true, message = "Empleado activado exitosamente", nuevoEstado = "Activo" });
				}
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Error al procesar: " + ex.Message });
			}
		}

		/// <summary>
		/// GET: Empleados/GetHorarios
		/// Obtiene lista de horarios activos para llenar dropdowns
		/// </summary>
		/// <returns>JSON con lista de horarios (ID, Nombre, HoraEntrada, HoraSalida)</returns>
		[HttpGet]
		public JsonResult GetHorarios()
		{
			var horarios = db.Horarios
				.Where(h => h.Activo == true)
				.ToList()
				.Select(h => new {
					h.HorarioID,
					h.Nombre,
					HoraEntrada = TimeSpan.Parse(h.HoraEntrada.ToString()).ToString(@"hh\:mm"),
					HoraSalida = TimeSpan.Parse(h.HoraSalida.ToString()).ToString(@"hh\:mm")
				})
				.OrderBy(h => h.Nombre)
				.ToList();

			return Json(horarios, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Asigna un horario a un empleado (solo para creación)
		/// Si ya existe una asignación activa, no crea duplicado
		/// </summary>
		/// <param name="empleadoId">ID del empleado</param>
		/// <param name="horarioId">ID del horario a asignar</param>
		private void AsignarHorarioAEmpleado(int empleadoId, int horarioId)
		{
			// Verificar si ya existe una asignación activa
			var asignacionExistente = db.AsignacionHorarios
				.FirstOrDefault(a => a.EmpleadoID == empleadoId && a.Activo == true);

			if (asignacionExistente == null)
			{
				// Solo crear nueva asignación si no existe una activa
				var nuevaAsignacion = new AsignacionHorarios
				{
					EmpleadoID = empleadoId,
					HorarioID = horarioId,
					Activo = true,
					FechaAsignacion = DateTime.Now,
					FechaCreacion = DateTime.Now
				};
				db.AsignacionHorarios.Add(nuevaAsignacion);
				db.SaveChanges();
			}
		}

		/// <summary>
		/// GET: Empleados/GetAll
		/// Obtiene lista de empleados con filtros (búsqueda, cargo, activos/inactivos)
		/// Utilizado por AJAX para cargar la tabla principal
		/// </summary>
		/// <param name="busqueda">Texto de búsqueda (nombre, email, teléfono)</param>
		/// <param name="cargoId">ID del cargo para filtrar</param>
		/// <param name="mostrarInactivos">Indica si mostrar empleados inactivos</param>
		/// <returns>JSON con lista de empleados y estadísticas</returns>
		[HttpGet]
		public JsonResult GetAll(string busqueda, int? cargoId, bool mostrarInactivos = false)
		{
			var empleadosQuery = db.Empleados
				.Include(e => e.Cargos)
				.Include(e => e.Salarios)
				.AsQueryable();

			// Filtro por estado activo/inactivo
			if (mostrarInactivos)
			{
				empleadosQuery = empleadosQuery.Where(e => e.Activo == false);
			}
			else
			{
				empleadosQuery = empleadosQuery.Where(e => e.Activo == true);
			}

			// Filtro por texto de búsqueda
			if (!string.IsNullOrEmpty(busqueda))
			{
				empleadosQuery = empleadosQuery.Where(e =>
					(e.Nombre + " " + e.Apellido).Contains(busqueda) ||
					e.Email.Contains(busqueda) ||
					e.Telefono.Contains(busqueda)
				);
			}

			// Filtro por cargo
			if (cargoId.HasValue && cargoId.Value > 0)
			{
				empleadosQuery = empleadosQuery.Where(e => e.CargoID == cargoId.Value);
			}

			// Traer datos a memoria para evitar errores de LINQ to Entities
			var empleadosList = empleadosQuery
				.OrderBy(e => e.EmpleadoID)
				.ToList();

			// Proyección en memoria (LINQ to Objects) para formatear datos
			var empleados = empleadosList.Select(e => new
			{
				e.EmpleadoID,
				NombreCompleto = e.Nombre + " " + e.Apellido,
				e.Telefono,
				e.Email,
				NombreCargo = e.Cargos != null ? e.Cargos.NombreCargo : "Sin Cargo",
				e.CargoID,
				e.Observaciones,
				SalarioActual = e.Salarios.Where(s => s.Activo == true).Select(s => s.Monto).FirstOrDefault(),
				FechaIngreso = e.FechaIngreso.HasValue ? e.FechaIngreso.Value.ToString("dd/MM/yyyy") : "No registrada",
				Edad = e.FechaNacimiento.HasValue ? (int?)(DateTime.Now.Year - e.FechaNacimiento.Value.Year) : 0,
				Estado = e.Activo == true ? "Activo" : "Inactivo",
				e.Activo,
				// Obtener nombre del horario activo
				NombreHorario = db.AsignacionHorarios
					.Where(a => a.EmpleadoID == e.EmpleadoID && a.Activo == true)
					.Select(a => a.Horarios.Nombre)
					.FirstOrDefault() ?? "Sin Horario",
				HoraEntrada = db.AsignacionHorarios
					.Where(a => a.EmpleadoID == e.EmpleadoID && a.Activo == true)
					.Select(a => a.Horarios.HoraEntrada.ToString())
					.FirstOrDefault(),
				HoraSalida = db.AsignacionHorarios
					.Where(a => a.EmpleadoID == e.EmpleadoID && a.Activo == true)
					.Select(a => a.Horarios.HoraSalida.ToString())
					.FirstOrDefault()
			}).ToList();

			// Estadísticas para tarjetas de resumen
			var estadisticas = new
			{
				Total = db.Empleados.Count(),
				Activos = db.Empleados.Count(e => e.Activo == true),
				Inactivos = db.Empleados.Count(e => e.Activo == false),
				SalarioPromedio = empleados.Where(e => e.SalarioActual.HasValue)
										  .Select(e => e.SalarioActual.Value)
										  .DefaultIfEmpty(0)
										  .Average()
			};

			return Json(new { data = empleados, estadisticas = estadisticas }, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// GET: Empleados/GetCargos
		/// Obtiene lista de cargos activos para llenar dropdowns
		/// </summary>
		/// <returns>JSON con lista de cargos</returns>
		[HttpGet]
		public JsonResult GetCargos()
		{
			var cargos = db.Cargos
				.Where(c => c.Activo == true)
				.Select(c => new { c.CargoID, c.NombreCargo })
				.OrderBy(c => c.NombreCargo)
				.ToList();

			return Json(cargos, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// GET: Empleados/GetDetalleEmpleado
		/// Obtiene todos los detalles de un empleado específico para el modal de detalles
		/// </summary>
		/// <param name="id">ID del empleado</param>
		/// <returns>JSON con datos detallados del empleado</returns>
		[HttpGet]
		public JsonResult GetDetalleEmpleado(int id)
		{
			// Primero traemos los datos sin formatear (evita errores LINQ to Entities)
			var empleadoQuery = db.Empleados
				.Where(e => e.EmpleadoID == id)
				.Select(e => new
				{
					e.EmpleadoID,
					NombreCompleto = e.Nombre + " " + e.Apellido,
					e.Email,
					e.Telefono,
					e.Direccion,
					e.Observaciones,
					Cargo = e.Cargos != null ? e.Cargos.NombreCargo : "Sin Cargo",
					FechaIngreso = e.FechaIngreso,
					SalarioActual = e.Salarios.Where(s => s.Activo == true).Select(s => s.Monto).FirstOrDefault(),
					FechaNacimiento = e.FechaNacimiento,
					Estado = e.Activo == true ? "Activo" : "Inactivo",
					e.Activo
				})
				.FirstOrDefault();

			if (empleadoQuery == null)
			{
				return Json(new { success = false, message = "Empleado no encontrado" }, JsonRequestBehavior.AllowGet);
			}

			// Formateamos fechas en memoria
			var empleado = new
			{
				empleadoQuery.EmpleadoID,
				empleadoQuery.NombreCompleto,
				empleadoQuery.Email,
				empleadoQuery.Telefono,
				empleadoQuery.Direccion,
				empleadoQuery.Observaciones,
				empleadoQuery.Cargo,
				FechaIngreso = empleadoQuery.FechaIngreso.HasValue
					? empleadoQuery.FechaIngreso.Value.ToString("dd/MM/yyyy")
					: "No registrada",
				SalarioActual = empleadoQuery.SalarioActual,
				Edad = empleadoQuery.FechaNacimiento.HasValue
					? DateTime.Now.Year - empleadoQuery.FechaNacimiento.Value.Year
					: 0,
				empleadoQuery.Estado,
				empleadoQuery.Activo
			};

			return Json(new { success = true, data = empleado }, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// GET: Empleados/GetEstadisticasPorDepartamento
		/// Obtiene estadísticas agrupadas por departamento (cantidad, salario promedio, antigüedad)
		/// </summary>
		/// <returns>JSON con estadísticas por departamento</returns>
		[HttpGet]
		public JsonResult GetEstadisticasPorDepartamento()
		{
			var empleadosConDepto = db.Empleados
				.Where(e => e.Cargos != null && e.Cargos.Departamentos != null && e.Activo == true)
				.Select(e => new
				{
					Departamento = e.Cargos.Departamentos.Nombre ?? "Sin Departamento",
					SalarioActivo = e.Salarios.Where(s => s.Activo == true).Select(s => s.Monto).FirstOrDefault(),
					FechaIngreso = e.FechaIngreso
				})
				.ToList();

			var estadisticas = empleadosConDepto
				.GroupBy(e => e.Departamento)
				.Select(g => new
				{
					Departamento = g.Key,
					Cantidad = g.Count(),
					SalarioPromedio = g.Where(x => x.SalarioActivo.HasValue)
									  .Select(x => x.SalarioActivo.Value)
									  .DefaultIfEmpty(0)
									  .Average(),
					// Calcular antigüedad promedio en años
					AntiguedadPromedio = g.Where(x => x.FechaIngreso.HasValue)
										 .Average(x => CalcularAntiguedadEnAños(x.FechaIngreso.Value))
				})
				.OrderByDescending(x => x.Cantidad)
				.ToList();

			return Json(estadisticas, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Calcula la antigüedad en años desde la fecha de ingreso hasta hoy
		/// </summary>
		/// <param name="fechaIngreso">Fecha de ingreso del empleado</param>
		/// <returns>Número de años trabajados</returns>
		private int CalcularAntiguedadEnAños(DateTime fechaIngreso)
		{
			var hoy = DateTime.Now;
			var años = hoy.Year - fechaIngreso.Year;
			// Si aún no ha pasado el aniversario este año, restar un año
			if (fechaIngreso.Date > hoy.AddYears(-años)) años--;
			return años;
		}

		/// <summary>
		/// Asigna un salario al empleado basado en su cargo
		/// Busca salarios de referencia de otros empleados con el mismo cargo
		/// </summary>
		/// <param name="empleadoId">ID del empleado</param>
		/// <param name="cargoId">ID del cargo</param>
		private void AsignarSalarioDesdeTablaSalarios(int empleadoId, int cargoId)
		{
			// Buscar salario de referencia de empleados activos con el mismo cargo
			var salarioReferencia = db.Salarios
				.Where(s => s.Empleados.CargoID == cargoId && s.Activo == true && s.Empleados.Activo == true)
				.Select(s => s.Monto)
				.FirstOrDefault();

			// Si no hay referencia, usar salario base
			decimal montoSalario = salarioReferencia ?? 800.00m;

			var salario = new Salarios
			{
				EmpleadoID = empleadoId,
				Monto = montoSalario,
				FechaInicio = DateTime.Now,
				FechaFin = null,
				Activo = true,
				FechaCreacion = DateTime.Now
			};

			db.Salarios.Add(salario);
			db.SaveChanges();
		}

		/// <summary>
		/// Libera los recursos del contexto de base de datos
		/// </summary>
		/// <param name="disposing">Indica si se están liberando recursos administrados</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) db.Dispose();
			base.Dispose(disposing);
		}
	}
}