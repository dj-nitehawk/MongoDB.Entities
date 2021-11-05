using MongoDB.Driver;
using System.Collections.Generic;
#nullable enable
namespace MongoDB.Entities
{
    /// <summary>
    /// MongoContext is a wrapper around an <see cref="IMongoClient"/>
    /// </summary>
    public partial class MongoContext : IMongoClient
    {
        /// <summary>
        /// Creates a new context
        /// </summary>        
        /// <param name="client">The backing client, usually a <see cref="MongoClient"/></param>
        /// <param name="options">The options to configure the context</param>
        public MongoContext(IMongoClient client, MongoContextOptions? options = null)
        {
            Client = client;
            Options = options ?? new();
        }

        /// <summary>
        /// The backing client
        /// </summary>
        public IMongoClient Client { get; }

        public MongoContextOptions Options { get; set; }

        /// <inheritdoc cref="MongoContextOptions.ModifiedBy"/>
        public ModifiedBy? ModifiedBy => Options.ModifiedBy;
    }

}
