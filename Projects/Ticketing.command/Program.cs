using Scalar.AspNetCore;
using Ticketing.command.Application;
using Ticketing.command.Features.Apis;
using Ticketing.command.Infrastructure;

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
  
  // Scalar es una interfaz de usuario moderna (alternativa a Swagger) para visualizar y probar APIs.
  // Aquí estamos configurando cómo se verá la documentación interactiva de nuestra API.
  app.MapScalarApiReference(opt =>
  {
    opt.Title = "Microservice Command con scalar"; // Título de la página de documentación
    opt.DarkMode = true; // Forzar el modo oscuro para una mejor apariencia
    opt.Theme = ScalarTheme.Purple; // Definimos el tema de color púrpura
    opt.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.Http11); // Cliente por defecto para probar la API desde la interfaz
  });
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

// Escanea el proyecto y registra automáticamente todos los endpoints definidos 
// en las clases que implementan IMinimalApi (como nuestro TicketCreate.cs).
app.MapMinimalApisEndpoints();

// Arrancamos la aplicación web para que empiece a escuchar peticiones HTTP entrantes.
app.Run();

