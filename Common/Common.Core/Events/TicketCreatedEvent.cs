
namespace Common.Core.Events
{
  /// <summary>
  /// Evento de dominio que representa la creación de un ticket de soporte/error en el sistema.
  ///
  /// Este evento se publica (y almacena) cuando un usuario reporta un problema.
  /// Al heredar de BaseEvent, lleva automáticamente el tipo de evento ("TicketCreatedEvent")
  /// como discriminador para la deserialización polimórfica en MongoDB.
  ///
  /// Patrón Event Sourcing: este evento es la "fuente de verdad" del hecho de que
  /// se creó un ticket — el estado de la aplicación se puede reconstruir
  /// reproduciéndolo junto con otros eventos.
  /// </summary>
  public class TicketCreatedEvent : BaseEvent
  {
    /// <summary>
    /// Pasa "TicketCreatedEvent" como tipo al constructor de BaseEvent.
    /// nameof() evita errores de typo y facilita refactorizaciones.
    /// </summary>
    public TicketCreatedEvent() : base(nameof(TicketCreatedEvent))
    {
    }

    /// <summary>
    /// Nombre de usuario que creó el ticket.
    /// 'required' obliga a que se asigne antes de usar el objeto (C# 11+).
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Categoría o tipo del error (opcional). Ej: "NullReferenceException", "Timeout", etc.
    /// El símbolo ? indica que puede ser null si el usuario no lo especificó.
    /// </summary>
    public string? TypeError { get; set; }

    /// <summary>
    /// Descripción detallada del error. Campo obligatorio ('required').
    /// </summary>
    public required string DetailError { get; set; }
  }
}
