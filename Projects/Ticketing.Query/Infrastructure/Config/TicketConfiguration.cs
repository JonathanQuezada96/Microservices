using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tiketing.Query.Domain.Tickets;
using Tiketing.Query.Domain.TicketTypes;

namespace Tiketing.Query.Infrastructure.Config
{
  public class TicketEmployeeConfiguration : IEntityTypeConfiguration<TicketEmployee>
  {
    public void Configure(EntityTypeBuilder<TicketEmployee> builder)
    {
      builder.ToTable("ticket_employess");
    }
  }

  public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
  {
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
      builder.ToTable("tickects"); // Mapea la tabla
      builder.HasKey(x => x.Id);
      
      // Conversión de Valor (Value Conversion): 
      // Al guardar en BD guarda el 'Id' (int) del enum, pero al leer de la BD 
      // invoca 'TicketType.Create' para reconstruir el objeto Smart Enum en memoria.
      builder.Property(x => x.TicketType).HasConversion(ticketType => ticketType!.Id, value => TicketType.Create(value));
      // Configuración explicita de relación Muchos a Muchos usando la entidad intermedia TicketEmployee.
      builder.HasMany(x => x.Employees).WithMany(x => x.Tickets).UsingEntity<TicketEmployee>(
        j => j.HasOne(p => p.Employee)
              .WithMany(p => p.TicketEmployees)
              .HasForeignKey(p => p.EmployedId),
        j => j.HasOne(p => p.Ticket)
              .WithMany(p => p.TicketEmployees)
              .HasForeignKey(p => p.TickedId),
        j =>
          {
            j.HasKey(t => new { t.TickedId, t.EmployedId });
          }
        );
    }
  }
}
