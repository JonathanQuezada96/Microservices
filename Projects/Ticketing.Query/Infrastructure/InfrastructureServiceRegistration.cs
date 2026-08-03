using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Domain.Employees;
using Tiketing.Query.Infrastructure.Consumer;
using Tiketing.Query.Infrastructure.Persistence;
using Tiketing.Query.Infrastructure.Persistence.Interceptors;
using Tiketing.Query.Infrastructure.Repos;

namespace Tiketing.Query.Infrastructure
{
  // Clase encargada de registrar todos los servicios e infraestructura (BD, Repositorios)
  // en el contenedor de Inyección de Dependencias (DI).
  public static class InfrastructureServiceRegistration
  {
    public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {

      services.AddSingleton<AuditEntitiesInterceptor>();

      // Leemos la cadena de conexión a PostgreSQL desde appsettings.json.
      // El operador ?? throw lanza una excepción inmediatamente si no está configurada,
      // evitando errores crípticos más adelante en tiempo de ejecución.
      var connectionString = configuration.GetConnectionString("PostgresConnectionString") 
                           ?? throw new ArgumentException(nameof(configuration));

      // Preparamos una acción de configuración del DbContext que:
      //   - UseLazyLoadingProxies(): carga las propiedades de navegación (Employees, Tickets) 
      //     automáticamente cuando se acceden por primera vez (lazy loading).
      //   - UseNpgsql(): usa el driver de PostgreSQL.
      //   - UseSnakeCaseNamingConvention(): convierte nombres de C# (PascalCase) a snake_case
      //     automáticamente en la base de datos (ej: FirstName → first_name).
      Action<DbContextOptionsBuilder> configureDbContext;
      configureDbContext = o => 
                           o.UseLazyLoadingProxies()
                           .UseNpgsql(connectionString)
                           .UseSnakeCaseNamingConvention()
                           .AddInterceptors(new AuditEntitiesInterceptor()
                           );

      // Registramos el DbContext principal para peticiones HTTP normales (Scoped = una instancia por request).
      // Nota: esta versión NO usa LazyLoading, la versión de arriba (configureDbContext) sí lo usa
      // porque se pasa a la DatabaseContextFactory que el Consumer de Kafka usa en segundo plano.
      //services.AddDbContext<TicketDbContext>(configureDbContext); // versión anterior con LazyLoading
      services.AddDbContext<TicketDbContext>(opt =>
      {
        opt.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
      });

      // DatabaseContextFactory es Singleton porque el ComsumerHostedService (background service)
      // la usa para crear instancias de DbContext fuera del ciclo de vida de una petición HTTP.
      services.AddSingleton<DatabaseContextFactory>(new DatabaseContextFactory(configureDbContext));

      // UnitOfWork es Scoped: una instancia por request, coordina la transacción de BD.
      services.AddScoped<IUnitOfWork, UnitOfWork>();

      // Registra el repositorio genérico. El typeof(IGenericRepository<>) con <> abierto
      // permite que .NET resuelva IGenericRepository<Ticket>, IGenericRepository<TicketEmployee>, etc.
      // automáticamente, todos usando la implementación GenericRepo<T>.
      services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepo<>));

      // Lee la sección "ConsumerConfig" del appsettings.json y la mapea a ConsumerConfig de Kafka.
      // Así ComsumerHostedService recibe la configuración de Kafka (bootstrap servers, group id, etc.)
      // automáticamente vía IOptions<ConsumerConfig>.
      services.Configure<ConsumerConfig>(configuration.GetSection(nameof(ConsumerConfig)));

      // Repositorio específico para Employee (extiende el genérico con GetByUsernameAsync).
      services.AddScoped<IemployeeRepository, EmployeeRepository>();

      // Registra el Consumer de Kafka como un servicio en segundo plano.
      // .NET lo iniciará automáticamente al arrancar la app llamando a StartAsync().
      services.AddHostedService<ComsumerHostedService>();

      // Registra el EventHandler que traduce eventos de Kafka en comandos MediatR.
      services.AddScoped<IEventHandler, Handlers.EventHandler>();

      // MEJORA: Health Checks — endpoints para monitorear la salud del servicio.
      // Son esenciales en entornos con Docker/Kubernetes: el orquestador llama a /health
      // periódicamente para saber si el contenedor está saludable.
      //
      // AddHealthChecks() habilita el sistema de health checks de .NET.
      // AddDbContextCheck<T>(): verifica que el DbContext puede conectarse a PostgreSQL
      //   ejecutando una query simple (SELECT 1). Si falla, el servicio se marca como "Unhealthy".
      services.AddHealthChecks()
        .AddDbContextCheck<TicketDbContext>(
          name: "postgresql",        // Nombre que aparecerá en la respuesta JSON del endpoint.
          failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
          tags: new[] { "database", "postgresql" } // Tags para filtrar checks por categoría.
        );

      return services;
    }
  }
}
