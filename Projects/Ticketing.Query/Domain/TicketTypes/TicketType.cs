using Common.Core.Domain;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Tiketing.Query.Domain.TicketTypes
{
  // Implementación del patrón "Smart Enum" o Enumeración Fuertemente Tipada.
  // En lugar de usar un simple enum de C#, usamos una clase que nos permite 
  // tener comportamiento y mapearse fácilmente a una tabla de catálogo en EF Core.
  public class TicketType
  {
    private TicketType()
    {
      
    }
    [SetsRequiredMembers]
    private TicketType(int id, string name) => (Id, Name) = (id, name);
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    public static TicketType Create(int id)
    {
      var ticketTypeEnum = (TicketTypeEnum)id;
      string stringValue = ticketTypeEnum.ToString();

      return new TicketType(id, stringValue);
    }
  }
}
