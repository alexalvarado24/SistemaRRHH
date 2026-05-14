using System;
using System.Linq;
using System.Web.Mvc;
using SistemaRRHH.Models;
using System.Data.Entity;
using System.Collections.Generic;

namespace SistemaRRHH.Controllers
{
	public class EmpleadosController : Controller
	{
		private RecursosHumanosDBEntities db = new RecursosHumanosDBEntities();

		// GET: Empleados con LINQ avanzado
		public ActionResult Index(string busqueda, int? cargoId)
		{
			// LINQ con filtros dinámicos
			var empleadosQuery = db.Empleados
				.Include(e => e.Cargos)
				.Include(e => e.Salarios)
				.Select(e => new EmpleadoViewModel
				{
					EmpleadoID = e.EmpleadoID,
					NombreCompleto = e.Nombre + " " + e.Apellido,
					Telefono = e.Telefono,
					Email = e.Email,
					NombreCargo = e.Cargos.NombreCargo,
					CargoID = e.CargoID,
					SalarioActual = e.Salarios
						.Where(s => s.EsActivo == true)
						.Select(s => s.Monto)
						.FirstOrDefault(),
					FechaIngreso = e.FechaIngreso,
					Edad = (int)(e.FechaNacimiento.HasValue ?
						   (int?)(DateTime.Now.Year - e.FechaNacimiento.Value.Year) : 0)
				})
				.AsQueryable();

			// LINQ: Filtro por búsqueda
			if (!string.IsNullOrEmpty(busqueda))
			{
				empleadosQuery = empleadosQuery.Where(e =>
					e.NombreCompleto.Contains(busqueda) ||
					e.Email.Contains(busqueda) ||
					e.Telefono.Contains(busqueda)
				);
			}

			// LINQ: Filtro por cargo
			if (cargoId.HasValue && cargoId.Value > 0)
			{
				empleadosQuery = empleadosQuery.Where(e => e.CargoID == cargoId.Value);
			}

			// LINQ: Ordenamiento
			var empleados = empleadosQuery.OrderBy(e => e.NombreCompleto).ToList();

			// LINQ: Agregación para estadísticas
			ViewBag.TotalEmpleados = db.Empleados.Count();
			ViewBag.TotalActivos = db.Empleados.Count(e => e.FechaSalida == null);
			ViewBag.SalarioPromedio = db.Salarios
				.Where(s => s.EsActivo == true)
				.Select(s => s.Monto)
				.DefaultIfEmpty(0)
				.Average() ?? 0;

			ViewBag.CargosList = new SelectList(db.Cargos, "CargoID", "NombreCargo");
			ViewBag.BusquedaActual = busqueda;
			ViewBag.CargoFiltro = cargoId;

			return View(empleados);
		}

		// AJAX: Búsqueda en tiempo real
		[HttpGet]
		public JsonResult BuscarEmpleadosAjax(string termino)
		{
			if (string.IsNullOrEmpty(termino))
			{
				return Json(new List<object>(), JsonRequestBehavior.AllowGet);
			}

			var resultados = db.Empleados
				.Where(e => e.Nombre.Contains(termino) ||
							e.Apellido.Contains(termino) ||
							e.Email.Contains(termino))
				.Take(10)
				.Select(e => new
				{
					id = e.EmpleadoID,
					text = e.Nombre + " " + e.Apellido + " - " + e.Email,
					cargo = e.Cargos.NombreCargo
				})
				.ToList();

			return Json(resultados, JsonRequestBehavior.AllowGet);
		}

		// AJAX: Obtener detalles rápidos sin recargar
		[HttpGet]
		public JsonResult GetDetalleEmpleado(int id)
		{
			// Primero obtenemos los datos sin formatear
			var empleado = db.Empleados
				.Include(e => e.Cargos)
				.Include(e => e.Salarios)
				.Where(e => e.EmpleadoID == id)
				.Select(e => new
				{
					e.EmpleadoID,
					NombreCompleto = e.Nombre + " " + e.Apellido,
					e.Email,
					e.Telefono,
					e.Direccion,
					Cargo = e.Cargos.NombreCargo,
					FechaIngreso = e.FechaIngreso,  // ← DateTime? sin formatear
					SalarioActual = e.Salarios
						.Where(s => s.EsActivo == true)
						.Select(s => s.Monto)
						.FirstOrDefault(),
					FechaNacimiento = e.FechaNacimiento  // ← DateTime? sin formatear
				})
				.FirstOrDefault();

			if (empleado == null)
			{
				return Json(new { error = "Empleado no encontrado" }, JsonRequestBehavior.AllowGet);
			}

			// Luego formateamos en memoria (después de materializar)
			var resultado = new
			{
				empleado.EmpleadoID,
				empleado.NombreCompleto,
				empleado.Email,
				empleado.Telefono,
				empleado.Direccion,
				empleado.Cargo,
				FechaIngreso = empleado.FechaIngreso.HasValue ?
							   empleado.FechaIngreso.Value.ToString("dd/MM/yyyy") : "No registrada",
				empleado.SalarioActual,
				Edad = empleado.FechaNacimiento.HasValue ?
					   DateTime.Now.Year - empleado.FechaNacimiento.Value.Year : 0
			};

			return Json(resultado, JsonRequestBehavior.AllowGet);
		}

		// AJAX: Validación en tiempo real (usando LINQ)
		[HttpGet]
		public JsonResult ValidarEmailUnico(string email, int? id)
		{
			bool existe;
			if (id.HasValue)
			{
				existe = db.Empleados.Any(e => e.Email == email && e.EmpleadoID != id.Value);
			}
			else
			{
				existe = db.Empleados.Any(e => e.Email == email);
			}
			return Json(!existe, JsonRequestBehavior.AllowGet);
		}

		// LINQ: Reporte de empleados por departamento
		[HttpGet]
		public JsonResult GetEstadisticasPorDepartamento()
		{
			// Verificar si hay datos antes de agrupar
			var estadisticas = db.Empleados
				.Where(e => e.Cargos != null && e.Cargos.Departamentos != null)
				.GroupBy(e => e.Cargos.Departamentos.Nombre)
				.Select(g => new
				{
					Departamento = g.Key ?? "Sin Departamento",
					Cantidad = g.Count(),
					SalarioPromedio = g.SelectMany(e => e.Salarios)
						.Where(s => s.EsActivo == true)
						.Select(s => s.Monto)
						.DefaultIfEmpty(0)
						.Average(),
					AntiguedadPromedio = g.Average(e => e.FechaIngreso.HasValue ?
						(DateTime.Now.Year - e.FechaIngreso.Value.Year) : 0)
				})
				.OrderByDescending(x => x.Cantidad)
				.ToList();

			return Json(estadisticas, JsonRequestBehavior.AllowGet);
		}

		// GET: api/empleados (REST API)
		[System.Web.Mvc.HttpGet]
		public JsonResult GetEmpleadosAPI()
		{
			var empleados = db.Empleados
				.Select(e => new
				{
					e.EmpleadoID,
					e.Nombre,
					e.Apellido,
					e.Email,
					Cargo = e.Cargos != null ? e.Cargos.NombreCargo : "Sin Cargo"
				})
				.ToList();
			return Json(empleados, JsonRequestBehavior.AllowGet);
		}

		// GET: Empleados/Details/5
		public ActionResult Details(int id)
		{
			var empleado = db.Empleados.Find(id);
			if (empleado == null)
			{
				return HttpNotFound();
			}
			return View(empleado);
		}

		// GET: Empleados/Create
		public ActionResult Create()
		{
			ViewBag.CargoID = new SelectList(db.Cargos, "CargoID", "NombreCargo");
			return View();
		}

		// POST: Empleados/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create(Empleados empleado)
		{
			if (ModelState.IsValid)
			{
				db.Empleados.Add(empleado);
				db.SaveChanges();
				return RedirectToAction("Index");
			}
			ViewBag.CargoID = new SelectList(db.Cargos, "CargoID", "NombreCargo", empleado.CargoID);
			return View(empleado);
		}

		// GET: Empleados/Edit/5
		public ActionResult Edit(int id)
		{
			var empleado = db.Empleados.Find(id);
			if (empleado == null)
			{
				return HttpNotFound();
			}
			ViewBag.CargoID = new SelectList(db.Cargos, "CargoID", "NombreCargo", empleado.CargoID);
			return View(empleado);
		}

		// POST: Empleados/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(Empleados empleado)
		{
			if (ModelState.IsValid)
			{
				db.Entry(empleado).State = EntityState.Modified;
				db.SaveChanges();
				return RedirectToAction("Index");
			}
			ViewBag.CargoID = new SelectList(db.Cargos, "CargoID", "NombreCargo", empleado.CargoID);
			return View(empleado);
		}

		// GET: Empleados/Delete/5
		public ActionResult Delete(int id)
		{
			var empleado = db.Empleados.Find(id);
			if (empleado == null)
			{
				return HttpNotFound();
			}
			return View(empleado);
		}

		// POST: Empleados/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public ActionResult DeleteConfirmed(int id)
		{
			var empleado = db.Empleados.Find(id);
			db.Empleados.Remove(empleado);
			db.SaveChanges();
			return RedirectToAction("Index");
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

	// ViewModel para empleados
	public class EmpleadoViewModel
	{
		public int EmpleadoID { get; set; }
		public string NombreCompleto { get; set; }
		public string Telefono { get; set; }
		public string Email { get; set; }
		public string NombreCargo { get; set; }
		public int CargoID { get; set; }
		public decimal? SalarioActual { get; set; }
		public DateTime? FechaIngreso { get; set; }
		public int Edad { get; set; }
	}
}