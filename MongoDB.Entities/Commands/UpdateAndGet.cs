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
    /// Update and retrieve the first document that was updated.
    /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public class UpdateAndGet<T> : UpdateAndGet<T, T> where T : IEntity
    {
        internal UpdateAndGet(IClientSessionHandle session = null) : base(session) { }
    }

    /// <summary>
    /// Update and retrieve the first document that was updated.
    /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TProjection">The type to project to</typeparam>
    public class UpdateAndGet<T, TProjection> where T : IEntity
    {
        private readonly Collection<UpdateDefinition<T>> defs = new Collection<UpdateDefinition<T>>();
        private readonly Collection<PipelineStageDefinition<T, TProjection>> stages = new Collection<PipelineStageDefinition<T, TProjection>>();
        private FilterDefinition<T> filter = Builders<T>.Filter.Empty;
        private readonly FindOneAndUpdateOptions<T, TProjection> options = new FindOneAndUpdateOptions<T, TProjection>() { ReturnDocument = ReturnDocument.After };
        private readonly IClientSessionHandle session;

        internal UpdateAndGet(IClientSessionHandle session = null)
        {
            this.session = session;
        }

        /// <summary>
        /// Specify an IEntity ID as the matching criteria
        /// </summary>
        /// <param name="ID">A unique IEntity ID</param>
        public UpdateAndGet<T, TProjection> MatchID(string ID)
        {
            return Match(f => f.Eq(t => t.ID, ID));
        }

        /// <summary>
        /// Specify the IEntity matching criteria with a lambda expression
        /// </summary>
        /// <param name="expression">A lambda expression to select the Entities to update</param>
        public UpdateAndGet<T, TProjection> Match(Expression<Func<T, bool>> expression)
        {
            return Match(f => f.Where(expression));
        }

        /// <summary>
        /// Specify the Entity matching criteria with a filter expression
        /// </summary>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        public UpdateAndGet<T, TProjection> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            this.filter &= filter(Builders<T>.Filter);
            return this;
        }

        /// <summary>
        /// Specify the Entity matching criteria with a Template
        /// </summary>
        /// <param name="template">The filter Template</param>
        public UpdateAndGet<T, TProjection> Match(Template template)
        {
            filter &= template.ToString();
            return this;
        }

        /// <summary>
        /// Specify the Entity matching criteria with a JSON string
        /// </summary>
        /// <param name="jsonString">{ Title : 'The Power Of Now' }</param>
        public UpdateAndGet<T, TProjection> Match(string jsonString)
        {
            filter &= jsonString;
            return this;
        }

        /// <summary>
        /// Specify the property and it's value to modify (use multiple times if needed)
        /// </summary>
        /// <param name="property">x => x.Property</param>
        /// <param name="value">The value to set on the property</param>
        public UpdateAndGet<T, TProjection> Modify<TProp>(Expression<Func<T, TProp>> property, TProp value)
        {
            defs.Add(Builders<T>.Update.Set(property, value));
            return this;
        }

        /// <summary>
        /// Specify the update definition builder operation to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="operation">b => b.Inc(x => x.PropName, Value)</param>
        public UpdateAndGet<T, TProjection> Modify(Func<UpdateDefinitionBuilder<T>, UpdateDefinition<T>> operation)
        {
            defs.Add(operation(Builders<T>.Update));
            return this;
        }

        /// <summary>
        /// Specify an update (json string) to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="update">{ $set: { 'RootProp.$[x].SubProp' : 321 } }</param>
        public UpdateAndGet<T, TProjection> Modify(string update)
        {
            defs.Add(update);
            return this;
        }

        /// <summary>
        /// Specify an update with a Template to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="template">A Template with a single update</param>
        public UpdateAndGet<T, TProjection> Modify(Template template)
        {
            Modify(template.ToString());
            return this;
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
                stages.Add(stage);
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
            stages.Add(stage);
            return this;
        }

        /// <summary>
        /// Specify an update pipeline stage using a Template to modify the Entities (use multiple times if needed)
        /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
        /// </summary>
        /// <param name="template">A Template object containing a pipeline stage</param>
        public UpdateAndGet<T, TProjection> WithPipelineStage(Template template)
        {
            return WithPipelineStage(template.ToString());
        }

        /// <summary>
        /// Specify an array filter to target nested entities for updates (use multiple times if needed).
        /// </summary>
        /// <param name="filter">{ 'x.SubProp': { $gte: 123 } }</param>
        public UpdateAndGet<T, TProjection> WithArrayFilter(string filter)
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
        public UpdateAndGet<T, TProjection> WithArrayFilter(Template template)
        {
            WithArrayFilter(template.ToString());
            return this;
        }

        /// <summary>
        /// Specify multiple array filters with a Template to target nested entities for updates.
        /// </summary>
        /// <param name="template">The template with an array [...] of filters</param>
        public UpdateAndGet<T, TProjection> WithArrayFilters(Template template)
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
        public UpdateAndGet<T, TProjection> Option(Action<FindOneAndUpdateOptions<T, TProjection>> option)
        {
            option(options);
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
            options.Projection = projection(Builders<T>.Projection);
            return this;
        }

        /// <summary>
        /// Run the update command in MongoDB and retrieve the first document modified
        /// </summary>
        public TProjection Execute()
        {
            return Run.Sync(() => ExecuteAsync());
        }

        /// <summary>
        /// Run the update command in MongoDB and retrieve the first document modified
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task<TProjection> ExecuteAsync(CancellationToken cancellation = default)
        {
            if (filter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
            if (defs.Count == 0) throw new ArgumentException("Please use Modify() method first!");
            if (stages.Count > 0) throw new ArgumentException("Regular updates and Pipeline updates cannot be used together!");
            if (Cache<T>.HasModifiedOn) Modify(b => b.CurrentDate(Cache<T>.ModifiedOnPropName));

            return await DB.UpdateAndGetAsync(filter, Builders<T>.Update.Combine(defs), options, session, cancellation).ConfigureAwait(false);
        }

        /// <summary>
        /// Run the update command with pipeline stages and retrieve the first document modified
        /// </summary>
        public TProjection ExecutePipeline()
        {
            return Run.Sync(() => ExecutePipelineAsync());
        }

        /// <summary>
        /// Run the update command with pipeline stages and retrieve the first document modified
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<TProjection> ExecutePipelineAsync(CancellationToken cancellation = default)
        {
            if (filter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
            if (stages.Count == 0) throw new ArgumentException("Please use WithPipelineStage() method first!");
            if (defs.Count > 0) throw new ArgumentException("Pipeline updates cannot be used together with regular updates!");
            if (Cache<T>.HasModifiedOn) WithPipelineStage($"{{ $set: {{ '{Cache<T>.ModifiedOnPropName}': new Date() }} }}");

            return DB.UpdateAndGetAsync(filter, Builders<T>.Update.Pipeline(stages.ToArray()), options, session, cancellation);
        }
    }
}
