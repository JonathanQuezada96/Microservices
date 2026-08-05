// =========================================================================
// CLASE: InfrastructureServiceRegistration
// PROPÓSITO: Registra todos los servicios de infraestructura en el contenedor
// de Inyección de Dependencias (DI) de .NET.
//
// ¿Qué es la Inyección de Dependencias?
// Es un patrón donde las clases no crean sus dependencias directamente,
// sino que .NET las "inyecta" en el constructor automáticamente.
// Esto permite cambiar implementaciones sin tocar el código que las usa.
//
// ¿Qué es un "lifetime" en DI?
//   - Singleton: se crea UNA sola instancia para toda la vida de la app.
//   - Scoped: se crea una instancia por petición HTTP (o por scope manual).
//   - Transient: se crea una instancia nueva cada vez que se pide.
//
// Este método de extensión se llama desde Program.cs:
//   builder.Services.RegisterInfrastructureServices(builder.Configuration);
// =========================================================================
using Common.Core.Consumers;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using System;
using Ticketing.Query.Infrastructure.Consumers;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Domain.Employees;
using Tiketing.Query.Infrastructure.Persistence;
using Tiketing.Query.Infrastructure.Persistence.Interceptors;
using Tiketing.Query.Infrastructure.Repos;

namespace Ticketing.Query.Infrastructure;

public static class InfrastructureServiceRegistration
{
  // Método de extensión sobre IServiceCollection.
  // 'this IServiceCollection services' permite llamarlo como:
  //   services.RegisterInfrastructureServices(configuration)
  public static IServiceCollection RegisterInfrastructureServices(
    this IServiceCollection services,
    IConfiguration configuration
  )
  {
    // =========================================================================
    // INTERCEPTOR DE AUDITORÍA
    // =========================================================================
    // AuditEntitiesInterceptor es un interceptor de EF Core que se ejecuta
    // automáticamente ANTES de cada SaveChanges.
    // Rellena campos como CreatedOn, CreatedBy, LastModificateOn en las entidades.
    // Es Singleton porque no tiene estado que cambie por request.
    services.AddSingleton<AuditEntitiesInterceptor>();

    // Leemos la cadena de conexión a PostgreSQL desde appsettings.json.
    // ?? throw: si no existe la configuración, fallamos rápido con un error claro.
    var connectionString = configuration
                       .GetConnectionString("PostgresConnectionString")
                       ?? throw new ArgumentException("PostgresConnectionString no está configurado.");

    // =========================================================================
    // DbContext Factory — para el UnitOfWork en contextos fuera de HTTP
    // =========================================================================
    // Esta configuración define CÓMO se construirá el DbContext.
    // UseLazyLoadingProxies(): activa la carga perezosa de propiedades 'virtual'.
    // UseNpgsql(): usa el driver de PostgreSQL.
    // UseSnakeCaseNamingConvention(): convierte PascalCase a snake_case automáticamente.
    //   Ej: "TicketType" → columna "ticket_type" en PostgreSQL.
    Action<DbContextOptionsBuilder> configureDbContext = o => o
      .UseLazyLoadingProxies()
      .UseNpgsql(connectionString)
      .UseSnakeCaseNamingConvention()
      .AddInterceptors(new AuditEntitiesInterceptor());

    // =========================================================================
    // REGISTRO DEL DbContext (EF Core)
    // =========================================================================
    // AddDbContext registra el TicketDbContext como Scoped (una instancia por request HTTP).
    // NOTA: La configuración aquí es simplificada (sin LazyLoading) para el contexto normal.
    // El DatabaseContextFactory (abajo) usa la configuración completa para el consumer de Kafka.
    services.AddDbContext<TicketDbContext>(opt =>
    {
      opt.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
    });

    // DatabaseContextFactory: permite crear instancias del DbContext fuera del ciclo
    // de vida normal de HTTP (por ejemplo, en el ConsumerHostedService de Kafka).
    // Es Singleton porque la factory en sí no tiene estado que cambie.
    services.AddSingleton<DatabaseContextFactory>(
          new DatabaseContextFactory(configureDbContext)
    );

    // =========================================================================
    // REPOSITORIOS Y UNIT OF WORK
    // =========================================================================
    // UnitOfWork: Scoped (una instancia por request). Coordina los repositorios
    // y asegura que todos los cambios se confirmen en una sola transacción.
    services.AddScoped<IUnitOfWork, UnitOfWork>();

    // GenericRepo<>: repositorio genérico para CRUD básico.
    // typeof(IGenericRepository<>) registra el tipo genérico abierto,
    // lo que permite que .NET resuelva IGenericRepository<Ticket>,
    // IGenericRepository<TicketEmployee>, etc. automáticamente.
    services.AddScoped(
      typeof(IGenericRepository<>),
      typeof(GenericRepo<>)
    );

    // EmployeeRepository: repositorio específico de empleados con métodos propios
    // (como GetByUsernameAsync) que el repositorio genérico no tiene.
    services.AddScoped<IemployeeRepository, EmployeeRepository>();

    // =========================================================================
    // KAFKA CONSUMER (Comunicación entre microservicios)
    // =========================================================================
    // EventConsumer: Scoped — se crea un nuevo consumer por scope (por request del ConsumerHostedService).
    services.AddScoped<IEventConsumer, EventConsumer>();

    // ConsumerHostedService: el background service que arranca el consumer de Kafka.
    // AddHostedService lo registra para que .NET lo inicie automáticamente al arrancar la app.
    services.AddHostedService<ConsumerHostedService>();

    // EventHandler: Scoped — une el evento de Kafka con MediatR para procesarlo.
    services.AddScoped<IEventHandler, Tiketing.Query.Infrastructure.Handlers.EventHandler>();

    // =========================================================================
    // CONFIGURACIÓN DE KAFKA
    // =========================================================================
    // Lee la sección "ConsumerConfig" de appsettings.json y la mapea a
    // la clase ConsumerConfig de Confluent.Kafka (GroupId, BootstrapServers, etc.)
    services.Configure<ConsumerConfig>(
      configuration.GetSection(nameof(ConsumerConfig))
    );

    return services;
  }
}

