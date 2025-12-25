using System;
using System.Linq.Expressions;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Returns a reference to this entity.
    /// </summary>
    public static One<T> ToReference<T>(this T entity) where T : IEntity
        => new(entity);

    /// <summary>
    /// Initializes supplied property with a new One-To-Many relationship.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="propertyToInit">() => PropertyName</param>
    /// <param name="db">the database instance to work with</param>
    public static void InitOneToMany<TChild, TParent>(this TParent parent, Expression<Func<Many<TChild, TParent>?>> propertyToInit, DB db)
        where TChild : IEntity where TParent : IEntity
    {
        var property = propertyToInit.PropertyInfo();
        property.SetValue(parent, new Many<TChild, TParent>(parent, property.Name, db));
    }

    /// <summary>
    /// Initializes supplied property with a new Many-To-Many relationship.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="propertyToInit">() = > PropertyName</param>
    /// <param name="propertyOtherSide">x => x.PropertyName</param>
    /// <param name="db">the DB instance to work with</param>
    public static void InitManyToMany<TChild, TParent>(this IEntity parent,
                                                       Expression<Func<Many<TChild, TParent>?>> propertyToInit,
                                                       Expression<Func<TChild, object>?> propertyOtherSide,
                                                       DB db)
        where TChild : IEntity where TParent : IEntity
    {
        var property = propertyToInit.PropertyInfo();
        var hasOwnerAttrib = property.IsDefined(typeof(OwnerSideAttribute), false);
        var hasInverseAttrib = property.IsDefined(typeof(InverseSideAttribute), false);

        switch (hasOwnerAttrib)
        {
            case true when hasInverseAttrib:
                throw new InvalidOperationException("Only one type of relationship side attribute is allowed on a property");
            case false when !hasInverseAttrib:
                throw new InvalidOperationException("Missing attribute for determining relationship side of a many-to-many relationship");
        }

        var osProperty = propertyOtherSide.MemberInfo();
        var osHasOwnerAttrib = osProperty.IsDefined(typeof(OwnerSideAttribute), false);
        var osHasInverseAttrib = osProperty.IsDefined(typeof(InverseSideAttribute), false);

        switch (osHasOwnerAttrib)
        {
            case true when osHasInverseAttrib:
                throw new InvalidOperationException("Only one type of relationship side attribute is allowed on a property");
            case false when !osHasInverseAttrib:
                throw new InvalidOperationException("Missing attribute for determining relationship side of a many-to-many relationship");
        }

        if (hasOwnerAttrib == osHasOwnerAttrib || hasInverseAttrib == osHasInverseAttrib)
            throw new InvalidOperationException("Both sides of the relationship cannot have the same attribute");

        property.SetValue(parent, new Many<TChild, TParent>(parent, property.Name, osProperty.Name, hasInverseAttrib, db));
    }
}