namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DBInstance
{
    /// <summary>
    /// Represents an index for a given IEntity
    /// <para>TIP: Define the keys first with .Key() method and finally call the .InitAsync() method.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public Index<T> Index<T>() where T : IEntity
        => new(this);
}