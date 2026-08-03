using Common.Core.Events;
using Common.Core.Producers;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Ticketing.command.Application.Agregates;
using Ticketing.command.Domain.Abstracts;
using Ticketing.command.Domain.EventModels;
using Ticketing.command.Infrastructure.EventSourcings;
using Ticketing.command.Infrastructure.Persistence;
using Ticketing.command.Infrastructure.Repositories;

namespace Ticketing.command.Infrastructure
{
  /// <summary>
  /// Clase estática que centraliza el registro de todos los servicios de la capa de Infraestructura
  /// en el contenedor de Inyección de Dependencias (DI) de ASP.NET Core.
  ///
  /// El patrón "Extension Method sobre IServiceCollection" permite organizar los registros
  /// por capa (Application, Infrastructure, etc.) y mantener el Program.cs limpio:
  ///   builder.Services.AddInfrastructureServices(configuration);
  /// </summary>
  public static class InfrastructureServiceRegistration
  {
    /// <summary>
    /// Método de extensión que registra todos los servicios de infraestructura.
    /// Se llama desde Program.cs pasando la configuración de la aplicación.
    /// Devuelve IServiceCollection para permitir el encadenamiento fluido (method chaining).
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
      // --- REGISTRO DE TIPOS BSON (POLIMORFISMO CON MONGODB) ---
      // BsonClassMap.RegisterClassMap le dice al driver de MongoDB cómo serializar/deserializar
      // las clases concretas que heredan de BaseEvent.
      // Esto es OBLIGATORIO para que MongoDB pueda reconstruir el tipo correcto al leer EventData,
      // ya que BaseEvent es abstracta y no se puede instanciar directamente.
      BsonClassMap.RegisterClassMap<BaseEvent>();
      BsonClassMap.RegisterClassMap<TicketCreatedEvent>(); // Registrar cada evento concreto aquí
      BsonClassMap.RegisterClassMap<TicketUpdatedEvent>(); // Registrar cada evento concreto aquí

      // --- REGISTRO DEL REPOSITORIO GENÉRICO ---
      // Scoped: se crea UNA instancia por petición HTTP (request).
      // typeof(IMongoRepository<>) → registro de tipo abierto (open generic),
      // permite que DI resuelva IMongoRepository<CualquierTipo> automáticamente.
      services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));

      // --- REGISTRO DEL REPOSITORIO ESPECÍFICO DEL EVENT STORE ---
      // Transient: se crea una nueva instancia cada vez que se inyecta.
      // IEventModelRepository → EventModelRepository (inyección por interfaz, no por implementación).
      services.AddTransient<IEventModelRepository, EventModelRepository>();

      // --- REGISTRO DEL CLIENTE DE MONGODB ---
      // Singleton: se crea UNA sola instancia para toda la vida de la aplicación.
      // IMongoClient es thread-safe y costoso de crear, por eso es Singleton.
      // La cadena de conexión viene de "ConnectionStrings:MongoDb" en appsettings.json.
      services.AddSingleton<IMongoClient, MongoClient>(sp => new MongoClient(configuration.GetConnectionString("MongoDb")));

      services.AddTransient<IeventStore, EventStore>();
      services.AddTransient<IEventSourcingHandler<TicketAggregate>, TicketingEventSourcingHandler>();
      services.AddScoped<IEventProducer, TicketEventProducer>();

      return services;
    }
  }
}
