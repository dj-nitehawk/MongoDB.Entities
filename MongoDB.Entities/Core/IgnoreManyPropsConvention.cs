using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Entities.NewMany;

namespace MongoDB.Entities;

internal class IgnoreManyPropsConvention : ConventionBase, IMemberMapConvention
{
    public void Apply(BsonMemberMap mMap)
    {
        if (typeof(IMany<,>).IsAssignableFrom(mMap.MemberType))
        {
            _ = mMap.SetShouldSerializeMethod(_ => false);
        }
    }
}
