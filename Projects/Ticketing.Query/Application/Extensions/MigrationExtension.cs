using Microsoft.EntityFrameworkCore;
using Tiketing.Query.Infrastructure.Persistence;

namespace Tiketing.Query.Application.Extensions
{
  // Método de extensión para ejecutar automáticamente las migraciones de EF Core
  // al arrancar el microservicio. Evita tener que correr 'dotnet ef database update' manualmente.
  public static class MigrationExtension
  {
    public static async Task ApplyMigration(this IApplicationBuilder app)
    {
      using (var scope = app.ApplicationServices.CreateScope())
      {
        var service = scope.ServiceProvider;
        var loggerFactory = service.GetRequiredService<ILoggerFactory>();
        try
        {
          var contextFactory = service.GetRequiredService<DatabaseContextFactory>();
          using TicketDbContext dbContext = contextFactory.CreateDbContext();
          await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
          var logger = loggerFactory.CreateLogger<Program>();
          logger.LogError(ex, "error en la migracion");
        }
      }
    }
  }
}
