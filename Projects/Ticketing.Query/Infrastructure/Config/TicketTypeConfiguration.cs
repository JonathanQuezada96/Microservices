using Common.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tiketing.Query.Domain.TicketTypes;

namespace Tiketing.Query.Infrastructure.Config
{
  public class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
  {
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
      builder.ToTable("ticket_type");
      builder.HasKey(x => x.Id);
      
      // Data Seeding: Extraemos todos los valores del enum 'TicketTypeEnum', los convertimos a 
      // la clase 'TicketType' y le decimos a EF Core que los inserte automáticamente (HasData) en la tabla catálogo al migrar.
      IEnumerable<TicketType> ticketTypes = Enum
                              .GetValues<TicketTypeEnum>()
                              .Select(p => TicketType.Create((int)p)
                              );
      builder.HasData(ticketTypes);
    }
  }
}
