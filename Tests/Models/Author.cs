using MongoDB.Entities.Common;

namespace MongoDB.Entities.Tests
{
    [Name("Writer")]
    public class Author : Entity
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        public int Age2 { get; set; }
        public One<Book> BestSeller { get; set; }
        public Many<Book> Books { get; set; }

        public Author() => this.InitOneToMany(() => Books);
    }
}
