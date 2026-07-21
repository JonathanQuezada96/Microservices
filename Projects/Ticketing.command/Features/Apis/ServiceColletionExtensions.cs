using System.Reflection;

namespace Ticketing.command.Features.Apis
{
  public static class ServiceColletionExtensions
  {
    public static IServiceCollection RegisterMinimalApis(this IServiceCollection services)
    {
      var currenAssembly = Assembly.GetExecutingAssembly();
      var minimalApis = currenAssembly.GetTypes().Where
        (
          t => typeof(IMinimalApi).IsAssignableFrom(t) &&
          t != typeof(IMinimalApi) &&
          t.IsPublic &&
          !t.IsAbstract
        );
      foreach(var minimalApi in minimalApis)
      {
        services.AddSingleton(typeof(IMinimalApi), minimalApi);
      }
      return services;
    }
  }
}
