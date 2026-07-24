using Microsoft.EntityFrameworkCore;

namespace Tiketing.Query.Infrastructure.Persistence
{
  // Fábrica para instanciar el TicketDbContext manualmente.
  // Muy útil para inyectarlo en Background Services (como los Consumers de Kafka) 
  // o para aplicar migraciones al inicio.
  public class DatabaseContextFactory
  {
    private readonly Action<DbContextOptionsBuilder> _configureDbContext;
    public DatabaseContextFactory(Action<DbContextOptionsBuilder> configureDbContext)
    {
      _configureDbContext = configureDbContext;
    }
    public TicketDbContext CreateDbContext()
    {
      DbContextOptionsBuilder<TicketDbContext> optionsBuilder = new();
      _configureDbContext(optionsBuilder);
      return new TicketDbContext(optionsBuilder.Options);
    }
  }
}
