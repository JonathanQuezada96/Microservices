// =========================================================================
// ARCHIVO: TicketDto.cs
// PROPÓSITO: Define el DTO (Data Transfer Object) que enviamos al cliente HTTP,
// y el Mapper que convierte la entidad de base de datos al DTO.
//
// ¿Qué es un DTO?
// Es un objeto simple que "transporta" datos hacia afuera de la aplicación.
// NO es una entidad de base de datos — no tiene lógica de negocio.
// Solo tiene propiedades para serializar a JSON y enviar al cliente.
//
// ¿Por qué no devolver la entidad Ticket directamente?
// Porque la entidad tiene propiedades de EF Core (virtual, colecciones de navegación)
// que pueden causar loops infinitos al serializar a JSON (serialización circular).
// Además, podrías querer devolver más o menos campos que los que tiene la entidad.
// =========================================================================
using Tiketing.Query.Domain.Tickets;

namespace Tiketing.Query.Features.Tickets.Dtos
{
  // =========================================================================
  // CLASE ESTÁTICA: TicketMapper
  // Contiene métodos de extensión que convierten una entidad Ticket al DTO.
  //
  // ¿Qué es un método de extensión?
  // Es un método que "parece" parte de una clase, pero se define fuera.
  // Gracias a 'this Ticket ticket', podemos llamarlo como: ticket.ToTicketDto()
  // =========================================================================
  public static class TicketMapper
  {
    // Método de extensión: convierte un objeto Ticket (entidad de BD) a TicketDto.
    // Se llama así: var dto = ticket.ToTicketDto();
    public static TicketDto ToTicketDto(this Ticket ticket)
    {
      return new TicketDto(
        ticket.Id,                               // El Guid único del ticket
        ticket.Description ?? string.Empty,      // La descripción (o vacío si es null)
        ticket.TicketType?.Id ?? 0,              // El número del tipo de error (1-5), o 0 si no tiene tipo
        ticket.CreatedOn,                        // Fecha de creación (viene de la clase base Entity)
        ticket.LastModificateOn,                 // Última fecha de modificación
        ticket.CreatedBy                         // Quién creó el ticket (lo setea el interceptor de auditoría)
      );
    }
  }

  // =========================================================================
  // RECORD: TicketDto
  // Es el "contrato de respuesta" de la API para un ticket.
  // 'record' en C# es ideal para DTOs porque:
  //   - Es inmutable por defecto (no se puede modificar después de creado).
  //   - Implementa Equals y GetHashCode automáticamente (compara por valor).
  //   - Se puede crear con sintaxis corta (Primary Constructor).
  // =========================================================================
  public record TicketDto(
    Guid Id,                      // Identificador único del ticket
    string Description,           // Descripción del problema
    int TicketType,               // Categoría del error (1=Bug crítico, 2=Error UI, etc.)
    DateTime? CreatedOn,          // Fecha y hora en que se creó el ticket (UTC)
    DateTime? LastModifiedOn,     // Fecha y hora de la última actualización
    string? CreatedBy             // Email del empleado que reportó el ticket
  );
}
