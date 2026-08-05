using MediatR;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Domain.Tickets;

namespace Tiketing.Query.Features.Tickets.Commands
{
  // =========================================================================
  // CLASE: TicketDelete (En el Query Side)
  // PROPÓSITO: Cuando el lado de lectura (Query Side) recibe un mensaje de que un 
  // ticket fue eliminado, necesita borrarlo de su propia base de datos (PostgreSQL).
  // Esta clase agrupa el Comando y su respectivo Handler para llevar a cabo esa tarea.
  // =========================================================================
  public class TicketDelete
  {
    // =========================================================================
    // COMANDO (CQRS)
    // Este objeto sirve como un paquete de datos. Transporta el ID del ticket 
    // y el Username desde el EventHandler hasta el CommandHandler.
    // =========================================================================
    public record TicketDeleteCommand(
      string Id,
      string Username
      ) : IRequest<string>;

    // =========================================================================
    // HANDLER (Manejador)
    // Es la clase responsable de recibir el TicketDeleteCommand y ejecutar 
    // las acciones reales sobre la base de datos (a través de Entity Framework).
    // =========================================================================
    public class TicketDeleteCommandHandler : IRequestHandler<TicketDeleteCommand, string>
    {
      // IUnitOfWork agrupa todos los repositorios y permite guardar cambios
      // en la base de datos en una sola transacción segura.
      private readonly IUnitOfWork _unitOfWork;
      
      public TicketDeleteCommandHandler(IUnitOfWork unitOfWork)
      {
        _unitOfWork = unitOfWork;
      }

      public async Task<string> Handle(TicketDeleteCommand request, CancellationToken cancellationToken)
      {
        // 1. BUSCAR: Primero buscamos en la base de datos si el ticket existe, 
        // usando el Repositorio Genérico para la entidad 'Ticket'.
        var ticket = await _unitOfWork.RepositoryGeneric<Ticket>().GetByIdAsync(new Guid(request.Id));

        // Si por alguna razón no lo encontramos, lanzamos una excepción.
        // Esto evita que intentemos borrar algo que no existe.
        if (ticket is null)
        {
          throw new Exception($"Ticket with Id {request.Id} not found.");
        }

        // 2. ELIMINAR: Le decimos al repositorio que queremos borrar este ticket.
        // Nota: Hasta este punto, NO se ha borrado de la base de datos real, 
        // solo se marcó para ser eliminado en memoria.
        _unitOfWork.RepositoryGeneric<Ticket>().DeleteEntity(ticket);

        // 3. GUARDAR: 'Complete' ejecuta el SAVE en la base de datos real. 
        // Aquí es donde sucede el verdadero 'DELETE FROM Tickets WHERE Id = ...'
        await _unitOfWork.Complete();

        // Finalmente, devolvemos el ID del ticket borrado como confirmación.
        return Convert.ToString(ticket.Id)!;
      }
    }
  }
}
