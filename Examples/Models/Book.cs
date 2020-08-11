using MongoDB.Entities;

namespace Examples.Models
{
    public class Book : Entity
    {
        public string Title { get; set; }
        public Author RelatedAuthor { get; set; } //Store an entity       
        public Author[] OtherAuthors { get; set; } //Store an entity array    
        public Review Review { get; set; } //Store an unlinked object
        public One<Author> MainAuthor { get; set; } //Specify a reference to an entity 
        public Many<Author> Authors { get; set; } //Specify references to multiple entities

        [OwnerSide]
        public Many<Genre> AllGenres { get; set; }//Owner side of many-to-many relationship

        [Ignore]
        public int DontSaveThis { get; set; } //Property is not saved to database

        public Book()
        {
            this.InitOneToMany(() => Authors);
            this.InitManyToMany(() => AllGenres, genre => genre.AllBooks);
        }
    }
}
