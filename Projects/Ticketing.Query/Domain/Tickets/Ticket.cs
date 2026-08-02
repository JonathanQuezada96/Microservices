using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Domain.Employees;
using Tiketing.Query.Domain.TicketTypes;

namespace Tiketing.Query.Domain.Tickets
{
  // Representación del Ticket en el modelo de lectura (Query Side).
  // A diferencia del Command Side (que usa Event Sourcing), aquí modelamos 
  // la estructura final ("Materialized View") lista para ser consultada rápidamente.
  // Hereda de Entity que provee el Id (Guid) único.
  public class Ticket : Entity
  {
    // Descripción del problema o detalle del ticket. Es nullable porque
    // puede llegar vacío desde el evento de Kafka en algunos casos.
    public string? Description { get; set; }

    // Propiedad de navegación al tipo de ticket (ej: error de sistema, error de usuario, etc.)
    // 'virtual' habilita el Lazy Loading de EF Core.
    public virtual TicketType? TicketType { get; set; }

    // Relaciones N:M con Employee — un ticket puede tener múltiples empleados asignados.
    public virtual ICollection<Employee> Employees { get; set; } = [];
    public virtual ICollection<TicketEmployee> TicketEmployees { get; set; } = [];

    // Constructor público vacío requerido por EF Core para materializar resultados de consultas.
    public Ticket()
    {

    }

    // Constructor privado — solo accesible desde el factory method Create().
    // Recibe el ID explicitamente (en lugar de generarlo) porque debe coincidir
    // con el ID del evento de Kafka que viene del Command Side.
    private Ticket(Guid id, TicketType? ticketType, string description) : base(id)
    {
      TicketType = ticketType;
      Description = description;
    }

    // Factory Method: crea un Ticket con el ID especificado (NO genera uno nuevo).
    // Esto es clave para mantener la consistencia entre Command Side (MongoDB) y Query Side (PostgreSQL):
    // ambos lados deben referenciar el mismo ticket con el mismo ID.
    public static Ticket Create(Guid id, TicketType? ticketType, string description)
    {
      return new Ticket(id, ticketType, description);
    }
  }

}
