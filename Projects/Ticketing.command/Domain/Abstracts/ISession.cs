using MongoDB.Driver;

namespace Ticketing.command.Domain.Abstracts
{
  /// <summary>
  /// Define el contrato para gestionar sesiones y transacciones en MongoDB.
  ///
  /// En Event Sourcing es crítico que el guardado de un evento sea ATÓMICO:
  /// si algo falla, el evento no debe persistirse a medias. Las transacciones
  /// de MongoDB (disponibles en Replica Sets) garantizan esta atomicidad.
  ///
  /// Esta interfaz abstrae las operaciones de sesión para que el dominio
  /// no dependa directamente del driver de MongoDB (principio de inversión de dependencias).
  /// </summary>
  public interface ISession
  {
    /// <summary>
    /// Inicia una sesión con el servidor de MongoDB.
    /// Una sesión es el contexto necesario para poder abrir transacciones.
    /// </summary>
    Task<IClientSessionHandle> BeginSessionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Inicia una transacción dentro de la sesión activa.
    /// A partir de aquí, todas las operaciones de escritura son parte de la transacción.
    /// </summary>
    void BeginTransaction(IClientSessionHandle session);

    /// <summary>
    /// Confirma (hace permanentes) todas las operaciones realizadas dentro de la transacción.
    /// Si esto falla, se debe llamar a RollbackTransactionAsync.
    /// </summary>
    Task CommintTransactionAsync(IClientSessionHandle session, CancellationToken cancellationToken);

    /// <summary>
    /// Revierte (deshace) todas las operaciones de la transacción en curso.
    /// Se usa en el bloque catch cuando ocurre un error para evitar estados inconsistentes.
    /// </summary>
    Task RollbackTransactionAsync(IClientSessionHandle clientSession, CancellationToken cancellationToken);

    /// <summary>
    /// Libera los recursos de la sesión.
    /// Equivalente a cerrar la conexión de la sesión con MongoDB.
    /// </summary>
    void DisoseSession(IClientSessionHandle session);
  }
}
