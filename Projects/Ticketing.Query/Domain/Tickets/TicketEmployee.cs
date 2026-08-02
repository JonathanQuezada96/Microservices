using Microsoft.EntityFrameworkCore;
using Tiketing.Query.Domain.Employees;

namespace Tiketing.Query.Domain.Tickets
{
  // Entidad intermedia para mapear la relación de Muchos a Muchos (N:M) 
  // entre Tickets y Empleados en Entity Framework Core.
  //
  // En bases de datos relacionales, una relación N:M requiere una tabla intermedia.
  // EF Core puede generar esta tabla automáticamente, pero al crear la entidad explícita
  // tenemos control total sobre la tabla y podemos agregar columnas adicionales si fuera necesario.
  public class TicketEmployee
  {
    // Propiedades de navegación: EF Core las usa para hacer JOIN entre tablas.
    // Son nullable porque al construir el objeto solo necesitamos los IDs.
    public virtual Ticket? Ticket { get; set; }
    public virtual Employee? Employee { get; set; }

    // Claves foráneas explícitas. EF Core las usa para configurar la relación en la BD.
    // Nota: "TickedId" y "EmployedId" tienen un typo (falta la 't' y la 'ee') — así están en la BD.
    public Guid TickedId { get; set;  }
    public Guid EmployedId { get; set; }

    // Constructor privado con parámetros: único punto de creación interna.
    // Solo se llama desde el factory method Create().
    private TicketEmployee(Guid ticketId, Guid employeeId)
    {
      TickedId = ticketId;
      EmployedId = employeeId;
    }

    // Constructor privado vacío requerido por EF Core para materializar registros de la BD.
    private TicketEmployee()
    {
      
    }

    // Factory Method: recibe las entidades completas pero solo usa sus IDs.
    // Esto garantiza que la relación siempre se crea con IDs válidos de entidades existentes.
    public static TicketEmployee Create(Ticket ticket, Employee employee)
    {
      return new TicketEmployee(ticket.Id, employee.Id);
    }
  }
}
