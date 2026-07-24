using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Domain.Employees;
using Tiketing.Query.Domain.TicketTypes;

namespace Tiketing.Query.Domain.Tickets
{
  // Representación del Ticket en el modelo de lectura (Query Side).
  // A diferencia del Command Side (que usa Event Sourcing), aquí modelamos 
  // la estructura final ("Materialized View") lista para ser consultada rápidamente.
  public class Ticket : Entity
  {
    public string? Description { get; set; }
    public virtual TicketType? TicketType { get; set; }
    public virtual ICollection<Employee> Employees { get; set; } = [];
    public virtual ICollection<TicketEmployee> TicketEmployees { get; set; } = [];
  }
}
