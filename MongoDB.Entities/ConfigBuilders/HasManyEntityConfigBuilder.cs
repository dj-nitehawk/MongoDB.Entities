using MongoDB.Entities.NewMany;

namespace MongoDB.Entities.ConfigBuilders;

public class HasManyEntityConfigBuilder<T1, T2>
{
    internal string PropName { get; }
    internal EntityConfigBuilder<T1> Parent { get; }
    internal MemberExpression MemberExp { get; }
    internal Expression<Func<T1, IManyRelation<T2>>> Selector { get; }
    public HasManyEntityConfigBuilder(EntityConfigBuilder<T1> parent, string propName, MemberExpression memberExp, Expression<Func<T1, IManyRelation<T2>>> selector)
    {
        Parent = parent;
        PropName = propName;
        MemberExp = memberExp;
        Selector = selector;
    }

    public void WithOne(Expression<Func<T2, IOneRelation<T1>>> oneInverse)
    {
        if (oneInverse.Body is MemberExpression inverseMember)
        {
            Parent._relationDecisions.Add(new OneManyRelationDecision(typeof(T2), inverseMember.Member.Name, typeof(T1), PropName));
        }
        throw new ArgumentException("must be a Member expression", nameof(oneInverse));
    }
    public void WithOne()
    {
        Parent._relationDecisions.Add(new OneManyRelationDecision(typeof(T2), default, typeof(T1), PropName));
    }

    public void WithMany(Expression<Func<T2, IManyRelation<T1>>> manyInverse)
    {
        if (manyInverse.Body is MemberExpression inverseMember)
        {
            Parent._relationDecisions.Add(new ManyManyRelationDecision(typeof(T1), PropName, typeof(T2), inverseMember.Member.Name));
        }
        throw new ArgumentException("must be a Member expression", nameof(manyInverse));
    }
    public void WithMany()
    {
        Parent._relationDecisions.Add(new ManyManyRelationDecision(typeof(T1), PropName, typeof(T2), default));
    }
}
