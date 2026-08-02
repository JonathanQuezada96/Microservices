using System.Collections;
using Tiketing.Query.Domain.Abstractions;
using Tiketing.Query.Domain.Employees;
using Tiketing.Query.Infrastructure.Repos;

namespace Tiketing.Query.Infrastructure.Persistence
{
  // Implementación del Unit Of Work. 
  // Cachea las instancias de los repositorios en una tabla hash para no instanciarlos múltiples veces por request.
  //
  // El Unit of Work coordina una "unidad de trabajo": un conjunto de operaciones que deben
  // confirmarse o revertirse juntas (transacción atómica). Si algo falla antes de Complete(),
  // ninguno de los cambios se persiste en la base de datos.
  public class UnitOfWork : IUnitOfWork
  {
    // Hashtable (diccionario) que guarda los repositorios ya creados, indexados por el nombre del tipo.
    // Ej: _repositories["Ticket"] = instancia de GenericRepo<Ticket>
    // Esto evita crear múltiples instancias del mismo repositorio en una sola request.
    private Hashtable _repositories = new();

    // Factory del DbContext — usada para crear el contexto en el constructor.
    // Es necesaria porque el ComsumerHostedService (background) crea su propio contexto
    // fuera del ciclo de vida normal de las peticiones HTTP.
    private readonly DatabaseContextFactory _contextFactory;

    // El DbContext compartido por todos los repositorios en esta unidad de trabajo.
    // Es el mismo objeto que asegura que todas las operaciones forman parte de la misma transacción.
    private readonly TicketDbContext _context;

    // Repositorio específico de empleados, inicializado de forma perezosa (Lazy Initialization).
    // El operador ??= asigna el valor solo la primera vez que se accede a la propiedad.
    // Así evitamos crear el repositorio hasta que realmente se necesite.
    private  IemployeeRepository? _employeeRepository;
    public IemployeeRepository EmployeeRepository => _employeeRepository ??= new EmployeeRepository(_context);

    // El constructor recibe la DatabaseContextFactory y crea el DbContext inmediatamente.
    // Todos los repositorios que se creen desde este UnitOfWork compartirán este mismo _context.
    public UnitOfWork(DatabaseContextFactory contextFactory)
    {
      _contextFactory = contextFactory;
      _context = contextFactory.CreateDbContext();
    }

    // Confirma todos los cambios pendientes en el DbContext a la base de datos.
    // SaveChangesAsync() genera y ejecuta las sentencias SQL (INSERT/UPDATE/DELETE)
    // en una única transacción. Retorna el número de filas afectadas.
    public async Task<int> Complete()
    {
      return await _context.SaveChangesAsync();
    }

    // Crea (o recupera del caché) un repositorio genérico para el tipo TEntity solicitado.
    // Usa reflexión con Activator.CreateInstance para instanciar GenericRepo<TEntity>
    // dinámicamente sin necesidad de escribir un caso específico para cada entidad.
    public IGenericRepository<TEntity> RepositoryGeneric<TEntity>() where TEntity : class
    {
      if(_repositories is null)
      {
        _repositories = new Hashtable();
      }

      // Usamos el nombre del tipo como clave del diccionario (ej: "Ticket", "TicketEmployee").
      var type = typeof(TEntity).Name;

      if (!_repositories.Contains(type))
      {
        // BUG FIX: Activator necesita la implementación concreta (GenericRepo<>), no la interfaz.
        // MakeGenericType(typeof(TEntity)) crea el tipo concreto: GenericRepo<Ticket>, GenericRepo<TicketEmployee>, etc.
        // CreateInstance pasa _context como argumento al constructor de GenericRepo<TEntity>.
        var repositoryType = typeof(GenericRepo<>);
        var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(TEntity)), _context);
        _repositories.Add(type, repositoryInstance);
      }

      // Devolvemos el repositorio casteado al tipo de interfaz correcto.
      // El operador ! indica al compilador que estamos seguros de que no es null.
      return (IGenericRepository<TEntity>)_repositories[type]!;
    }
  }
}
