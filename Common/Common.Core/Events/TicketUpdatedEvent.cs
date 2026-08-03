using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Core.Events
{
  // Evento de dominio que representa la ACTUALIZACIÓN de un ticket existente.
  //
  // En Event Sourcing, cada cambio de estado se modela como un evento inmutable.
  // Este evento es publicado por el Command Side (Ticketing.command) a través de Kafka,
  // y consumido por el Query Side (Ticketing.Query) para actualizar su base de datos de lectura.
  //
  // Hereda de BaseEvent, que aporta propiedades comunes como el campo "Type"
  // que se usa para identificar el tipo de evento durante la deserialización en Kafka.
  public class TicketUpdatedEvent : BaseEvent
  {
    // Constructor por defecto que registra el nombre del tipo de evento,
    // mantiene la consistencia con TicketCreatedEvent y permite
    // la inicialización mediante object initializers.
    public TicketUpdatedEvent() : base(nameof(TicketUpdatedEvent))
    {
    }

    // El constructor recibe el tipo de evento (ej: "TicketUpdatedEvent")
    // y lo pasa a la clase base para que quede registrado en el campo "Type".
    public TicketUpdatedEvent(string type) : base(type)
    {
    }

    // Tipo de ticket actualizado (ej: 1, 2, 3, 4, 5)
    public int? TicketType { get; set; }

    // Descripción o detalle actualizado del ticket
    public string? Description { get; set; }

    // Email/Username del empleado relacionado con esta actualización
    public string? Username { get; set; }

  }
}
