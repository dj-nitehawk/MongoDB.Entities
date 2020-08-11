using System;

namespace MongoDB.Entities.Tests
{
    public class Genre : Entity
    {
        public string Name { get; set; }
        public Guid GuidID { get; set; }
        public int Position { get; set; }
        public double SortScore { get; set; }
        public Review Review { get; set; }

        [InverseSide]
        public Many<Book> Books { get; set; }

        public Genre() => this.InitManyToMany(() => Books, b => b.Genres);
    }
}
