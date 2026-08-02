using Tiketing.Query.Domain.Abstractions;

namespace Tiketing.Query.Domain.Employees
{
  // Repositorio específico para la entidad Employee.
  //
  // Hereda de IGenericRepository<Employee>, por lo que ya incluye gratis las operaciones
  // básicas: AddEntity, DeleteEntity, GetAllAsync, GetByIdAsync, UpdateEntity.
  //
  // Aquí solo se declaran las operaciones ADICIONALES que son específicas de Employee
  // y que no pueden resolverse con el repositorio genérico.
  public interface IemployeeRepository : IGenericRepository<Employee>
  {
    // Busca un empleado por su email (que se usa como nombre de usuario en el sistema).
    // Devuelve null si no existe ningún empleado con ese email.
    Task<Employee?> GetByUsernameAsync(string username);
  }
}
