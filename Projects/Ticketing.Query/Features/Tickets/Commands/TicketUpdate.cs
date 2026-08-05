// =========================================================================
// ARCHIVO: TicketUpdate.cs (Query Side)
// PROPÓSITO: Cuando el Query Side recibe un TicketUpdatedEvent de Kafka,
// este archivo se encarga de actualizar el ticket en PostgreSQL.
//
// FLUJO COMPLETO de una actualización:
//   1. Usuario llama PUT /api/tickets/{id} en el Command Side
//   2. Command Side genera TicketUpdatedEvent → lo publica en Kafka
//   3. EventConsumer (Query Side) recibe el mensaje de Kafka
//   4. EventHandler.On(TicketUpdatedEvent) llama TicketUpdateCommand
//   5. TicketUpdateCommandHandler (este archivo) actualiza PostgreSQL
// =========================================================================
using MediatR;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Domain.Employees;
using Tiketing.Query.Domain.Tickets;
using Tiketing.Query.Domain.TicketTypes;

namespace Tiketing.Query.Features.Tickets.Commands
{
  public class TicketUpdate
  {
    // =========================================================================
    // COMANDO: TicketUpdateCommand
    // Encapsula todos los datos necesarios para actualizar un ticket en PostgreSQL.
    // Estos datos vienen del TicketUpdatedEvent que llegó de Kafka.
    // =========================================================================
    public record TicketUpdateCommand(
      string Id,           // ID del ticket a actualizar (el mismo GUID del Command Side)
      int TicketType,      // Nuevo tipo de error del ticket
      string Description,  // Nueva descripción del problema
      string Username      // Email del empleado que hizo la actualización
      ) : IRequest<string>;

    // =========================================================================
    // HANDLER: TicketUpdateCommandHandler
    // Recibe el comando y ejecuta las operaciones en PostgreSQL.
    // =========================================================================
    public class TicketUpdateCommandHandler : IRequestHandler<TicketUpdateCommand, string>
    {
      // IUnitOfWork coordina los repositorios y la transacción de base de datos.
      private readonly IUnitOfWork _unitOfWork;

      public TicketUpdateCommandHandler(IUnitOfWork unitOfWork)
      {
        _unitOfWork = unitOfWork;
      }

      public async Task<string> Handle(TicketUpdateCommand request, CancellationToken cancellationToken)
      {
        // 1. BUSCAR el ticket existente en PostgreSQL por su ID.
        // Si no existe, lanzamos una excepción — no podemos actualizar algo que no está.
        var ticket = await _unitOfWork.RepositoryGeneric<Ticket>().GetByIdAsync(new Guid(request.Id));

        if (ticket is null)
        {
          throw new Exception($"Ticket with Id {request.Id} not found.");
        }

        // 2. BUSCAR o CREAR el empleado asociado.
        // Es posible que el empleado que actualizó el ticket no exista aún en PostgreSQL
        // (si es la primera vez que ese email interactúa con el sistema).
        var employee = await _unitOfWork.EmployeeRepository.GetByUsernameAsync(request.Username);

        if (employee is null)
        {
          // Lo creamos con datos mínimos. Solo tenemos el email del evento.
          employee = Employee.Create(
            string.Empty,    // FirstName no disponible en este evento
            string.Empty,    // LastName no disponible en este evento
            request.Username, // Email (actúa como identificador único)
            null!            // Address no disponible en este evento
          );
          _unitOfWork.EmployeeRepository.AddEntity(employee);
        }

        // 3. ACTUALIZAR las propiedades del ticket con los nuevos datos del evento.
        ticket.Description = request.Description;
        ticket.TicketType = TicketType.Create(request.TicketType);

        // Marcamos explícitamente la entidad como Modified.
        // EF Core no siempre detecta cambios en propiedades de tipo referencia (como TicketType)
        // porque compara por referencia de objeto, no por valor. UpdateEntity() fuerza
        // EntityState.Modified → SaveChangesAsync() genera el UPDATE en PostgreSQL.
        _unitOfWork.RepositoryGeneric<Ticket>().UpdateEntity(ticket);

        // 4. CONFIRMAR todos los cambios en una sola transacción.
        await _unitOfWork.Complete();

        // Devolvemos el ID del ticket actualizado como confirmación.
        return Convert.ToString(ticket.Id)!;
      }
    }
  }
}

