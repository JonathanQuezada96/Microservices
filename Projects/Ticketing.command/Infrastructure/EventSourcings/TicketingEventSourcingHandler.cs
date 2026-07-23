using Ticketing.command.Application.Agregates;
using Ticketing.command.Domain.Abstracts;

namespace Ticketing.command.Infrastructure.EventSourcings
{
  public class TicketingEventSourcingHandler : IEventSourcingHandler<TicketAggregate>
  {
    private readonly IeventStore _eventStore;
    public TicketingEventSourcingHandler(IeventStore evenStore)
    {
      _eventStore = evenStore;
    }
    // GetByIdAsync: Rehidrata el agregado reconstruyendo su estado a partir de los eventos pasados.
    public async Task<TicketAggregate> GetByIdAsync(string aggregateId, CancellationToken cancellationToken)
    {
      var aggregate = new TicketAggregate();
      var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
      
      if (events is null || !events.Any())
      {
        return aggregate; // Retorna un agregado vacío si no hay eventos
      }
      
      // ReplayEvents aplica todos los eventos históricos en orden para reconstruir el estado actual.
      aggregate.ReplayEvents(events);
      // Setea la versión actual del agregado a la del último evento aplicado.
      aggregate.Version = events.Select(x => x.Version).Max();
      return aggregate;
    }

    // SaveAsync: Toma los cambios (nuevos eventos) del agregado y los persiste en la base de datos.
    public async Task SaveAsync(AggregateRoot aggregateRoot, CancellationToken cancellationToken)
    {
      // Guarda solo los eventos no confirmados en el EventStore
      await _eventStore.SaveEventsAsync(aggregateRoot.Id, aggregateRoot.GetUncommitedChanges(), aggregateRoot.Version, cancellationToken);
      // Una vez guardados, limpia la lista de cambios pendientes del agregado.
      aggregateRoot.MarkChangesAsCommited();
    }
  }
}
