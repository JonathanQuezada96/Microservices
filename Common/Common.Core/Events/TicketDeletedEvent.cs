using System;

namespace Common.Core.Events
{
  // =========================================================================
  // CLASE: TicketDeletedEvent
  // PROPÓSITO: Este es un "Evento de Dominio". En la arquitectura Event Sourcing, 
  // los eventos representan cosas que ya sucedieron en el pasado y no pueden cambiar.
  // 
  // En este caso, este evento significa: "Un ticket ha sido eliminado o desactivado".
  // 
  // ¿Por qué hereda de BaseEvent?
  // Porque todos los eventos en nuestro sistema comparten ciertas propiedades 
  // comunes (como un ID y una Versión). Al heredar, obtenemos eso gratis.
  // =========================================================================
  public class TicketDeletedEvent : BaseEvent
  {
    // Constructor vacío por defecto.
    // Llama al constructor de la clase base pasándole el nombre de esta misma clase 
    // ("TicketDeletedEvent"). Esto ayuda al sistema a saber qué tipo de evento es 
    // cuando lo leemos de la base de datos (MongoDB) o de Kafka.
    public TicketDeletedEvent() : base(nameof(TicketDeletedEvent))
    {
    }

    // Constructor alternativo que permite especificar el nombre del tipo manualmente,
    // en caso de que sea necesario por alguna herramienta de deserialización.
    public TicketDeletedEvent(string type) : base(type)
    {
    }

    // Propiedad que guarda el Email o Username de la persona que decidió eliminar este ticket.
    // Es útil para mantener un "historial de auditoría" (saber quién hizo qué).
    // El símbolo '?' indica que puede ser un valor nulo si no se proporciona.
    public string? Username { get; set; }
  }
}
