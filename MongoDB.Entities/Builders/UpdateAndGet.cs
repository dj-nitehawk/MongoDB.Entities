using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Update and retrieve the first document that was updated.
    /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public class UpdateAndGet<T> : UpdateAndGet<T, T> where T : IEntity
    {
        internal UpdateAndGet(DBContext context, IMongoCollection<T> collection, UpdateBase<T, UpdateAndGet<T, T>> other) : base(context, collection, other)
        {
        }

        internal UpdateAndGet(DBContext context, IMongoCollection<T> collection, Dictionary<Type, (object filterDef, bool prepend)> globalFilters, Action<UpdateAndGet<T, T>>? onUpdateAction, List<UpdateDefinition<T>>? defs) : base(context, collection, globalFilters, onUpdateAction, defs)
        {
        }
    }

    /// <summary>
    /// Update and retrieve the first document that was updated.
    /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TProjection">The type to project to</typeparam>
    public class UpdateAndGet<T, TProjection> : UpdateBase<T, UpdateAndGet<T, TProjection>>, ICollectionRelated<T> where T : IEntity
    {
        private readonly List<PipelineStageDefinition<T, TProjection>> _stages = new();
        private protected readonly FindOneAndUpdateOptions<T, TProjection> _options = new() { ReturnDocument = ReturnDocument.After };

        public DBContext Context { get; }
        public IMongoCollection<T> Collection { get; }

        internal UpdateAndGet(DBContext context, IMongoCollection<T> collection, UpdateBase<T, UpdateAndGet<T, TProjection>> other) : base(other)
        {
            Context = context;
            Collection = collection;
        }

        internal UpdateAndGet(DBContext context, IMongoCollection<T> collection, Dictionary<Type, (object filterDef, bool prepend)> globalFilters, Action<UpdateAndGet<T, TProjection>>? onUpdateAction = null, List<UpdateDefinition<T>>? defs = null) : base(globalFilters, onUpdateAction, defs)
        {
            Context = context;
            Collection = collection;
        }



        /// <summary>
        /// Specify an update pipeline with multiple stages using a Template to modify the Entities.
        /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
        /// </summary>
        /// <param name="template">A Template object containing multiple pipeline stages</param>
        public UpdateAndGet<T, TProjection> WithPipeline(Template template)
        {
            foreach (var stage in template.ToStages())
            {
                _stages.Add(stage);
            }

            return this;
        }

        /// <summary>
        /// Specify an update pipeline stage to modify the Entities (use multiple times if needed)
        /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
        /// </summary>
        /// <param name="stage">{ $set: { FullName: { $concat: ['$Name', ' ', '$Surname'] } } }</param>
        public UpdateAndGet<T, TProjection> WithPipelineStage(string stage)
        {
            _stages.Add(stage);
            return this;
        }

        /// <summary>
        /// Specify an update pipeline stage using a Template to modify the Entities (use multiple times if needed)
        /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
        /// </summary>
        /// <param name="template">A Template object containing a pipeline stage</param>
        public UpdateAndGet<T, TProjection> WithPipelineStage(Template template)
        {
            return WithPipelineStage(template.RenderToString());
        }

        /// <summary>
        /// Specify an array filter to target nested entities for updates (use multiple times if needed).
        /// </summary>
        /// <param name="filter">{ 'x.SubProp': { $gte: 123 } }</param>
        public UpdateAndGet<T, TProjection> WithArrayFilter(string filter)
        {
            ArrayFilterDefinition<T> def = filter;

            _options.ArrayFilters =
                _options.ArrayFilters == null
                ? new[] { def }
                : _options.ArrayFilters.Concat(new[] { def });

            return this;
        }

        /// <summary>
        /// Specify a single array filter using a Template to target nested entities for updates
        /// </summary>
        /// <param name="template"></param>
        public UpdateAndGet<T, TProjection> WithArrayFilter(Template template)
        {
            WithArrayFilter(template.RenderToString());
            return this;
        }

        /// <summary>
        /// Specify multiple array filters with a Template to target nested entities for updates.
        /// </summary>
        /// <param name="template">The template with an array [...] of filters</param>
        public UpdateAndGet<T, TProjection> WithArrayFilters(Template template)
        {
            var defs = template.ToArrayFilters<T>();

            _options.ArrayFilters =
                _options.ArrayFilters == null
                ? defs
                : _options.ArrayFilters.Concat(defs);

            return this;
        }

        /// <summary>
        /// Specify an option for this update command (use multiple times if needed)
        /// <para>TIP: Setting options is not required</para>
        /// </summary>
        /// <param name="option">x => x.OptionName = OptionValue</param>
        public UpdateAndGet<T, TProjection> Option(Action<FindOneAndUpdateOptions<T, TProjection>> option)
        {
            option(_options);
            return this;
        }

        /// <summary>
        /// Specify how to project the results using a lambda expression
        /// </summary>
        /// <param name="expression">x => new Test { PropName = x.Prop }</param>
        public UpdateAndGet<T, TProjection> Project(Expression<Func<T, TProjection>> expression)
        {
            return Project(p => p.Expression(expression));
        }

        /// <summary>
        /// Specify how to project the results using a projection expression
        /// </summary>
        /// <param name="projection">p => p.Include("Prop1").Exclude("Prop2")</param>
        public UpdateAndGet<T, TProjection> Project(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, TProjection>> projection)
        {
            _options.Projection = projection(Builders<T>.Projection);
            return this;
        }

        /// <summary>
        /// Specify to automatically include all properties marked with [BsonRequired] attribute on the entity in the final projection. 
        /// <para>HINT: this method should only be called after the .Project() method.</para>
        /// </summary>
        public UpdateAndGet<T, TProjection> IncludeRequiredProps()
        {
            if (typeof(T) != typeof(TProjection))
                throw new InvalidOperationException("IncludeRequiredProps() cannot be used when projecting to a different type.");

            _options.Projection = Cache<T>.Instance.CombineWithRequiredProps(_options.Projection);
            return this;
        }

        /// <summary>
        /// Run the update command in MongoDB and retrieve the first document modified
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task<TProjection> ExecuteAsync(CancellationToken cancellation = default)
        {
            var mergedFilter = MergedFilter;
            if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
            if (defs.Count == 0) throw new ArgumentException("Please use Modify() method first!");
            if (_stages.Count > 0) throw new ArgumentException("Regular updates and Pipeline updates cannot be used together!");
            if (ShouldSetModDate()) Modify(b => b.CurrentDate(Cache<T>.Instance.ModifiedOnPropName));
            onUpdateAction?.Invoke(this);
            return await UpdateAndGetAsync(mergedFilter, Builders<T>.Update.Combine(defs), _options, this.Session(), cancellation).ConfigureAwait(false);
        }

        /// <summary>
        /// Run the update command with pipeline stages and retrieve the first document modified
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<TProjection> ExecutePipelineAsync(CancellationToken cancellation = default)
        {
            var mergedFilter = MergedFilter;
            if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
            if (_stages.Count == 0) throw new ArgumentException("Please use WithPipelineStage() method first!");
            if (defs.Count > 0) throw new ArgumentException("Pipeline updates cannot be used together with regular updates!");
            if (ShouldSetModDate()) WithPipelineStage($"{{ $set: {{ '{Cache<T>.Instance.ModifiedOnPropName}': new Date() }} }}");


            return UpdateAndGetAsync(mergedFilter, Builders<T>.Update.Pipeline(_stages.ToArray()), _options, this.Session(), cancellation);
        }

        private bool ShouldSetModDate()
        {
            //only set mod date by library if user hasn't done anything with the ModifiedOn property

            return
                Cache<T>.Instance.HasModifiedOn &&
                !defs.Any(d => d
                       .Render(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry)
                       .ToString()
                       .Contains($"\"{Cache<T>.Instance.ModifiedOnPropName}\""));
        }

        private Task<TProjection> UpdateAndGetAsync(FilterDefinition<T> filter, UpdateDefinition<T> definition, FindOneAndUpdateOptions<T, TProjection> options, IClientSessionHandle? session = null, CancellationToken cancellation = default)
        {
            return session == null
                ? Collection.FindOneAndUpdateAsync(filter, definition, options, cancellation)
                : Collection.FindOneAndUpdateAsync(session, filter, definition, options, cancellation);
        }
    }
}
