// =========================================================================
// INTERFAZ: IEventConsumer
// PROPÓSITO: Define el contrato para cualquier clase que quiera ser un
// "consumidor de eventos" en nuestro sistema.
//
// ¿Qué es una interfaz en C#?
// Es un contrato. Dice "cualquier clase que me implemente DEBE tener estos métodos".
// No tiene lógica adentro — solo define QUÉ debe hacer, no CÓMO.
//
// ¿Por qué usarla aquí?
// Desacoplamiento: el ConsumerHostedService no sabe nada de Kafka ni de Confluent.
// Solo sabe que necesita algo que tenga un método Consume(string topic).
// En el futuro, podrías cambiar Kafka por RabbitMQ simplemente creando una nueva
// implementación de IEventConsumer — sin tocar el ConsumerHostedService.
// =========================================================================
namespace Common.Core.Consumers;

public interface IEventConsumer
{
  // Consume: inicia el loop de lectura de mensajes en el topic especificado.
  // IMPORTANTE: este método se espera que corra indefinidamente (loop infinito)
  // en un hilo aparte, por eso lo llama ConsumerHostedService con Task.Run().
  void Consume(string topic);
}
