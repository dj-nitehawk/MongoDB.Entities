namespace MongoDB.Entities.Tests
{
    public class Genre : Entity
    {
        public string Name { get; set; }
        public Many<Book> AllBooks { get; set; }

        public Genre() => this.InitManyToMany(() => AllBooks, b => b.AllGenres, Side.Invese);
    }
}
