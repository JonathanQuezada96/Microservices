using Common.Core.Events;
using Ticketing.command.Domain.Abstracts;
using Ticketing.command.Features.Tickets;

namespace Ticketing.command.Application.Agregates
{
  // TicketAggregate hereda de AggregateRoot. 
  // En Domain-Driven Design (DDD), un "Agregado" es un conjunto de objetos de dominio que se tratan como una sola unidad.
  // Un "Aggregate Root" (Raíz del Agregado) es la única clase que permite interacciones con el resto del agregado, 
  // garantizando la consistencia interna. En este caso, esta clase representa el estado y el ciclo de vida completo de un Ticket.
  public class TicketAggregate : AggregateRoot
  {
    // Constructor sin parámetros requerido para rehidratar el agregado desde los eventos (Reflection).
    public TicketAggregate()
    {
      
    }
    
    // Estado interno del Agregado
    public bool Active { get; set; } 
    
    // Constructor principal que recibe el comando y genera el primer evento del ciclo de vida.
    // Aquí es donde residiría la lógica de negocio y las validaciones de dominio iniciales.
    public TicketAggregate(TicketCreateCommand command)
    {
      var ticketCreatedEvent = new TicketCreatedEvent
      {
        ID = command.Id,
        Username = command.ticketCreateRequest.Username,
        TypeError = command.ticketCreateRequest.TypeError,
        DetailError = command.ticketCreateRequest.DetailError,
      };
      // RaiseEvent añade el evento a la lista de cambios sin confirmar y llama al método Apply()
      RaiseEvent(ticketCreatedEvent);
    }
    
    // Método Apply: muta el estado del Agregado basándose EXCLUSIVAMENTE en los eventos.
    // Nunca se debe mutar el estado directamente desde afuera, siempre a través de un Apply de un evento.
    public void Apply(TicketCreatedEvent @event)
    {
      _id = @event.ID;
      Active = true;
    }
  }
}
