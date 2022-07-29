namespace MongoDB.Entities.Configuration;

using System;

internal abstract class PerRelationConfiguration<T>
{
    protected PerRelationConfiguration(string fieldName, Type otherType, string otherDatabaseName, string otherCollectionName)
    {
        FieldName = fieldName;
        OtherType = otherType;
        OtherDatabaseName = otherDatabaseName;
        OtherCollectionName = otherCollectionName;
    }

    public string FieldName { get; set; }
    public Type OtherType { get; set; }

    /// <summary>
    /// Allows for cross-server queries
    /// </summary>
    public string? OtherConnectionString { get; set; }

    /// <summary>
    /// Allows for cross-database queries
    /// </summary>
    public string? OtherDatabaseName { get; set; }

    /// <summary>
    /// The name of the other collection
    /// </summary>
    public string OtherCollectionName { get; set; }
}
