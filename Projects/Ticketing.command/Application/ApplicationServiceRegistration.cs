using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.command.Application.Core;
using Ticketing.command.Application.Models;

namespace Ticketing.command.Application
{
  /// <summary>
  /// Clase estática que centraliza el registro de todos los servicios de la capa de Application
  /// en el contenedor de Inyección de Dependencias (DI) de ASP.NET Core.
  ///
  /// La capa de Application contiene la lógica de negocio orquestada:
  /// comandos, queries, validaciones y mappings. No conoce detalles de HTTP ni de base de datos.
  /// </summary>
  public static class ApplicationServiceRegistration
  {
    /// <summary>
    /// Método de extensión sobre IServiceCollection que registra los servicios de Application.
    /// Se invoca desde Program.cs: builder.Services.AddApplicationServices(configuration)
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
      // --- CONFIGURACIÓN DE MONGOSETTINGS (Patrón Options) ---
      // Configure<T> enlaza la sección "MongoSettings" de appsettings.json
      // al POCO MongoSettings, disponible mediante IOptions<MongoSettings> en cualquier clase.
      // nameof(MongoSettings) → "MongoSettings" (evita strings mágicos hardcodeados)
      services.Configure<MongoSettings>(
          configuration.GetSection(nameof(MongoSettings)));

      // --- REGISTRO DE MEDIATR ---
      // MediatR implementa el patrón Mediator: desacopla quién envía un comando/query
      // de quién lo procesa. El endpoint solo hace mediator.Send(command) sin saber
      // qué handler lo va a manejar.
      // RegisterServicesFromAssembly escanea el ensamblado en busca de todos los
      // IRequestHandler<,> y los registra automáticamente en el DI.
      services.AddMediatR(cfg =>
      {
        cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
      });

      // --- REGISTRO DE FLUENTVALIDATION ---
      // AddValidatorsFromAssembly escanea el ensamblado y registra automáticamente
      // todos los AbstractValidator<T> encontrados (ej: TicketCreateCommandValidator).
      // FluentValidation puede integrarse con MediatR via pipeline behaviors para
      // validar comandos ANTES de que lleguen al handler.
      services.AddValidatorsFromAssembly(
          typeof(ApplicationServiceRegistration).Assembly);

      // --- REGISTRO DE AUTOMAPPER ---
      // AutoMapper convierte objetos entre capas sin escribir asignaciones manuales.
      // Aquí se le indica que cargue los perfiles de mapeo (MappingProfile) del ensamblado.
      // Ejemplo: TicketCreateRequest → TicketCreatedEvent (definido en MappingProfile)
      services.AddAutoMapper(typeof(MappingProfile).Assembly);

      return services;
    }
  }
}
