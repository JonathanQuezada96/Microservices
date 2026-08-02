
using Common.Core.Events;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Infrastructure.Converters;

namespace Tiketing.Query.Infrastructure.Consumer
{
  // Servicio en segundo plano (Background Service) que escucha mensajes de Kafka continuamente.
  //
  // Implementa IHostedService: una interfaz de .NET que permite ejecutar lógica en el fondo
  // mientras la aplicación principal (API HTTP) sigue funcionando.
  // .NET llama automáticamente a StartAsync() cuando la app inicia, y StopAsync() cuando se detiene.
  //
  // Flujo de este servicio:
  //   1. Se conecta a Kafka y se suscribe al topic.
  //   2. Entra en un bucle infinito esperando mensajes.
  //   3. Al recibir un mensaje, deserializa el JSON usando EventJsonConverter (polimorfismo).
  //   4. Usa reflexión para encontrar el método "On" correcto en el EventHandler.
  //   5. Invoca ese método para que el EventHandler procese el evento (→ MediatR → PostgreSQL).
  //   6. Confirma (Commit) el mensaje a Kafka para que no sea procesado de nuevo.
  public class ComsumerHostedService : IHostedService
  {
    private readonly ILogger<ComsumerHostedService> _logger;
    // IServiceProvider es necesario porque IEventHandler es un servicio Scoped,
    // pero este HostedService es Singleton. Para obtener servicios Scoped desde un Singleton,
    // debemos crear un "scope" manualmente con CreateScope().
    private readonly IServiceProvider _serviceProvider;
    private readonly ConsumerConfig _config;
    public ComsumerHostedService(ILogger<ComsumerHostedService> logger, IOptions<ConsumerConfig> config, IServiceProvider serviceProvider)
    {
      _logger = logger;
      // IOptions<T> es la forma idiomática de .NET para leer configuración fuertemente tipada.
      // config.Value accede a la instancia concreta del ConsumerConfig leída del appsettings.json.
      _config = config.Value;
      _serviceProvider = serviceProvider;
    }

    // StartAsync se ejecuta cuando la aplicación arranca.
    // CancellationToken se activa cuando la app recibe señal de apagado (ej: Ctrl+C o Docker stop).
    public async Task StartAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("El event consumer está chambeando");

      var topic = "KAFKA_TOPIC";

      // ConsumerBuilder construye el consumidor de Kafka con la configuración inyectada.
      // SetKeyDeserializer y SetValueDeserializer indican que tanto la clave como el valor
      // del mensaje de Kafka son texto UTF-8 plano (el JSON del evento).
      // "using" garantiza que el consumer se cierre y libere recursos al salir del bloque.
      using var consumer = new ConsumerBuilder<string, string>(_config)
                          .SetKeyDeserializer(Deserializers.Utf8)
                          .SetValueDeserializer(Deserializers.Utf8)
                          .Build();

      // Nos suscribimos al topic de Kafka para empezar a recibir mensajes.
      consumer.Subscribe(topic);

      // Bucle infinito: el consumer permanece activo escuchando mensajes hasta que
      // el CancellationToken sea cancelado (apagado de la aplicación).
      while (true)
      {
        // Consume() es una llamada bloqueante: espera hasta que llega un mensaje o se cancela.
        var consumeResult = consumer.Consume(cancellationToken);
        if (consumeResult is null) continue;       // Mensaje vacío, ignorar
        if (consumeResult.Message is null) continue; // Sin cuerpo de mensaje, ignorar

        //if(consumeResult is not null)
        //{
        //  _logger.LogInformation($"Mensaje recibido: {consumeResult.Message.Value}");
        //  // Aquí puedes procesar el mensaje recibido
        //}

        // Configuramos las opciones de JSON con nuestro converter personalizado.
        // Esto le dice a System.Text.Json que use EventJsonConverter cuando
        // encuentre un tipo BaseEvent (o derivado) durante la deserialización.
        var options = new JsonSerializerOptions
        {
          Converters = { new EventJsonConverter() }
        };

        // Deserializamos el valor del mensaje (string JSON) a una instancia de BaseEvent.
        // Gracias al EventJsonConverter, el objeto resultante será del tipo concreto correcto
        // (ej: TicketCreatedEvent o TicketUpdatedEvent), aunque la variable sea BaseEvent.
        var @event = JsonSerializer.Deserialize<BaseEvent>(consumeResult.Message.Value, options);
        ArgumentNullException.ThrowIfNull(@event, "No se pudo procesar el parseo de json");

        // Creamos un Scope de DI para poder obtener el IEventHandler (que es Scoped).
        // Un Scope simula el ciclo de vida de una petición HTTP — al finalizar el using,
        // el scope se destruye y los servicios Scoped son liberados correctamente.
        using IServiceScope scope = _serviceProvider.CreateScope();
        var _evenHandler = scope.ServiceProvider.GetRequiredService<IEventHandler>();

        // REFLEXIÓN: buscamos dinámicamente el método "On" que acepta el tipo concreto del evento.
        // Por ejemplo, si @event es TicketCreatedEvent, buscamos: On(TicketCreatedEvent event)
        // Esto evita un gran switch/if-else y permite extender fácilmente con nuevos eventos.
        var handlerMethod = _evenHandler.GetType().GetMethod("On", new Type[] { @event.GetType() });

        if(handlerMethod is null)
        {
          // Si no hay un método "On" para este tipo de evento, es un error de programación.
          throw new ArgumentNullException(nameof(handlerMethod), "No se encontró el método On para el evento especificado.");
        }

        // Invocamos el método "On" encontrado por reflexión, pasando el evento como argumento.
        // Equivale a llamar: _evenHandler.On((TicketCreatedEvent)@event);
        handlerMethod.Invoke(_evenHandler, new object[] { @event });

        // Confirmamos (Commit) el offset del mensaje en Kafka.
        // Esto le dice a Kafka que este mensaje fue procesado exitosamente
        // y no debe ser entregado de nuevo en caso de reinicio del consumer.
        consumer.Commit(consumeResult);
      }

    }

    // StopAsync se llama cuando la aplicación se está apagando (Ctrl+C, Docker stop, etc.).
    // Aquí deberías liberar el consumer de Kafka y detener el bucle limpiamente.
    // Aún pendiente de implementar.
    public Task StopAsync(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }
}
