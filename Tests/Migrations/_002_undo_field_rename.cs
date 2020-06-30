namespace MongoDB.Entities.Tests
{
    public class _002_undo_field_rename : IMigration
    {
        public void Upgrade()
        {
            DB.Update<Book>()
              .Match(_ => true)
              .Modify(b => b.Rename("Price", "SellingPrice"))
              .Execute();
        }
    }
}
