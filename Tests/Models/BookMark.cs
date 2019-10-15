using MongoDB.Entities.Core;

namespace MongoDB.Entities.Tests.Models
{
    [Database("mongodb-entities-test-multi")]
    public class BookMark : Entity
    {
        public One<BookCover> BookCover { get; set; }
        public string BookName { get; set; }
    }
}
