namespace MongoDB.Entities.ConfigBuilders;

public class EntityConfigBuilder<T>
{
    internal DBContextConfigBuilder Parent { get; }
    internal DBContext Context => Parent.Context;
    internal HashSet<RelationDecision> _relationDecisions => Parent._relationDecisions;
    public EntityConfigBuilder(DBContextConfigBuilder parent, string collectionName)
    {
        Parent = parent;
        CollectionName = collectionName;
    }
    public string CollectionName { get; }

    internal Expression<Func<T, object>>? _keySelector;
    public void HasKey(Expression<Func<T, object>> keySelector)
    {
        _keySelector = keySelector;
    }
    public HasOneEntityConfigBuilder<T, TProp> HasOne<TProp>(Expression<Func<T, IOneRelation<TProp>>> oneSelector)
    {
        if (oneSelector.Body is MemberExpression member)
        {
            return new HasOneEntityConfigBuilder<T, TProp>(this, member.Member.Name, member, oneSelector);
        }
        throw new ArgumentException("Expression must be a member");
    }
}
