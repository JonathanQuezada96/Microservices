// =========================================================================
// ARCHIVO: TicketGet.cs
// PROPÓSITO: Define las Queries (consultas de solo lectura) del Query Side.
//
// En CQRS, una "Query" es diferente a un "Comando":
//   - QUERY: Solo LEER datos. No cambia nada en la base de datos.
//   - COMANDO: Modifica el estado del sistema.
//
// Este archivo tiene DOS queries:
//   1. TicketGetQuery    → Obtener TODOS los tickets (GET /api/tickets)
//   2. TicketGetByIdQuery → Obtener UN ticket por su ID (GET /api/tickets/{id})
//
// Ambas leen directamente del DbContext (PostgreSQL) sin pasar por repositorios,
// porque las queries son simples y se benefician de acceso directo a EF Core.
// =========================================================================
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Domain.Tickets;
using Tiketing.Query.Features.Tickets.Dtos;
using Tiketing.Query.Infrastructure.Persistence;

namespace Tiketing.Query.Features.Tickets.Queries
{
  // =========================================================================
  // QUERY 1: TicketGetQuery — Obtener todos los tickets
  // =========================================================================
  // Esta clase es el "mensaje" que mandamos a MediatR.
  // Al estar vacía no necesita parámetros — solo pedimos "dame todos".
  // IRequest<List<TicketDto>> le dice a MediatR que el resultado es una lista de DTOs.
  public class TicketGetQuery : IRequest<List<TicketDto>>
  {
  }

  // Handler que procesa TicketGetQuery y devuelve todos los tickets de PostgreSQL.
  public class TicketGetQueryHandler : IRequestHandler<TicketGetQuery, List<TicketDto>>
  {
    // Accedemos directamente al DbContext de EF Core para la query.
    // En el Query Side, esto es aceptable — no necesitamos el patrón Repository
    // para consultas simples. La capa Repository agrega complejidad innecesaria aquí.
    private readonly TicketDbContext _context;

    public TicketGetQueryHandler(TicketDbContext context)
    {
      _context = context;
    }

    public async Task<List<TicketDto>> Handle(TicketGetQuery request, CancellationToken cancellationToken)
    {
      // ToListAsync() ejecuta un SELECT * FROM tickets en PostgreSQL de forma asíncrona.
      // No bloqueamos el hilo mientras esperamos la respuesta de la base de datos.
      var tickets = await _context.Tickets.ToListAsync(cancellationToken);

      // ConvertAll convierte la lista de entidades Ticket a una lista de TicketDto
      // usando el método de extensión ToTicketDto() definido en TicketDto.cs.
      return tickets.ConvertAll(t => t.ToTicketDto());
    }
  }

  // =========================================================================
  // QUERY 2: TicketGetByIdQuery — Obtener un ticket específico por su ID
  // =========================================================================
  // A diferencia de TicketGetQuery, esta sí necesita un parámetro: el ID del ticket.
  // IRequest<TicketDto?> devuelve un solo DTO o null si no se encontró el ticket.
  public class TicketGetByIdQuery : IRequest<TicketDto?>
  {
    // El ID del ticket que queremos buscar (viene de la URL: /api/tickets/{id})
    public Guid Id { get; init; }

    public TicketGetByIdQuery(Guid id)
    {
      Id = id;
    }
  }

  // Handler que procesa TicketGetByIdQuery.
  public class TicketGetByIdQueryHandler : IRequestHandler<TicketGetByIdQuery, TicketDto?>
  {
    private readonly TicketDbContext _context;

    public TicketGetByIdQueryHandler(TicketDbContext context)
    {
      _context = context;
    }

    public async Task<TicketDto?> Handle(TicketGetByIdQuery request, CancellationToken cancellationToken)
    {
      // FirstOrDefaultAsync busca el primer ticket cuyo Id coincida con el solicitado.
      // Devuelve null si no existe — sin lanzar excepción.
      // El controller se encargará de devolver 404 si el resultado es null.
      var ticket = await _context.Tickets
        .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

      // Si no encontramos el ticket, devolvemos null.
      // El operador ?. (null-conditional) evita un NullReferenceException.
      return ticket?.ToTicketDto();
    }
  }
}
