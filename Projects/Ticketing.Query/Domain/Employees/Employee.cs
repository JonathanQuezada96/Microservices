using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Domain.Addresses;
using Tiketing.Query.Domain.Tickets;

namespace Tiketing.Query.Domain.Employees
{
  // Entidad de Lectura que representa a un empleado en PostgreSQL.
  public class Employee : Entity
  {
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email {  get; set; }
    public required Address Address { get; set; }
    public virtual ICollection<Ticket> Tickets { get; set; } = [];
    public virtual ICollection<TicketEmployee> TicketEmployees { get; set; } = [];
  }
}
