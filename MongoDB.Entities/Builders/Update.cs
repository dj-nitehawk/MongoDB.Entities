namespace MongoDB.Entities;

public abstract class UpdateBase<T, TId, TSelf> : FilterQueryBase<T, TSelf>
    where TId : IComparable<TId>, IEquatable<TId>
    where T : IEntity<TId>
    where TSelf : UpdateBase<T, TId, TSelf>
{
    protected readonly List<UpdateDefinition<T>> defs;
    protected readonly Action<TSelf>? onUpdateAction;

    public abstract DBContext Context { get; }
    private EntityCache<T>? _cache;
    internal EntityCache<T> Cache() => _cache ??= Context.Cache<T>();

    internal UpdateBase(UpdateBase<T, TId, TSelf> other) : base(other)
    {
        onUpdateAction = other.onUpdateAction;
        defs = other.defs;
    }
    private TSelf This => (TSelf)this;
    internal UpdateBase(Dictionary<Type, (object filterDef, bool prepend)> globalFilters, Action<TSelf>? onUpdateAction = null, List<UpdateDefinition<T>>? defs = null) : base(globalFilters)
    {
        this.onUpdateAction = onUpdateAction;
        this.defs = defs ?? new();
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


    /// <summary>
    /// Specify the property and it's value to modify (use multiple times if needed)
    /// </summary>
    /// <param name="property">x => x.Property</param>
    /// <param name="value">The value to set on the property</param>
    public TSelf Modify<TProp>(Expression<Func<T, TProp>> property, TProp value)
    {
        AddModification(property, value);
        return This;
    }

    /// <summary>
    /// Specify the update definition builder operation to modify the Entities (use multiple times if needed)
    /// </summary>
    /// <param name="operation">b => b.Inc(x => x.PropName, Value)</param>
    /// <returns></returns>
    public TSelf Modify(Func<UpdateDefinitionBuilder<T>, UpdateDefinition<T>> operation)
    {
        AddModification(operation);
        return This;
    }

    /// <summary>
    /// Specify an update (json string) to modify the Entities (use multiple times if needed)
    /// </summary>
    /// <param name="update">{ $set: { 'RootProp.$[x].SubProp' : 321 } }</param>
    public TSelf Modify(string update)
    {
        AddModification(update);
        return This;
    }

    /// <summary>
    /// Specify an update with a Template to modify the Entities (use multiple times if needed)
    /// </summary>
    /// <param name="template">A Template with a single update</param>
    public TSelf Modify(Template template)
    {
        AddModification(template.RenderToString());
        return This;
    }

    /// <summary>
    /// Modify ALL properties with the values from the supplied entity instance.
    /// </summary>
    /// <param name="entity">The entity instance to read the property values from</param>
    public TSelf ModifyWith(T entity)
    {

        if (Cache().HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
        defs.AddRange(Logic.BuildUpdateDefs(entity, Context));
        return This;
    }

    /// <summary>
    /// Modify ONLY the specified properties with the values from a given entity instance.
    /// </summary>
    /// <param name="members">A new expression with the properties to include. Ex: <c>x => new { x.PropOne, x.PropTwo }</c></param>
    /// <param name="entity">The entity instance to read the corresponding values from</param>
    public TSelf ModifyOnly(Expression<Func<T, object>> members, T entity)
    {
        if (Cache().HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
        defs.AddRange(Logic.BuildUpdateDefs(entity, members, Context));
        return This;
    }

    /// <summary>
    /// Modify all EXCEPT the specified properties with the values from a given entity instance.
    /// </summary>
    /// <param name="members">Supply a new expression with the properties to exclude. Ex: <c>x => new { x.Prop1, x.Prop2 }</c></param>
    /// <param name="entity">The entity instance to read the corresponding values from</param>
    public TSelf ModifyExcept(Expression<Func<T, object>> members, T entity)
    {
        if (Cache().HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
        defs.AddRange(Logic.BuildUpdateDefs(entity, members, Context, excludeMode: true));
        return This;
    }
}

/// <summary>
/// Represents an update command
/// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
/// <typeparam name="TId">ID type</typeparam>
public class Update<T, TId> : UpdateBase<T, TId, Update<T, TId>>, ICollectionRelated<T>
    where TId : IComparable<TId>, IEquatable<TId>
    where T : IEntity<TId>
{
    private readonly List<PipelineStageDefinition<T, T>> _stages = new();
    private UpdateOptions _options = new();
    private readonly List<UpdateManyModel<T>> _models = new();

    internal Update(DBContext context, IMongoCollection<T> collection, UpdateBase<T, TId, Update<T, TId>> other) : base(other)
    {
        Context = context;
        Collection = collection;
    }

    internal Update(DBContext context, IMongoCollection<T> collection, Action<Update<T, TId>>? onUpdateAction, List<UpdateDefinition<T>>? defs = null) : base(context.GlobalFilters, onUpdateAction, defs)
    {
        Context = context;
        Collection = collection;
    }

    public override DBContext Context { get; }
    public IMongoCollection<T> Collection { get; }


    /// <summary>
    /// Specify an update pipeline with multiple stages using a Template to modify the Entities.
    /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
    /// </summary>
    /// <param name="template">A Template object containing multiple pipeline stages</param>
    public Update<T, TId> WithPipeline(Template template)
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
    public Update<T, TId> WithPipelineStage(string stage)
    {
        _stages.Add(stage);
        return this;
    }

    /// <summary>
    /// Specify an update pipeline stage using a Template to modify the Entities (use multiple times if needed)
    /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
    /// </summary>
    /// <param name="template">A Template object containing a pipeline stage</param>
    public Update<T, TId> WithPipelineStage(Template template)
    {
        return WithPipelineStage(template.RenderToString());
    }

    /// <summary>
    /// Specify an array filter to target nested entities for updates (use multiple times if needed).
    /// </summary>
    /// <param name="filter">{ 'x.SubProp': { $gte: 123 } }</param>
    public Update<T, TId> WithArrayFilter(string filter)
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
    public Update<T, TId> WithArrayFilter(Template template)
    {
        WithArrayFilter(template.RenderToString());
        return this;
    }

    /// <summary>
    /// Specify multiple array filters with a Template to target nested entities for updates.
    /// </summary>
    /// <param name="template">The template with an array [...] of filters</param>
    public Update<T, TId> WithArrayFilters(Template template)
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
    public Update<T, TId> Option(Action<UpdateOptions> option)
    {
        option(_options);
        return this;
    }



    /// <summary>
    /// Queue up an update command for bulk execution later.
    /// </summary>
    public Update<T, TId> AddToQueue()
    {
        var mergedFilter = MergedFilter;
        if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
        if (defs.Count == 0) throw new ArgumentException("Please use Modify() method first!");
        if (ShouldSetModDate()) Modify(b => b.CurrentDate(Cache().ModifiedOnPropName));
        onUpdateAction?.Invoke(this);
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
            if (ShouldSetModDate()) Modify(b => b.CurrentDate(Cache().ModifiedOnPropName));

            onUpdateAction?.Invoke(this);
            return await UpdateAsync(mergedFilter, Builders<T>.Update.Combine(defs), _options, cancellation).ConfigureAwait(false);
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
        if (ShouldSetModDate()) WithPipelineStage($"{{ $set: {{ '{Cache().ModifiedOnPropName}': new Date() }} }}");

        return UpdateAsync(
            mergedFilter,
            Builders<T>.Update.Pipeline(_stages.ToArray()),
            _options,
            cancellation);
    }

    private bool ShouldSetModDate()
    {
        //only set mod date by library if user hasn't done anything with the ModifiedOn property

        return
            Cache().HasModifiedOn &&
            !defs.Any(d => d
                   .Render(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry)
                   .ToString()
                   .Contains($"\"{Cache().ModifiedOnPropName}\""));
    }

    private Task<UpdateResult> UpdateAsync(FilterDefinition<T> filter, UpdateDefinition<T> definition, UpdateOptions options, CancellationToken cancellation = default)
    {
        return Context.Session is null
               ? Collection.UpdateManyAsync(filter, definition, options, cancellation)
               : Collection.UpdateManyAsync(Context.Session, filter, definition, options, cancellation);
    }

}
