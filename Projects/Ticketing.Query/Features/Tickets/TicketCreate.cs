using MediatR;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Domain.Employees;
using Tiketing.Query.Domain.Tickets;
using Tiketing.Query.Domain.TicketTypes;

// Este archivo vive en el Query Side, pero aplica también el patrón Vertical Slice:
// todo lo relacionado a "crear un ticket en la base de datos de lectura" está aquí.
//
// Importante: Este "crear" no viene de una petición HTTP directa del usuario,
// sino que es disparado por el EventHandler cuando llega un TicketCreatedEvent de Kafka.
// El Query Side "replica" el ticket en PostgreSQL para poder servirlo rápido en consultas.

namespace Tiketing.Query.Features.Tickets
{
  public sealed class TicketCreate
  {
    // Comando CQRS que encapsula los datos del evento recibido desde Kafka.
    // Es un record (inmutable por diseño) — perfecto para comandos que no deben cambiar.
    // IRequest<string> indica a MediatR que este comando devuelve un string como resultado (el ID).
    public record TicketCreateCommand(string Id, string Username, int TicketType, string DetailError) : IRequest<string>;

    // Handler del comando — aquí está la lógica real de persistencia en PostgreSQL.
    // MediatR lo detecta automáticamente y lo invoca cuando alguien llama mediator.Send(command).
    public class TicketCreateCommandHandler : IRequestHandler<TicketCreateCommand, string>
    {
      // El UnitOfWork agrupa todos los repositorios y coordina la transacción de base de datos.
      private readonly IUnitOfWork _unitOfWork;
      public TicketCreateCommandHandler(IUnitOfWork unitOfWork)
      {
        _unitOfWork = unitOfWork;
      }

      public async Task<string> Handle(TicketCreateCommand request, CancellationToken cancellationToken)
      {
        // PASO 1: Buscar o crear el Empleado asociado al ticket.
        // Si ya existe un empleado con ese email en PostgreSQL, lo reutilizamos.
        // Si no existe (primera vez), lo creamos con datos mínimos (solo el email).
        var employee = await _unitOfWork.EmployeeRepository.GetByUsernameAsync(request.Username);

        if(employee is null)
        {
           // Creamos el empleado con campos vacíos porque desde el evento solo tenemos el email.
           // El resto de campos (FirstName, LastName, Address) se completarían en otro flujo.
           employee = Employee.Create(
             string.Empty, // FirstName
             string.Empty, // LastName
             request.Username, // Email (funciona como username/identificador único)
             null! // Address — no disponible en este evento
             );
          _unitOfWork.EmployeeRepository.AddEntity(employee);
        }

        // PASO 2: Crear el Ticket en la base de datos de lectura.
        // Usamos el mismo ID del evento para mantener consistencia entre Command y Query Side.
        var ticket = Ticket.Create(
          new Guid(request.Id),        // ID idéntico al del Command Side (MongoDB)
          TicketType.Create(request.TicketType), // Tipo de error del ticket (1-5)
          request.DetailError          // Descripción del problema
        );
        // Usamos el repositorio genérico para Ticket, ya que no hay uno específico.
        _unitOfWork.RepositoryGeneric<Ticket>().AddEntity(ticket);

        // PASO 3: Crear la relación Ticket-Empleado (tabla intermedia N:M).
        // Esta tabla permite saber qué empleados están asignados a qué tickets.
        var ticketEmployee = TicketEmployee.Create(ticket, employee);
        _unitOfWork.RepositoryGeneric<TicketEmployee>().AddEntity(ticketEmployee);

        // PASO 4: Confirmar todos los cambios en una sola transacción de base de datos.
        // Si algo falla antes de aquí, ningún cambio se persiste (atomicidad).
        await _unitOfWork.Complete();

        // Devolvemos el ID del ticket creado para que el llamador pueda referenciarlo.
        return ticket.Id.ToString();
      }
    }
  }
}
