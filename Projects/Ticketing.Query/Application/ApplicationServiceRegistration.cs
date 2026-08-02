using System.Reflection;

namespace Tiketing.Query.Application
{
  // Clase estática que registra los servicios de la capa de Aplicación en el contenedor DI.
  //
  // En Clean Architecture, la capa de Aplicación contiene la lógica de negocio
  // (handlers de CQRS, validadores, etc.). Esta clase se encarga de registrar esos
  // servicios para que el sistema de Inyección de Dependencias de .NET los provea
  // automáticamente donde se necesiten.
  //
  // El método es un "Extension Method" sobre IServiceCollection — eso significa que
  // puedes llamarlo como si fuera un método propio de IServiceCollection en Program.cs:
  //   builder.Services.RegisterApplicationServices();
  public static class ApplicationServiceRegistration
  {
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
    {
      // Assembly.GetExecutingAssembly() obtiene el ensamblado actual (este proyecto).
      // Lo necesitamos para que MediatR sepa dónde buscar los Handlers (IRequestHandler<>).
      var currentAssembly = Assembly.GetExecutingAssembly();

      // Registra MediatR y le indica que busque automáticamente todos los Handlers
      // (clases que implementan IRequestHandler<TCommand, TResult>) en este ensamblado.
      // Gracias a esto, no es necesario registrar cada Handler manualmente.
      services.AddMediatR(m => m.RegisterServicesFromAssembly(currentAssembly));

      return services;
    }
  }
}
