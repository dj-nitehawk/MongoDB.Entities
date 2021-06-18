using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class Replace<T> where T : IEntity
    {
        private FilterDefinition<T> filter = Builders<T>.Filter.Empty;
        private ReplaceOptions options = new ReplaceOptions();
        private readonly IClientSessionHandle session;
        private readonly Collection<ReplaceOneModel<T>> models = new Collection<ReplaceOneModel<T>>();
        private readonly ModifiedBy modifiedBy;
        private readonly Dictionary<Type, (object filterDef, bool prepend)> globalFilters;
        private readonly Action<T> onSaveAction;
        private bool ignoreGlobalFilters;

        internal Replace(
            IClientSessionHandle session,
            ModifiedBy modifiedBy,
            Dictionary<Type, (object filterDef, bool prepend)> globalFilters,
            Action<T> onSaveAction)
        {
            this.session = session;
            this.modifiedBy = modifiedBy;
            this.globalFilters = globalFilters;
            this.onSaveAction = onSaveAction;
        }

        private T entity { get; set; }

        /// <summary>
        /// Specify an IEntity ID as the matching criteria
        /// </summary>
        /// <param name="ID">A unique IEntity ID</param>
        public Replace<T> MatchID(string ID)
        {
            return Match(f => f.Eq(t => t.ID, ID));
        }

        /// <summary>
        /// Specify the matching criteria with a lambda expression
        /// </summary>
        /// <param name="expression">x => x.Property == Value</param>
        public Replace<T> Match(Expression<Func<T, bool>> expression)
        {
            return Match(f => f.Where(expression));
        }

        /// <summary>
        /// Specify the matching criteria with a filter expression
        /// </summary>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        public Replace<T> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            this.filter &= filter(Builders<T>.Filter);
            return this;
        }

        /// <summary>
        /// Specify the matching criteria with a template
        /// </summary>
        /// <param name="template">A Template with a find query</param>
        public Replace<T> Match(Template template)
        {
            filter &= template.ToString();
            return this;
        }

        /// <summary>
        /// Specify a search term to find results from the text index of this particular collection.
        /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
        /// </summary>
        /// <param name="searchType">The type of text matching to do</param>
        /// <param name="searchTerm">The search term</param>
        /// <param name="caseSensitive">Case sensitivity of the search (optional)</param>
        /// <param name="diacriticSensitive">Diacritic sensitivity of the search (optional)</param>
        /// <param name="language">The language for the search (optional)</param>
        public Replace<T> Match(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string language = null)
        {
            if (searchType == Search.Fuzzy)
            {
                searchTerm = searchTerm.ToDoubleMetaphoneHash();
                caseSensitive = false;
                diacriticSensitive = false;
                language = null;
            }

            return Match(
                f => f.Text(
                    searchTerm,
                    new TextSearchOptions
                    {
                        CaseSensitive = caseSensitive,
                        DiacriticSensitive = diacriticSensitive,
                        Language = language
                    }));
        }

        /// <summary>
        /// Specify criteria for matching entities based on GeoSpatial data (longitude &amp; latitude)
        /// <para>TIP: Make sure to define a Geo2DSphere index with DB.Index&lt;T&gt;() before searching</para>
        /// <para>Note: DB.FluentGeoNear() supports more advanced options</para>
        /// </summary>
        /// <param name="coordinatesProperty">The property where 2DCoordinates are stored</param>
        /// <param name="nearCoordinates">The search point</param>
        /// <param name="maxDistance">Maximum distance in meters from the search point</param>
        /// <param name="minDistance">Minimum distance in meters from the search point</param>
        public Replace<T> Match(Expression<Func<T, object>> coordinatesProperty, Coordinates2D nearCoordinates, double? maxDistance = null, double? minDistance = null)
        {
            return Match(f => f.Near(coordinatesProperty, nearCoordinates.ToGeoJsonPoint(), maxDistance, minDistance));
        }

        /// <summary>
        /// Specify the matching criteria with a JSON string
        /// </summary>
        /// <param name="jsonString">{ Title : 'The Power Of Now' }</param>
        public Replace<T> MatchString(string jsonString)
        {
            filter &= jsonString;
            return this;
        }

        /// <summary>
        /// Specify the matching criteria with an aggregation expression (i.e. $expr)
        /// </summary>
        /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
        public Replace<T> MatchExpression(string expression)
        {
            filter &= "{$expr:" + expression + "}";
            return this;
        }

        /// <summary>
        /// Specify the matching criteria with a Template
        /// </summary>
        /// <param name="template">A Template object</param>
        public Replace<T> MatchExpression(Template template)
        {
            filter &= "{$expr:" + template.ToString() + "}";
            return this;
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

            onSaveAction?.Invoke(entity);

            this.entity = entity;

            return this;
        }

        /// <summary>
        /// Specify an option for this replace command (use multiple times if needed)
        /// <para>TIP: Setting options is not required</para>
        /// </summary>
        /// <param name="option">x => x.OptionName = OptionValue</param>
        public Replace<T> Option(Action<ReplaceOptions> option)
        {
            option(options);
            return this;
        }

        /// <summary>
        /// Specify that this operation should ignore any global filters
        /// </summary>
        public Replace<T> IgnoreGlobalFilters()
        {
            ignoreGlobalFilters = true;
            return this;
        }

        /// <summary>
        /// Queue up a replace command for bulk execution later.
        /// </summary>
        public Replace<T> AddToQueue()
        {
            var mergedFilter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, globalFilters, filter);
            if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
            if (entity == null) throw new ArgumentException("Please use WithEntity() method first!");
            SetModOnAndByValues();

            models.Add(new ReplaceOneModel<T>(mergedFilter, entity)
            {
                Collation = options.Collation,
                Hint = options.Hint,
                IsUpsert = options.IsUpsert
            });
            filter = Builders<T>.Filter.Empty;
            entity = default;
            options = new ReplaceOptions();
            return this;
        }

        /// <summary>
        /// Run the replace command in MongoDB.
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task<ReplaceOneResult> ExecuteAsync(CancellationToken cancellation = default)
        {
            if (models.Count > 0)
            {
                var bulkWriteResult = await (
                    session == null
                    ? DB.Collection<T>().BulkWriteAsync(models, null, cancellation)
                    : DB.Collection<T>().BulkWriteAsync(session, models, null, cancellation)
                    ).ConfigureAwait(false);

                models.Clear();

                if (!bulkWriteResult.IsAcknowledged)
                    return ReplaceOneResult.Unacknowledged.Instance;

                return new ReplaceOneResult.Acknowledged(bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount, null);
            }
            else
            {
                var mergedFilter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, globalFilters, filter);
                if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
                if (entity == null) throw new ArgumentException("Please use WithEntity() method first!");
                SetModOnAndByValues();

                return session == null
                       ? await DB.Collection<T>().ReplaceOneAsync(mergedFilter, entity, options, cancellation).ConfigureAwait(false)
                       : await DB.Collection<T>().ReplaceOneAsync(session, mergedFilter, entity, options, cancellation).ConfigureAwait(false);
            }
        }

        private void SetModOnAndByValues()
        {
            if (Cache<T>.HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
            if (Cache<T>.ModifiedByProp != null && modifiedBy != null)
            {
                Cache<T>.ModifiedByProp.SetValue(
                    entity,
                    BsonSerializer.Deserialize(modifiedBy.ToBson(), Cache<T>.ModifiedByProp.PropertyType));
            }
        }
    }
}
