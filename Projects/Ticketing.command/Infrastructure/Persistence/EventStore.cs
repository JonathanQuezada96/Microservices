using Common.Core.Events;
using Common.Core.Producers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Ticketing.command.Application.Models;
using Ticketing.command.Domain.Abstracts;
using Ticketing.command.Domain.EventModels;

namespace Ticketing.command.Infrastructure.Persistence
{
  public class EventStore : IeventStore
  {
    private readonly IEventModelRepository _eventModelRepository;
    private readonly KafkaSettings _kafkaSettings;
    private readonly IEventProducer _eventProducer;
    public EventStore(
                      IEventModelRepository eventModelRepository,
                      IOptions<KafkaSettings> kafkaSettings,
                      IEventProducer eventProducer)
    {
      _eventModelRepository = eventModelRepository;
      _kafkaSettings = kafkaSettings.Value;
      _eventProducer = eventProducer;
    }
    public async Task<List<BaseEvent>> GetEventsAsync(string aggregateId, CancellationToken cancellation)
    {
      var eventStream = await _eventModelRepository.FilterByAsync(doc => doc.AggregateIdentifier == aggregateId, cancellation);
      if (eventStream == null || !eventStream.Any())
      {
        throw new Exception("El aggregate no tiene eventos");
      }
      return eventStream.OrderBy(x => x.Version).Select(x => x.EventData).ToList()!;
    }

    // SaveEventsAsync: Guarda una lista de eventos validando concurrencia optimista.
    public async Task SaveEventsAsync(string aggregateId, IEnumerable<BaseEvent> events, int expectedVersion, CancellationToken cancellation)
    {
      var eventStream = await _eventModelRepository.FilterByAsync(doc => doc.AggregateIdentifier == aggregateId, cancellation);

      // Verificación de Concurrencia Optimista (Optimistic Concurrency):
      // Si la versión esperada (la versión con la que el usuario empezó a operar) es diferente a la última versión real
      // en la base de datos, significa que alguien más modificó el agregado en el medio. Lanzamos error.
      if (eventStream.Any() && expectedVersion != -1 && eventStream.Last().Version != expectedVersion)
      {
        throw new Exception("Error de concurrencia");
      }

      var version = expectedVersion;

      foreach (var @event in events)
      {
        version++;
        @event.Version = version;
        var evenType = @event.GetType().Name;

        // Empaqueta el evento de dominio en el modelo de base de datos (EventModel)
        var eventModel = new EventModel
        {
          TimeStamp = DateTime.UtcNow,
          AggregateIdentifier = aggregateId,
          Version = version,
          EventType = evenType,
          EventData = @event
        };
        await AddEventStore(eventModel, cancellation);
        var topic = _kafkaSettings.Topic ?? throw new Exception("No se encuentra el topic");
        await _eventProducer.ProduceAsync(topic, @event);
      }
    }

    // Inserta un EventModel asegurando atomicidad mediante transacciones de MongoDB.
    private async Task AddEventStore(EventModel eventModel, CancellationToken cancellation)
    {
      IClientSessionHandle session = await _eventModelRepository.BeginSessionAsync(cancellation);
      try
      {
        _eventModelRepository.BeginTransaction(session);
        await _eventModelRepository.InsertOneAsync(eventModel, session, cancellation);
        await _eventModelRepository.CommitTransactionAsync(session, cancellation);
      }
      catch (Exception)
      {
        await _eventModelRepository.RollbackTransactionAsync(session, cancellation);
        throw;
      }
      finally
      {
        // EXCELENTE PRÁCTICA: El bloque finally asegura que la sesión de base de datos 
        // siempre se libere, previniendo memory leaks de conexiones a MongoDB.
        _eventModelRepository.DisposeSession(session);
      }
    }
  }
}
