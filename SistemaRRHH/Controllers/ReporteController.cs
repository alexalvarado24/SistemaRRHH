using System;
using System.IO;
using System.Web.Mvc;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using SistemaRRHH.Helpers;

namespace SistemaRRHH.Controllers
{
	/// <summary>
	/// Controlador para la generacion de reportes con Crystal Reports
	/// Maneja la visualizacion y descarga de reportes PDF de empleados
	/// </summary>
	public class ReporteController : Controller
	{
		// ==================== VISTA PRINCIPAL ====================

		/// <summary>
		/// GET: Reporte/Empleados
		/// Muestra la vista con los botones para ver/descargar el reporte
		/// </summary>
		public ActionResult Empleados()
		{
			return View();
		}

		// ==================== VER REPORTE EN IFRAME ====================

		/// <summary>
		/// GET: Reporte/VerEmpleados
		/// Genera y muestra el reporte PDF en el navegador (para incrustar en iframe)
		/// Solo incluye empleados ACTIVOS (Activo = true)
		/// </summary>
		/// <returns>Archivo PDF para visualizacion en navegador</returns>
		public ActionResult VerEmpleados()
		{
			try
			{
				// ========== 1. VERIFICAR EXISTENCIA DEL ARCHIVO DE REPORTE ==========
				string reportPath = Server.MapPath("~/Reports/rptEmpleados.rpt");

				if (!System.IO.File.Exists(reportPath))
				{
					return Content("Error: No se encuentra el archivo de reporte en: " + reportPath);
				}

				// ========== 2. CARGAR EL REPORTE ==========
				ReportDocument reporte = new ReportDocument();
				reporte.Load(reportPath);

				// ========== 3. CONFIGURAR CONEXION A BASE DE DATOS ==========
				var coninfo = ReportesConexion.GetConexion();

				// Aplica la informacion de conexion a cada tabla del reporte
				foreach (Table table in reporte.Database.Tables)
				{
					table.LogOnInfo.ConnectionInfo = coninfo;
					table.ApplyLogOnInfo(table.LogOnInfo);
				}

				// ========== 4. APLICAR FILTROS ==========
				// Filtro: solo muestra empleados ACTIVOS (Activo = true)
				// Los empleados inactivos NO aparecen en el reporte
				reporte.RecordSelectionFormula = "{Empleados.Activo} = true";

				// ========== 5. EXPORTAR A PDF ==========
				Stream stream = reporte.ExportToStream(ExportFormatType.PortableDocFormat);
				stream.Seek(0, SeekOrigin.Begin);

				// Retorna el PDF para visualizacion en navegador (NO descarga)
				return File(stream, "application/pdf");
			}
			catch (Exception ex)
			{
				// Si ocurre algun error, muestra el mensaje y el stack trace para depuracion
				return Content("Error: " + ex.Message + "\n\nDetalle: " + ex.StackTrace);
			}
		}

		// ==================== DESCARGAR REPORTE ====================

		/// <summary>
		/// GET: Reporte/DescargarEmpleados
		/// Genera y fuerza la descarga del reporte PDF
		/// Solo incluye empleados ACTIVOS (Activo = true)
		/// </summary>
		/// <returns>Archivo PDF para descarga</returns>
		public ActionResult DescargarEmpleados()
		{
			try
			{
				// ========== 1. VERIFICAR EXISTENCIA DEL ARCHIVO DE REPORTE ==========
				string reportPath = Server.MapPath("~/Reports/rptEmpleados.rpt");

				if (!System.IO.File.Exists(reportPath))
				{
					return Content("Error: No se encuentra el archivo de reporte");
				}

				// ========== 2. CARGAR EL REPORTE ==========
				ReportDocument reporte = new ReportDocument();
				reporte.Load(reportPath);

				// ========== 3. CONFIGURAR CONEXION A BASE DE DATOS ==========
				var coninfo = ReportesConexion.GetConexion();

				foreach (Table table in reporte.Database.Tables)
				{
					table.LogOnInfo.ConnectionInfo = coninfo;
					table.ApplyLogOnInfo(table.LogOnInfo);
				}

				// ========== 4. APLICAR FILTROS ==========
				// Filtro: solo muestra empleados ACTIVOS (Activo = true)
				reporte.RecordSelectionFormula = "{Empleados.Activo} = true";

				// ========== 5. EXPORTAR A PDF ==========
				Stream stream = reporte.ExportToStream(ExportFormatType.PortableDocFormat);
				stream.Seek(0, SeekOrigin.Begin);

				// Convierte el stream a byte array para la descarga
				byte[] bytes = new byte[stream.Length];
				stream.Read(bytes, 0, (int)stream.Length);

				// ========== 6. FORZAR DESCARGA ==========
				// El nombre del archivo incluye fecha y hora para evitar cache
				return File(bytes, "application/pdf", $"ReporteEmpleados_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
			}
			catch (Exception ex)
			{
				return Content("Error: " + ex.Message);
			}
		}
	}
}