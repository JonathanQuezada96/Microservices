using MongoDB.Driver;
using System.Linq.Expressions;
using Ticketing.command.Domain.Common;

namespace Ticketing.command.Domain.Abstracts
{
  /// <summary>
  /// Interfaz genérica de repositorio para colecciones de MongoDB.
  ///
  /// Combina dos responsabilidades:
  ///   1. ISession → manejo de sesiones y transacciones (heredado).
  ///   2. Operaciones CRUD sobre documentos del tipo TDocument.
  ///
  /// La restricción "where TDocument : IDocuments" garantiza que solo se pueda
  /// usar con entidades que implementen IDocuments (es decir, que tengan un ObjectId).
  ///
  /// Al ser genérico, una sola implementación (MongoRepository&lt;T&gt;) sirve para
  /// cualquier colección de MongoDB, siguiendo el principio DRY (Don't Repeat Yourself).
  /// </summary>
  public interface IMongoRepository<TDocument> : ISession where TDocument : IDocuments
  {
    /// <summary>
    /// Expone la colección como IQueryable para poder hacer consultas LINQ
    /// sobre los documentos de MongoDB (lectura, filtrado, proyección, etc.).
    /// </summary>
    IQueryable<TDocument> AsQuerable();

    /// <summary>
    /// Inserta un documento dentro de una sesión/transacción activa.
    /// Al recibir el IClientSessionHandle, la operación forma parte de la
    /// transacción actual y puede revertirse si algo falla.
    /// </summary>
    Task InsertOneAsync(TDocument document, IClientSessionHandle sessionHandle, CancellationToken cancellationToken);

    Task<IEnumerable<TDocument>> FilterByAsync(Expression<Func<TDocument, bool>> filterExpression, CancellationToken cancellationToken);
  }
}
