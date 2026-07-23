using Common.Core.Events;

namespace Common.Core.Producers
{
  // Interfaz que define el contrato para publicar eventos en un Message Broker (ej. Kafka, RabbitMQ).
  // Separar esto en una interfaz permite que el dominio no dependa directamente de una tecnología específica.
  public interface IEventProducer
  {
    // Envía un evento de dominio a un "Tópico" (Topic) específico de forma asíncrona.
    Task ProduceAsync(string topic, BaseEvent @event);
  }
}
