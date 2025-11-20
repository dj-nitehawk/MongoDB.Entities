using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Represents a ReplaceOne command, which can replace the first matched document with a given entity
    /// <para>TIP: Specify a filter first with the .Match(). Then set entity with .WithEntity() and finally call .Execute() to run the command.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="session">An optional session if using within a transaction</param>
    public Replace<T> Replace<T>(IClientSessionHandle? session = null) where T : IEntity
        => new(session, null, null, null, this);
}