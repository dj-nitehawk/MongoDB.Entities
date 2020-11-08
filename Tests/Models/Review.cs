using System.Collections.ObjectModel;

namespace MongoDB.Entities.Tests
{
    public class Review
    {
        public int Stars { get; set; }
        public string Reviewer { get; set; }
        public double Rating { get; set; }
        public FuzzyString Fuzzy { get; set; }
        public Collection<Book> Books { get; set; }
    }
}
