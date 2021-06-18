using MongoDB.Entities;
using System;

namespace Benchmark
{
    public class Book : Entity
    {
        public string Title { get; set; }
        public One<Author> Author { get; set; }
        public DateTime PublishedOn { get; set; }
    }
}
