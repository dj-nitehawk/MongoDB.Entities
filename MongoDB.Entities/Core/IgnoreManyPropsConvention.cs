using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Entities;

sealed class IgnoreManyPropsConvention : ConventionBase, IMemberMapConvention
{
    public void Apply(BsonMemberMap mMap)
    {
        if (mMap.MemberType.Name == ManyBase.PropTypeName)
            _ = mMap.SetShouldSerializeMethod(_ => false);
    }
}