namespace Tiketing.Query.Domain.Abstractions
{
  // Patrón Repositorio Genérico. Abstrae el acceso a datos para operaciones CRUD comunes.
  // Facilita cambiar de ORM en el futuro y facilita el testing.
  public interface IGenericRepository<T> where T : class
  {
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T?> GetByIdAsync(Guid id);
    void AddEntity(T entity);
    void UpdateEntity(T entity);
    void DeleteEntity(T entity);
  }
}
