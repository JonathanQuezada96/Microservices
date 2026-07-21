namespace Ticketing.command.Domain.Common
{   /// <summary>
    /// Atributo que permite asociar una entidad con una colección de MongoDB.
    /// </summary>

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BsonCollectionAttribute : Attribute
    {

        public BsonCollectionAttribute(string collectionName)
        {
            CollectionName = collectionName;
        }
        public string CollectionName { get; set; }
    }
}