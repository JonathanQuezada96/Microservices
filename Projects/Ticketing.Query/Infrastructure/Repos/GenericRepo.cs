using Microsoft.EntityFrameworkCore;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Infrastructure.Persistence;

namespace Tiketing.Query.Infrastructure.Repos
{
  // Implementación del Repositorio Genérico.
  // Usa el TicketDbContext para encapsular las operaciones de lectura y escritura.
  //
  // El parámetro de tipo T es genérico y restringido a 'class' (tipos de referencia).
  // Esto permite reutilizar esta clase para cualquier entidad: Ticket, Employee, TicketEmployee, etc.
  // El patrón se llama "Repository Pattern" y sirve para aislar la lógica de acceso a datos.
  public class GenericRepo<T> : IGenericRepository<T> where T : class
  {
    // El DbContext es el puente entre el código C# y la base de datos PostgreSQL.
    // 'protected' permite que las clases hijas (como EmployeeRepository) accedan a él.
    protected readonly TicketDbContext _context;

    // Inyección de dependencias: el DbContext lo provee el contenedor de DI de .NET.
    public GenericRepo(TicketDbContext context)
    {
      _context = context;
    }

    // Marca la entidad como "Added" en el contexto de EF Core.
    // Los cambios se guardan en la BD solo cuando se llama _context.SaveChangesAsync()
    // (que es lo que hace UnitOfWork.Complete()).
    public void AddEntity(T entity)
    {
      _context.Set<T>().Add(entity);
    }

    // Marca la entidad como "Deleted" en EF Core.
    // Al llamar SaveChangesAsync(), EF Core ejecutará un DELETE en la BD.
    public void DeleteEntity(T entity)
    {
      _context.Set<T>().Remove(entity);
    }

    // Obtiene todos los registros de tipo T de la BD de forma asíncrona.
    // ToListAsync() ejecuta: SELECT * FROM [tabla] y lo retorna como lista de solo lectura.
    // IReadOnlyList evita que el llamador modifique la colección devuelta.
    public async Task<IReadOnlyList<T>> GetAllAsync()
    {
      return await _context.Set<T>().ToListAsync();
    }

    // Busca un registro por su clave primaria (Guid id) de forma asíncrona.
    // FindAsync es más eficiente que FirstOrDefaultAsync porque primero busca
    // en el caché local del DbContext antes de ir a la BD.
    // Retorna null si no encuentra el registro (por eso T?).
    public async Task<T?> GetByIdAsync(Guid id)
    {
      return await _context.Set<T>().FindAsync(id);
    }

    // Marca la entidad como "Modified" en EF Core.
    // Attach() adjunta la entidad al contexto (en caso de que no lo estuviera),
    // y luego EntityState.Modified fuerza a EF a actualizar TODAS las columnas del registro.
    public void UpdateEntity(T entity)
    {
     _context.Set<T>().Attach(entity);
      _context.Entry(entity).State = EntityState.Modified;
    }
  }
}
