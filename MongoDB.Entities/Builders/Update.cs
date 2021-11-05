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
    public class UpdateBase<T, TSelf> : FilterQueryBase<T, TSelf> where T : IEntity where TSelf : UpdateBase<T, TSelf>
    {
        //note: this base class exists for facilating the OnBeforeUpdate custom hook of DBContext class
        //      there's no other purpose for this.

        protected readonly List<UpdateDefinition<T>> defs = new();

        internal UpdateBase(FilterQueryBase<T, TSelf> other) : base(other)
        {
        }

        internal UpdateBase(Dictionary<Type, (object filterDef, bool prepend)> globalFilters) : base(globalFilters)
        {
        }

        /// <summary>
        /// Specify the property and it's value to modify (use multiple times if needed)
        /// </summary>
        /// <param name="property">x => x.Property</param>
        /// <param name="value">The value to set on the property</param>
        public void AddModification<TProp>(Expression<Func<T, TProp>> property, TProp value)
        {
            defs.Add(Builders<T>.Update.Set(property, value));
        }

        /// <summary>
        /// Specify the update definition builder operation to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="operation">b => b.Inc(x => x.PropName, Value)</param>
        public void AddModification(Func<UpdateDefinitionBuilder<T>, UpdateDefinition<T>> operation)
        {
            defs.Add(operation(Builders<T>.Update));
        }

        /// <summary>
        /// Specify an update (json string) to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="update">{ $set: { 'RootProp.$[x].SubProp' : 321 } }</param>
        public void AddModification(string update)
        {
            defs.Add(update);
        }

        /// <summary>
        /// Specify an update with a Template to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="template">A Template with a single update</param>
        public void AddModification(Template template)
        {
            AddModification(template.RenderToString());
        }

        //protected void SetTenantDbOnFileEntities(string tenantPrefix)
        //{
        //    if (Cache<T>.Instance.IsFileEntity)
        //    {
        //        defs.Add(Builders<T>.Update.Set(
        //            nameof(FileEntity.TenantPrefix),
        //            Cache<T>.Instance.Collection(tenantPrefix).Database.DatabaseNamespace.DatabaseName));
        //    }
        //}
    }

    /// <summary>
    /// Represents an update command
    /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public class Update<T> : UpdateBase<T, Update<T>>, ICollectionRelated<T> where T : IEntity
    {
        private readonly List<PipelineStageDefinition<T, T>> _stages = new();
        private UpdateOptions _options = new();
        private readonly List<UpdateManyModel<T>> _models = new();
        private readonly Action<Update<T>> _onUpdateAction;

        public DBContext Context { get; }
        public IMongoCollection<T> Collection { get; }

        internal Update(
            DBContext context,
            IMongoCollection<T> collection,
            Dictionary<Type, (object filterDef, bool prepend)> globalFilters,
            Action<Update<T>> onUpdateAction) :
            base(globalFilters)
        {
            Context = context;
            Collection = collection;
            _onUpdateAction = onUpdateAction;
        }



        /// <summary>
        /// Specify the property and it's value to modify (use multiple times if needed)
        /// </summary>
        /// <param name="property">x => x.Property</param>
        /// <param name="value">The value to set on the property</param>
        public Update<T> Modify<TProp>(Expression<Func<T, TProp>> property, TProp value)
        {
            AddModification(property, value);
            return this;
        }

        /// <summary>
        /// Specify the update definition builder operation to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="operation">b => b.Inc(x => x.PropName, Value)</param>
        /// <returns></returns>
        public Update<T> Modify(Func<UpdateDefinitionBuilder<T>, UpdateDefinition<T>> operation)
        {
            AddModification(operation);
            return this;
        }

        /// <summary>
        /// Specify an update (json string) to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="update">{ $set: { 'RootProp.$[x].SubProp' : 321 } }</param>
        public Update<T> Modify(string update)
        {
            AddModification(update);
            return this;
        }

        /// <summary>
        /// Specify an update with a Template to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="template">A Template with a single update</param>
        public Update<T> Modify(Template template)
        {
            AddModification(template.RenderToString());
            return this;
        }

        /// <summary>
        /// Modify ALL properties with the values from the supplied entity instance.
        /// </summary>
        /// <param name="entity">The entity instance to read the property values from</param>
        public Update<T> ModifyWith(T entity)
        {
            if (Cache<T>.Instance.HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
            defs.AddRange(Logic.BuildUpdateDefs(entity));
            return this;
        }

        /// <summary>
        /// Modify ONLY the specified properties with the values from a given entity instance.
        /// </summary>
        /// <param name="members">A new expression with the properties to include. Ex: <c>x => new { x.PropOne, x.PropTwo }</c></param>
        /// <param name="entity">The entity instance to read the corresponding values from</param>
        public Update<T> ModifyOnly(Expression<Func<T, object>> members, T entity)
        {
            if (Cache<T>.Instance.HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
            defs.AddRange(Logic.BuildUpdateDefs(entity, members));
            return this;
        }

        /// <summary>
        /// Modify all EXCEPT the specified properties with the values from a given entity instance.
        /// </summary>
        /// <param name="members">Supply a new expression with the properties to exclude. Ex: <c>x => new { x.Prop1, x.Prop2 }</c></param>
        /// <param name="entity">The entity instance to read the corresponding values from</param>
        public Update<T> ModifyExcept(Expression<Func<T, object>> members, T entity)
        {
            if (Cache<T>.Instance.HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
            defs.AddRange(Logic.BuildUpdateDefs(entity, members, excludeMode: true));
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
                _stages.Add(stage);
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
            _stages.Add(stage);
            return this;
        }

        /// <summary>
        /// Specify an update pipeline stage using a Template to modify the Entities (use multiple times if needed)
        /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
        /// </summary>
        /// <param name="template">A Template object containing a pipeline stage</param>
        public Update<T> WithPipelineStage(Template template)
        {
            return WithPipelineStage(template.RenderToString());
        }

        /// <summary>
        /// Specify an array filter to target nested entities for updates (use multiple times if needed).
        /// </summary>
        /// <param name="filter">{ 'x.SubProp': { $gte: 123 } }</param>
        public Update<T> WithArrayFilter(string filter)
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
        public Update<T> WithArrayFilter(Template template)
        {
            WithArrayFilter(template.RenderToString());
            return this;
        }

        /// <summary>
        /// Specify multiple array filters with a Template to target nested entities for updates.
        /// </summary>
        /// <param name="template">The template with an array [...] of filters</param>
        public Update<T> WithArrayFilters(Template template)
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
        public Update<T> Option(Action<UpdateOptions> option)
        {
            option(_options);
            return this;
        }



        /// <summary>
        /// Queue up an update command for bulk execution later.
        /// </summary>
        public Update<T> AddToQueue()
        {
            var mergedFilter = MergedFilter;
            if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
            if (defs.Count == 0) throw new ArgumentException("Please use Modify() method first!");
            if (Cache<T>.Instance.HasModifiedOn) Modify(b => b.CurrentDate(Cache<T>.Instance.ModifiedOnPropName));
            _onUpdateAction?.Invoke(this);
            _models.Add(new UpdateManyModel<T>(mergedFilter, Builders<T>.Update.Combine(defs))
            {
                ArrayFilters = _options.ArrayFilters,
                Collation = _options.Collation,
                Hint = _options.Hint,
                IsUpsert = _options.IsUpsert
            });
            _filter = Builders<T>.Filter.Empty;
            defs.Clear();
            _options = new UpdateOptions();
            return this;
        }

        /// <summary>
        /// Run the update command in MongoDB.
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task<UpdateResult> ExecuteAsync(CancellationToken cancellation = default)
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
                    return UpdateResult.Unacknowledged.Instance;

                return new UpdateResult.Acknowledged(bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount, null);
            }
            else
            {
                var mergedFilter = MergedFilter;
                if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
                if (defs.Count == 0) throw new ArgumentException("Please use a Modify() method first!");
                if (_stages.Count > 0) throw new ArgumentException("Regular updates and Pipeline updates cannot be used together!");
                if (ShouldSetModDate()) Modify(b => b.CurrentDate(Cache<T>.Instance.ModifiedOnPropName));

                _onUpdateAction?.Invoke(this);
                return await UpdateAsync(mergedFilter, Builders<T>.Update.Combine(defs), _options, this.Session(), cancellation).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Run the update command with pipeline stages
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<UpdateResult> ExecutePipelineAsync(CancellationToken cancellation = default)
        {
            var mergedFilter = MergedFilter;
            if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
            if (_stages.Count == 0) throw new ArgumentException("Please use WithPipelineStage() method first!");
            if (defs.Count > 0) throw new ArgumentException("Pipeline updates cannot be used together with regular updates!");
            if (ShouldSetModDate()) WithPipelineStage($"{{ $set: {{ '{Cache<T>.Instance.ModifiedOnPropName}': new Date() }} }}");

            return UpdateAsync(
                mergedFilter,
                Builders<T>.Update.Pipeline(_stages.ToArray()),
                _options,
                this.Session(),
                cancellation);
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

        private Task<UpdateResult> UpdateAsync(FilterDefinition<T> filter, UpdateDefinition<T> definition, UpdateOptions options, IClientSessionHandle? session = null, CancellationToken cancellation = default)
        {
            return session == null
                   ? Collection.UpdateManyAsync(filter, definition, options, cancellation)
                   : Collection.UpdateManyAsync(session, filter, definition, options, cancellation);
        }
    }
}
