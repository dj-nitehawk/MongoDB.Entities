
namespace MongoDB.Entities
{
    public interface IIndexBuilder<T, TSelf>
        where TSelf : IIndexBuilder<T, TSelf>
    {
        /// <summary>
        /// Adds a key definition to the index
        /// <para>TIP: At least one key definition is required</para>
        /// </summary>
        /// <param name="propertyToIndex">x => x.PropertyName</param>
        /// <param name="type">The type of the key</param>
        TSelf Key(Expression<Func<T, object>> propertyToIndex, KeyType type);

        /// <summary>
        /// Set the options for this index definition
        /// <para>TIP: Setting options is not required.</para>
        /// </summary>
        /// <param name="option">x => x.OptionName = OptionValue</param>
        TSelf Option(Action<CreateIndexOptions<T>> option);
    }
}