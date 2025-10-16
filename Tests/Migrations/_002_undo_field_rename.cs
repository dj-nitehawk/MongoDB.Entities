using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

public class _002_undo_field_rename : IMigration
{
    public async Task UpgradeAsync()
    {
        await DBInstance.Instance().Update<BookEntity>()
          .Match(_ => true)
          .Modify(b => b.Rename("Price", "SellingPrice"))
          .ExecuteAsync().ConfigureAwait(false);
    }
}
