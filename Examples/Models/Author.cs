using MongoDB.Entities;

namespace Examples.Models
{
    public class Author : Entity
    {
        public string Name { get; set; }
        public One<Book> BestSeller { get; set; }
        public Many<Author, Book> Books { get; set; }

        public Author() => Books = Books.Initialize(this);
    }
}
