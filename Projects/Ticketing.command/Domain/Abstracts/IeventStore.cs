using Common.Core.Events;

namespace Ticketing.command.Domain.Abstracts
{
  public interface IeventStore
  {
    Task<List<BaseEvent>> GetEventsAsync(
      string aggregateId,
      CancellationToken cancellation
      );
    Task SaveEventsAsync(string aggregateId, IEnumerable<BaseEvent> events, int expectedVersion, CancellationToken cancellation);
  }
}
