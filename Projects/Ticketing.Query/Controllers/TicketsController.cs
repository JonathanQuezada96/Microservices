// =========================================================================
// CLASE: TicketsController
// PROPÓSITO: Es el "portero" de la API REST del Query Side.
//
// Recibe peticiones HTTP del cliente (ej: Postman, frontend, otro microservicio),
// las convierte en Queries de MediatR, y devuelve las respuestas en formato JSON.
//
// IMPORTANTE: El controller NO hace lógica de negocio ni toca la base de datos.
// Su única responsabilidad es: recibir HTTP → delegar a MediatR → responder HTTP.
// Esto se llama principio de "Thin Controllers" (controladores delgados).
//
// RUTAS DISPONIBLES:
//   GET /api/tickets        → Obtener todos los tickets
//   GET /api/tickets/{id}   → Obtener un ticket específico por su ID (GUID)
// =========================================================================
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tiketing.Query.Features.Tickets.Dtos;
using Tiketing.Query.Features.Tickets.Queries;

namespace Tiketing.Query.Controllers
{
  // [ApiController]: activa funcionalidades automáticas útiles en APIs:
  //   - Validación del modelo automática (devuelve 400 si el modelo no es válido).
  //   - Binding automático de parámetros desde el body, query string y route.
  //
  // [Route("api/tickets")]: define la URL base como /api/tickets
  //   - [controller] se reemplaza automáticamente por el nombre de la clase sin "Controller".
  //   - TicketsController → /api/tickets
  [ApiController]
  [Route("api/tickets")]
  public class TicketsController : ControllerBase
  {
    // IMediator es el bus de MediatR.
    // En lugar de inyectar directamente los handlers o repositorios,
    // enviamos a "pregunta" (Query) y MediatR encuentra el handler correcto.
    // Esto mantiene el controller delgado: solo recibe HTTP, delega y devuelve respuesta.
    private readonly IMediator _mediator;

    // El constructor recibe IMediator por Inyección de Dependencias.
    // .NET busca automáticamente el IMediator registrado en Program.cs y lo pasa aquí.
    public TicketsController(IMediator mediator)
    {
      _mediator = mediator;
    }

    // =========================================================================
    // ENDPOINT: GET /api/tickets
    // DESCRIPCIÓN: Devuelve la lista completa de tickets almacenados en PostgreSQL.
    // RESPUESTA EXITOSA: 200 OK + JSON array de TicketDto
    // =========================================================================
    [HttpGet]
    public async Task<ActionResult<List<TicketDto>>> GetAll(CancellationToken cancellationToken)
    {
      // Creamos la Query (sin parámetros en este caso) y la enviamos a MediatR.
      // MediatR encontrará TicketGetQueryHandler y ejecutará la consulta a PostgreSQL.
      var query = new TicketGetQuery();
      var results = await _mediator.Send(query, cancellationToken);

      // Ok() genera una respuesta HTTP 200 con el contenido serializado a JSON.
      return Ok(results);
    }

    // =========================================================================
    // ENDPOINT: GET /api/tickets/{id}
    // DESCRIPCIÓN: Devuelve el detalle de un ticket específico según su ID (GUID).
    // RESPUESTA EXITOSA: 200 OK + JSON de TicketDto
    // RESPUESTA NO ENCONTRADO: 404 Not Found si el ticket no existe
    // =========================================================================
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDto>> GetById(
      Guid id,                       // {id} viene de la URL, .NET lo parsea automáticamente a Guid
      CancellationToken cancellationToken)
    {
      // Creamos la Query con el ID del ticket que queremos buscar.
      var query = new TicketGetByIdQuery(id);
      var result = await _mediator.Send(query, cancellationToken);

      // Si el handler devuelve null, significa que no encontró el ticket.
      // Devolvemos 404 Not Found con un mensaje descriptivo en lugar de 200 con null.
      // Esto es una buena práctica REST: los status codes deben comunicar el resultado.
      if (result is null)
        return NotFound(new { Message = $"No se encontró ningún ticket con el ID: {id}" });

      return Ok(result);
    }
  }
}
