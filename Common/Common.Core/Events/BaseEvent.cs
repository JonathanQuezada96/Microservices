
using Common.Core.Messages;

namespace Common.Core.Events
{
  /// <summary>
  /// Clase abstracta base para todos los eventos de dominio del sistema.
  ///
  /// En Event Sourcing, un "evento" representa algo que YA ocurrió en el sistema
  /// (por ejemplo: "TicketCreado", "UsuarioRegistrado"). Los eventos son inmutables
  /// y son la fuente de verdad del estado de la aplicación.
  ///
  /// Hereda de Message para obtener el ID único de MongoDB.
  /// </summary>
  public abstract class BaseEvent : Message
  {
    /// <summary>
    /// Constructor que recibe el nombre del tipo de evento concreto.
    /// Se almacena en la propiedad Type para poder distinguir el evento
    /// al leerlo de la base de datos (discriminador de tipo).
    /// </summary>
    protected BaseEvent(string type)
    {
      Type = type;
    }

    /// <summary>
    /// Versión del evento dentro de un agregado.
    /// Útil para mantener el orden cronológico de los eventos y para
    /// detectar conflictos de concurrencia (optimistic concurrency control).
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Nombre del tipo concreto de evento (ej: "TicketCreatedEvent").
    /// Sirve como discriminador al deserializar el evento desde MongoDB.
    /// </summary>
    public string Type { get; set; }
  }
}
