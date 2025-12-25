using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Inserts a new entity into the collection.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The instance to persist</param>
    /// <param name="cancellation">And optional cancellation token</param>
    public Task InsertAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
    {
        PrepAndCheckIfInsert(entity);
        SetModifiedBySingle(entity);
        OnBeforeSave<T>()?.Invoke(entity);

        return SessionHandle == null
                   ? Collection<T>().InsertOneAsync(entity, null, cancellation)
                   : Collection<T>().InsertOneAsync(SessionHandle, entity, null, cancellation);
    }

    /// <summary>
    /// Inserts a batch of new entities into the collection.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The entities to persist</param>
    /// <param name="cancellation">And optional cancellation token</param>
    public Task<BulkWriteResult<T>> InsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellation = default) where T : IEntity
    {
        var models = new List<WriteModel<T>>(entities.Count());

        foreach (var ent in entities)
        {
            PrepAndCheckIfInsert(ent);
            SetModifiedBySingle(ent);
            OnBeforeSave<T>()?.Invoke(ent);
            models.Add(new InsertOneModel<T>(ent));
        }

        return SessionHandle == null
                   ? Collection<T>().BulkWriteAsync(models, _unOrdBlkOpts, cancellation)
                   : Collection<T>().BulkWriteAsync(SessionHandle, models, _unOrdBlkOpts, cancellation);
    }
}