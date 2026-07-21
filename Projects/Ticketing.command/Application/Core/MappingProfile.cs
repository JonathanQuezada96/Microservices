using static Ticketing.command.Features.Tickets.TicketCreate;
using AutoMapper;
using Common.Core.Events;

namespace Ticketing.command.Application.Core
{
  /// <summary>
  /// Perfil de mapeo de AutoMapper para la capa de Application.
  ///
  /// Un "Profile" agrupa las reglas de conversión entre tipos relacionados.
  /// Al heredar de Profile (clase de AutoMapper), esta clase se detecta automáticamente
  /// al hacer services.AddAutoMapper(assembly) en ApplicationServiceRegistration.
  ///
  /// Por qué mapeamos:
  ///   - TicketCreateRequest  → es el DTO que llega desde el cliente HTTP (capa de presentación).
  ///   - TicketCreatedEvent   → es el evento de dominio (capa de dominio/Common).
  ///   Mapear entre ellos evita que el dominio dependa de los DTOs HTTP y viceversa.
  /// </summary>
  public class MappingProfile : Profile
  {
    public MappingProfile()
    {
      // CreateMap<TSource, TDestination>() define la regla de conversión.
      // AutoMapper mapea automáticamente propiedades con el MISMO NOMBRE (convention-based mapping).
      // Username, TypeError y DetailError coinciden en TicketCreateRequest y TicketCreatedEvent,
      // por lo que no es necesaria ninguna configuración adicional con .ForMember().
      CreateMap<TicketCreateRequest, TicketCreatedEvent>();
    }
  }
}
