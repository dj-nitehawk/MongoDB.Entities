namespace MongoDB.Entities.NewApi;

/// <summary>
/// Marks an entity that can be queried by id
/// </summary>
/// <typeparam name="TId"></typeparam>
public interface IEntity<TId>
{
    TId Id { get; }
}
