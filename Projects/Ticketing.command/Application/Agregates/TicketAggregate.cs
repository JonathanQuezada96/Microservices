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
    public TicketAggregate(TicketCreateCommand command)
    {
      var ticketCreatedEvent = new TicketCreatedEvent
    }
  }
}
