// Punto de entrada para el microservicio de Lectura (Ticketing.Query).
// Este servicio se enfoca 100% en recuperar y servir datos desde PostgreSQL.
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

app.Run();