using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

public class _001_rename_field : IMigration
{
    public async Task UpgradeAsync()
    {
        await DB.Default.Update<BookEntity>()
          .Match(_ => true)
          .Modify(b => b.Rename("SellingPrice", "Price"))
          .ExecuteAsync().ConfigureAwait(false);
    }
}
