using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

public abstract class UpdateBase<T> where T : IEntity
{
    //note: this base class exists for facilating the OnBeforeUpdate custom hook of DBContext class
    //      there's no other purpose for this.

    protected readonly List<UpdateDefinition<T>> defs = new();

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
}

/// <summary>
/// Represents an update command
/// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
public class Update<T> : UpdateBase<T> where T : IEntity
{
    private readonly List<PipelineStageDefinition<T, T>> stages = new();
    private FilterDefinition<T> filter = Builders<T>.Filter.Empty;
    private UpdateOptions options = new();
    private readonly IClientSessionHandle session;
    private readonly List<UpdateManyModel<T>> models = new();
    private readonly Dictionary<Type, (object filterDef, bool prepend)> globalFilters;
    private readonly Action<UpdateBase<T>> onUpdateAction;
    private bool ignoreGlobalFilters;

    internal Update(
        IClientSessionHandle session,
        Dictionary<Type, (object filterDef, bool prepend)> globalFilters,
        Action<UpdateBase<T>> onUpdateAction)
    {
        this.session = session;
        this.globalFilters = globalFilters;
        this.onUpdateAction = onUpdateAction;
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
    /// Specify the matching criteria with a filter definition
    /// </summary>
    /// <param name="filterDefinition">A filter definition</param>
    public Update<T> Match(FilterDefinition<T> filterDefinition)
    {
        filter &= filterDefinition;
        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a template
    /// </summary>
    /// <param name="template">A Template with a find query</param>
    public Update<T> Match(Template template)
    {
        filter &= template.RenderToString();
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
        filter &= "{$expr:" + template.RenderToString() + "}";
        return this;
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
        if (Cache<T>.HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
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
        if (Cache<T>.HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
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
        if (Cache<T>.HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
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
        return WithPipelineStage(template.RenderToString());
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
            ? new[] { def }
            : options.ArrayFilters.Concat(new[] { def });

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
    /// Specify that this operation should ignore any global filters
    /// </summary>
    public Update<T> IgnoreGlobalFilters()
    {
        ignoreGlobalFilters = true;
        return this;
    }

    /// <summary>
    /// Queue up an update command for bulk execution later.
    /// </summary>
    public Update<T> AddToQueue()
    {
        var mergedFilter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, globalFilters, filter);
        if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
        if (defs.Count == 0) throw new ArgumentException("Please use Modify() method first!");
        if (ShouldSetModDate()) Modify(b => b.CurrentDate(Cache<T>.ModifiedOnPropName));
        onUpdateAction?.Invoke(this);
        models.Add(new UpdateManyModel<T>(mergedFilter, Builders<T>.Update.Combine(defs))
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

            return !bulkWriteResult.IsAcknowledged
                ? UpdateResult.Unacknowledged.Instance
                : new UpdateResult.Acknowledged(bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount, null);
        }
        else
        {
            var mergedFilter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, globalFilters, filter);
            if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
            if (defs.Count == 0) throw new ArgumentException("Please use a Modify() method first!");
            if (stages.Count > 0) throw new ArgumentException("Regular updates and Pipeline updates cannot be used together!");
            if (ShouldSetModDate()) Modify(b => b.CurrentDate(Cache<T>.ModifiedOnPropName));
            onUpdateAction?.Invoke(this);
            return await UpdateAsync(mergedFilter, Builders<T>.Update.Combine(defs), options, session, cancellation).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Run the update command with pipeline stages
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<UpdateResult> ExecutePipelineAsync(CancellationToken cancellation = default)
    {
        var mergedFilter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, globalFilters, filter);
        if (mergedFilter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
        if (stages.Count == 0) throw new ArgumentException("Please use WithPipelineStage() method first!");
        if (defs.Count > 0) throw new ArgumentException("Pipeline updates cannot be used together with regular updates!");
        if (ShouldSetModDate()) WithPipelineStage($"{{ $set: {{ '{Cache<T>.ModifiedOnPropName}': new Date() }} }}");

        return UpdateAsync(
            mergedFilter,
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
