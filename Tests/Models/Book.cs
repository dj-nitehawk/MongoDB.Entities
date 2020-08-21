using System;
using System.Collections.Generic;

namespace MongoDB.Entities.Tests
{
    public class Book : Entity, IModifiedOn
    {
        public Date PublishedOn { get; set; }

        [DontPreserve] public string Title { get; set; }
        [DontPreserve] public decimal Price { get; set; }

        public int PriceInt { get; set; }
        public long PriceLong { get; set; }
        public double PriceDbl { get; set; }
        public float PriceFloat { get; set; }
        public Author RelatedAuthor { get; set; }
        public Author[] OtherAuthors { get; set; }
        public Review Review { get; set; }
        public Review[] ReviewArray { get; set; }
        public string[] Tags { get; set; }
        public IList<Review> ReviewList { get; set; }
        public One<Author> MainAuthor { get; set; }
        public Many<Author> GoodAuthors { get; set; }
        public Many<Author> BadAuthors { get; set; }

        [OwnerSide]
        public Many<Genre> Genres { get; set; }

        [Ignore]
        public int DontSaveThis { get; set; }

        public DateTime ModifiedOn { get; set; }

        public Book()
        {
            this.InitOneToMany(() => GoodAuthors);
            this.InitOneToMany(() => BadAuthors);
            this.InitManyToMany(() => Genres, g => g.Books);
        }
    }
}
