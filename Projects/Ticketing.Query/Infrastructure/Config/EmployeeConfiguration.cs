using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tiketing.Query.Domain.Employees;

namespace Tiketing.Query.Infrastructure.Config
{
  public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
  {
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
      builder.ToTable("employess");
      builder.HasKey(x => x.Id);
      
      // OwnsOne mapea el Value Object (Address). EF Core no creará una tabla 'Address', 
      // sino que agregará sus campos (Street, City, Country) como columnas en la tabla 'employess'.
      builder.OwnsOne(x => x.Address);
    }
  }
}
