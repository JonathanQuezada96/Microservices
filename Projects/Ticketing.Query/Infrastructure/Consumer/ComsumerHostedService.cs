// =========================================================================
// CLASE: ConsumerHostedService
// PROPÓSITO: Es un servicio en segundo plano (Background Service) de .NET.
//
// ¿Qué es un IHostedService?
// Es una interfaz de .NET que te permite correr lógica en paralelo al servidor web,
// sin necesidad de atender una petición HTTP. Cuando la aplicación arranca,
// .NET llama automáticamente a StartAsync(). Cuando se detiene, llama StopAsync().
//
// ¿Por qué lo necesitamos aquí?
// El consumidor de Kafka necesita estar siempre escuchando mensajes en un loop
// infinito. Si lo corriéramos en el hilo principal del servidor web, bloquearíamos
// toda la aplicación. Con IHostedService, corre en su propio hilo en paralelo.
//
// FLUJO COMPLETO DE COMUNICACIÓN ENTRE MICROSERVICIOS:
//   [Ticketing.command]         [Kafka]              [Ticketing.Query]
//   TicketAggregate             Broker               ConsumerHostedService
//       |                          |                         |
//       | -- RaiseEvent() -->      |                         |
//       | -- Kafka.Produce() ->    |                         |
//                                  | <-- Consume(topic) --   |
//                                  | -- mensaje JSON  -->    |
//                                                       EventConsumer
//                                                            |
//                                                       EventHandler
//                                                            |
//                                                       PostgreSQL
// =========================================================================

using Common.Core.Consumers;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Ticketing.Query.Infrastructure.Consumers;

public class ConsumerHostedService : IHostedService
{
  // Logger: para registrar en consola o archivo qué está pasando con el servicio.
  // Útil para saber cuándo arrancó, cuándo se detuvo, y si hubo errores.
  private readonly ILogger<ConsumerHostedService> _logger;

  // IServiceProvider: el contenedor de inyección de dependencias de .NET.
  // Lo usamos para crear instancias de servicios (como IEventConsumer)
  // en el momento correcto, dentro de un "Scope" apropiado.
  private readonly IServiceProvider _serviceProvider;

  // La configuración de Kafka (bootstrap servers, group ID, etc.) viene de appsettings.json.
  private readonly ConsumerConfig _config;

  // IConfiguration nos permite leer valores de appsettings.json en tiempo de ejecución.
  // Así podemos cambiar el nombre del topic sin recompilar el código.
  private readonly IConfiguration _configuration;

  public ConsumerHostedService(
      ILogger<ConsumerHostedService> logger,
      IServiceProvider serviceProvider,
      IOptions<ConsumerConfig> config,
      IConfiguration configuration
  )
  {
    _logger = logger;
    _serviceProvider = serviceProvider;
    _config = config.Value;
    _configuration = configuration;
  }

  // StartAsync: se llama automáticamente cuando la aplicación arranca.
  // Aquí iniciamos el loop de consumo de Kafka en un hilo aparte.
  public Task StartAsync(CancellationToken cancellationToken)
  {
    // Leemos el nombre del topic desde appsettings.json (sección KafkaSettings:TopicName).
    // ANTES estaba hardcodeado como "KAFKA_TOPIC" — eso era un bug: el consumer nunca
    // encontraría el topic real ("TICKET_EVENTS") y no recibiría ningún evento.
    var topic = _configuration["KafkaSettings:TopicName"]
                ?? throw new InvalidOperationException(
                   "KafkaSettings:TopicName no está configurado en appsettings.json");

    _logger.LogInformation(
      "ConsumerHostedService arrancando. Escuchando el topic de Kafka: {Topic}", topic);

    // =========================================================================
    // BUG FIX CRÍTICO: Comunicación entre microservicios
    // =========================================================================
    // ANTES (código roto):
    //   using (IServiceScope scope = _serviceProvider.CreateScope()) {
    //       var eventConsumer = scope.ServiceProvider.GetRequiredService<IEventConsumer>();
    //       Task.Run(() => eventConsumer.Consume(topic), cancellationToken);
    //   }  ← El 'using' cierra el scope AQUÍ, antes de que Task.Run termine.
    //      Esto destruye el IEventConsumer y todo su contexto de base de datos.
    //      Resultado: el consumer arranca pero falla silenciosamente sin recibir eventos.
    //
    // AHORA (código correcto):
    //   Creamos el scope SIN 'using' para que viva mientras el background task corre.
    //   El scope se cerrará cuando la aplicación se detenga.
    // =========================================================================
    var scope = _serviceProvider.CreateScope();
    var eventConsumer = scope.ServiceProvider.GetRequiredService<IEventConsumer>();

    // Task.Run lanza el loop de Kafka en un hilo del ThreadPool (fuera del hilo principal).
    // Así el servidor web puede seguir atendiendo peticiones HTTP normalmente.
    Task.Run(() => eventConsumer.Consume(topic), cancellationToken);

    // Retornamos Task.CompletedTask porque StartAsync solo inicia el background task.
    // El trabajo real ocurre en el hilo de Task.Run de forma indefinida.
    return Task.CompletedTask;
  }

  // StopAsync: se llama cuando la aplicación se está cerrando (Ctrl+C, Docker stop, etc.)
  // Aquí podríamos cerrar conexiones o liberar recursos si fuera necesario.
  public Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("ConsumerHostedService detenido. Ya no se consumen eventos de Kafka.");
    return Task.CompletedTask;
  }
}
