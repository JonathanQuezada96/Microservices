using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Linq.Expressions;
using Ticketing.command.Application.Models;
using Ticketing.command.Domain.Abstracts;
using Ticketing.command.Domain.Common;

namespace Ticketing.command.Infrastructure.Repositories
{
  /// <summary>
  /// Implementación genérica del repositorio de MongoDB.
  ///
  /// Implementa IMongoRepository&lt;TDocument&gt;, lo que significa que maneja tanto
  /// las operaciones CRUD como las transacciones de sesión para cualquier tipo de documento.
  ///
  /// Patrón Repository: desacopla la lógica de negocio del dominio de los detalles
  /// de acceso a datos (MongoDB), facilitando los tests y el cambio de base de datos.
  ///
  /// Al ser genérico (&lt;TDocument&gt;), una sola clase sirve para todas las colecciones.
  /// La restricción "where TDocument : IDocuments" garantiza que el tipo tenga un Id.
  /// </summary>
  public class MongoRepository<TDocument> : IMongoRepository<TDocument> where TDocument : IDocuments
  {
    // Referencia a la colección específica de MongoDB (análoga a una tabla en SQL).
    // Es de solo lectura porque se inicializa en el constructor y no debe cambiar.
    private readonly IMongoCollection<TDocument> _collection;

    /// <summary>
    /// Constructor: recibe el cliente de MongoDB y la configuración vía IOptions.
    /// IOptions&lt;MongoSettings&gt; es la forma idiomática de .NET para inyectar configuración tipada.
    /// Resuelve automáticamente la colección correcta usando el atributo [BsonCollection].
    /// </summary>
    public MongoRepository(IMongoClient mongoClient, IOptions<MongoSettings> options)
    {
      // 1. Obtiene la base de datos configurada en appsettings.json ("ticketingApp").
      // 2. Dentro de esa BD, obtiene la colección correspondiente al tipo TDocument.
      //    GetCollectionName() lee el atributo [BsonCollection] de la clase para saber el nombre.
      _collection = mongoClient.GetDatabase(options.Value.Database).GetCollection<TDocument>(GetCollectionName(typeof(TDocument)));
    }

    /// <summary>
    /// Lee el atributo [BsonCollection] del tipo de documento para obtener el nombre
    /// de la colección de MongoDB en tiempo de ejecución (reflexión/reflection).
    ///
    /// Si el tipo no tiene el atributo, lanza una excepción explícita en lugar de
    /// fallar silenciosamente más adelante — esto es "fail fast".
    /// </summary>
    private protected string GetCollectionName(Type documentType)
    {
      // GetCustomAttributes busca el atributo BsonCollectionAttribute en la clase
      var name = documentType.GetCustomAttributes(typeof(BsonCollectionAttribute), true).FirstOrDefault();

      // Si el atributo existe, devuelve el CollectionName; si no, lanza excepción.
      return name != null ? ((BsonCollectionAttribute)name).CollectionName : throw new ArgumentException("La colleccion es desconocida");
    }

    /// <summary>
    /// Expone la colección como IQueryable para poder usar LINQ en las consultas.
    /// Permite al llamador hacer: repo.AsQueryable().Where(e => e.AggregateId == id).ToList()
    /// </summary>
    public IQueryable<TDocument> AsQuerable()
    {
      return _collection.AsQueryable();
    }

    /// <summary>
    /// Abre una sesión con el servidor MongoDB.
    /// Una sesión es el prerequisito para iniciar una transacción.
    /// ClientSessionOptions permite configurar opciones de lectura/escritura por defecto.
    /// </summary>
    public async Task<IClientSessionHandle> BeginSessionAsync(CancellationToken cancellationToken)
    {
      var option = new ClientSessionOptions();
      option.DefaultTransactionOptions = new TransactionOptions();
      // StartSessionAsync devuelve un "handle" (manejador) que se pasa a las demás operaciones.
      return await _collection.Database.Client.StartSessionAsync(option, cancellationToken);
    }

    /// <summary>
    /// Inicia una transacción dentro de la sesión activa.
    /// A partir de aquí, InsertOneAsync y otras operaciones quedan "en vuelo"
    /// hasta que se llame a CommitTransaction o RollbackTransaction.
    /// </summary>
    public void BeginTransaction(IClientSessionHandle session) => session.StartTransaction();

    /// <summary>
    /// Confirma la transacción: hace permanentes todas las escrituras realizadas.
    /// Si esto falla (ej: conflicto de red), se debería llamar a RollbackTransactionAsync.
    /// </summary>
    public Task CommitTransactionAsync(IClientSessionHandle session, CancellationToken cancellationToken) => session.CommitTransactionAsync(cancellationToken);

    /// <summary>
    /// Libera los recursos de la sesión (cierra la conexión de sesión con MongoDB).
    /// Siempre debe llamarse en un bloque finally para evitar fugas de recursos.
    /// </summary>
    public void DisposeSession(IClientSessionHandle session)
      => session.Dispose();

    /// <summary>
    /// Inserta un documento dentro de una transacción activa.
    /// Al pasar el sessionHandle, MongoDB sabe que esta inserción pertenece
    /// a la transacción en curso y puede revertirla si algo falla.
    /// </summary>
    public async Task InsertOneAsync(TDocument document, IClientSessionHandle sessionHandle, CancellationToken cancellationToken)
    {
      await _collection.InsertOneAsync(sessionHandle, document, null, cancellationToken);
    }

    /// <summary>
    /// Revierte (aborta) la transacción, deshaciendo todas las escrituras realizadas.
    /// Equivalente al ROLLBACK de SQL. Se llama en el bloque catch cuando ocurre un error.
    /// </summary>
    public Task RollbackTransactionAsync(IClientSessionHandle clientSession, CancellationToken cancellationToken) => clientSession.AbortTransactionAsync(cancellationToken);

    public async Task<IEnumerable<TDocument>> FilterByAsync(
    Expression<Func<TDocument, bool>> filterExpression,
    CancellationToken cancellationToken)
    {
      return await _collection
          .Find(filterExpression)
          .ToListAsync(cancellationToken);
    }
  }
}
