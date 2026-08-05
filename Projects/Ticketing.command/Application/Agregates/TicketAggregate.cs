using Common.Core.Events;
using Ticketing.command.Domain.Abstracts;
using Ticketing.command.Features.Tickets;

namespace Ticketing.command.Application.Agregates
{
  // TicketAggregate hereda de AggregateRoot. 
  // En Domain-Driven Design (DDD), un "Agregado" es un conjunto de objetos de dominio que se tratan como una sola unidad.
  // Un "Aggregate Root" (Raíz del Agregado) es la única clase que permite interacciones con el resto del agregado, 
  // garantizando la consistencia interna. En este caso, esta clase representa el estado y el ciclo de vida completo de un Ticket.
  public class TicketAggregate : AggregateRoot
  {
    // Constructor sin parámetros requerido para rehidratar el agregado desde los eventos (Reflection).
    public TicketAggregate()
    {

    }

    // Estado interno del Agregado
    public bool Active { get; set; }

    // Constructor principal que recibe el comando y genera el primer evento del ciclo de vida.
    // Aquí es donde residiría la lógica de negocio y las validaciones de dominio iniciales.
    public TicketAggregate(TicketCreateCommand command)
    {
      var ticketCreatedEvent = new TicketCreatedEvent
      {
        ID = command.Id,
        Username = command.ticketCreateRequest.Username,
        TypeError = command.ticketCreateRequest.TypeError,
        DetailError = command.ticketCreateRequest.DetailError,
      };
      // RaiseEvent añade el evento a la lista de cambios sin confirmar y llama al método Apply()
      RaiseEvent(ticketCreatedEvent);
    }

    // Método Apply: muta el estado del Agregado basándose EXCLUSIVAMENTE en los eventos.
    // Nunca se debe mutar el estado directamente desde afuera, siempre a través de un Apply de un evento.
    public void Apply(TicketCreatedEvent @event)
    {
      _id = @event.ID;
      Active = true;
    }

    // Método de dominio para aplicar cambios a un ticket ya existente.
    //
    // En Event Sourcing NO se modifican datos directamente en la base de datos.
    // En su lugar, GENERAMOS un nuevo evento (TicketUpdatedEvent) que describe el cambio.
    // Ese evento queda registrado en el Event Store (MongoDB) como parte del historial inmutable.
    //
    // Parámetros:
    //   ticketType  → nuevo tipo de error del ticket (1-5)
    //   description → nueva descripción del problema
    //   userName    → email del empleado que realiza la edición
    public void EditTicket(int ticketType, string description, string userName)
    {
      // REGLA DE NEGOCIO: No se puede editar un ticket que está inactivo (ej: cerrado o eliminado).
      // Si el agregado no está activo, lanzamos una excepción de dominio.
      // Esta validación protege la consistencia del sistema — un ticket cerrado no debería poder cambiar.
      if (!Active)
      {
        throw new InvalidOperationException("Cannot edit an inactive ticket.");
      }

      // RaiseEvent hace dos cosas:
      //   1. Añade el TicketUpdatedEvent a la lista de "cambios pendientes" del agregado.
      //   2. Llama a Apply(TicketUpdatedEvent) para actualizar el estado interno del agregado en memoria.
      // El evento se persiste en MongoDB y se publica en Kafka cuando el Handler llame a SaveAsync().
      RaiseEvent(new TicketUpdatedEvent
      {
        ID = Id,              // ID del ticket que se actualiza (para que el Query Side sepa qué registro tocar)
        TicketType = ticketType,
        Description = description,
        Username = userName
      });

    }

    // Apply recibe el TicketUpdatedEvent y muta el estado interno del agregado.
    //
    // REGLA FUNDAMENTAL de Event Sourcing:
    //   El método Apply es el ÚNICO lugar donde el estado del agregado puede cambiar.
    //   Esto garantiza que si "rehidratamos" el agregado reproduciendo todos sus eventos históricos,
    //   el estado resultante siempre será idéntico al estado actual — consistencia total.
    //
    // ¿Por qué aquí solo actualizamos _id y no TicketType ni Description?
    //   Porque este agregado no necesita ese estado en memoria para validar reglas de negocio.
    //   Solo necesita saber su ID (para referenciarse) y si está activo (para la guardia en EditTicket).
    //   En sistemas más complejos, Apply actualizaría todas las propiedades relevantes del agregado.
    public void Apply(TicketUpdatedEvent @event)
    {
      _id = @event.ID;

    }

    // =========================================================================
    // MÉTODO DE DOMINIO: DeleteTicket
    // =========================================================================
    // Este método representa la acción de "eliminar" o desactivar un ticket.
    // Al igual que en EditTicket, no borramos información de la base de datos de verdad,
    // sino que emitimos un EVENTO diciendo "Esto fue eliminado".
    public void DeleteTicket(string userName)
    {
      // REGLA DE NEGOCIO: No puedes borrar un ticket que ya está inactivo/eliminado.
      if (!Active)
      {
        throw new InvalidOperationException("Cannot delete an inactive ticket.");
      }

      // RaiseEvent crea el evento y lo pone en la lista de cosas por guardar.
      RaiseEvent(new TicketDeletedEvent
      {
        ID = Id,
        Username = userName
      });
    }

    // =========================================================================
    // MÉTODO APPLY: Para TicketDeletedEvent
    // =========================================================================
    // Este método se manda a llamar automáticamente después del RaiseEvent anterior,
    // o cuando estamos "rehidratando" (recargando) el ticket desde la base de datos.
    public void Apply(TicketDeletedEvent @event)
    {
      _id = @event.ID;
      // Aquí cambiamos el estado interno del agregado.
      // Al poner Active = false, las reglas de negocio (como EditTicket o DeleteTicket)
      // ya no permitirán hacer más cambios a este ticket en el futuro.
      Active = false;
    }
  }
 }
