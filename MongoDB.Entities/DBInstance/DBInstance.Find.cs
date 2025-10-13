using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DBInstance
{
    /// <summary>
    /// Represents a MongoDB Find command
    /// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="session">An optional session if using within a transaction</param>
    public Find<T> Find<T>(IClientSessionHandle? session = null) where T : IEntity
        => new(session, null, this);

    /// <summary>
    /// Represents a MongoDB Find command
    /// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TProjection">The type that is returned by projection</typeparam>
    /// <param name="session">An optional session if using within a transaction</param>
    public Find<T, TProjection> Find<T, TProjection>(IClientSessionHandle? session = null) where T : IEntity
        => new(session, null, this);
}