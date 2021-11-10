using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents an UpdateOne command, which can replace the first matched document with a given entity
    /// <para>TIP: Specify a filter first with the .Match(). Then set entity with .WithEntity() and finally call .Execute() to run the command.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public class Replace<T> : FilterQueryBase<T, Replace<T>>, ICollectionRelated<T> where T : IEntity
    {
        private ReplaceOptions _options = new();
        private readonly List<ReplaceOneModel<T>> _models = new();
        private readonly ModifiedBy? _modifiedBy;
        private readonly Action<T>? _onSaveAction;
        private T? _entity;

        public DBContext Context { get; }
        public IMongoCollection<T> Collection { get; }

        internal Replace(
            DBContext context,
            IMongoCollection<T> collection,
            Action<T>? onSaveAction) : base(context.GlobalFilters)
        {
            Context = context;
            Collection = collection;
            _modifiedBy = context.ModifiedBy;
            _onSaveAction = onSaveAction;
        }


        /// <summary>
        /// Supply the entity to replace the first matched document with
        /// <para>TIP: If the entity ID is empty, a new ID will be generated before being stored</para>
        /// </summary>
        /// <param name="entity"></param>
        public Replace<T> WithEntity(T entity)
        {
            if (string.IsNullOrEmpty(entity.ID))
                throw new InvalidOperationException("Cannot replace an entity with an empty ID value!");

            _onSaveAction?.Invoke(entity);

            _entity = entity;
            return this;
        }

        /// <summary>
        /// Specify an option for this replace command (use multiple times if needed)
        /// <para>TIP: Setting options is not required</para>
        /// </summary>
        /// <param name="option">x => x.OptionName = OptionValue</param>
        public Replace<T> Option(Action<ReplaceOptions> option)
        {
            option(_options);
            return this;
        }


        /// <summary>
        /// Queue up a replace command for bulk execution later.
        /// </summary>
        public Replace<T> AddToQueue()
        {
            var mergedFilter = Logic.MergeWithGlobalFilter(_ignoreGlobalFilters, _globalFilters, _filter);
            if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
            if (_entity == null) throw new ArgumentException("Please use WithEntity() method first!");
            SetModOnAndByValues();

            _models.Add(new ReplaceOneModel<T>(mergedFilter, _entity)
            {
                Collation = _options.Collation,
                Hint = _options.Hint,
                IsUpsert = _options.IsUpsert
            });
            _filter = Builders<T>.Filter.Empty;
            _entity = default;
            _options = new ReplaceOptions();
            return this;
        }

        /// <summary>
        /// Run the replace command in MongoDB.
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task<ReplaceOneResult> ExecuteAsync(CancellationToken cancellation = default)
        {
            if (_models.Count > 0)
            {
                var bulkWriteResult = await (
                    this.Session() is not IClientSessionHandle session
                    ? Collection.BulkWriteAsync(_models, null, cancellation)
                    : Collection.BulkWriteAsync(session, _models, null, cancellation)
                    ).ConfigureAwait(false);

                _models.Clear();

                if (!bulkWriteResult.IsAcknowledged)
                    return ReplaceOneResult.Unacknowledged.Instance;

                return new ReplaceOneResult.Acknowledged(bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount, null);
            }
            else
            {
                var mergedFilter = MergedFilter;
                if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
                if (_entity == null) throw new ArgumentException("Please use WithEntity() method first!");
                SetModOnAndByValues();

                return this.Session() is not IClientSessionHandle session
                       ? await Collection.ReplaceOneAsync(mergedFilter, _entity, _options, cancellation).ConfigureAwait(false)
                       : await Collection.ReplaceOneAsync(session, mergedFilter, _entity, _options, cancellation).ConfigureAwait(false);
            }
        }

        private void SetModOnAndByValues()
        {
            var cache = Context.Cache<T>();
            if (cache.HasModifiedOn && _entity is IModifiedOn _entityModifiedOn) _entityModifiedOn.ModifiedOn = DateTime.UtcNow;
            if (cache.ModifiedByProp != null && _modifiedBy != null)
            {
                cache.ModifiedByProp.SetValue(
                    _entity,
                    BsonSerializer.Deserialize(_modifiedBy.ToBson(), cache.ModifiedByProp.PropertyType));
            }
        }
    }
}
