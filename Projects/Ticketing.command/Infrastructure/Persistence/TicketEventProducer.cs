using Common.Core.Events;
using Common.Core.Producers;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Ticketing.command.Application.Models;

namespace Ticketing.command.Infrastructure.Persistence
{
  // Implementación concreta del productor de eventos utilizando Apache Kafka (vía Confluent.Kafka).
  // Un "Productor" (Producer) es el encargado de publicar mensajes (nuestros Eventos de Dominio)
  // en un "Tópico" (Topic) para que otros microservicios (ej. el microservicio de Query) puedan consumirlos.
  public class TicketEventProducer : IEventProducer
  {
    private readonly KafkaSettings _kafkaSettings;
    public TicketEventProducer(IOptions<KafkaSettings> kafkaSettings)
    {
      _kafkaSettings = kafkaSettings.Value;
    }
    public async Task ProduceAsync(string topic, BaseEvent @event)
    {
      // 1. Configuración de conexión al clúster de Kafka usando los datos de appsettings.json
      var config = new ProducerConfig
      {
        BootstrapServers = $"{_kafkaSettings.Hostname}:{_kafkaSettings.Port}",
      };

      // 2. Construcción del Productor de Kafka.
      // Se define que tanto la llave (Key) como el valor (Value) del mensaje serán serializados como texto UTF-8.
      using var producer = new ProducerBuilder<string, string>(config)
        .SetKeySerializer(Serializers.Utf8)
        .SetValueSerializer(Serializers.Utf8)
        .Build();

      // 3. Creación del Mensaje de Kafka.
      // La "Key" (llave) ayuda a Kafka a garantizar el orden de los mensajes en las particiones.
      // El "Value" (valor) es nuestro evento de dominio serializado a formato JSON.
      var eventMessage = new Message<string, string>
      {
        Key = Guid.NewGuid().ToString(),
        Value = JsonConvert.SerializeObject(@event)
      };

      // 4. Se publica el mensaje de forma asíncrona en el tópico indicado.
      var deliveryStatus = await producer.ProduceAsync(topic, eventMessage);

      // 5. Se verifica si el mensaje realmente llegó y se guardó en Kafka.
      // Si falla, se lanza una excepción para evitar inconsistencias en el sistema.
      if (deliveryStatus.Status == PersistenceStatus.NotPersisted)
      {
        throw new Exception(
                              @$"No se pudo enviar el mensaje {@event.GetType().Name}, 
                              hacia el topic - {topic}, por la siguiente razon: 
                              {deliveryStatus.Message}");
      }
    }
  }
}
