using Microsoft.EntityFrameworkCore;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Infrastructure.Persistence;

namespace Tiketing.Query.Infrastructure.Repos
{
  // Implementación del Repositorio Genérico.
  // Usa el TicketDbContext para encapsular las operaciones de lectura y escritura.
  public class GenericRepo<T> : IGenericRepository<T> where T : class
  {
    private readonly TicketDbContext _context;

    public GenericRepo(TicketDbContext context)
    {
      _context = context;
    }
    public void AddEntity(T entity)
    {
      _context.Set<T>().Add(entity);
    }

    public void DeleteEntity(T entity)
    {
      _context.Set<T>().Remove(entity);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync()
    {
      return await _context.Set<T>().ToListAsync();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
      return await _context.Set<T>().FindAsync(id);
    }

    public void UpdateEntity(T entity)
    {
     _context.Set<T>().Attach(entity);
      _context.Entry(entity).State = EntityState.Modified;
    }
  }
}
