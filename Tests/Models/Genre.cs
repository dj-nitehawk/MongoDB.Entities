using MongoDB.Entities.Core;

namespace MongoDB.Entities.Tests
{
    public class Genre : Entity
    {
        public string Name { get; set; }

        [InverseSide]
        public Many<Book> Books { get; set; }

        public Genre() => this.InitManyToMany(() => Books, b => b.Genres);
    }
}
