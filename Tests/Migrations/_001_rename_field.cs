namespace MongoDB.Entities.Tests
{
    public class _001_rename_field : IMigration
    {
        public void Upgrade()
        {
            DB.Update<Book>()
              .Modify(b => b.Rename("SellingPrice", "Price"))
              .Execute();
        }
    }
}
