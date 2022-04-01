namespace MongoDB.Entities;

public abstract class FindBase<T, TProjection, TSelf> :
    SortFilterQueryBase<T, TSelf>, IFindBuilder<T, TProjection, TSelf>
    where TSelf : FindBase<T, TProjection, TSelf>
{
    internal FindOptions<T, TProjection> _options = new();

    internal FindBase(FindBase<T, TProjection, TSelf> other) : base(other)
    {
        _options = other._options;
    }
    internal FindBase(Dictionary<Type, (object filterDef, bool prepend)> globalFilters) : base(globalFilters: globalFilters)
    {
        _globalFilters = globalFilters;
    }
    public abstract DBContext Context { get; }
    private TSelf This => (TSelf)this;


    public TSelf Skip(int skipCount)
    {
        _options.Skip = skipCount;
        return This;
    }


    public TSelf Limit(int takeCount)
    {
        _options.Limit = takeCount;
        return This;
    }


    public TSelf Project(Expression<Func<T, TProjection>> expression)
    {
        return Project(p => p.Expression(expression));
    }

    public TSelf Project(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, TProjection>> projection)
    {
        _options.Projection = projection(Builders<T>.Projection);
        return This;
    }


    public TSelf IncludeRequiredProps()
    {
        if (typeof(T) != typeof(TProjection))
            throw new InvalidOperationException("IncludeRequiredProps() cannot be used when projecting to a different type.");

        _options.Projection = Context.Cache<T>().CombineWithRequiredProps(_options.Projection);
        return This;
    }


    public TSelf ProjectExcluding(Expression<Func<T, object>> exclusion)
    {
        var props = (exclusion.Body as NewExpression)?.Arguments
            .Select(a => a.ToString().Split('.')[1]);

        if (props == null || !props.Any())
            throw new ArgumentException("Unable to get any properties from the exclusion expression!");

        var defs = new List<ProjectionDefinition<T>>(props.Count());

        foreach (var prop in props)
        {
            defs.Add(Builders<T>.Projection.Exclude(prop));
        }

        _options.Projection = Builders<T>.Projection.Combine(defs);

        return This;
    }


    public TSelf Option(Action<FindOptions<T, TProjection>> option)
    {
        option(_options);
        return This;
    }

    private void AddTxtScoreToProjection(string propName)
    {
        if (_options.Projection == null) _options.Projection = "{}";

        _options.Projection =
            _options.Projection
            .Render(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry)
            .Document.Add(propName, new BsonDocument { { "$meta", "textScore" } });
    }


    public TSelf SortByTextScore()
    {
        return SortByTextScore<object?>(null);
    }


    public TSelf SortByTextScore<TProp>(Expression<Func<T, TProp>>? scoreProperty)
    {
        switch (scoreProperty)
        {
            case null:
                AddTxtScoreToProjection("_Text_Match_Score_");
                return Sort(s => s.MetaTextScore("_Text_Match_Score_"));

            default:
                AddTxtScoreToProjection(Prop.Path(scoreProperty));
                return Sort(s => s.MetaTextScore(Prop.Path(scoreProperty)));
        }
    }
}
