using MongoDAL;

namespace Examples.Models
{
    public class Book : Entity
    {
        public string Title { get; set; }
        public One<Author> MainAuthor { get; set; }
        public Many<Book,Author> Authors { get; set; }

        public Book()
        {
            Authors = Authors.Initialize(this);
        }
    }
}
