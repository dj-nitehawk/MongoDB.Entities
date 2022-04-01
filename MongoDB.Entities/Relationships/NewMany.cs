namespace MongoDB.Entities.NewMany;

/*    
 results in the following:
 =========
 Collection A:
 _id
 BRef: { ID: xxxxxxxx }
 ------
 Collection B:
 _id
 -------
 Collection C:
 _id
 ------
 Collection B~C(CRef):
 ParentId: b._id
 ChildId: c._id
*/
/*
Queries needed:
One-Many (B-A)
 * for a document B (parent) get children (A) {collection A where BRef == B.id}
 * for a document A (child) get parent (B) {A.BRef}
 * for multiple documents B (parents) get children (A) [to solve N+1 problem] {collection A where BRef.ID in b_ids}
 * for multiple documents A (children) get parents (B) [to solve N+1 problem] {collection B where _id in a_BRefs}
 * Add A to the Many list in B
 * set A.BRef = B.Id

Many(Parent)-Many(Child) (B-C)
 * for a document B (parent) get children (C) {collection B~C(CRef) where ParentId==B.id} then {join result ChildId with collection C}
 * for a document C (child) get parents (B) {collection B~C(CRef) where ChildId==C.id} then {join result ParentId with collection B}
 * for multiple documents B (parents) get children (C) [to solve N+1 problem] {collection B~C(CRef) where ParentId in b_ids} then {join result ChildId with collection C}
 * for multiple documents C (children) get parents (B) [to solve N+1 problem] {collection B~C(CRef) where ChildId in c_ids} then {join result ParentId with collection B}
 * Add B to the Many list in C
 * Add C to the Many list in B
 * Add an arbitrary join record
 */
//class A : Entity
//{
//    public One<B, ObjectId>? SingleB1 { get; set; }
//    public One<B, ObjectId>? SingleB2 { get; set; }
//}
//class B : Entity<ObjectId>
//{
//    public IManyS<A> ManyAVia1 { get; set; }
//    public IManyS<A> ManyAVia2 { get; set; }

//    [OwnerSide]
//    public IMany<C, Guid> ManyC { get; set; }

//    public B()
//    {
//        ManyAVia1 = (IManyS<A>)this.InitManyToOne(x => x.ManyAVia1, x => x.SingleB1);
//        ManyAVia2 = (IManyS<A>)this.InitManyToOne(x => x.ManyAVia2, x => x.SingleB2);
//        ManyC = this.InitManyToMany(x => x.ManyC, x => x.ManyB);
//    }

//    public override ObjectId GenerateNewID()
//    {
//        return ObjectId.GenerateNewId();
//    }
//}
//class C : Entity<Guid>
//{
//    [InverseSide]
//    public IMany<B, ObjectId> ManyB { get; set; }

//    public C()
//    {
//        ManyB = this.InitManyToMany(c => c.ManyB, b => b.ManyC);
//    }

//    public override Guid GenerateNewID() => Guid.NewGuid();
//}

public interface IManyRelation<TChild>
{
    Find<TChild, TChild> GetChildrenFind(DBContext context, string? childCollectionName = null, IMongoCollection<TChild>? collection = null);
    Find<TChild, TProjection> GetChildrenFind<TProjection>(DBContext context, string? childCollectionName = null, IMongoCollection<TChild>? collection = null);
    IMongoQueryable<TChild> GetChildrenQuery(DBContext context, string? childCollectionName = null, IMongoCollection<TChild>? collection = null);
}

public interface IMany<TParent, TChild> : IManyRelation<TChild>
{
    TParent Parent { get; }

    FilterDefinition<TChild> GetFilterForSingleDocument();
}

public interface IManyToMany<TParent, TChild> : IMany<TParent, TChild>
{
    bool IsParentOwner { get; }
}
public interface IManyToOne<TParent, TChild> : IMany<TParent, TChild>
{
}


/// <summary>
/// Marker class
/// </summary>
/// <typeparam name="TChild"></typeparam>
/// <typeparam name="TChildId"></typeparam>
public abstract class Many<TChild> : IManyRelation<TChild>
{
    protected Many(PropertyInfo parentProperty, PropertyInfo childProperty)
    {
        ParentProperty = parentProperty;
        ChildProperty = childProperty;
    }

    internal PropertyInfo ParentProperty { get; }
    internal PropertyInfo ChildProperty { get; }

    public abstract IMongoQueryable<TChild> GetChildrenQuery(DBContext context, string? childCollectionName = null, IMongoCollection<TChild>? collection = null);
    public Find<TChild, TChild> GetChildrenFind(DBContext context, string? childCollectionName = null, IMongoCollection<TChild>? collection = null) => GetChildrenFind<TChild>(context, childCollectionName, collection);
    public abstract Find<TChild, TProjection> GetChildrenFind<TProjection>(DBContext context, string? childCollectionName = null, IMongoCollection<TChild>? collection = null);
}
public abstract class Many<TParent, TChild> : Many<TChild>, IMany<TParent, TChild>
{
    public TParent Parent { get; }
    protected Many(TParent parent, PropertyInfo parentProperty, PropertyInfo childProperty) : base(parentProperty, childProperty)
    {
        Parent = parent;
    }
    public FilterDefinition<TChild> GetFilterForSingleDocument() => Builders<TChild>.Filter.Eq(ChildProperty.Name, /*TODO: uncomment me Parent.ID*/ "");
}

public sealed class ManyToMany<TParent, TChild> : Many<TParent, TChild>, IManyToMany<TParent, TChild>
{
    public ManyToMany(bool isParentOwner, TParent parent, PropertyInfo parentProperty, PropertyInfo childProperty) : base(parent, parentProperty, childProperty)
    {
        IsParentOwner = isParentOwner;
    }

    public bool IsParentOwner { get; }

    public override Find<TChild, TProjection> GetChildrenFind<TProjection>(DBContext context, string? childCollectionName = null, IMongoCollection<TChild>? collection = null)
    {
        throw new NotImplementedException();
    }

    public override IMongoQueryable<TChild> GetChildrenQuery(DBContext context, string? childCollectionName = null, IMongoCollection<TChild>? collection = null)
    {
        throw new NotImplementedException();
    }
}

public sealed class ManyToOne<TParent, TChild> : Many<TParent, TChild>, IManyToOne<TParent, TChild>
{
    public ManyToOne(TParent parent, PropertyInfo parentProperty, PropertyInfo childProperty) : base(parent, parentProperty, childProperty)
    {
    }

    public override Find<TChild, TProjection> GetChildrenFind<TProjection>(DBContext context, string? childCollectionName = null, IMongoCollection<TChild>? childCollection = null)
    {
        return context.Find<TChild, TProjection>(childCollectionName, childCollection)
            .Match(GetFilterForSingleDocument()); //BRef==Parent.Id
    }

    public override IMongoQueryable<TChild> GetChildrenQuery(DBContext context, string? childCollectionName = null, IMongoCollection<TChild>? childCollection = null)
    {
        return context.Queryable(collectionName: childCollectionName, collection: childCollection)
             .Where(_ => GetFilterForSingleDocument().Inject());
    }
}


public static class RelationsExt
{
    public static PropertyInfo GetPropertyInfo<TSource, TProperty>(this Expression<Func<TSource, TProperty>> propertyLambda)
    {
        Type type = typeof(TSource);

        if (propertyLambda.Body is not MemberExpression member)
            throw new ArgumentException(string.Format(
                "Expression '{0}' refers to a method, not a property.",
                propertyLambda.ToString()));


        if (member.Member is not PropertyInfo propInfo)
            throw new ArgumentException(string.Format(
                "Expression '{0}' refers to a field, not a property.",
                propertyLambda.ToString()));

        if (type != propInfo.ReflectedType &&
            !type.IsSubclassOf(propInfo.ReflectedType))
            throw new ArgumentException(string.Format(
                "Expression '{0}' refers to a property that is not from type {1}.",
                propertyLambda.ToString(),
                type));

        return propInfo;
    }

    public static IManyToMany<TParent, TChild> InitManyToMany<TParent, TParentId, TChild, TChildId>(this TParent parent, Expression<Func<TParent, IMany<TChild, TChildId>>> propertyExpression, Expression<Func<TChild, IMany<TParent, TParentId>>> propertyOtherSide)
        where TParent : IEntity<TParentId>
        where TParentId : IComparable<TParentId>, IEquatable<TParentId>
        where TChild : IEntity<TChildId>
        where TChildId : IComparable<TChildId>, IEquatable<TChildId>
    {
        var property = propertyExpression.GetPropertyInfo();
        var hasOwnerAttrib = property.IsDefined(typeof(OwnerSideAttribute), false);
        var hasInverseAttrib = property.IsDefined(typeof(InverseSideAttribute), false);
        if (hasOwnerAttrib && hasInverseAttrib) throw new InvalidOperationException("Only one type of relationship side attribute is allowed on a property");
        if (!hasOwnerAttrib && !hasInverseAttrib) throw new InvalidOperationException("Missing attribute for determining relationship side of a many-to-many relationship");

        var osProperty = propertyOtherSide.GetPropertyInfo();
        var osHasOwnerAttrib = osProperty.IsDefined(typeof(OwnerSideAttribute), false);
        var osHasInverseAttrib = osProperty.IsDefined(typeof(InverseSideAttribute), false);
        if (osHasOwnerAttrib && osHasInverseAttrib) throw new InvalidOperationException("Only one type of relationship side attribute is allowed on a property");
        if (!osHasOwnerAttrib && !osHasInverseAttrib) throw new InvalidOperationException("Missing attribute for determining relationship side of a many-to-many relationship");

        if ((hasOwnerAttrib == osHasOwnerAttrib) || (hasInverseAttrib == osHasInverseAttrib)) throw new InvalidOperationException("Both sides of the relationship cannot have the same attribute");
        var res = new ManyToMany<TParent, TChild>(hasInverseAttrib, parent, property, osProperty);
        //should we set the property ourself or let the user handle it ?
        //property.SetValue(parent, res);
        return res;
    }
    public static IManyToOne<TParent, TChild> InitManyToOne<TParent, TParentId, TChild, TChildId>(this TParent parent, Expression<Func<TParent, IMany<TChild, TChildId>>> propertyExpression, Expression<Func<TChild, One<TParent, TParentId>?>> propertyOtherSide)
          where TParent : IEntity<TParentId>
        where TParentId : IComparable<TParentId>, IEquatable<TParentId>
        where TChild : IEntity<TChildId>
        where TChildId : IComparable<TChildId>, IEquatable<TChildId>
    {
        var property = propertyExpression.GetPropertyInfo();
        var osProperty = propertyOtherSide.GetPropertyInfo();

        return new ManyToOne<TParent, TChild>(parent, property, osProperty);
    }
}