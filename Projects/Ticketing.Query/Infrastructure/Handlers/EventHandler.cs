using Common.Core.Events;
using MediatR;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Features.Tickets.Commands;
using static Tiketing.Query.Features.Tickets.Commands.TicketCreate;
using static Tiketing.Query.Features.Tickets.Commands.TicketUpdate;

namespace Tiketing.Query.Infrastructure.Handlers
{
  // Implementación concreta del IEventHandler.
  //
  // Este es el punto de enlace entre el Consumer de Kafka y la lógica de negocio del Query Side.
  // Flujo completo:
  //   Kafka → ComsumerHostedService → (deserializa JSON) → EventHandler.On(evento) → MediatR → Handler → PostgreSQL
  //
  // Usamos MediatR aquí para mantener la separación: el EventHandler no sabe nada de
  // cómo se guarda en base de datos; solo traduce el evento en un comando y lo despacha.
  public class EventHandler : IEventHandler
  {
    // IMediator es el "bus" de MediatR — le pasamos el comando y él encuentra el Handler correcto.
    private readonly IMediator _mediator;
    public EventHandler(IMediator mediator)
    {
      _mediator = mediator;
    }

    // Se invoca cuando llega un TicketCreatedEvent de Kafka.
    // Extrae los datos relevantes del evento y los empaqueta en un TicketCreateCommand
    // que MediatR despachará al TicketCreateCommandHandler para persistir en PostgreSQL.
    public async Task On(TicketCreatedEvent @event)
    {
      // Construimos el comando con los datos que vienen en el evento.
      // El prefijo @ en @event es necesario porque "event" es palabra reservada en C#.
      var command = new TicketCreateCommand(
          @event.ID,          // ID del ticket (generado en el Command Side)
          @event.Username,    // Email del empleado que creó el ticket
          @event.TypeError,   // Tipo de error (número del 1 al 5)
          @event.DetailError  // Descripción detallada del problema
        );
      await _mediator.Send(command);
    }

    // Se invoca cuando llega un TicketUpdatedEvent de Kafka.
    // Aún no está implementado — lanzar NotImplementedException es una práctica común
    // como recordatorio de que esta funcionalidad está pendiente.
    public async Task On(TicketUpdatedEvent @event)
    {
      var ticketUpdateEvent = new TicketUpdateCommand(
        @event.ID,
        @event.TicketType,
        @event.Description!,
        @event.Username!
      );
      await _mediator.Send(ticketUpdateEvent);

    }

    // =========================================================================
    // MÉTODO: On(TicketDeletedEvent)
    // =========================================================================
    // Este método se invoca automáticamente cuando nuestro Consumer de Kafka 
    // recibe un evento de tipo "TicketDeletedEvent" proveniente del Command Side.
    public async Task On(TicketDeletedEvent @event)
    {
      // 1. Extraemos los datos del evento (ID y Username) y los metemos en un Comando.
      // El signo de exclamación (!) al final de Username es para decirle al 
      // compilador: "Confía en mí, sé que esto no será nulo aquí".
      var ticketDeleteEvent = new TicketDelete.TicketDeleteCommand(
        @event.ID,
        @event.Username!
      );
      
      // 2. Usamos MediatR para enviar este comando. MediatR buscará la clase 
      // TicketDeleteCommandHandler y le pasará este comando para que borre el 
      // ticket de PostgreSQL.
      await _mediator.Send(ticketDeleteEvent);
    }
  }
}
