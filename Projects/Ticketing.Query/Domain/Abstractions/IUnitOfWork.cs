using Tiketing.Query.Domain.Employees;

namespace Tiketing.Query.Domain.Abstractions
{
  // Patrón Unit of Work (Unidad de Trabajo).
  // Asegura que todos los repositorios compartan la misma transacción de BD.
  // Permite confirmar múltiples cambios (o deshacerlos) en un solo commit.
  public interface IUnitOfWork
  {
    IemployeeRepository EmployeeRepository { get; }
    IGenericRepository<TEntity> RepositoryGeneric<TEntity>() where TEntity: class;
    Task<int> Complete();
  }
}
