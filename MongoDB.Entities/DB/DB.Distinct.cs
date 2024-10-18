using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public static partial class DB
{
    /// <summary>
    /// Represents a MongoDB Distinct command where you can get back distinct values for a given property of a given Entity.
    /// </summary>
    /// <typeparam name="T">Any Entity that implements IEntity interface</typeparam>
    /// <typeparam name="TProperty">The type of the property of the entity you'd like to get unique values for</typeparam>
    /// <param name="session">An optional session if using within a transaction</param>
    public static Distinct<T, TProperty> Distinct<T, TProperty>(IClientSessionHandle? session = null) where T : IEntity
        => new(session, null);
}