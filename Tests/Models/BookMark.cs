namespace MongoDB.Entities.Tests.Models
{
    public class BookMark : Entity
    {
        public One<BookCover> BookCover { get; set; }
        public string BookName { get; set; }
    }
}
