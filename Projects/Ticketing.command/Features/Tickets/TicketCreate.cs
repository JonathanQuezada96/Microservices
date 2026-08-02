using FluentValidation;
using MediatR;
using Ticketing.command.Application.Agregates;
using Ticketing.command.Domain.Abstracts;
using Ticketing.command.Features.Apis;
using static Ticketing.command.Features.Tickets.TicketCreate;

// Este archivo implementa el patrón "Vertical Slice Architecture":
// en lugar de separar el código por capas técnicas (Controllers/Services/Repos),
// agrupa TODO lo relacionado a una funcionalidad (crear ticket) en un solo lugar.
// Aquí conviven: el DTO de entrada, el comando CQRS, la validación y el handler.

namespace Ticketing.command.Features.Tickets
{
  public sealed class TicketCreate : IMinimalApi
  {
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
      // Configuramos una ruta HTTP POST en la URL "/api/ticket".
      // Minimal APIs (característica de .NET) nos permite inyectar dependencias directamente en los parámetros.
      // Aquí inyectamos el cuerpo de la petición (TicketCreateRequest), el mediador (IMediator) y el token de cancelación.
      endpointRouteBuilder.MapPost("/api/ticket", async (TicketCreateRequest ticketCreateRequest, IMediator mediator, CancellationToken cancellation) =>
      {
        // 1. Encapsulamos los datos de entrada en un "Comando" (Command).
        // Este comando representa la intención de crear un ticket
        var id = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString();
        var command = new TicketCreateCommand(id, ticketCreateRequest);
        
        // 2. Enviamos el comando a MediatR. MediatR buscará automáticamente el "Handler" 
        // (TicketCreateCommandHandler) que sabe cómo procesar este comando y lo ejecutará.
        var result = await mediator.Send(command);
        
        // 3. Devolvemos una respuesta HTTP 200 OK al cliente, incluyendo el resultado de la operación.
        return Results.Ok(result);
      });
    }

    /// <summary>
    /// DTO (Data Transfer Object) de entrada que llega desde el cliente HTTP (body del POST).
    /// Usa "Primary Constructor" de C# 12 para inicializar las propiedades en una sola línea.
    /// 'sealed' evita que esta clase sea heredada — es intencional porque es un DTO simple.
    /// </summary>
    public sealed class TicketCreateRequest(string username, int typeError, string detailError)
    {
      public string Username { get; set; } = username;
      public int TypeError { get; set; } = typeError;
      public string DetailError { get; set; } = detailError;
    }
  }

  /// <summary>
  /// Comando CQRS que representa la INTENCIÓN de crear un ticket.
  ///
  /// En CQRS (Command Query Responsibility Segregation):
  ///   - Los COMANDOS modifican el estado del sistema (write side).
  ///   - Las QUERIES leen el estado sin modificarlo (read side).
  ///
  /// 'record' es ideal para comandos porque son inmutables por diseño.
  /// IRequest&lt;bool&gt; le indica a MediatR que este comando devuelve un bool como resultado.
  /// </summary>
  public record TicketCreateCommand( string Id, TicketCreateRequest ticketCreateRequest) : IRequest<bool>;

  /// <summary>
  /// Validador del COMANDO (capa externa de validación).
  /// Delega la validación real al validador del Request interno (TicketCreateValidator).
  /// Esto permite reutilizar la validación del DTO en otros contextos.
  ///
  /// FluentValidation intercepta el comando ANTES de que llegue al Handler,
  /// gracias al pipeline behavior registrado con AddValidatorsFromAssembly().
  /// Si la validación falla, el Handler nunca se ejecuta.
  /// </summary>
  public class TicketCreateCommandValidator : AbstractValidator<TicketCreateCommand>
  {
    public TicketCreateCommandValidator()
    {
      // SetValidator encadena el validador del objeto anidado (TicketCreateRequest)
      RuleFor(x => x.Id).NotEmpty().WithMessage("The id should´nt be empty");
      RuleFor(x => x.ticketCreateRequest).SetValidator(new TicketCreateValidator());
    }
  }

  /// <summary>
  /// Validador de las reglas de negocio sobre el DTO de entrada.
  /// Cada RuleFor define una regla sobre una propiedad específica.
  /// WithMessage() personaliza el mensaje de error que recibe el cliente.
  /// </summary>
  public class TicketCreateValidator : AbstractValidator<TicketCreateRequest>
  {
    public TicketCreateValidator()
    {
      // NotEmpty() rechaza null, string vacío ("") y solo espacios en blanco
      RuleFor(x => x.Username).NotEmpty().WithMessage("There is not a username, please sign it").EmailAddress().WithMessage("Debe ser un email");
      RuleFor(x => x.TypeError).NotEmpty().WithMessage("Debe existir el tipo de error").InclusiveBetween(1, 5).WithMessage("El rango del error es de 1 a 5");
      RuleFor(x => x.DetailError).NotEmpty().WithMessage("There is not a detail error, please sign it");
    }
  }

  /// <summary>
  /// Handler del comando — aquí orquestamos el flujo de dominio.
  ///
  /// MediatR invoca Handle() automáticamente cuando alguien llama a mediator.Send(command).
  /// Usa "Primary Constructor" de C# 12 para inyectar el EventSourcingHandler.
  ///
  /// Flujo refactorizado (DDD + Event Sourcing):
  ///   1. Se instancia el Agregado (TicketAggregate) usando el comando. Esto genera el primer 
  ///      evento de dominio (TicketCreatedEvent) de forma interna.
  ///   2. El EventSourcingHandler delega al EventStore la persistencia de esos eventos nuevos.
  ///   De esta forma, este Handler queda completamente limpio y no sabe nada de MongoDB.
  /// </summary>
  public sealed class TicketCreateCommandHandler(IEventSourcingHandler<TicketAggregate> eventSourcingHandler)
    : IRequestHandler<TicketCreateCommand, bool>
  {
    private readonly IEventSourcingHandler<TicketAggregate> 
      _eventSourcingHandler = eventSourcingHandler;
    public async Task<bool> Handle(TicketCreateCommand request, CancellationToken cancellationToken)
    {
      var aggregate = new TicketAggregate(request);
      await _eventSourcingHandler.SaveAsync(aggregate, cancellationToken);
      return true;
    }
  }
}
