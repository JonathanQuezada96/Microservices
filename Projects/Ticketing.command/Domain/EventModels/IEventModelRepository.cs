using Ticketing.command.Domain.Abstracts;

namespace Ticketing.command.Domain.EventModels
{
  /// <summary>
  /// Interfaz específica del repositorio para la colección de eventos (Event Store).
  ///
  /// Al heredar de IMongoRepository&lt;EventModel&gt;, obtiene automáticamente:
  ///   - Los métodos de sesión/transacción (BeginSessionAsync, CommitTransaction, etc.)
  ///   - Los métodos CRUD genéricos (InsertOneAsync, AsQueryable)
  ///
  /// Se puede extender aquí con métodos de consulta propios del Event Store,
  /// por ejemplo: GetByAggregateIdAsync(string aggregateId).
  ///
  /// Esta interfaz se registra en el contenedor de DI para que el resto de
  /// la aplicación la consuma sin conocer la implementación concreta.
  /// </summary>
  public interface IEventModelRepository : IMongoRepository<EventModel>
  {
  }
}
