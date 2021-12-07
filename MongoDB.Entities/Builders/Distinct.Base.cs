namespace MongoDB.Entities;

public class DistinctBase<T, TProperty, TSelf> : FilterQueryBase<T, TSelf>, IDistinct<T,TProperty,TSelf>
    where TSelf : DistinctBase<T, TProperty, TSelf>
{
    internal DistinctOptions _options = new();
    internal FieldDefinition<T, TProperty>? _field;

    internal DistinctBase(DistinctBase<T, TProperty, TSelf> other) : base(other)
    {
        _options = other._options;
        _field = other._field;
    }
    internal DistinctBase(Dictionary<Type, (object filterDef, bool prepend)> globalFilters) : base(globalFilters)
    {
        _globalFilters = globalFilters;
    }


    private TSelf This => (TSelf)this;


    /// <summary>
    /// Specify an option for this find command (use multiple times if needed)
    /// </summary>
    /// <param name="option">x => x.OptionName = OptionValue</param>
    public TSelf Option(Action<DistinctOptions> option)
    {
        option(_options);
        return This;
    }

    /// <summary>
    /// Specify the property you want to get the unique values for (as a string path)
    /// </summary>
    /// <param name="property">ex: "Address.Street"</param>
    public TSelf Property(string property)
    {
        _field = property;
        return This;
    }

    /// <summary>
    /// Specify the property you want to get the unique values for (as a member expression)
    /// </summary>
    /// <param name="property">x => x.Address.Street</param>
    public TSelf Property(Expression<Func<T, TProperty>> property)
    {
        _field = property.FullPath();
        return This;
    }
}
