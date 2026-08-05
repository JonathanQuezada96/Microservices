using Common.Core.Events;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tiketing.Query.Infrastructure.Converters
{
  // Conversor personalizado de JSON para deserializar polimórficamente eventos de dominio.
  //
  // PROBLEMA que resuelve: cuando Kafka envía un mensaje, el JSON contiene un evento concreto
  // (ej: TicketCreatedEvent), pero al deserializar solo conocemos el tipo base (BaseEvent).
  // System.Text.Json por defecto no sabe qué tipo concreto instanciar.
  //
  // SOLUCIÓN (patrón "Type Discriminator"): el JSON incluye un campo "Type" con el nombre
  // de la clase concreta. Este converter lee ese campo primero, y luego deserializa al tipo
  // correcto usando un switch expression.
  //
  // Hereda de JsonConverter<BaseEvent> para que System.Text.Json lo aplique automáticamente
  // cada vez que intente deserializar algo de tipo BaseEvent.
  public class EventJsonConverter : JsonConverter<BaseEvent>
  {

    // CanConvert: le dice a System.Text.Json en qué casos debe usar este converter.
    // Retorna true cuando el tipo a convertir es BaseEvent o cualquier tipo que lo herede.
    public override bool CanConvert(Type type)
    {
      // typeof(BaseEvent).IsAssignableFrom(type): ¿hereda 'type' de BaseEvent?
      // Ej: CanConvert(typeof(TicketCreatedEvent)) → true ✅
      // Antes estaba invertido: type.IsAssignableFrom(typeof(BaseEvent)) → casi siempre false ❌
      return typeof(BaseEvent).IsAssignableFrom(type);
    }

    // Read: lógica principal de deserialización.
    // Recibe el JSON raw (como Utf8JsonReader) y debe devolver una instancia de BaseEvent.
    public override BaseEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      // Primero parseamos el JSON completo en un documento para poder inspeccionarlo.
      if (!JsonDocument.TryParseValue(ref reader, out var document))
      {
        throw new JsonException($"Failed to parse JSON document {nameof(JsonDocument)}.");
      }

      // Leemos el campo "Type" del JSON — este campo nos dice qué clase concreta debemos instanciar.
      // Ejemplo de JSON: {{ "Type": "TicketCreatedEvent", "Username": "...", ... }}
      if (!document.RootElement.TryGetProperty("Type", out var type))
      {
        throw new JsonException($"Failed to get property {nameof(type)} from JSON document.");
      }

      // Extraemos el valor del campo "Type" como string (ej: "TicketCreatedEvent")
      var typeDiscriminator = type.GetString();
      // Obtenemos el JSON completo como texto para re-deserializar al tipo concreto
      var json = document.RootElement.GetRawText();

      // Switch expression: según el valor de "Type", deserializamos al tipo concreto correcto.
      //
      // IMPORTANTE: NO se pasa `options` aquí adrede.
      // Si pasáramos `options`, el EventJsonConverter volvería a activarse para TicketCreatedEvent
      // y TicketUpdatedEvent (porque también heredan de BaseEvent y CanConvert devuelve true),
      // causando una recursión infinita → StackOverflowException.
      // Sin `options`, System.Text.Json usa su comportamiento por defecto para los tipos concretos.
      return typeDiscriminator switch
      {
        nameof(TicketCreatedEvent) => JsonSerializer.Deserialize<TicketCreatedEvent>(json),
        nameof(TicketUpdatedEvent) => JsonSerializer.Deserialize<TicketUpdatedEvent>(json),
        
        // Si el "Type" dice "TicketDeletedEvent", entonces lo convertimos (deserializamos)
        // a nuestra clase C# TicketDeletedEvent, que es la que se acaba de agregar.
        nameof(TicketDeletedEvent) => JsonSerializer.Deserialize<TicketDeletedEvent>(json),
        
        _ => throw new JsonException($"{typeDiscriminator} no es soportado")
      };

    }

    // Write: lógica de serialización (convertir objeto → JSON).
    // No implementado porque el Query Side solo CONSUME eventos, nunca los produce.
    public override void Write(Utf8JsonWriter writer, BaseEvent value, JsonSerializerOptions options)
    {
      throw new NotImplementedException();
    }
  }
}
