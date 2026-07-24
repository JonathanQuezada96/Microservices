using System.Collections;
using Tiketing.Query.Domain.Abstractions;

using Tiketing.Query.Infrastructure.Repos;

namespace Tiketing.Query.Infrastructure.Persistence
{
  // Implementación del Unit Of Work. 
  // Cachea las instancias de los repositorios en una tabla hash para no instanciarlos múltiples veces por request.
  public class UnitOfWork : IUnitOfWork
  {
    private Hashtable _repositories = new();
    private readonly DatabaseContextFactory _contextFactory;
    private readonly TicketDbContext _context;

    public UnitOfWork(DatabaseContextFactory contextFactory)
    {
      _contextFactory = contextFactory;
      _context = contextFactory.CreateDbContext();
    }
    public async Task<int> Complete()
    {
      return await _context.SaveChangesAsync();
    }

    public IGenericRepository<TEntity> RepositoryGeneric<TEntity>() where TEntity : class
    {
      if(_repositories is null)
      {
        _repositories = new Hashtable();
      }
      var type = typeof(TEntity).Name;
      if (!_repositories.Contains(type))
      {
        // BUG FIX: Activator necesita la implementación concreta (GenericRepo<>), no la interfaz.
        var repositoryType = typeof(GenericRepo<>);
        var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(TEntity)), _context);
        _repositories.Add(type, repositoryInstance);
      }
      return (IGenericRepository<TEntity>)_repositories[type]!;
    }
  }
}
