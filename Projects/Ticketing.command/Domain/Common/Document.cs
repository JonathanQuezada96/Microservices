using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ticketing.command.Domain.Common
{
  /// <summary>
  /// Implementación base concreta de IDocuments.
  ///
  /// Las entidades del dominio que se persisten en MongoDB pueden heredar de esta clase
  /// para obtener el Id automáticamente, en lugar de tener que declarar la propiedad
  /// en cada entidad individualmente.
  ///
  /// Ejemplo de uso:
  ///   public class EventModel : Document { ... }
  /// </summary>
  public class Document : IDocuments
  {
    /// <summary>
    /// Identificador único generado por MongoDB (ObjectId).
    /// [BsonRepresentation(BsonType.String)] → serializa el ObjectId como cadena de texto
    /// en la base de datos, mejorando la legibilidad en Mongo Compass o en logs.
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public ObjectId Id { get; set; }
  }
}
