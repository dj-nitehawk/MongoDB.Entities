namespace MongoDB.Entities.ConfigBuilders;


internal class RelationDecision
{
    public RelationDecision(Type firstType, string propInFirst, Type secondType, string? propInSecond)
    {
        FirstType = firstType;
        PropInFirst = propInFirst;
        SecondType = secondType;
        PropInSecond = propInSecond;
    }

    public Type FirstType { get; }
    public string PropInFirst { get; }
    public Type SecondType { get; }
    public string? PropInSecond { get; }
}
internal class OneOneRelationDecision : RelationDecision
{
    public OneOneRelationDecision(Type firstType, string propInFirst, Type secondType, string? propInSecond) : base(firstType, propInFirst, secondType, propInSecond)
    {
    }
}

internal class OneManyRelationDecision : RelationDecision
{
    public OneManyRelationDecision(Type firstType, string propInFirst, Type secondType, string? propInSecond) : base(firstType, propInFirst, secondType, propInSecond)
    {
    }
}
