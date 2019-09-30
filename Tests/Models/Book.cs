
using System.Collections.Generic;

namespace MongoDB.Entities.Tests
{
    public class Book : Entity
    {
        public string Title { get; set; }
        public decimal SellingPrice { get; set; }
        public Author RelatedAuthor { get; set; }
        public Author[] OtherAuthors { get; set; }
        public Review Review { get; set; }
        public Review[] MoreReviews { get; set; }
        public List<Review> ReviewList { get; set; }
        public One<Author> MainAuthor { get; set; }
        public Many<Author> GoodAuthors { get; set; }
        public Many<Author> BadAuthors { get; set; }

        [OwnerSide]
        public Many<Genre> Genres { get; set; }

        [Ignore]
        public int DontSaveThis { get; set; }

        public Book()
        {
            this.InitOneToMany(() => GoodAuthors);
            this.InitOneToMany(() => BadAuthors);
            this.InitManyToMany(() => Genres, g => g.Books);
        }

    }
}
