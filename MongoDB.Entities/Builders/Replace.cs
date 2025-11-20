using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Entities;

/// <summary>
/// Represents an UpdateOne command, which can replace the first matched document with a given entity
/// <para>TIP: Specify a filter first with the .Match(). Then set entity with .WithEntity() and finally call .Execute() to run the command.</para>
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
public class Replace<T> where T : IEntity
{
    FilterDefinition<T> _filter = Builders<T>.Filter.Empty;
    ReplaceOptions _options = new();
    readonly IClientSessionHandle? _session;
    readonly List<ReplaceOneModel<T>> _models = [];
    readonly ModifiedBy? _modifiedBy;
    readonly Dictionary<Type, (object filterDef, bool prepend)>? _globalFilters;
    readonly Action<T>? _onSaveAction;
    readonly DB _db;
    bool _ignoreGlobalFilters;

    internal Replace(IClientSessionHandle? session,
                     ModifiedBy? modifiedBy,
                     Dictionary<Type, (object filterDef, bool prepend)>? globalFilters,
                     Action<T>? onSaveAction,
                     DB db)
    {
        _session = session;
        _modifiedBy = modifiedBy;
        _globalFilters = globalFilters;
        _onSaveAction = onSaveAction;
        _db = db;
    }

    T? Entity { get; set; }

    /// <summary>
    /// Specify an IEntity ID as the matching criteria
    /// </summary>
    /// <param name="ID">A unique IEntity ID</param>
    public Replace<T> MatchID(object ID)
        => Match(f => f.Eq(Cache<T>.IdPropName, ID));

    /// <summary>
    /// Specify the matching criteria with a lambda expression
    /// </summary>
    /// <param name="expression">x => x.Property == Value</param>
    public Replace<T> Match(Expression<Func<T, bool>> expression)
        => Match(f => f.Where(expression));

    /// <summary>
    /// Specify the matching criteria with a filter expression
    /// </summary>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    public Replace<T> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
    {
        _filter &= filter(Builders<T>.Filter);

        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a filter definition
    /// </summary>
    /// <param name="filterDefinition">A filter definition</param>
    public Replace<T> Match(FilterDefinition<T> filterDefinition)
    {
        _filter &= filterDefinition;

        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a template
    /// </summary>
    /// <param name="template">A Template with a find query</param>
    public Replace<T> Match(Template template)
    {
        _filter &= template.RenderToString();

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
    public Replace<T> Match(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string? language = null)
    {
        if (searchType != Search.Fuzzy)
        {
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

        searchTerm = searchTerm.ToDoubleMetaphoneHash();
        caseSensitive = false;
        diacriticSensitive = false;
        language = null;

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
    public Replace<T> Match(Expression<Func<T, object?>> coordinatesProperty,
                            Coordinates2D nearCoordinates,
                            double? maxDistance = null,
                            double? minDistance = null)
        => Match(f => f.Near(coordinatesProperty, nearCoordinates.ToGeoJsonPoint(), maxDistance, minDistance));

    /// <summary>
    /// Specify the matching criteria with a JSON string
    /// </summary>
    /// <param name="jsonString">{ Title : 'The Power Of Now' }</param>
    public Replace<T> MatchString(string jsonString)
    {
        _filter &= jsonString;

        return this;
    }

    /// <summary>
    /// Specify the matching criteria with an aggregation expression (i.e. $expr)
    /// </summary>
    /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
    public Replace<T> MatchExpression(string expression)
    {
        _filter &= "{$expr:" + expression + "}";

        return this;
    }

    /// <summary>
    /// Specify the matching criteria with a Template
    /// </summary>
    /// <param name="template">A Template object</param>
    public Replace<T> MatchExpression(Template template)
    {
        _filter &= "{$expr:" + template.RenderToString() + "}";

        return this;
    }

    /// <summary>
    /// Supply the entity to replace the first matched document with
    /// <para>TIP: If the entity ID is empty, a new ID will be generated before being stored</para>
    /// </summary>
    /// <param name="entity"></param>
    public Replace<T> WithEntity(T entity)
    {
        if (string.IsNullOrEmpty(entity.GetId().ToString()))
            throw new InvalidOperationException("Cannot replace an entity with an empty ID value!");

        _onSaveAction?.Invoke(entity);

        Entity = entity;

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
    /// Specify that this operation should ignore any global filters
    /// </summary>
    public Replace<T> IgnoreGlobalFilters()
    {
        _ignoreGlobalFilters = true;

        return this;
    }

    /// <summary>
    /// Queue up a replace command for bulk execution later.
    /// </summary>
    public Replace<T> AddToQueue()
    {
        var mergedFilter = Logic.MergeWithGlobalFilter(_ignoreGlobalFilters, _globalFilters, _filter);

        if (mergedFilter == Builders<T>.Filter.Empty)
            throw new ArgumentException("Please use Match() method first!");
        if (Entity == null)
            throw new ArgumentException("Please use WithEntity() method first!");

        SetModOnAndByValues();

        _models.Add(
            new(mergedFilter, Entity)
            {
                Collation = _options.Collation,
                Hint = _options.Hint,
                IsUpsert = _options.IsUpsert
            });
        _filter = Builders<T>.Filter.Empty;
        Entity = default;
        _options = new();

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
                                            _session == null
                                                ? _db.Collection<T>().BulkWriteAsync(_models, null, cancellation)
                                                : _db.Collection<T>().BulkWriteAsync(_session, _models, null, cancellation)
                                        ).ConfigureAwait(false);

            _models.Clear();

            return !bulkWriteResult.IsAcknowledged
                       ? ReplaceOneResult.Unacknowledged.Instance
                       : new ReplaceOneResult.Acknowledged(bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount, null);
        }

        var mergedFilter = Logic.MergeWithGlobalFilter(_ignoreGlobalFilters, _globalFilters, _filter);

        if (mergedFilter == Builders<T>.Filter.Empty)
            throw new ArgumentException("Please use Match() method first!");
        if (Entity == null)
            throw new ArgumentException("Please use WithEntity() method first!");

        SetModOnAndByValues();

        return _session == null
                   ? await _db.Collection<T>().ReplaceOneAsync(mergedFilter, Entity, _options, cancellation).ConfigureAwait(false)
                   : await _db.Collection<T>().ReplaceOneAsync(_session, mergedFilter, Entity, _options, cancellation).ConfigureAwait(false);
    }

    void SetModOnAndByValues()
    {
        if (Cache<T>.HasModifiedOn && Entity != null)
            ((IModifiedOn)Entity).ModifiedOn = DateTime.UtcNow;

        if (Cache<T>.ModifiedByProp != null && _modifiedBy != null)
        {
            Cache<T>.ModifiedByProp.SetValue(
                Entity,
                BsonSerializer.Deserialize(_modifiedBy.ToBson(), Cache<T>.ModifiedByProp.PropertyType));
        }
    }
}