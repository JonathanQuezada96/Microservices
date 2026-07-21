using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Ticketing.command.Application.Models;
using Ticketing.command.Domain.EventModels;

namespace Ticketing.command.Infrastructure.Repositories
{
  /// <summary>
  /// Implementación concreta del repositorio del Event Store para la colección "eventStores".
  ///
  /// Hereda de MongoRepository&lt;EventModel&gt; para obtener todas las operaciones genéricas
  /// (CRUD, sesiones, transacciones) sin repetir código.
  ///
  /// Implementa IEventModelRepository para que el contenedor de DI pueda resolver
  /// esta clase cuando alguien inyecte IEventModelRepository en su constructor.
  ///
  /// En este punto el constructor simplemente delega al constructor base,
  /// pasando el cliente de MongoDB y la configuración. Aquí se podrían agregar
  /// inicializaciones específicas del Event Store en el futuro.
  /// </summary>
  public class EventModelRepository : MongoRepository<EventModel>, IEventModelRepository
  {
    public EventModelRepository(IMongoClient mongoClient, IOptions<MongoSettings> options) : base(mongoClient, options)
    {
      // El constructor base se encarga de inicializar la colección "eventStores"
      // usando el atributo [BsonCollection("eventStores")] de EventModel.
    }
  }
}
