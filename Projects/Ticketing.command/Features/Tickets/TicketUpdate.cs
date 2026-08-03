using FluentValidation;
using MediatR;
using Ticketing.command.Application.Agregates;
using Ticketing.command.Domain.Abstracts;
using Ticketing.command.Features.Apis;

// Este archivo implementa la operación de ACTUALIZAR un ticket, siguiendo Vertical Slice Architecture.
// Todo lo necesario para esta funcionalidad está aquí en un solo lugar:
//   DTO de entrada → Comando CQRS → Validadores → Handler → respuesta HTTP.
//
// A diferencia de TicketCreate (que crea un agregado nuevo), aquí primero CARGAMOS
// el agregado existente desde el Event Store y luego le aplicamos el nuevo evento.

namespace Ticketing.command.Features.Tickets
{
  public class TicketUpdate : IMinimalApi
  {
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
      // Registramos una ruta HTTP PUT en "/api/tickets/{id}".
      // PUT es el verbo correcto para actualización completa de un recurso existente.
      // El {id} en la ruta es el identificador del ticket a actualizar (extraído automáticamente de la URL).
      endpointRouteBuilder.MapPut("/api/tickets/{id}", async (
        string id,                          // ID del ticket a actualizar (viene del path de la URL)
        TicketUpdateRequest ticketUpdateRequest, // Cuerpo del request (JSON deserializado automáticamente)
        IMediator mediator,                 // Bus de MediatR — despacha el comando al handler correcto
        CancellationToken cancellationToken // Token para cancelar si el cliente cierra la conexión
        ) =>
      {
        // Encapsulamos los datos (ID de ruta + body) en un Comando inmutable.
        var command = new TicketUpdateCommand(id, ticketUpdateRequest);

        // MediatR encuentra el TicketUpdateCommandHandler y ejecuta la lógica de dominio.
        var result = await mediator.Send(command, cancellationToken);

        // Retornamos HTTP 200 OK con el resultado de la operación.
        return Results.Ok(result);
      }
        ).WithName("UpdateTicket"); // Nombre del endpoint para documentación y LinkGenerator
    }

    /// <summary>
    /// DTO (Data Transfer Object) de entrada para la actualización del ticket.
    /// Usa "Primary Constructor" de C# 12 para inicializar las propiedades en una sola línea.
    /// Las propiedades son de solo lectura (solo tienen getter) para garantizar inmutabilidad:
    /// una vez construido el DTO, nadie puede modificar sus valores accidentalmente.
    /// </summary>
    public sealed class TicketUpdateRequest(int ticketType, string description, string username)
    {
      public int TicketType { get; } = ticketType;       // Nuevo tipo de error del ticket (1-5)
      public string Description { get; } = description;  // Nueva descripción o detalle del problema
      public string Username { get; } = username;         // Email del empleado que realiza la actualización
    }

    /// <summary>
    /// Comando CQRS que representa la INTENCIÓN de actualizar un ticket.
    /// Es un 'record' para garantizar inmutabilidad — los comandos no deben cambiar una vez creados.
    /// IRequest&lt;bool&gt; indica a MediatR que este comando devuelve un bool como resultado.
    /// </summary>
    public record TicketUpdateCommand(string Id, TicketUpdateRequest TicketUpdateRequest) : IRequest<bool>;

    /// <summary>
    /// Validador del comando — actúa como "guardia de entrada" antes de que llegue al Handler.
    /// FluentValidation lo intercepta automáticamente gracias al pipeline behavior registrado.
    /// Si alguna regla falla, el Handler NUNCA se ejecuta y se devuelve HTTP 400 al cliente.
    /// </summary>
    public class TicketUpdateCommandValidator : AbstractValidator<TicketUpdateCommand>
    {
      public TicketUpdateCommandValidator()
      {
        // El ID del ticket debe venir en la URL — no puede estar vacío.
        RuleFor(x => x.Id).NotEmpty().WithMessage("Ticket ID is required.");

        // Delegamos la validación del body al validador específico del DTO.
        // SetValidator encadena validadores y permite reutilizar TicketRequestValidator en otros contextos.
        RuleFor(X => X.TicketUpdateRequest).SetValidator(new TicketRequestValidator());
      }
    }

    /// <summary>
    /// Validador de las reglas de negocio sobre el DTO de entrada TicketUpdateRequest.
    /// Separado del validador del comando para poder reutilizarlo de forma independiente.
    /// </summary>
    public class TicketRequestValidator : AbstractValidator<TicketUpdateRequest>
    {
      public TicketRequestValidator()
      {
        // La descripción no puede estar vacía — es el campo principal del ticket.
        RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");

        // El username (email) es obligatorio para saber quién hizo el cambio.
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.");

        // El tipo de ticket debe ser un número entre 1 y 5 (categorías de error definidas en el dominio).
        // InclusiveBetween verifica que 1 ≤ valor ≤ 5.
        RuleFor(x => x.TicketType)
          .NotEmpty().WithMessage("Ticket type is required.")
          .InclusiveBetween(1, 5).WithMessage("Ticket type must be between 1 and 5.");
      }
    }

    /// <summary>
    /// Handler del comando — aquí está la lógica de dominio para actualizar un ticket.
    ///
    /// Flujo de Event Sourcing para la actualización:
    ///   1. GetByIdAsync() "rehidrata" el agregado: lee todos los eventos históricos del ticket
    ///      desde el Event Store (MongoDB) y los reproduce en orden (Apply) para reconstruir el estado actual.
    ///   2. EditTicket() valida las reglas de negocio y genera un nuevo TicketUpdatedEvent.
    ///   3. SaveAsync() persiste el nuevo evento en MongoDB y lo publica en Kafka.
    ///      El Query Side lo consumirá y actualizará su base de datos de lectura (PostgreSQL).
    ///
    /// Usa "Primary Constructor" de C# 12 para inyectar el EventSourcingHandler en una sola línea.
    /// </summary>
    public sealed class TicketUpdateCommandHandler(IEventSourcingHandler<TicketAggregate> eventSourcingHandler) : IRequestHandler<TicketUpdateCommand, bool>
    {
      private readonly IEventSourcingHandler<TicketAggregate> _eventSourcingHandler = eventSourcingHandler;

      public async Task<bool> Handle(TicketUpdateCommand request, CancellationToken cancellationToken)
      {
        // PASO 1: Rehidratamos el agregado desde el Event Store.
        // GetByIdAsync lee todos los eventos del ticket con ese ID desde MongoDB
        // y reconstruye el estado actual reproduciéndolos uno por uno (Apply).
        var aggregate = await _eventSourcingHandler.GetByIdAsync(request.Id, cancellationToken);

        // PASO 2: Llamamos al método de dominio EditTicket().
        // Este método valida reglas de negocio (ej: el ticket debe estar activo)
        // y genera internamente el TicketUpdatedEvent con los nuevos datos.
        aggregate.EditTicket(
          request.TicketUpdateRequest.TicketType,
          request.TicketUpdateRequest.Description,
          request.TicketUpdateRequest.Username
          );

        // PASO 3: Persistimos el nuevo evento y lo publicamos a Kafka.
        // SaveAsync guarda el TicketUpdatedEvent en MongoDB y lo publica en el topic de Kafka
        // para que el Query Side lo consuma y actualice PostgreSQL.
        await _eventSourcingHandler.SaveAsync(aggregate, cancellationToken);

        return true;
      }
    }
  }
}
