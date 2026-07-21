namespace Ticketing.command.Application.Models
{
  /// <summary>
  /// Clase de configuración (POCO) que mapea la sección "MongoSettings" del archivo appsettings.json.
  ///
  /// En .NET, el patrón Options permite inyectar configuración fuertemente tipada
  /// usando IOptions&lt;MongoSettings&gt; en lugar de leer strings "mágicos" del IConfiguration.
  ///
  /// Ejemplo en appsettings.json:
  ///   "MongoSettings": {
  ///     "Database": "ticketingApp"
  ///   }
  /// </summary>
  public class MongoSettings
  {
    /// <summary>
    /// Cadena de conexión completa a MongoDB (ej: "mongodb://localhost:27017/?replicaSet=rs0").
    /// Se registra por separado bajo "ConnectionStrings:MongoDb" en appsettings.json.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la base de datos a la que el microservicio se conecta (ej: "ticketingApp").
    /// </summary>
    public string Database { get; set; } = string.Empty;
  }
}

