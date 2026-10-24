using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

/// <summary>
/// A data migration that receives the database instance running it and a cancellation token.
/// This contract is independent of <see cref="IMigration" />.
/// </summary>
public interface IDatabaseMigration
{
    /// <summary>
    /// Transforms the database. Forward the cancellation token to database operations.
    /// </summary>
    /// <param name="db">The same database instance that reads and writes migration history</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <remarks>
    /// Cancellation does not roll back changes. Make the migration safe to retry if completion is not recorded.
    /// </remarks>
    Task UpgradeAsync(DB db, CancellationToken cancellation = default);
}
