// =========================================================================
// CLASE: EventConsumer
// PROPÓSITO: Es el consumidor real de mensajes de Kafka.
//
// Implementa IEventConsumer (definido en Common.Core).
// Su único trabajo es: conectarse a Kafka, escuchar mensajes en un topic,
// deserializarlos al tipo de evento correcto y pasarlos al EventHandler
// para que actualice la base de datos de lectura (PostgreSQL).
//
// RELACIÓN CON ConsumerHostedService:
// ConsumerHostedService (el IHostedService) arranca EventConsumer en un hilo aparte.
// ConsumerHostedService → llama → EventConsumer.Consume(topic)
//                                         ↓ (loop infinito)
//                               Lee mensaje JSON de Kafka
//                                         ↓
//                               Deserializa a BaseEvent (con EventJsonConverter)
//                                         ↓
//                               Usa Reflexión para llamar EventHandler.On(evento)
//                                         ↓
//                               MediatR → TicketCreateCommandHandler / TicketUpdateCommandHandler / TicketDeleteCommandHandler
//                                         ↓
//                               PostgreSQL actualizado
// =========================================================================
using Common.Core.Consumers;
using Common.Core.Events;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Infrastructure.Converters;

namespace Ticketing.Query.Infrastructure.Consumers;

public class EventConsumer : IEventConsumer
{
  // La configuración de Kafka (bootstrap servers, group id, etc.) leída de appsettings.json.
  private readonly ConsumerConfig _config;

  // IServiceScopeFactory: nos permite crear "scopes" de inyección de dependencias.
  // Un scope garantiza que cada vez que creamos servicios (como IEventHandler),
  // tienen el ciclo de vida correcto (Scoped) y no se mezclan entre requests.
  private readonly IServiceScopeFactory _serviceProvider;

  // Logger para registrar qué eventos llegan y posibles errores.
  private readonly ILogger<EventConsumer> _logger;

  public EventConsumer(
      IOptions<ConsumerConfig> config,
      IServiceScopeFactory serviceProvider,
      ILogger<EventConsumer> logger)
  {
    _config = config.Value;
    _serviceProvider = serviceProvider;
    _logger = logger;
  }

  // Consume: el loop principal que escucha mensajes de Kafka indefinidamente.
  // Este método NUNCA termina por sí solo — solo para cuando la aplicación se cierra.
  public void Consume(string topic)
  {
    // Construimos el consumidor de Kafka usando la configuración de appsettings.json.
    // ConsumerBuilder<string, string>: la clave y el valor del mensaje son strings (JSON).
    using var consumer = new ConsumerBuilder<string, string>(_config)
                     .SetKeyDeserializer(Deserializers.Utf8)    // Clave del mensaje como texto UTF-8
                     .SetValueDeserializer(Deserializers.Utf8)  // Valor (JSON del evento) como texto UTF-8
                     .Build();

    // Le decimos a Kafka a qué topic queremos suscribirnos.
    // Desde este momento, Kafka empezará a enviarnos mensajes de ese topic.
    consumer.Subscribe(topic);

    _logger.LogInformation("EventConsumer suscrito al topic '{Topic}'. Esperando mensajes...", topic);

    // Loop infinito: seguimos consumiendo mensajes hasta que la aplicación se cierre.
    while (true)
    {
      // consumer.Consume() BLOQUEA el hilo hasta que llegue un nuevo mensaje.
      // Esto está bien porque corremos en un hilo aparte (Task.Run en ConsumerHostedService).
      var consumeResult = consumer.Consume();

      // Validaciones de seguridad: si el mensaje está vacío, lo ignoramos.
      if (consumeResult is null) continue;
      if (consumeResult.Message is null) continue;

      _logger.LogInformation(
        "Mensaje recibido de Kafka. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
        consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);

      // =========================================================================
      // DESERIALIZACIÓN POLIMÓRFICA
      // =========================================================================
      // El JSON del mensaje se ve así:
      //   { "Type": "TicketCreatedEvent", "Username": "...", "DetailError": "..." }
      //
      // El problema es que solo sabemos que es un BaseEvent, no el tipo concreto.
      // EventJsonConverter lee el campo "Type" y decide a qué clase C# deserializar.
      // =========================================================================
      var options = new JsonSerializerOptions
      {
        Converters = { new EventJsonConverter() }
      };

      var @event = JsonSerializer
                      .Deserialize<BaseEvent>(
                          consumeResult.Message.Value,
                          options
                      );

      if (@event is null)
      {
        _logger.LogWarning("No se pudo deserializar el mensaje de Kafka. Contenido: {Msg}", consumeResult.Message.Value);
        continue; // Mejor continuar que tirar una excepción que frene el consumer.
      }

      // =========================================================================
      // DESPACHO DINÁMICO CON REFLEXIÓN
      // =========================================================================
      // Aquí usamos reflexión (GetType, GetMethod, Invoke) para llamar al método
      // correcto del EventHandler sin necesitar un switch/if.
      //
      // Por ejemplo, si @event es un TicketCreatedEvent, llamamos:
      //   eventHandler.On(ticketCreatedEvent) → que llama al TicketCreateCommandHandler
      //
      // VENTAJA: si añades un nuevo tipo de evento, solo agrega On(NuevoEvento) al
      // IEventHandler y EventHandler, y este código lo llamará automáticamente.
      // =========================================================================
      using var scope = _serviceProvider.CreateScope();
      var eventHandler = scope.ServiceProvider.GetRequiredService<IEventHandler>();

      // Buscamos el método On() que acepte exactamente el tipo del evento recibido.
      var handlerMethod = eventHandler
                          .GetType()
                          .GetMethod("On", new Type[] { @event.GetType() });

      if (handlerMethod is null)
      {
        _logger.LogWarning(
          "No existe un handler para el tipo de evento '{EventType}'. Ignorando mensaje.",
          @event.GetType().Name);
        continue; // No podemos procesar este tipo de evento, pero no frenes el consumer.
      }

      // Invocamos el método On() de forma dinámica. Es equivalente a:
      //   eventHandler.On((TicketCreatedEvent)@event)  ← pero sin saber el tipo en tiempo de compilación.
      handlerMethod.Invoke(eventHandler, new object[] { @event });

      // COMMIT: Le confirmamos a Kafka que procesamos este mensaje exitosamente.
      // Solo hacemos commit si todo salió bien. Si hubiera error antes,
      // Kafka re-entregará el mensaje al reiniciar el consumer (exactly-once semantics).
      consumer.Commit(consumeResult);

      _logger.LogInformation(
        "Evento '{EventType}' procesado y commiteado exitosamente.", @event.GetType().Name);
    }
  }
}

