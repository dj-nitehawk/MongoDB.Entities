namespace MongoDB.Entities.Configuration;

using System;

/// <summary>
/// Describes a CollectionReference
/// </summary>
internal class CollectionRelationConfig<T> : PerRelationConfiguration<T>
{
    public CollectionRelationConfig(string fieldName, Type otherType, string otherDatabaseName, string otherCollectionName) : base(fieldName, otherType, otherDatabaseName, otherCollectionName)
    {

    }
    //since this is the inverse side of a DocumentRelationConfig, we don't need to
    //add anything here, we only need to configure the DocumentRelationConfig for each relationship
}