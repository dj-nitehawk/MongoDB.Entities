using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities;

namespace BookShop.Api.Entities;

/// <summary>
/// Author entity demonstrating referenced relationships
/// </summary>
[Collection("Authors")]
public class Author : Entity, ICreatedOn, IModifiedOn
{
    public string Name { get; set; } = null!;
    public string Biography { get; set; } = null!;
    public string? Website { get; set; }
    public Many<Book, Author> Books { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }

    public Author()
    {
        this.InitOneToMany(() => Books);
    }
}

/// <summary>
/// Genre entity demonstrating Many-to-Many relationships
/// </summary>
[Collection("Genres")]
public class Genre : Entity, ICreatedOn, IModifiedOn
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    [InverseSide]
    public Many<Book, Genre> Books { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }

    public Genre()
    {
        this.InitManyToMany(() => Books, book => book.Genres);
    }
}

/// <summary>
/// Book entity with fuzzy search, relationships, and audit fields
/// </summary>
[Collection("Books")]
public class Book : Entity, ICreatedOn, IModifiedOn
{
    public FuzzyString Title { get; set; } = null!;
    public string? ISBN { get; set; }

    [Field("book_description")]
    public string Description { get; set; } = null!;

    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int PageCount { get; set; }
    public Date? PublishedDate { get; set; }
    public One<Author>? MainAuthor { get; set; }

    [OwnerSide]
    public Many<Genre, Book> Genres { get; set; }

    public ModifiedBy? ModifiedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }

    public Book()
    {
        this.InitManyToMany(() => Genres, genre => genre.Books);
    }
}

/// <summary>
/// Customer entity with soft delete and sequential ID
/// </summary>
[Collection("Customers")]
public class Customer : Entity, ICreatedOn, IModifiedOn
{
    public string CustomerId { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public Address? ShippingAddress { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}

public class Address
{
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string Country { get; set; } = null!;
}

/// <summary>
/// ShopOrder entity for transaction demonstration
/// </summary>
[Collection("Orders")]
public class ShopOrder : Entity, ICreatedOn, IModifiedOn
{
    public string OrderNumber { get; set; } = null!;
    public One<Customer>? Customer { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public Address? ShippingAddress { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}

public class OrderItem
{
    public string BookId { get; set; } = null!;
    public string BookTitle { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
