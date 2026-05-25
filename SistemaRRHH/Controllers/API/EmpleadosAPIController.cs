using System;
using System.Linq;
using System.Web.Http;
using SistemaRRHH.Models;

namespace SistemaRRHH.Controllers.API
{
	/// <summary>
	/// API para reportes de empleados
	/// Ruta base: /api/reportes
	/// </summary>
	[RoutePrefix("api/reportes")]
	public class ReportesAPIController : ApiController
	{
		// Contexto de la base de datos (Entity Framework)
		private RecursosHumanosDBEntities db = new RecursosHumanosDBEntities();

		/// <summary>
		/// GET: api/reportes/empleados
		/// Obtiene la lista de empleados para reportes en tiempo real
		/// Parámetro opcional: soloActivos (default: true)
		/// </summary>
		/// <param name="soloActivos">Si es true, solo devuelve empleados activos (Activo = true)</param>
		/// <returns>JSON con lista de empleados, total y fecha del reporte</returns>
		[HttpGet]
		[Route("empleados")]
		public IHttpActionResult GetEmpleados(bool soloActivos = true)
		{
			try
			{
				// Consulta LINQ para obtener empleados
				var empleados = db.Empleados
					// Filtro: si soloActivos es true, trae solo empleados con Activo = true
					// Si soloActivos es false, trae todos (activos e inactivos)
					.Where(e => soloActivos ? e.Activo == true : true)
					.Select(e => new
					{
						// Datos básicos del empleado
						e.EmpleadoID,
						// Concatena nombre y apellido
						NombreCompleto = e.Nombre + " " + e.Apellido,
						e.Telefono,
						e.Email,

						// Obtiene el nombre del cargo (si existe, sino "Sin Cargo")
						Cargo = e.Cargos != null ? e.Cargos.NombreCargo : "Sin Cargo",

						// Obtiene el nombre del departamento (si existe, sino "Sin Departamento")
						Departamento = e.Cargos.Departamentos != null ? e.Cargos.Departamentos.Nombre : "Sin Departamento",

						// Obtiene el salario activo del empleado (el último salario con Activo = true)
						// Si no hay salario, devuelve 0
						Salario = e.Salarios.Where(s => s.Activo == true).Select(s => s.Monto).FirstOrDefault() ?? 0,

						// Fecha de ingreso formateada a dd/MM/yyyy, si es null muestra "N/A"
						FechaIngreso = e.FechaIngreso != null ? e.FechaIngreso.Value.ToString("dd/MM/yyyy") : "N/A",

						// Calcula la edad basada en la fecha de nacimiento
						Edad = e.FechaNacimiento.HasValue ? DateTime.Now.Year - e.FechaNacimiento.Value.Year : 0,

						// Estado del empleado: "Activo" si Activo = true, "Inactivo" si no
						Estado = e.Activo == true ? "Activo" : "Inactivo"
					})
					// Ordena alfabéticamente por nombre completo
					.OrderBy(e => e.NombreCompleto)
					// Ejecuta la consulta y convierte a lista
					.ToList();

				// Retorna JSON con éxito, los datos, el total y la fecha/hora actual
				return Ok(new
				{
					success = true,           // Indica que la operación fue exitosa
					data = empleados,          // Lista de empleados
					count = empleados.Count,   // Cantidad total de empleados obtenidos
					fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm")  // Fecha y hora de generación
				});
			}
			catch (Exception ex)
			{
				// Si ocurre algún error, retorna un mensaje de error
				return BadRequest(ex.Message);
			}
		}

		/// <summary>
		/// GET: api/reportes/cargos
		/// Obtiene la lista de cargos activos para usar en filtros de reportes
		/// </summary>
		/// <returns>JSON con lista de cargos (ID y Nombre)</returns>
		[HttpGet]
		[Route("cargos")]
		public IHttpActionResult GetCargos()
		{
			// Consulta LINQ para obtener cargos activos
			var cargos = db.Cargos
				// Solo cargos activos (Activo = true)
				.Where(c => c.Activo == true)
				// Selecciona solo ID y Nombre (proyección)
				.Select(c => new { c.CargoID, c.NombreCargo })
				// Ordena alfabéticamente por nombre del cargo
				.OrderBy(c => c.NombreCargo)
				// Ejecuta la consulta y convierte a lista
				.ToList();

			// Retorna JSON con éxito y los datos
			return Ok(new { success = true, data = cargos });
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