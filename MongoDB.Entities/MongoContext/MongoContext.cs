using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using System.Collections.Generic;

namespace MongoDB.Entities
{
    /// <summary>
    /// MongoContext is simply a wrapper around a <see cref="IMongoClient"/>
    /// </summary>
    public partial class MongoContext : IMongoClient
    {
        /// <summary>
        /// Creates a new <see cref="MongoContext"/> from an existing <see cref="IMongoClient"/>
        /// </summary>        
        public MongoContext(IMongoClient client)
        {
            Client = client;
        }

        /// <summary>
        /// Creates a new <see cref="MongoContext"/> and a new <see cref="MongoClient"/> from a <see cref="MongoClientSettings"/> object
        /// </summary>      
        public MongoContext(MongoClientSettings settings) : this(new MongoClient(settings))
        {
        }

        /// <summary>
        /// Creates a new <see cref="MongoContext"/>, <see cref="MongoClient"/> and <see cref="MongoClientSettings"/> from a <see cref="MongoUrl"/> object
        /// </summary>
        public MongoContext(MongoUrl url) : this(MongoClientSettings.FromUrl(url))
        {
        }



        /// <summary>
        /// The backing client
        /// </summary>
        public IMongoClient Client { get; }

        /// <summary>
        /// Stores the <see cref="DBContext"/>s managed by this <see cref="MongoContext"/>
        /// <para>This maps Database name to the matching <see cref="DBContext"/></para>
        /// </summary>
        public IDictionary<string, DBContext> Contexts { get; } = new Dictionary<string, DBContext>();

        public ICluster Cluster => Client.Cluster;

        public MongoClientSettings Settings => Client.Settings;
    }

}
