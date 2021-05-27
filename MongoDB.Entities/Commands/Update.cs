using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents an update command
    /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public class Update<T> where T : IEntity
    {
        private readonly Collection<UpdateDefinition<T>> defs = new Collection<UpdateDefinition<T>>();
        private readonly Collection<PipelineStageDefinition<T, T>> stages = new Collection<PipelineStageDefinition<T, T>>();
        private FilterDefinition<T> filter = Builders<T>.Filter.Empty;
        private UpdateOptions options = new UpdateOptions();
        private readonly IClientSessionHandle session;
        private readonly Collection<UpdateManyModel<T>> models = new Collection<UpdateManyModel<T>>();

        internal Update(IClientSessionHandle session = null)
        {
            this.session = session;
        }

        /// <summary>
        /// Specify an IEntity ID as the matching criteria
        /// </summary>
        /// <param name="ID">A unique IEntity ID</param>
        public Update<T> MatchID(string ID)
        {
            return Match(f => f.Eq(t => t.ID, ID));
        }

        /// <summary>
        /// Specify the matching criteria with a lambda expression
        /// </summary>
        /// <param name="expression">x => x.Property == Value</param>
        public Update<T> Match(Expression<Func<T, bool>> expression)
        {
            return Match(f => f.Where(expression));
        }

        /// <summary>
        /// Specify the matching criteria with a filter expression
        /// </summary>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        public Update<T> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            this.filter &= filter(Builders<T>.Filter);
            return this;
        }

        /// <summary>
        /// Specify the matching criteria with a template
        /// </summary>
        /// <param name="template">A Template with a find query</param>
        public Update<T> Match(Template template)
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
        public Update<T> Match(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string language = null)
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
        public Update<T> Match(Expression<Func<T, object>> coordinatesProperty, Coordinates2D nearCoordinates, double? maxDistance = null, double? minDistance = null)
        {
            return Match(f => f.Near(coordinatesProperty, nearCoordinates.ToGeoJsonPoint(), maxDistance, minDistance));
        }

        /// <summary>
        /// Specify the matching criteria with a JSON string
        /// </summary>
        /// <param name="jsonString">{ Title : 'The Power Of Now' }</param>
        public Update<T> MatchString(string jsonString)
        {
            filter &= jsonString;
            return this;
        }

        /// <summary>
        /// Specify the matching criteria with an aggregation expression (i.e. $expr)
        /// </summary>
        /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
        public Update<T> MatchExpression(string expression)
        {
            filter &= "{$expr:" + expression + "}";
            return this;
        }

        /// <summary>
        /// Specify the matching criteria with a Template
        /// </summary>
        /// <param name="template">A Template object</param>
        public Update<T> MatchExpression(Template template)
        {
            filter &= "{$expr:" + template.ToString() + "}";
            return this;
        }

        /// <summary>
        /// Specify the property and it's value to modify (use multiple times if needed)
        /// </summary>
        /// <param name="property">x => x.Property</param>
        /// <param name="value">The value to set on the property</param>
        /// <returns></returns>
        public Update<T> Modify<TProp>(Expression<Func<T, TProp>> property, TProp value)
        {
            defs.Add(Builders<T>.Update.Set(property, value));
            return this;
        }

        /// <summary>
        /// Specify the update definition builder operation to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="operation">b => b.Inc(x => x.PropName, Value)</param>
        /// <returns></returns>
        public Update<T> Modify(Func<UpdateDefinitionBuilder<T>, UpdateDefinition<T>> operation)
        {
            defs.Add(operation(Builders<T>.Update));
            return this;
        }

        /// <summary>
        /// Specify an update (json string) to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="update">{ $set: { 'RootProp.$[x].SubProp' : 321 } }</param>
        public Update<T> Modify(string update)
        {
            defs.Add(update);
            return this;
        }

        /// <summary>
        /// Specify an update with a Template to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="template">A Template with a single update</param>
        public Update<T> Modify(Template template)
        {
            Modify(template.ToString());
            return this;
        }

        /// <summary>
        /// Specify an update pipeline with multiple stages using a Template to modify the Entities.
        /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
        /// </summary>
        /// <param name="template">A Template object containing multiple pipeline stages</param>
        public Update<T> WithPipeline(Template template)
        {
            foreach (var stage in template.ToStages())
            {
                stages.Add(stage);
            }

            return this;
        }

        /// <summary>
        /// Specify an update pipeline stage to modify the Entities (use multiple times if needed)
        /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
        /// </summary>
        /// <param name="stage">{ $set: { FullName: { $concat: ['$Name', ' ', '$Surname'] } } }</param>
        public Update<T> WithPipelineStage(string stage)
        {
            stages.Add(stage);
            return this;
        }

        /// <summary>
        /// Specify an update pipeline stage using a Template to modify the Entities (use multiple times if needed)
        /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
        /// </summary>
        /// <param name="template">A Template object containing a pipeline stage</param>
        public Update<T> WithPipelineStage(Template template)
        {
            return WithPipelineStage(template.ToString());
        }

        /// <summary>
        /// Specify an array filter to target nested entities for updates (use multiple times if needed).
        /// </summary>
        /// <param name="filter">{ 'x.SubProp': { $gte: 123 } }</param>
        public Update<T> WithArrayFilter(string filter)
        {
            ArrayFilterDefinition<T> def = filter;

            options.ArrayFilters =
                options.ArrayFilters == null
                ? new List<ArrayFilterDefinition>() { def }
                : options.ArrayFilters.Concat(new List<ArrayFilterDefinition> { def });

            return this;
        }

        /// <summary>
        /// Specify a single array filter using a Template to target nested entities for updates
        /// </summary>
        /// <param name="template"></param>
        public Update<T> WithArrayFilter(Template template)
        {
            WithArrayFilter(template.ToString());
            return this;
        }

        /// <summary>
        /// Specify multiple array filters with a Template to target nested entities for updates.
        /// </summary>
        /// <param name="template">The template with an array [...] of filters</param>
        public Update<T> WithArrayFilters(Template template)
        {
            var defs = template.ToArrayFilters<T>();

            options.ArrayFilters =
                options.ArrayFilters == null
                ? defs
                : options.ArrayFilters.Concat(defs);

            return this;
        }

        /// <summary>
        /// Specify an option for this update command (use multiple times if needed)
        /// <para>TIP: Setting options is not required</para>
        /// </summary>
        /// <param name="option">x => x.OptionName = OptionValue</param>
        public Update<T> Option(Action<UpdateOptions> option)
        {
            option(options);
            return this;
        }

        /// <summary>
        /// Queue up an update command for bulk execution later.
        /// </summary>
        public Update<T> AddToQueue()
        {
            if (filter == null) throw new ArgumentException("Please use Match() method first!");
            if (defs.Count == 0) throw new ArgumentException("Please use Modify() method first!");
            if (Cache<T>.HasModifiedOn) Modify(b => b.CurrentDate(Cache<T>.ModifiedOnPropName));
            models.Add(new UpdateManyModel<T>(filter, Builders<T>.Update.Combine(defs))
            {
                ArrayFilters = options.ArrayFilters,
                Collation = options.Collation,
                Hint = options.Hint,
                IsUpsert = options.IsUpsert
            });
            filter = Builders<T>.Filter.Empty;
            defs.Clear();
            options = new UpdateOptions();
            return this;
        }

        /// <summary>
        /// Run the update command in MongoDB.
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task<UpdateResult> ExecuteAsync(CancellationToken cancellation = default)
        {
            if (models.Count > 0)
            {
                var bulkWriteResult = await (
                    session == null
                    ? DB.Collection<T>().BulkWriteAsync(models, null, cancellation)
                    : DB.Collection<T>().BulkWriteAsync(session, models, null, cancellation)
                    ).ConfigureAwait(false);

                models.Clear();

                return new UpdateResult.Acknowledged(bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount, null);
            }
            else
            {
                if (filter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
                if (defs.Count == 0) throw new ArgumentException("Please use Modify() method first!");
                if (stages.Count > 0) throw new ArgumentException("Regular updates and Pipeline updates cannot be used together!");
                if (ShouldSetModDate())
                {
                    Modify(b => b.CurrentDate(Cache<T>.ModifiedOnPropName));
                }

                return await UpdateAsync(filter, Builders<T>.Update.Combine(defs), options, session, cancellation).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Run the update command with pipeline stages
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<UpdateResult> ExecutePipelineAsync(CancellationToken cancellation = default)
        {
            if (filter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
            if (stages.Count == 0) throw new ArgumentException("Please use WithPipelineStage() method first!");
            if (defs.Count > 0) throw new ArgumentException("Pipeline updates cannot be used together with regular updates!");
            if (ShouldSetModDate()) WithPipelineStage($"{{ $set: {{ '{Cache<T>.ModifiedOnPropName}': new Date() }} }}");

            return UpdateAsync(
                filter,
                Builders<T>.Update.Pipeline(stages.ToArray()),
                options,
                session,
                cancellation);
        }

        private bool ShouldSetModDate()
        {
            //only set mod date by library if user hasn't done anything with the ModifiedOn property

            return
                Cache<T>.HasModifiedOn &&
                !defs.Any(d => d
                       .Render(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry)
                       .ToString()
                       .Contains($"\"{Cache<T>.ModifiedOnPropName}\""));
        }

        private Task<UpdateResult> UpdateAsync(FilterDefinition<T> filter, UpdateDefinition<T> definition, UpdateOptions options, IClientSessionHandle session = null, CancellationToken cancellation = default)
        {
            return session == null
                   ? DB.Collection<T>().UpdateManyAsync(filter, definition, options, cancellation)
                   : DB.Collection<T>().UpdateManyAsync(session, filter, definition, options, cancellation);
        }
    }
}
