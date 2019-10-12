using MongoDB.Entities;
using MongoDB.Entities.Common;

namespace Examples.Models
{
    public class Author : Entity
    {
        public string Name { get; set; }
        public One<Book> BestSeller { get; set; }
        public Many<Book> Books { get; set; }

        public Author() => this.InitOneToMany(() => Books);
    }
}
