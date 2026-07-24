namespace Tiketing.Query.Domain.Abstractions
{
  // Clase base abstracta para todas las entidades del dominio de lectura (Query).
  // Centraliza las propiedades de auditoría (quién y cuándo creó/modificó el registro).
  public abstract class Entity(Guid id)
  {
    // Constructor sin parámetros necesario para que Entity Framework Core 
    // pueda instanciar la clase mediante Reflexión al leer de la base de datos.
    protected Entity() : this(default)
    {
      
    }
    public Guid Id { get; set; } = id;
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModificateOn { get; set; }
    public string? LastModifieBy { get; set; }
  }
}
