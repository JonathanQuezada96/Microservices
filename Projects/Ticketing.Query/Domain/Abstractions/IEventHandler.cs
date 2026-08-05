using Common.Core.Events;

namespace Tiketing.Query.Domain.Abstractions
{
  // Contrato (interfaz) que define cómo el Query Side reacciona ante los eventos de dominio.
  //
  // Cada método "On" representa el manejador para un tipo específico de evento.
  // Cuando llega un mensaje de Kafka, el ComsumerHostedService usa reflexión (GetType + GetMethod)
  // para encontrar y llamar el método "On" correcto según el tipo de evento deserializado.
  //
  // Este diseño sigue el patrón "Event Handler" y permite extensión fácil:
  // solo añade un nuevo método On(NuevoEvento @event) para soportar otro tipo de evento.
  public interface IEventHandler
  {
    // Se ejecuta cuando llega un TicketCreatedEvent desde Kafka.
    // Responsable de persistir el nuevo ticket en la base de datos de lectura (PostgreSQL).
    Task On(TicketCreatedEvent @event);

    // Se ejecuta cuando llega un TicketUpdatedEvent desde Kafka.
    // Responsable de actualizar el ticket existente en la base de datos de lectura.
    Task On(TicketUpdatedEvent @event);

    // Se ejecuta cuando llega un TicketDeletedEvent desde Kafka.
    // Responsable de eliminar el ticket en la base de datos de lectura.
    Task On(TicketDeletedEvent @event);
  }
}
