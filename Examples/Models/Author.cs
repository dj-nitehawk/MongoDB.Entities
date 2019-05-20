using MongoDB.Entities;

namespace Examples.Models
{
	[Collection(Name = "test_author")]
    public class Author : Entity
    {
        public string Name { get; set; }
        public One<Book> BestSeller { get; set; }
        public Many<Book> Books { get; set; }

        public Author() => this.InitOneToMany(() => Books);
    }
}
