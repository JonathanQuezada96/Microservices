// Punto de entrada para el microservicio de Lectura (Ticketing.Query).
// Este servicio se enfoca 100% en recuperar y servir datos desde PostgreSQL.
using Tiketing.Query.Application.Extensions;
using Tiketing.Query.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

await app.ApplyMigration();
app.Run();