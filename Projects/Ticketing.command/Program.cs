using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Ticketing.command.Application;
using Ticketing.command.Features.Apis;
using Ticketing.command.Features.Tickets;
using Ticketing.command.Infrastructure;
using static Ticketing.command.Features.Tickets.TicketCreate;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.RegisterMinimalApis();
// --- REGISTRO DE CAPAS ---
// Se conectan las dos capas del microservicio al contenedor de DI:
//   - AddInfrastructureServices → MongoDB, repositorios, BsonClassMap
//   - AddApplicationServices    → MediatR, FluentValidation, AutoMapper, MongoSettings
// Separar los registros por capa mantiene Program.cs limpio y cada capa independiente.
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // MapOpenApi expone la documentación OpenAPI/Swagger solo en desarrollo.
    // En producción este endpoint debería estar deshabilitado por seguridad.
    app.MapOpenApi();
}

app.UseHttpsRedirection();


// --- ENDPOINT REAL: Crear un Ticket ---
// MapPost define un endpoint HTTP POST en /api/ticket.
// Minimal APIs de .NET inyectan los parámetros automáticamente desde el DI (IMediator)
// y desde el body del request (TicketCreateRequest, deserializado desde JSON).
//app.MapPost("/api/ticket", async (
//  IMediator mediator,
//  TicketCreateRequest request,
//  CancellationToken cancellationToken
//  ) =>
//{
//  var command = new TicketCreateCommand(request);

//  // Envía el comando a MediatR, que lo enruta al TicketCreateCommandHandler.
//  // El handler ejecuta la lógica de negocio y devuelve true si el ticket se guardó.
//  var result = await mediator.Send(command, cancellationToken);
//  return Results.Ok(result);
//}).WithName("CreateTicket");

//app.MapMinimalApisEndpoints();
//app.Run();

// Record de prueba de la plantilla por defecto — puede eliminarse cuando
// se implementen los endpoints reales del microservicio.
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
