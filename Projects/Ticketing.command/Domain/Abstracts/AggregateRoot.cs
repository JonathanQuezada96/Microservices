using Common.Core.Events;

namespace Ticketing.command.Domain.Abstracts
{
  // Esta es la clase base abstracta para todos los "Aggregate Roots" (Raíces de Agregado) de nuestro dominio.
  // Proporciona la funcionalidad básica que cualquier Agregado necesita en Event Sourcing, como rastrear
  // los eventos que han ocurrido antes de que se guarden en la base de datos.
  public abstract class AggregateRoot
  {
    // Identificador único del Agregado.
    protected string _id = string.Empty;
    public string Id
    {
      get { return _id; }
    }

    // La versión actual del Agregado. Es útil para evitar problemas de concurrencia
    // cuando múltiples usuarios intentan modificar el mismo Agregado a la vez (Optimistic Concurrency).
    public int Version { get; set; }

    // Lista temporal para guardar los eventos de dominio (cambios) que han sucedido,
    // pero que aún no se han persistido (guardado) en el "Event Store" (Base de datos).
    private readonly List<BaseEvent> _changes = new();

    // Método para obtener todos los eventos que aún no han sido guardados.
    // Esto lo usará el repositorio para saber qué debe persistir en la base de datos.
    public IEnumerable<BaseEvent> GetUncommitedChanges()
    {
      return _changes;
    }

    // Una vez que los cambios se han guardado exitosamente en la base de datos,
    // se llama a este método para vaciar la lista temporal de cambios en memoria.
    public void MarkChangesAsCommited()
    {
      _changes.Clear();
    }
    public void ApplyChange(BaseEvent @event, bool isNewEvent)
    {
      var method = GetType().GetMethod("Apply", [@event.GetType()]);
      if (method is null)
      {
        throw new ArgumentNullException(nameof(method), $"El metodo Apply no fue encontrado dentro de {@event.GetType().Name}");
      }
      method.Invoke(this, [@event]);
      if (isNewEvent)
      {
        _changes.Add(@event);
      }
    }
    public void RaiseEvent(BaseEvent @event)
    {
      ApplyChange(@event, true);
    }
    public void ReplayEvents(IEnumerable<BaseEvent> events)
    {
      foreach (var @event in events)
      {
        ApplyChange(@event, false);
      }
    }
  }
}
