namespace MongoDB.Entities.ConfigBuilders;


internal class RelationDecision : IEquatable<RelationDecision?>
{
    public RelationDecision(Type firstType, string? propInFirst, Type secondType, string? propInSecond)
    {
        FirstType = firstType;
        PropInFirst = propInFirst;
        SecondType = secondType;
        PropInSecond = propInSecond;
    }

    public Type FirstType { get; }
    public string? PropInFirst { get; }
    public Type SecondType { get; }
    public string? PropInSecond { get; }

    public override bool Equals(object? obj)
    {
        return Equals(obj as RelationDecision);
    }

    public bool Equals(RelationDecision? other)
    {
        return other != null &&
               EqualityComparer<Type>.Default.Equals(FirstType, other.FirstType) &&
               PropInFirst == other.PropInFirst &&
               EqualityComparer<Type>.Default.Equals(SecondType, other.SecondType) &&
               PropInSecond == other.PropInSecond;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FirstType, PropInFirst, SecondType, PropInSecond);
    }

    public static bool operator ==(RelationDecision? left, RelationDecision? right)
    {
        return EqualityComparer<RelationDecision?>.Default.Equals(left, right);
    }

    public static bool operator !=(RelationDecision? left, RelationDecision? right)
    {
        return !(left == right);
    }
}
internal class OneOneRelationDecision : RelationDecision
{
    public OneOneRelationDecision(Type firstType, string? propInFirst, Type secondType, string? propInSecond) : base(firstType, propInFirst, secondType, propInSecond)
    {
    }
}

/// <summary>
/// First is One
/// Second is Many
/// </summary>
internal class OneManyRelationDecision : RelationDecision
{
    public OneManyRelationDecision(Type firstType, string? propInFirst, Type secondType, string? propInSecond) : base(firstType, propInFirst, secondType, propInSecond)
    {
    }
}
internal class ManyManyRelationDecision : RelationDecision
{
    public ManyManyRelationDecision(Type firstType, string? propInFirst, Type secondType, string? propInSecond) : base(firstType, propInFirst, secondType, propInSecond)
    {
    }
}
