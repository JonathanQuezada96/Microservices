using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ticketing.command.Domain.Common
{
  /// <summary>
  /// Contrato (interfaz) que deben cumplir todas las entidades que se almacenan
  /// como documentos en MongoDB.
  ///
  /// Garantiza que cada documento tenga un identificador único de tipo ObjectId,
  /// que es el tipo nativo de MongoDB para los _id.
  /// </summary>
  public interface IDocuments
  {
    /// <summary>
    /// Identificador único del documento en MongoDB.
    /// [BsonId]             → marca esta propiedad como el campo _id del documento.
    /// [BsonRepresentation] → almacena el ObjectId como string en la BD en lugar de bytes,
    ///                        lo que facilita la lectura humana y la interoperabilidad.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    ObjectId Id { get; set; }
  }
}
