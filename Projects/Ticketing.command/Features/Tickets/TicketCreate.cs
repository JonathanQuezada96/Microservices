using AutoMapper;
using Common.Core.Events;
using FluentValidation;
using MediatR;
using MongoDB.Driver;
using Ticketing.command.Domain.EventModels;
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
        // Este comando representa la intención de crear un ticket.
        var command = new TicketCreateCommand(ticketCreateRequest);
        
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
    public sealed class TicketCreateRequest(string username, string typeError, string detailError)
    {
      public string Username { get; set; } = username;
      public string TypeError { get; set; } = typeError;
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
  public record TicketCreateCommand(TicketCreateRequest ticketCreateRequest) : IRequest<bool>;

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
      RuleFor(x => x.Username).NotEmpty().WithMessage("There is not a username, please sign it");
      RuleFor(x => x.DetailError).NotEmpty().WithMessage("There is not a detail error, please sign it");
    }
  }

  /// <summary>
  /// Handler del comando — aquí vive la lógica de negocio real.
  ///
  /// MediatR invoca Handle() automáticamente cuando alguien llama a mediator.Send(command).
  /// Usa "Primary Constructor" de C# 12 para inyección de dependencias sin boilerplate.
  ///
  /// Flujo completo:
  ///   1. AutoMapper convierte el DTO de entrada (TicketCreateRequest) al evento de dominio (TicketCreatedEvent).
  ///   2. Se construye el EventModel (el "sobre" que va al Event Store).
  ///   3. Se abre una sesión y transacción en MongoDB.
  ///   4. Se inserta el evento dentro de la transacción.
  ///   5. Si todo ok → Commit. Si algo falla → Rollback.
  /// </summary>
  public sealed class TicketCreateCommandHandler(IEventModelRepository eventModelRepository, IMapper mapper)
    : IRequestHandler<TicketCreateCommand, bool>
  {
    // Campos privados readonly para guardar las dependencias inyectadas.
    // Aunque el primary constructor los asigna automáticamente, es buena práctica
    // declararlos explícitamente para mayor claridad y acceso interno.
    private readonly IEventModelRepository _eventModelRepository = eventModelRepository;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Método principal que MediatR invoca al recibir un TicketCreateCommand.
    /// CancellationToken permite cancelar la operación si el cliente se desconecta.
    /// </summary>
    public async Task<bool> Handle(TicketCreateCommand request, CancellationToken cancellationToken)
    {
      // PASO 1: Mapeo del DTO al evento de dominio usando AutoMapper.
      // CreateMap<TicketCreateRequest, TicketCreatedEvent>() en MappingProfile define el mapeo.
      // Esto desacopla la capa de presentación (request HTTP) del dominio (evento).
      var ticketEventData = _mapper.Map<TicketCreatedEvent>(request.ticketCreateRequest);

      // PASO 2: Construir el EventModel (el registro que va al Event Store en MongoDB).
      var eventModel = new EventModel
      {
        TimeStamp = DateTime.UtcNow,

        // Guid.CreateVersion7 genera un UUID v7 que incluye el timestamp en los bits más significativos,
        // lo que lo hace ordenable cronológicamente — ideal para Event Stores.
        AggregateIdentifier = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString(),

        AggregateType = "TicketAggregate", // Nombre del tipo de agregado al que pertenece el evento
        Version = 1,                        // Primera versión del evento en este agregado

        // Nota: hay un typo "TicketCreationEvanet" → debería ser "TicketCreationEvent"
        EventType = "TicketCreationEvanet",

        EventData = ticketEventData          // El evento de dominio real con los datos del ticket
      };

      // PASO 3: Iniciar sesión y transacción en MongoDB.
      // La sesión es obligatoria para poder usar transacciones en MongoDB Replica Set.
      IClientSessionHandle session = await _eventModelRepository.BeginSessionAsync(cancellationToken);

      try
      {
        // Inicio de la transacción — las operaciones siguientes son atómicas
        _eventModelRepository.BeginTransaction(session);

        // Insertar el evento en la colección "eventStores" dentro de la transacción
        await _eventModelRepository.InsertOneAsync(eventModel, session, cancellationToken);

        // Confirmar la transacción — el evento queda persistido de forma permanente
        await _eventModelRepository.CommitTransactionAsync(session, cancellationToken);

        // Liberar los recursos de la sesión (importante para evitar fugas de conexión)
        _eventModelRepository.DisoseSession(session);

        return true; // Operación exitosa
      }
      catch (Exception ex)
      {
        // Si algo falló (error de red, violación de constraints, etc.):
        // ROLLBACK → deshace la inserción, el evento NO queda en la base de datos.
        // Esto garantiza consistencia: o se guarda completo, o no se guarda nada.
        await _eventModelRepository.RollbackTransactionAsync(session, cancellationToken);
        _eventModelRepository.DisoseSession(session);

        // TODO: loguear el error (ex.Message) en lugar de solo retornar false silenciosamente
        return false;
      }
    }
  }
}
