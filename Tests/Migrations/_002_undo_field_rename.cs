using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

public class _002_undo_field_rename : IDatabaseMigration
{
    public async Task UpgradeAsync(DB db, CancellationToken cancellation = default)
    {
        await db.WithModifiedBy(new ModifiedBy { UserID = "migration-test" }).Update<BookEntity>()
          .Match(_ => true)
          .Modify(b => b.Rename("Price", "SellingPrice"))
          .ExecuteAsync(cancellation).ConfigureAwait(false);
    }
}
