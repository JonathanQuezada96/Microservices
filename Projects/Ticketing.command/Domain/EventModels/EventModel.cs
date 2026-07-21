using Common.Core.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Ticketing.command.Domain.Common;

namespace Ticketing.command.Domain.EventModels
{
  /// <summary>
  /// Representa un "sobre" (envelope) que envuelve un evento de dominio para
  /// almacenarlo de forma estructurada en el Event Store (colección "eventStores" de MongoDB).
  ///
  /// En Event Sourcing, no se guarda el estado actual de una entidad, sino la secuencia
  /// completa de eventos que la llevaron a ese estado. EventModel es el registro físico
  /// de cada uno de esos eventos en la base de datos.
  ///
  /// [BsonCollection("eventStores")] → indica que esta clase se mapea a la colección
  /// "eventStores" de MongoDB (usando el atributo personalizado BsonCollectionAttribute).
  /// </summary>
  [BsonCollection("eventStores")]
  public class EventModel : IDocuments
  {
    /// <summary>
    /// Identificador único del documento en MongoDB (campo _id).
    /// [BsonId] + [BsonRepresentation(BsonType.String)] → el ObjectId se almacena como string.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public ObjectId Id { get; set; }

    /// <summary>
    /// Fecha y hora exacta en que ocurrió el evento.
    /// [BsonElement("timestamp")] → mapea la propiedad C# al campo "timestamp" del documento MongoDB.
    /// Útil para auditoría y para reconstruir la línea de tiempo de eventos.
    /// </summary>
    [BsonElement("timestamp")]
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// Identificador del Agregado al que pertenece este evento.
    /// Un Agregado (Aggregate) es la raíz de un conjunto de entidades relacionadas.
    /// Ej: el Id del Ticket al que corresponde el evento TicketCreated.
    /// 'required' → debe asignarse obligatoriamente (C# 11+).
    /// </summary>
    [BsonElement("aggregateIdentifier")]
    public required string AggregateIdentifier { get; set; }

    /// <summary>
    /// Tipo o nombre de la clase del Agregado (ej: "TicketAggregate").
    /// Permite identificar a qué tipo de entidad pertenece el evento al leer el Event Store.
    /// </summary>
    [BsonElement("aggregateType")]
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Número de versión del evento dentro del agregado.
    /// Los eventos se ordenan por versión para reproducirlos en el orden correcto
    /// al reconstruir el estado (replay de eventos).
    /// </summary>
    [BsonElement("version")]
    public int Version { get; set; }

    /// <summary>
    /// Nombre del tipo concreto del evento almacenado (ej: "TicketCreatedEvent").
    /// Se usa como discriminador para deserializar correctamente EventData al tipo correcto.
    /// </summary>
    [BsonElement("eventType")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Payload (carga útil) del evento: los datos reales del evento de dominio.
    /// Al ser de tipo BaseEvent (clase abstracta), el driver de MongoDB usa el
    /// class map registrado (BsonClassMap) para deserializar al tipo concreto correcto.
    /// </summary>
    public BaseEvent? EventData { get; set; }
  }
}