using System;
using System.Linq.Expressions;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Returns a reference to this entity.
    /// </summary>
    public static One<T> ToReference<T>(this T entity) where T : IEntity
    {
        return new One<T>(entity);
    }


    /// <summary>
    /// Initializes supplied property with a new One-To-Many relationship.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="propertyToInit">() => PropertyName</param>
    public static void InitOneToMany<TChild>(this IEntity parent, Expression<Func<Many<TChild>?>> propertyToInit) where TChild : IEntity
    {
        var property = propertyToInit.PropertyInfo();
        property.SetValue(parent, new Many<TChild>(parent, property.Name));
    }

    /// <summary>
    /// Initializes supplied property with a new Many-To-Many relationship.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="propertyToInit">() = > PropertyName</param>
    /// <param name="propertyOtherSide">x => x.PropertyName</param>
    public static void InitManyToMany<TChild>(this IEntity parent, Expression<Func<Many<TChild>?>> propertyToInit, Expression<Func<TChild, object>> propertyOtherSide) where TChild : IEntity
    {
        var property = propertyToInit.PropertyInfo();
        var hasOwnerAttrib = property?.IsDefined(typeof(OwnerSideAttribute), false) ?? false;
        var hasInverseAttrib = property?.IsDefined(typeof(InverseSideAttribute), false) ?? false;
        if (hasOwnerAttrib && hasInverseAttrib) throw new InvalidOperationException("Only one type of relationship side attribute is allowed on a property");
        if (!hasOwnerAttrib && !hasInverseAttrib) throw new InvalidOperationException("Missing attribute for determining relationship side of a many-to-many relationship");

        var osProperty = propertyOtherSide.MemberInfo();
        var osHasOwnerAttrib = osProperty.IsDefined(typeof(OwnerSideAttribute), false);
        var osHasInverseAttrib = osProperty.IsDefined(typeof(InverseSideAttribute), false);
        if (osHasOwnerAttrib && osHasInverseAttrib) throw new InvalidOperationException("Only one type of relationship side attribute is allowed on a property");
        if (!osHasOwnerAttrib && !osHasInverseAttrib) throw new InvalidOperationException("Missing attribute for determining relationship side of a many-to-many relationship");

        if ((hasOwnerAttrib == osHasOwnerAttrib) || (hasInverseAttrib == osHasInverseAttrib)) throw new InvalidOperationException("Both sides of the relationship cannot have the same attribute");

        property?.SetValue(parent, new Many<TChild>(parent, property.Name, osProperty.Name, hasInverseAttrib));
    }
}
