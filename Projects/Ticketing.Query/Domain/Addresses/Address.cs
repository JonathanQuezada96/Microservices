using System.ComponentModel.DataAnnotations.Schema;

namespace Tiketing.Query.Domain.Addresses
{
  // [ComplexType] le indica a Entity Framework Core que esta clase es un 
  // "Value Object" (Objeto de Valor) en términos de DDD.
  // No tiene identidad propia (ID), sino que sus propiedades se guardarán 
  // como columnas adicionales en la tabla de la entidad que lo contenga (ej. Employee).
  [ComplexType]
  public class Address
  {
    public string?  Street { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }

  }
}
