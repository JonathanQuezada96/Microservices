using System.Diagnostics.CodeAnalysis;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Domain.Addresses;
using Tiketing.Query.Domain.Tickets;

namespace Tiketing.Query.Domain.Employees
{
  // Entidad de Lectura que representa a un empleado en PostgreSQL.
  // Hereda de Entity, que provee el campo Id (Guid) común a todas las entidades.
  public class Employee : Entity
  {
    // 'required' (C# 11+) obliga a que esta propiedad sea inicializada al crear el objeto.
    // Garantiza que nunca tengamos un Employee con FirstName nulo.
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }    // Funciona como identificador único (username)
    public required Address Address { get; set; } // Dirección del empleado (objeto de valor)

    // Propiedades de navegación de EF Core para la relación N:M con Ticket.
    // 'virtual' es necesario para que EF Core pueda aplicar Lazy Loading.
    // Se inicializan como colecciones vacías [] para evitar NullReferenceException.
    public virtual ICollection<Ticket> Tickets { get; set; } = [];
    public virtual ICollection<TicketEmployee> TicketEmployees { get; set; } = [];

    // Constructor público vacío requerido por Entity Framework Core para poder
    // instanciar la entidad cuando hace consultas a la base de datos (materialización).
    // Sin él, EF Core lanzaría una excepción al intentar crear instancias desde el resultado SQL.
    public Employee()
    {

    }

    // Constructor privado que fuerza el uso del factory method Create().
    // [SetsRequiredMembers] le dice al compilador que este constructor inicializa
    // todas las propiedades 'required', evitando advertencias de compilación.
    // Es privado porque no queremos que nadie cree Employees directamente — solo vía Create().
    [SetsRequiredMembers]
    private Employee(Guid id, string firstName, string lastName, string email, Address address) : base(id)
    {
      FirstName = firstName;
      LastName = lastName;
      Email = email;
      Address = address;
    }

    // Factory Method estático: único punto de entrada para crear un Employee.
    // Genera un nuevo Guid automáticamente para garantizar que cada Employee tenga
    // un ID único antes de ser persistido en la base de datos.
    public static Employee Create(string firstName, string lastName, string email, Address address)
    {
      var id = Guid.NewGuid();
      return new Employee(id, firstName, lastName, email, address);
    }
  }
}
