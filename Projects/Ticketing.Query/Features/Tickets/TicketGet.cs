using MediatR;
using Microsoft.EntityFrameworkCore;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Domain.Tickets;

// Este archivo implementa la CONSULTA (Query) de tickets en el Query Side.
// Siguiendo el patrón Vertical Slice, toda la lógica de "obtener tickets" está aquí:
// el record de la consulta, su handler, y el DTO de respuesta.
//
// En CQRS:
//   - Los COMANDOS modifican el estado (ej: TicketCreate, TicketUpdate).
//   - Las QUERIES solo LEEN el estado sin modificarlo — este archivo es una Query.
//
// Estas queries son el objetivo principal del Query Side: servir datos rápidamente
// desde PostgreSQL (la "Materialized View" replicada desde el Command Side via Kafka).

namespace Tiketing.Query.Features.Tickets
{
  // ─── DTO de respuesta ────────────────────────────────────────────────────────
  // DTO (Data Transfer Object): objeto que usamos para enviar datos al cliente HTTP.
  // NO exponemos las entidades de dominio directamente — el DTO puede tener
  // exactamente los campos que el cliente necesita, sin exponer detalles internos.
  public record TicketResponse(
    Guid Id,
    string? Description,
    string? TicketTypeName,
    List<string> EmployeeEmails // Lista de emails de empleados asignados al ticket
  );

  // ─── Query (la "pregunta") ───────────────────────────────────────────────────
  // IRequest<List<TicketResponse>> le dice a MediatR que esta query devuelve una lista de TicketResponse.
  // Es un record porque las queries deben ser inmutables — no tienen sentido modificarlas.
  public record GetAllTicketsQuery() : IRequest<List<TicketResponse>>;

  // ─── Query para un ticket específico por ID ──────────────────────────────────
  // Permite buscar un ticket por su Guid. Si no existe, el handler devuelve null.
  public record GetTicketByIdQuery(Guid Id) : IRequest<TicketResponse?>;

  // ─── Handler de GetAllTicketsQuery ──────────────────────────────────────────
  // MediatR invoca Handle() automáticamente cuando alguien hace mediator.Send(new GetAllTicketsQuery()).
  public class GetAllTicketsQueryHandler : IRequestHandler<GetAllTicketsQuery, List<TicketResponse>>
  {
    // Usamos el repositorio genérico de Ticket para leer de PostgreSQL.
    // No necesitamos UnitOfWork aquí porque las queries NO modifican datos.
    private readonly IGenericRepository<Ticket> _ticketRepository;

    public GetAllTicketsQueryHandler(IGenericRepository<Ticket> ticketRepository)
    {
      _ticketRepository = ticketRepository;
    }

    public async Task<List<TicketResponse>> Handle(GetAllTicketsQuery request, CancellationToken cancellationToken)
    {
      // GetAllAsync() ejecuta: SELECT * FROM tickets en PostgreSQL.
      // EF Core cargará las propiedades de navegación (TicketType, TicketEmployees)
      // automáticamente gracias al Lazy Loading configurado en el DbContext.
      var tickets = await _ticketRepository.GetAllAsync();

      // Proyectamos cada Ticket al DTO de respuesta usando LINQ.
      // Esto evita exponer la entidad de dominio directamente al cliente HTTP.
      return tickets.Select(ticket => new TicketResponse(
        ticket.Id,
        ticket.Description,
        // TicketType puede ser null si el ticket fue creado sin tipo
        ticket.TicketType?.Name,
        // Mapeamos los TicketEmployees a solo los emails de los empleados
        ticket.TicketEmployees
              .Where(te => te.Employee is not null)
              .Select(te => te.Employee!.Email)
              .ToList()
      )).ToList();
    }
  }

  // ─── Handler de GetTicketByIdQuery ──────────────────────────────────────────
  public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, TicketResponse?>
  {
    private readonly IGenericRepository<Ticket> _ticketRepository;

    public GetTicketByIdQueryHandler(IGenericRepository<Ticket> ticketRepository)
    {
      _ticketRepository = ticketRepository;
    }

    public async Task<TicketResponse?> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
      // GetByIdAsync busca por clave primaria — es más eficiente que filtrar con FirstOrDefault.
      // Retorna null si no existe un ticket con ese ID.
      var ticket = await _ticketRepository.GetByIdAsync(request.Id);

      // Si no encontramos el ticket, retornamos null.
      // El Controller se encargará de convertirlo en un HTTP 404.
      if (ticket is null) return null;

      return new TicketResponse(
        ticket.Id,
        ticket.Description,
        ticket.TicketType?.Name,
        ticket.TicketEmployees
              .Where(te => te.Employee is not null)
              .Select(te => te.Employee!.Email)
              .ToList()
      );
    }
  }
}
