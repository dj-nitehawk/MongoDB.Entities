using MongoDB.Entities.Common;

namespace MongoDB.Entities.Tests.Models
{
    [Database("mongodb-entities-test-multi")]
    public class BookCover : Entity
    {
        public string BookName { get; set; }
        public string BookID { get; set; }
        public Many<BookMark> BookMarks { get; set; }

        public BookCover()
        {
            this.InitOneToMany(() => BookMarks);
        }
    }
}
