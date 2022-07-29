namespace MongoDB.Entities.Configuration;

using System;
using System.Linq.Expressions;

/// <summary>
/// Describes a DocumentReference
/// </summary>
internal class DocumentRelationConfig<T> : PerRelationConfiguration<T>
{
    public DocumentRelationConfig(Expression<Func<T, object>> idSelector, string fieldName, Type otherType, string otherDatabaseName, string otherCollectionName) : base(fieldName, otherType, otherDatabaseName, otherCollectionName)
    {
        IdSelector = idSelector;
    }

    /// <summary>
    /// Selects an Id from the type
    /// </summary>
    public Expression<Func<T, object>> IdSelector { get; }
}
