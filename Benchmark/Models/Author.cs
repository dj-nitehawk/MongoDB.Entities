using MongoDB.Entities;
using System;

namespace Benchmark;

public class Author : Entity
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? Birthday { get; set; }
    public int Age { get; set; }
    public Many<Book> Books { get; set; } = null!;

    public Author() => this.InitOneToMany(() => Books);
}
