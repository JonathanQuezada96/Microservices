using Microsoft.EntityFrameworkCore;
using Tiketing.Query.Domain.Employees;
using Tiketing.Query.Domain.Tickets;

namespace Tiketing.Query.Infrastructure.Persistence
{
  // Contexto de base de datos principal para el lado de Query (PostgreSQL).
  // Es el puente entre nuestras entidades C# y las tablas físicas.
  public class TicketDbContext : DbContext
  {
    public TicketDbContext(DbContextOptions<TicketDbContext> options) : base(options)
    {
      
    }
    public virtual DbSet<Ticket> Tickets => Set<Ticket>();
    public virtual DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketDbContext).Assembly);
      base.OnModelCreating(modelBuilder);
    }
  }
}
