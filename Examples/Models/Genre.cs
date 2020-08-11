using MongoDB.Entities;

namespace Examples.Models
{
    public class Genre : Entity
    {
        public string Name { get; set; }

        [InverseSide]
        public Many<Book> AllBooks { get; set; }

        public Genre() => this.InitManyToMany(() => AllBooks, book => book.AllGenres);
    }
}
