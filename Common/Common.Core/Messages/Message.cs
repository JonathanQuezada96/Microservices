using MongoDB.Bson.Serialization.Attributes;

namespace Common.Core.Messages
{
  /// <summary>
  /// Clase base abstracta que representa cualquier "mensaje" dentro del sistema.
  /// En arquitecturas de microservicios basadas en Event Sourcing, todos los eventos
  /// y comandos derivan de esta clase para garantizar un identificador único universal.
  /// </summary>
  public abstract class Message
  {
    protected Message()
    {
    }

    /// <summary>
    /// Identificador único del mensaje.
    /// [BsonId] le indica al driver de MongoDB que esta propiedad es la clave primaria (_id)
    /// del documento cuando se almacena en la base de datos.
    /// </summary>
    [BsonId]
    public string ID { get; set; } = string.Empty;
  }
}
