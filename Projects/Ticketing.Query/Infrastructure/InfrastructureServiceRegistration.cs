using Microsoft.EntityFrameworkCore;
using Tiketing.Query.Infrastructure.Persistence;

namespace Tiketing.Query.Infrastructure
{
  // Clase encargada de registrar todos los servicios e infraestructura (BD, Repositorios)
  // en el contenedor de Inyección de Dependencias (DI).
  public static class InfrastructureServiceRegistration
  {
    public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
      Action<DbContextOptionsBuilder> configureDbContext;
      var connectionString = configuration.GetConnectionString("PostgresConnectionString") 
                           ?? throw new ArgumentException(nameof(configuration));
      configureDbContext = o => 
                           o.UseLazyLoadingProxies().UseNpgsql(connectionString)
                            .UseSnakeCaseNamingConvention();
      //services.AddDbContext<TicketDbContext>(configureDbContext);

      services.AddDbContext<TicketDbContext>(opt =>
      {
      opt.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
      });

      services.AddSingleton<DatabaseContextFactory>(new DatabaseContextFactory(configureDbContext));


      return services;
    }
  }
}
