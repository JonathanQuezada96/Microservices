using Microsoft.EntityFrameworkCore;
using Tiketing.Query.Domain.Employees;

namespace Tiketing.Query.Domain.Tickets
{
  // Entidad intermedia para mapear la relación de Muchos a Muchos (N:M) 
  // entre Tickets y Empleados en Entity Framework Core.
  // [Keyless] se declara aquí, pero la verdadera llave compuesta se define vía Fluent API.
  [Keyless]
  public class TicketEmployee
  {
    public virtual Ticket? Ticket { get; set; }
    public virtual Employee? Employee { get; set; }
    public Guid TickedId { get; set;  }
    public Guid EmployedId { get; set; }
  }
}
