namespace MongoDB.Entities.Tests.Models
{
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
