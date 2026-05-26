using CrystalDecisions.Shared;
using System.Configuration;

namespace SistemaRRHH.Helpers
{
	/// <summary>
	/// Clase helper para configurar la conexion de Crystal Reports a la base de datos
	/// Proporciona la informacion de conexion necesaria para que los reportes accedan a los datos
	/// </summary>
	public class ReportesConexion
	{
		/// <summary>
		/// Obtiene la informacion de conexion para Crystal Reports
		/// Lee la cadena de conexion desde Web.config y la convierte al formato que Crystal Reports entiende
		/// </summary>
		/// <returns>ConnectionInfo con los parametros de conexion (servidor, base de datos, autenticacion)</returns>
		public static ConnectionInfo GetConexion()
		{
			// ========== 1. CREAR OBJETO DE CONEXION ==========
			// ConnectionInfo es la clase de Crystal Reports que almacena los parametros de conexion
			ConnectionInfo conexion = new ConnectionInfo();

			// ========== 2. OBTENER CADENA DE CONEXION DEL Web.config ==========
			// Lee la cadena de conexion llamada "CrystalReportsConnection" del archivo Web.config
			// Esta cadena debe estar configurada con los datos del servidor SQL Server
			string connectionString = ConfigurationManager.ConnectionStrings["CrystalReportsConnection"].ConnectionString;

			// ========== 3. VALORES POR DEFECTO ==========
			// Estos valores se usan si no se pueden parsear correctamente
			string dataSource = "DESKTOP-3VMTRMA";  // Nombre del servidor SQL Server (cambiar a conveniencia)
			string database = "RecursosHumanosDB";   // Nombre de la base de datos

			// ========== 4. PARSEAR LA CADENA DE CONEXION ==========
			// Divide la cadena de conexion por punto y coma (;) para obtener cada parametro
			// Ejemplo: "Data Source=SERVIDOR;Initial Catalog=BD;Integrated Security=True"
			var parts = connectionString.Split(';');
			foreach (var part in parts)
			{
				var trimmed = part.Trim();

				// Busca el parametro "Data Source" (nombre del servidor)
				if (trimmed.StartsWith("Data Source="))
					dataSource = trimmed.Replace("Data Source=", "").Trim();

				// Busca el parametro "Initial Catalog" (nombre de la base de datos)
				else if (trimmed.StartsWith("Initial Catalog="))
					database = trimmed.Replace("Initial Catalog=", "").Trim();
			}

			// ========== 5. ASIGNAR VALORES A LA CONEXION ==========
			// Establece el nombre del servidor SQL Server
			conexion.ServerName = dataSource;

			// Establece el nombre de la base de datos
			conexion.DatabaseName = database;

			// Usa autenticacion de Windows (Integrated Security)
			// Esto significa que usa las credenciales del usuario que ejecuta la aplicacion
			conexion.IntegratedSecurity = true;

			// ========== 6. RETORNAR CONFIGURACION DE CONEXION ==========
			return conexion;
		}
	}
}