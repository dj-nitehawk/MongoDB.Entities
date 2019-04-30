
namespace MongoDB.Entities.Tests
{
    public class Book : Entity
    {
        public string Title { get; set; }
        public Author RelatedAuthor { get; set; }
        public Author[] OtherAuthors { get; set; }
        public Review Review { get; set; }
        public One<Author> MainAuthor { get; set; }
        public Many<Author> GoodAuthors { get; set; }
        public Many<Author> BadAuthors { get; set; }
        public Many<Genre> AllGenres { get; set; }

        [Ignore]
        public int DontSaveThis { get; set; }

        public Book()
        {
            this.InitOneToMany(() => GoodAuthors);
            this.InitOneToMany(() => BadAuthors);
            this.InitManyToMany(() => AllGenres, g => g.AllBooks, Side.Owner);
        }

    }
}
