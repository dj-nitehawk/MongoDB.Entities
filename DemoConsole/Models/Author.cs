using MongoDAL;
using System.Linq;

namespace DemoConsole.Models

{
    public class Author : Entity
    {
        public string Name { get; set; }
        public One<Book> BestSeller { get; set; }
        public Many<Author, Book> Books { get; set; }

        public Author()
        {
            Books = Books.Initialize(this);
        }

        public Author FindLast()
        {
            return (from p in DB.Collection<Author>()
                    orderby p.ModifiedOn descending
                    select p).FirstOrDefault();
        }

    }
}
