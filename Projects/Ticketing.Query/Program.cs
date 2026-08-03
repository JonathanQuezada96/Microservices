// Punto de entrada para el microservicio de Lectura (Ticketing.Query).
// Este servicio se enfoca 100% en recuperar y servir datos desde PostgreSQL.
using Tiketing.Query.Application;
using Tiketing.Query.Application.Extensions;
using Tiketing.Query.Infrastructure;

// WebApplication.CreateBuilder configura el host de la aplicación con
// la configuración por defecto (logging, IConfiguration, IWebHostEnvironment, etc.)
var builder = WebApplication.CreateBuilder(args);

// Registramos todos los servicios de infraestructura (DbContext, repositorios, Kafka consumer, etc.)
builder.Services.RegisterInfrastructureServices(builder.Configuration);

// Registramos los servicios de aplicación (MediatR y sus handlers de CQRS).
builder.Services.RegisterApplicationServices();

// Habilitamos el soporte para Controladores MVC (para exponer endpoints HTTP REST).
builder.Services.AddControllers();

// Build() finaliza la configuración y construye la aplicación lista para ejecutarse.
var app = builder.Build();

// Habilita el middleware de autorización (requiere que el usuario esté autenticado si se configura).
// Incluso sin políticas definidas, es buena práctica incluirlo en el pipeline.
app.UseAuthorization();

// Mapea automáticamente todas las rutas definidas en los Controladores de la aplicación.
app.MapControllers();

// MEJORA: Exponemos el endpoint de Health Checks en la ruta /health.
// Responde con JSON indicando el estado de cada dependencia registrada:
//   - "Healthy": todo funciona correctamente.
//   - "Degraded": funciona pero con advertencias.
//   - "Unhealthy": hay un problema crítico (ej: PostgreSQL no responde).
//
// Ejemplo de respuesta JSON en /health:
// {
//   "status": "Healthy",
//   "entries": {
//     "postgresql": { "status": "Healthy", "duration": "00:00:00.012" }
//   }
// }
//
// Docker Compose / Kubernetes usa este endpoint en "healthcheck" para decidir
// si reiniciar el contenedor o marcarlo como listo para recibir tráfico.
app.MapHealthChecks("/health");

// Método de extensión propio que aplica las migraciones de EF Core automáticamente al iniciar.
// Esto crea o actualiza las tablas en PostgreSQL sin necesidad de ejecutar 'dotnet ef database update' manualmente.
await app.ApplyMigration();

// Inicia el servidor HTTP y el ComsumerHostedService de Kafka.
// Este método bloquea hasta que la aplicación se detiene.
app.Run();