namespace MongoDB.Entities;

public interface IDistinct<T, TProperty, TSelf>
    where TSelf : IDistinct<T, TProperty, TSelf>
{
    /// <summary>
    /// Specify an option for this find command (use multiple times if needed)
    /// </summary>
    /// <param name="option">x => x.OptionName = OptionValue</param>
    public TSelf Option(Action<DistinctOptions> option);

    /// <summary>
    /// Specify the property you want to get the unique values for (as a string path)
    /// </summary>
    /// <param name="property">ex: "Address.Street"</param>
    public TSelf Property(string property);

    /// <summary>
    /// Specify the property you want to get the unique values for (as a member expression)
    /// </summary>
    /// <param name="property">x => x.Address.Street</param>
    public TSelf Property(Expression<Func<T, TProperty>> property);


}
