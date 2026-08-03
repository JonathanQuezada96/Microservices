using Common.Core.Events;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Infrastructure.Converters;

namespace Tiketing.Query.Infrastructure.Consumer
{
  // Servicio en segundo plano que escucha mensajes de Kafka continuamente.
  //
  // MEJORA: Ahora hereda de BackgroundService en lugar de implementar IHostedService directamente.
  // BackgroundService es la clase base recomendada por Microsoft para servicios en segundo plano.
  //
  // Ventajas clave sobre el IHostedService anterior:
  //   - StartAsync() retorna INMEDIATAMENTE (no bloquea el arranque del servidor HTTP).
  //   - El bucle pesado corre en ExecuteAsync(), que se lanza en un Task separado.
  //   - StopAsync() cancela el CancellationToken automáticamente → limpieza garantizada.
  //   - Ya no es necesario implementar StopAsync() manualmente.
  //
  // Flujo:
  //   App arranca → StartAsync() (BackgroundService) → lanza ExecuteAsync() en background
  //   → ConsumeLoop() espera mensajes de Kafka → ProcessMessageAsync() por cada mensaje
  //   → App se apaga → stoppingToken se cancela → ConsumeLoop() sale del while → consumer.Close()
  public class ComsumerHostedService : BackgroundService
  {
    private readonly ILogger<ComsumerHostedService> _logger;

    // IServiceProvider es necesario porque IEventHandler es un servicio Scoped,
    // pero este BackgroundService es registrado como Singleton.
    // Solución: creamos un IServiceScope manualmente por cada mensaje para obtener
    // el IEventHandler sin violar las reglas del ciclo de vida de DI.
    private readonly IServiceProvider _serviceProvider;

    // Configuración de Kafka (bootstrap servers, group id, etc.) leída del appsettings.json.
    private readonly ConsumerConfig _config;

    // MEJORA: El nombre del topic ya no está hardcodeado en el código.
    // Viene de appsettings.json → KafkaSettings:TopicName.
    // Así podemos cambiar el topic en producción sin recompilar.
    private readonly string _topic;

    public ComsumerHostedService(
      ILogger<ComsumerHostedService> logger,
      IOptions<ConsumerConfig> config,
      IServiceProvider serviceProvider,
      IConfiguration configuration)
    {
      _logger = logger;
      // IOptions<T> es la forma idiomática de .NET para inyectar configuración tipada.
      _config = config.Value;
      _serviceProvider = serviceProvider;
      // Si la clave no existe en appsettings.json, usamos "TICKET_EVENTS" como fallback seguro.
      _topic = configuration["KafkaSettings:TopicName"] ?? "TICKET_EVENTS";
    }

    // ExecuteAsync es el método central de BackgroundService.
    // .NET lo invoca automáticamente en un hilo separado apenas arranca la aplicación.
    // stoppingToken se cancela automáticamente cuando la app recibe señal de apagado
    // (Ctrl+C, SIGTERM de Docker/Kubernetes, reinicio de IIS, etc.)
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation("Consumer de Kafka iniciado. Escuchando topic: {Topic}", _topic);

      // Usamos Task.Run para mover la llamada bloqueante consumer.Consume() al ThreadPool.
      // Sin esto, aunque ExecuteAsync sea async, la primera llamada Consume() bloquearía
      // el hilo del host e impediría que otros hosted services arranquen correctamente.
      await Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

      _logger.LogInformation("Consumer de Kafka detenido limpiamente.");
    }

    // Método privado que contiene el bucle principal de consumo de Kafka.
    // Está separado de ExecuteAsync para mejorar la legibilidad y permitir
    // futuras pruebas unitarias (se puede probar ConsumeLoop independientemente).
    private async Task ConsumeLoop(CancellationToken stoppingToken)
    {
      // ConsumerBuilder construye el cliente de Kafka con la configuración inyectada.
      // "using" garantiza que el consumer se cierre y libere todos los recursos al salir.
      using var consumer = new ConsumerBuilder<string, string>(_config)
                          .SetKeyDeserializer(Deserializers.Utf8)
                          .SetValueDeserializer(Deserializers.Utf8)
                          .Build();

      // Nos suscribimos al topic para empezar a recibir mensajes del broker de Kafka.
      consumer.Subscribe(_topic);
      _logger.LogInformation("Suscrito exitosamente al topic '{Topic}'.", _topic);

      // El bucle corre mientras la app esté activa.
      // stoppingToken.IsCancellationRequested se vuelve true cuando la app se apaga.
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          // Consume() es bloqueante: espera hasta que llega un mensaje O se cancela el token.
          // Si el token se cancela, lanza OperationCanceledException (capturada abajo).
          var consumeResult = consumer.Consume(stoppingToken);

          // Guardias de seguridad: ignoramos mensajes vacíos o sin cuerpo.
          if (consumeResult is null || consumeResult.Message is null) continue;

          _logger.LogInformation(
            "Mensaje recibido | Topic: {Topic} | Offset: {Offset} | Valor: {Value}",
            consumeResult.Topic,
            consumeResult.Offset.Value,
            consumeResult.Message.Value);

          // MEJORA: Procesamos el mensaje en un método separado.
          // Si ProcessMessageAsync lanza una excepción, el catch la captura Y el bucle continúa.
          // Antes, un solo mensaje fallido mataba todo el consumer para siempre.
          await ProcessMessageAsync(consumeResult.Message.Value);

          // Confirmamos el offset SOLO después de procesar exitosamente.
          // Esto implementa "at-least-once delivery":
          //   - Si el proceso se cae antes del Commit, Kafka reentrega el mensaje al reiniciar.
          //   - Es preferible procesar un mensaje dos veces (idempotencia) a perderlo.
          consumer.Commit(consumeResult);

          _logger.LogInformation(
            "Mensaje procesado y confirmado | Offset: {Offset}",
            consumeResult.Offset.Value);
        }
        catch (OperationCanceledException)
        {
          // Esta excepción es ESPERADA cuando el stoppingToken se cancela (apagado limpio).
          // No es un error — es la señal de que debemos detener el bucle.
          _logger.LogInformation("Señal de cancelación recibida. Saliendo del bucle de consumo.");
          break;
        }
        catch (ConsumeException ex)
        {
          // Error al leer de Kafka: problemas de red, broker caído, deserialización de Kafka, etc.
          // Loggeamos el error pero CONTINUAMOS el bucle — el consumer intentará el siguiente mensaje.
          _logger.LogError(ex,
            "Error al consumir mensaje de Kafka. Razón: {Reason}", ex.Error.Reason);
        }
        catch (Exception ex)
        {
          // Error inesperado al procesar el mensaje: JSON inválido, error en el Handler, etc.
          // Loggeamos detalladamente y CONTINUAMOS — no sacrificamos el consumer por un mensaje malo.
          _logger.LogError(ex,
            "Error inesperado al procesar mensaje. El consumer continuará con el siguiente mensaje.");
        }
      }

      // Cerramos el consumer de forma limpia al salir del bucle.
      // Close() notifica al broker de Kafka que este consumer salió del grupo (consumer group rebalance).
      // Esto permite que otros consumers del mismo grupo tomen los partitions de inmediato,
      // sin esperar el timeout de sesión (típicamente 30-45 segundos).
      consumer.Close();
      _logger.LogInformation("Consumer de Kafka cerrado limpiamente.");
    }

    // Método que encapsula el procesamiento completo de un solo mensaje JSON de Kafka.
    // Es privado y async porque el EventHandler y MediatR son operaciones asíncronas.
    // Al estar separado del bucle, el manejo de errores es más claro y específico.
    private async Task ProcessMessageAsync(string messageValue)
    {
      // Configuramos las opciones de deserialización con nuestro converter personalizado.
      // EventJsonConverter resuelve el polimorfismo inspeccionando el campo "Type" del JSON.
      var options = new JsonSerializerOptions
      {
        Converters = { new EventJsonConverter() }
      };

      // Deserializamos el string JSON al tipo base BaseEvent.
      // Gracias a EventJsonConverter, el objeto real en memoria será del tipo concreto correcto:
      // ej: si Type = "TicketCreatedEvent", el objeto será una instancia de TicketCreatedEvent.
      var @event = JsonSerializer.Deserialize<BaseEvent>(messageValue, options);
      ArgumentNullException.ThrowIfNull(@event,
        "El mensaje de Kafka no pudo deserializarse como BaseEvent. Verifica que el JSON incluya el campo 'Type'.");

      // Creamos un Scope de DI por cada mensaje.
      // IEventHandler es Scoped → necesita un nuevo DbContext por operación.
      // Al terminar el "using", el scope y todos sus servicios Scoped se liberan correctamente.
      using IServiceScope scope = _serviceProvider.CreateScope();
      var eventHandler = scope.ServiceProvider.GetRequiredService<IEventHandler>();

      // REFLEXIÓN: buscamos dinámicamente el método "On" que acepta el tipo concreto del evento.
      // Ejemplo: si @event.GetType() == TicketCreatedEvent, buscamos: Task On(TicketCreatedEvent @event)
      // Ventaja: no necesitamos un switch/if-else que habría que actualizar cada vez que
      // se agrega un nuevo tipo de evento al sistema.
      var handlerMethod = eventHandler.GetType()
                            .GetMethod("On", new Type[] { @event.GetType() });

      if (handlerMethod is null)
      {
        // Si no hay método "On" para este tipo, es un error de programación.
        // El desarrollador olvidó agregar el método al IEventHandler y su implementación.
        throw new InvalidOperationException(
          $"No existe el método 'Task On({@event.GetType().Name} @event)' en el EventHandler. " +
          $"Agrega ese método al IEventHandler y a la clase EventHandler para soportar este tipo de evento.");
      }

      // MEJORA CRÍTICA: Awaitar correctamente el Task devuelto por la invocación por reflexión.
      //
      // ANTES (código roto):
      //   handlerMethod.Invoke(eventHandler, new object[] { @event });
      //   → El resultado (un Task) se descartaba silenciosamente (fire-and-forget sin querer).
      //   → Los errores del Handler NUNCA se propagaban → bugs silenciosos en producción.
      //
      // AHORA (correcto):
      //   Casteamos el resultado a Task y lo awaita explícitamente.
      //   → Los errores del Handler sí se propagan → se capturan en el catch del bucle → se loggean.
      var resultTask = (Task)handlerMethod.Invoke(eventHandler, new object[] { @event })!;
      await resultTask;
    }
  }
}
