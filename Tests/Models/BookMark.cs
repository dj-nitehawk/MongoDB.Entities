namespace MongoDB.Entities.Tests.Models
{
    [Database("mongodb-entitites-test-multi")]
    public class BookMark : Entity
    {
        public One<BookCover> BookCover { get; set; }
        public string BookName { get; set; }
    }
}
