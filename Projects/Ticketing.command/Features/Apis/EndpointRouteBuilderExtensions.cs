namespace Ticketing.command.Features.Apis
{
  public static class EndpointRouteBuilderExtensions
  {
    public static IEndpointRouteBuilder MapMinimalApisEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
      var minimalApis = endpointRouteBuilder.ServiceProvider.GetServices<IMinimalApi>();
      foreach (IMinimalApi minimalApi in minimalApis)
      {
        minimalApi.AddEndpoint(endpointRouteBuilder);
      }
      return endpointRouteBuilder;
    }
  }
}
