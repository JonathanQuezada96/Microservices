using Microsoft.EntityFrameworkCore;
using Tiketing.Query.Domain.Employees;
using Tiketing.Query.Infrastructure.Persistence;

namespace Tiketing.Query.Infrastructure.Repos
{
  // Implementación concreta del repositorio de Empleados.
  //
  // Al heredar de GenericRepo<Employee>, esta clase ya tiene implementadas todas las
  // operaciones CRUD básicas (Add, Delete, GetAll, GetById, Update) sin escribir una sola línea.
  // Solo necesitamos implementar los métodos específicos declarados en IemployeeRepository.
  //
  // Este patrón se llama "Template Method": la clase base hace el trabajo repetido,
  // la clase hija solo aporta lo específico.
  public class EmployeeRepository : GenericRepo<Employee>, IemployeeRepository
  {
    // Pasamos el contexto de base de datos al constructor de GenericRepo usando "base(context)".
    // Esto garantiza que tanto el repo genérico como este comparten el mismo DbContext.
    public EmployeeRepository(TicketDbContext context) : base(context) { }



    // Implementación de la búsqueda por email (username).
    // Usamos FirstOrDefaultAsync para obtener el primer resultado o null si no existe.
    // La expresión lambda "e => e.Email == username" actúa como filtro WHERE en SQL:
    //   SELECT * FROM employees WHERE email = @username LIMIT 1
    public async Task<Employee?> GetByUsernameAsync(string username)
    {
     return await _context.Employees.FirstOrDefaultAsync(e => e.Email == username);
    }
  }
}
