using FluentValidation;
using MediatR;
using Ticketing.command.Application.Agregates;
using Ticketing.command.Domain.Abstracts;
using Ticketing.command.Features.Apis;

namespace Ticketing.command.Features.Tickets
{
  // =========================================================================
  // CLASE: TicketDelete
  // PROPÓSITO: Esta clase agrupa toda la funcionalidad necesaria para ELIMINAR 
  // (o desactivar) un ticket. Sigue el patrón "Vertical Slice Architecture", 
  // lo que significa que el Endpoint de la API, el Comando, la Validación y 
  // la Lógica de Negocio (Handler) están todos juntos en este mismo archivo.
  // =========================================================================
  public class TicketDelete : IMinimalApi
  {
    // Método que registra la ruta en nuestra API (Endpoint).
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
      // Configuramos una ruta que responde al verbo HTTP DELETE.
      // La URL esperada será algo como: /api/tickets/12345?username=juan@gmail.com
      endpointRouteBuilder.MapDelete("/api/tickets/{id}", async (
        string id,           // El ID del ticket viene de la URL ({id})
        string username,     // El username viene como parámetro de consulta (Query string)
        IMediator mediator,  // MediatR nos ayuda a enviar el Comando al Handler correspondiente
        CancellationToken cancellationToken // Permite cancelar la operación si el usuario cierra su navegador
        ) =>
      {
        // 1. Empaquetamos los datos que recibimos en un "Comando".
        var command = new TicketDeleteCommand(id, username);
        
        // 2. Le decimos a MediatR que busque quién sabe manejar este comando y lo ejecute.
        var result = await mediator.Send(command, cancellationToken);
        
        // 3. Devolvemos una respuesta exitosa (HTTP 200 OK) al cliente.
        return Results.Ok(result);
      }
        ).WithName("DeleteTicket"); // Le damos un nombre interno a esta ruta.
    }

    // =========================================================================
    // COMANDO (CQRS)
    // Es un objeto inmutable (no se puede modificar después de creado).
    // Representa la INTENCIÓN de hacer algo, en este caso, borrar un ticket.
    // =========================================================================
    public record TicketDeleteCommand(string Id, string Username) : IRequest<bool>;

    // =========================================================================
    // VALIDADOR
    // Se asegura de que los datos del Comando sean correctos ANTES de procesarlos.
    // Usamos la librería FluentValidation para definir las reglas de forma legible.
    // =========================================================================
    public class TicketDeleteCommandValidator : AbstractValidator<TicketDeleteCommand>
    {
      public TicketDeleteCommandValidator()
      {
        // Regla: El ID no puede estar vacío. Si lo está, mostramos este mensaje de error.
        RuleFor(x => x.Id).NotEmpty().WithMessage("Ticket ID is required.");
        // Regla: El nombre de usuario tampoco puede estar vacío.
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.");
      }
    }

    // =========================================================================
    // HANDLER (Manejador del Comando)
    // Aquí es donde vive la Lógica de Negocio real. 
    // Recibe el Comando validado, hace el trabajo y devuelve un resultado.
    // =========================================================================
    public sealed class TicketDeleteCommandHandler(IEventSourcingHandler<TicketAggregate> eventSourcingHandler) : IRequestHandler<TicketDeleteCommand, bool>
    {
      // Guarda una referencia a nuestra base de datos orientada a eventos (Event Store)
      private readonly IEventSourcingHandler<TicketAggregate> _eventSourcingHandler = eventSourcingHandler;

      public async Task<bool> Handle(TicketDeleteCommand request, CancellationToken cancellationToken)
      {
        // 1. CARGAR: Vamos a buscar todo el historial de este ticket a la base de datos
        // y reconstruimos cómo se ve actualmente (rehidratar el Agregado).
        var aggregate = await _eventSourcingHandler.GetByIdAsync(request.Id, cancellationToken);

        // 2. ACTUAR: Le decimos a nuestro modelo (Agregado) que ejecute la acción.
        // Internamente, esto creará un evento TicketDeletedEvent.
        aggregate.DeleteTicket(request.Username);

        // 3. GUARDAR: Guardamos el nuevo evento generado en nuestra base de datos 
        // y lo publicamos (a través de Kafka) para que otros sistemas se enteren.
        await _eventSourcingHandler.SaveAsync(aggregate, cancellationToken);

        // Devolvemos true para indicar que todo salió bien.
        return true;
      }
    }
  }
}
