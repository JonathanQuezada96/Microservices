using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tiketing.Query.Features.Tickets;

namespace Tiketing.Query.Controllers
{
  // Controller REST que expone los endpoints HTTP para consultar tickets.
  //
  // [ApiController]: activa funcionalidades automáticas útiles en APIs:
  //   - Validación del modelo automática (devuelve 400 si el modelo no es válido).
  //   - Binding automático de parámetros desde el body, query string y route.
  //
  // [Route("api/[controller]")]: define la URL base como /api/tickets
  //   - [controller] se reemplaza automáticamente por el nombre de la clase sin "Controller".
  //   - TicketsController → /api/tickets
  [ApiController]
  [Route("api/[controller]")]
  public class TicketsController : ControllerBase
  {
    // IMediator es el bus de MediatR.
    // En lugar de inyectar directamente los handlers o repositorios,
    // enviamos la "pregunta" (Query) y MediatR encuentra el handler correcto.
    // Esto mantiene el controller delgado: solo recibe la petición HTTP, la delega y devuelve la respuesta.
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
      _mediator = mediator;
    }

    // GET /api/tickets
    // Retorna todos los tickets almacenados en PostgreSQL (Query Side).
    // HTTP 200 OK con la lista de tickets, o HTTP 200 con lista vacía si no hay tickets.
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
      // Enviamos la query a MediatR. Él encuentra GetAllTicketsQueryHandler y lo ejecuta.
      var tickets = await _mediator.Send(new GetAllTicketsQuery(), cancellationToken);

      // Ok() genera una respuesta HTTP 200 con el cuerpo serializado como JSON.
      return Ok(tickets);
    }

    // GET /api/tickets/{id}
    // Retorna un ticket específico por su ID (Guid).
    // HTTP 200 OK si existe, HTTP 404 Not Found si no existe.
    //
    // {id} en la ruta es un "route parameter": se extrae automáticamente de la URL.
    // Ejemplo: GET /api/tickets/3fa85f64-5717-4562-b3fc-2c963f66afa6
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
      var ticket = await _mediator.Send(new GetTicketByIdQuery(id), cancellationToken);

      // Si el handler devuelve null, el ticket no existe en PostgreSQL.
      // NotFound() genera HTTP 404 — el cliente sabe que no existe, no que hubo un error.
      if (ticket is null)
        return NotFound(new { Message = $"No se encontró un ticket con el ID: {id}" });

      return Ok(ticket);
    }
  }
}
