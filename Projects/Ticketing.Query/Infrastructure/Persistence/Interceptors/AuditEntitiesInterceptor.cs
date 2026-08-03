using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tiketing.Query.Domain.Abstractions;

namespace Tiketing.Query.Infrastructure.Persistence.Interceptors
{
  // Interceptor de EF Core que llena automáticamente los campos de auditoría
  // (CreatedOn, CreatedBy, LastModificateOn, LastModifieBy) en todas las entidades.
  //
  // ¿Qué es un Interceptor de EF Core?
  //   Es un gancho (hook) que EF Core llama automáticamente en puntos clave del ciclo de vida
  //   del DbContext. SaveChangesInterceptor específicamente se activa ANTES y DESPUÉS de guardar.
  //
  // ¿Por qué es útil?
  //   Sin esto, tendríamos que asignar CreatedOn/LastModificateOn manualmente en cada Handler.
  //   Con el interceptor, la auditoría es TRANSPARENTE — los desarrolladores no necesitan recordarla.
  //
  // Está registrado como Singleton en InfrastructureServiceRegistration y se añade
  // al DbContext con .AddInterceptors(new AuditEntitiesInterceptor()).
  public class AuditEntitiesInterceptor : SaveChangesInterceptor
  {
    // SavingChangesAsync se invoca justo ANTES de que EF Core ejecute los SQL en la base de datos.
    // Es el momento ideal para modificar propiedades de auditoría porque:
    //   1. EF Core ya rastreó todos los cambios en el ChangeTracker.
    //   2. Aún no generó los INSERT/UPDATE — nuestros cambios serán incluidos.
    //
    // ValueTask<InterceptionResult<int>>: el tipo de retorno es complejo porque EF Core permite
    // que el interceptor SUPRIMA la operación original devolviendo un InterceptionResult.Suppress().
    // En nuestro caso no suprimimos — dejamos que EF Core siga su flujo normal.
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
      // eventData.Context es el DbContext actual. Puede ser null en escenarios edge (ej: dispose racing).
      // Si es null, saltamos nuestro código y llamamos directamente al comportamiento base.
      var dbContext = eventData.Context;
      if (dbContext is null)
      {
        return base.SavingChangesAsync(eventData, result, cancellationToken);
      }

      // ChangeTracker.Entries<Entity>() devuelve todas las entidades que EF Core está rastreando
      // en este momento y que heredan de nuestra clase base "Entity" (la que tiene los campos de auditoría).
      // Solo afectamos las entidades que heredan de Entity — las demás se ignoran.
      var entries = dbContext.ChangeTracker.Entries<Entity>();

      // Iteramos cada entidad rastreada e inspeccionamos su Estado.
      // EF Core asigna uno de estos estados a cada entidad según lo que le pasó:
      //   - Added:    la entidad fue agregada con Add() o AddAsync() → generará un INSERT.
      //   - Modified: la entidad fue modificada → generará un UPDATE.
      //   - Deleted:  la entidad fue marcada para borrar → generará un DELETE.
      //   - Unchanged: no hubo cambios → no genera SQL.
      foreach (EntityEntry<Entity> entity in entries)
      {
        if (entity.State == EntityState.Added)
        {
          // Si es una entidad NUEVA, establecemos cuándo fue creada y quién la creó.
          // DateTime.UtcNow asegura consistencia sin importar la zona horaria del servidor.
          entity.Property(x => x.CreatedOn).CurrentValue = DateTime.UtcNow;

          // En este sistema, todas las entidades del Query Side son creadas por eventos de Kafka.
          // No hay un usuario humano que haga el INSERT — es el Consumer de Kafka quien lo dispara.
          // Si en el futuro hubiera usuarios humanos, aquí se inyectaría ICurrentUserService o similar.
          entity.Property(x => x.CreatedBy).CurrentValue = "Apache Kafka";
        }
        else if (entity.State == EntityState.Modified)
        {
          // Si es una entidad EXISTENTE que fue modificada, registramos la fecha y "usuario" de la última modificación.
          entity.Property(x => x.LastModificateOn).CurrentValue = DateTime.UtcNow;
          entity.Property(x => x.LastModifieBy).CurrentValue = "Apache Kafka";
        }
      }

      // Llamamos al interceptor base para que EF Core continúe con el flujo normal de guardado.
      // Si no llamáramos a base.SavingChangesAsync(), los datos NUNCA se guardarían en la BD.
      return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
  }
}
