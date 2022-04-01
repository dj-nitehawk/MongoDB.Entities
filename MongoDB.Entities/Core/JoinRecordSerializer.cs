namespace MongoDB.Entities;

internal class JoinRecordSerializer<T1, T2> : SerializerBase<JoinRecord<T1, T2>>, IBsonDocumentSerializer
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JoinRecord<T1, T2> value)
    {
        //documents will exist in 2 formats

    }
    public override JoinRecord<T1, T2> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return new JoinRecord<T1, T2>(default, default);
    }

    public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo? serializationInfo)
    {
        switch (memberName)
        {
            case nameof(JoinRecord<T1, T2>.ID):
                serializationInfo = null;
                return false;
            default:
                serializationInfo = null;
                return false;
        }
    }
}