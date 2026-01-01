using BookShop.Api.Entities;
using BookShop.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver.Linq;
using MongoDB.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BookShop API",
        Version = "v1",
        Description = """
            A demo Book Shop API demonstrating MongoDB.Entities features:
            - CRUD operations (Save, Update, Delete)
            - Find queries (Find, LINQ, Pipelines)
            - Paged search with fuzzy text search
            - Referenced relationships (One-to-One, One-to-Many, Many-to-Many)
            - Embedded documents
            - Transactions
            - Sequential number generation
            - Indexes and text search
            """
    });
});

var app = builder.Build();

// Initialize MongoDB
var connectionString = builder.Configuration.GetConnectionString("MongoDB")
    ?? "mongodb://localhost:27017/?directConnection=true";
await DatabaseSetup.InitializeAsync(connectionString);

app.UseSwagger();
app.UseSwaggerUI();

// ==================== BOOKS API ====================
var books = app.MapGroup("/api/books").WithTags("Books");

books.MapGet("/", async ([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] bool fuzzySearch = false) =>
{
    var db = DB.Default;
    var searchCmd = db.PagedSearch<Book>();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var searchType = fuzzySearch ? Search.Fuzzy : Search.Full;
        searchCmd.Match(searchType, search);
    }

    searchCmd.Sort(b => b.CreatedOn, Order.Descending);

    var result = await searchCmd.PageNumber(page).PageSize(pageSize).ExecuteAsync();

    return Results.Ok(new
    {
        Results = result.Results.Select(b => MapBook(b)).ToList(),
        result.TotalCount,
        result.PageCount
    });
});

books.MapGet("/{id}", async (string id) =>
{
    var book = await DB.Default.Find<Book>().OneAsync(id);
    return book is null ? Results.NotFound() : Results.Ok(MapBook(book));
});

books.MapPost("/", async ([FromBody] CreateBookRequest req) =>
{
    var db = DB.Default;
    var book = new Book
    {
        Title = new FuzzyString(req.Title),
        ISBN = req.ISBN,
        Description = req.Description,
        Price = req.Price,
        Stock = req.Stock,
        PageCount = req.PageCount
    };

    if (!string.IsNullOrWhiteSpace(req.AuthorId))
        book.MainAuthor = new(req.AuthorId);

    await db.SaveAsync(book);
    return Results.Created($"/api/books/{book.ID}", MapBook(book));
});

books.MapPut("/{id}", async (string id, [FromBody] UpdateBookRequest req) =>
{
    var db = DB.Default;
    var update = db.Update<Book>().MatchID(id);

    if (!string.IsNullOrWhiteSpace(req.Title))
        update.Modify(b => b.Title, new FuzzyString(req.Title));

    if (!string.IsNullOrWhiteSpace(req.Description))
        update.Modify(b => b.Description, req.Description);

    if (req.Price.HasValue)
        update.Modify(b => b.Price, req.Price.Value);

    if (req.Stock.HasValue)
        update.Modify(b => b.Stock, req.Stock.Value);

    var result = await update.ExecuteAsync();
    if (result.ModifiedCount == 0)
        return Results.NotFound();

    var book = await db.Find<Book>().OneAsync(id);
    return Results.Ok(MapBook(book!));
});

books.MapDelete("/{id}", async (string id) =>
{
    var result = await DB.Default.DeleteAsync<Book>(id);
    return result.DeletedCount == 0 ? Results.NotFound() : Results.NoContent();
});

// ==================== AUTHORS API ====================
var authors = app.MapGroup("/api/authors").WithTags("Authors");

authors.MapGet("/", async ([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null) =>
{
    var db = DB.Default;
    var query = db.Queryable<Author>();

    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(a => a.Name.Contains(search));

    var totalCount = await query.CountAsync();
    var skip = (page - 1) * pageSize;

    var list = await query.OrderByDescending(a => a.CreatedOn).Skip(skip).Take(pageSize).ToListAsync();

    return Results.Ok(new
    {
        Results = list.Select(a => MapAuthor(a)).ToList(),
        TotalCount = totalCount
    });
});

authors.MapGet("/{id}", async (string id) =>
{
    var author = await DB.Default.Find<Author>().OneAsync(id);
    return author is null ? Results.NotFound() : Results.Ok(MapAuthor(author));
});

authors.MapPost("/", async ([FromBody] CreateAuthorRequest req) =>
{
    var db = DB.Default;
    var author = new Author { Name = req.Name, Biography = req.Biography, Website = req.Website };
    await db.SaveAsync(author);
    return Results.Created($"/api/authors/{author.ID}", MapAuthor(author));
});

authors.MapPut("/{id}", async (string id, [FromBody] UpdateAuthorRequest req) =>
{
    var db = DB.Default;
    var author = await db.Find<Author>().OneAsync(id);
    if (author is null) return Results.NotFound();

    if (!string.IsNullOrWhiteSpace(req.Name)) author.Name = req.Name;
    if (!string.IsNullOrWhiteSpace(req.Biography)) author.Biography = req.Biography;
    if (req.Website is not null) author.Website = req.Website;

    await db.SaveAsync(author);
    return Results.Ok(MapAuthor(author));
});

authors.MapDelete("/{id}", async (string id) =>
{
    var result = await DB.Default.DeleteAsync<Author>(id);
    return result.DeletedCount == 0 ? Results.NotFound() : Results.NoContent();
});

// ==================== GENRES API ====================
var genres = app.MapGroup("/api/genres").WithTags("Genres");

genres.MapGet("/", async () =>
{
    var list = await DB.Default.Find<Genre>().Sort(g => g.Name, Order.Ascending).ExecuteAsync();
    return Results.Ok(list.Select(g => MapGenre(g)).ToList());
});

genres.MapGet("/{id}", async (string id) =>
{
    var genre = await DB.Default.Find<Genre>().OneAsync(id);
    return genre is null ? Results.NotFound() : Results.Ok(MapGenre(genre));
});

genres.MapPost("/", async ([FromBody] CreateGenreRequest req) =>
{
    var db = DB.Default;
    var genre = new Genre { Name = req.Name, Description = req.Description };
    await db.SaveAsync(genre);
    return Results.Created($"/api/genres/{genre.ID}", MapGenre(genre));
});

genres.MapDelete("/{id}", async (string id) =>
{
    var result = await DB.Default.DeleteAsync<Genre>(id);
    return result.DeletedCount == 0 ? Results.NotFound() : Results.NoContent();
});

// ==================== ORDERS API (demonstrates transactions) ====================
var orders = app.MapGroup("/api/orders").WithTags("Orders");

orders.MapGet("/", async ([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] OrderStatus? status = null) =>
{
    var db = DB.Default;
    var query = db.Find<ShopOrder>();

    if (status.HasValue)
        query.Match(o => o.Status == status.Value);

    query.Sort(o => o.CreatedOn, Order.Descending).Skip((page - 1) * pageSize).Limit(pageSize);

    var list = await query.ExecuteAsync();
    var totalCount = await db.CountAsync<ShopOrder>();

    return Results.Ok(new
    {
        Results = list.Select(o => MapOrder(o)).ToList(),
        TotalCount = totalCount
    });
});

orders.MapGet("/{id}", async (string id) =>
{
    var order = await DB.Default.Find<ShopOrder>().OneAsync(id);
    return order is null ? Results.NotFound() : Results.Ok(MapOrder(order));
});

orders.MapPost("/", async ([FromBody] CreateOrderRequest req) =>
{
    var db = DB.Default;
    var customer = await db.Find<Customer>().OneAsync(req.CustomerId);
    if (customer is null)
        return Results.BadRequest("Customer not found");

    // Transaction for atomic operation
    using var transaction = db.Transaction();

    var orderItems = new List<OrderItem>();
    decimal totalAmount = 0;

    foreach (var item in req.Items)
    {
        var book = await transaction.Find<Book>().OneAsync(item.BookId);
        if (book is null)
            return Results.BadRequest($"Book {item.BookId} not found");

        if (book.Stock < item.Quantity)
            return Results.BadRequest($"Insufficient stock for '{book.Title.Value}'");

        await transaction.Update<Book>()
            .MatchID(book.ID)
            .Modify(b => b.Stock, book.Stock - item.Quantity)
            .ExecuteAsync();

        orderItems.Add(new OrderItem
        {
            BookId = book.ID,
            BookTitle = book.Title.Value,
            Quantity = item.Quantity,
            UnitPrice = book.Price
        });

        totalAmount += book.Price * item.Quantity;
    }

    var orderNumber = await db.NextSequentialNumberAsync<ShopOrder>();

    var order = new ShopOrder
    {
        OrderNumber = $"ORD-{orderNumber:D8}",
        Customer = new(customer),
        Items = orderItems,
        TotalAmount = totalAmount,
        Status = OrderStatus.Pending,
        ShippingAddress = customer.ShippingAddress
    };

    await transaction.SaveAsync(order);
    await transaction.CommitAsync();

    return Results.Created($"/api/orders/{order.ID}", MapOrder(order));
});

orders.MapPatch("/{id}/status", async (string id, [FromBody] UpdateOrderStatusRequest req) =>
{
    var db = DB.Default;
    var updatedOrder = await db.UpdateAndGet<ShopOrder>()
        .MatchID(id)
        .Modify(o => o.Status, req.Status)
        .ExecuteAsync();

    return updatedOrder is null ? Results.NotFound() : Results.Ok(MapOrder(updatedOrder));
});

orders.MapPost("/{id}/cancel", async (string id) =>
{
    var db = DB.Default;
    var order = await db.Find<ShopOrder>().OneAsync(id);
    if (order is null) return Results.NotFound();

    if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered)
        return Results.BadRequest("Cannot cancel shipped/delivered order");

    using var transaction = db.Transaction();

    foreach (var item in order.Items)
    {
        var book = await transaction.Find<Book>().OneAsync(item.BookId);
        if (book is not null)
        {
            await transaction.Update<Book>()
                .MatchID(book.ID)
                .Modify(b => b.Stock, book.Stock + item.Quantity)
                .ExecuteAsync();
        }
    }

    order.Status = OrderStatus.Cancelled;
    await transaction.SaveAsync(order);
    await transaction.CommitAsync();

    return Results.Ok(MapOrder(order));
});

// ==================== CUSTOMERS API ====================
var customers = app.MapGroup("/api/customers").WithTags("Customers");

customers.MapGet("/", async ([FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
{
    var db = DB.Default;
    var list = await db.Find<Customer>()
        .Match(c => !c.IsDeleted)
        .Sort(c => c.CreatedOn, Order.Descending)
        .Skip((page - 1) * pageSize)
        .Limit(pageSize)
        .ExecuteAsync();

    return Results.Ok(list.Select(c => MapCustomer(c)).ToList());
});

customers.MapGet("/{id}", async (string id) =>
{
    var customer = await DB.Default.Find<Customer>().Match(c => !c.IsDeleted).OneAsync(id);
    return customer is null ? Results.NotFound() : Results.Ok(MapCustomer(customer));
});

customers.MapPost("/", async ([FromBody] CreateCustomerRequest req) =>
{
    var db = DB.Default;
    var sequenceNumber = await db.NextSequentialNumberAsync<Customer>();

    var customer = new Customer
    {
        CustomerId = $"CUST-{sequenceNumber:D8}",
        FirstName = req.FirstName,
        LastName = req.LastName,
        Email = req.Email,
        Phone = req.Phone,
        ShippingAddress = req.ShippingAddress
    };

    await db.SaveAsync(customer);
    return Results.Created($"/api/customers/{customer.ID}", MapCustomer(customer));
});

customers.MapDelete("/{id}", async (string id) =>
{
    // Soft delete
    var result = await DB.Default.Update<Customer>()
        .MatchID(id)
        .Modify(c => c.IsDeleted, true)
        .Modify(c => c.DeletedOn, DateTime.UtcNow)
        .ExecuteAsync();

    return result.ModifiedCount == 0 ? Results.NotFound() : Results.NoContent();
});

app.Run();

// ==================== MAPPING FUNCTIONS ====================
static object MapBook(Book b) => new
{
    b.ID,
    Title = b.Title.Value,
    b.ISBN,
    b.Description,
    b.Price,
    b.Stock,
    b.PageCount,
    PublishedDate = b.PublishedDate?.DateTime,
    AuthorId = b.MainAuthor?.ID,
    b.CreatedOn,
    b.ModifiedOn
};

static object MapAuthor(Author a) => new { a.ID, a.Name, a.Biography, a.Website, a.CreatedOn, a.ModifiedOn };
static object MapGenre(Genre g) => new { g.ID, g.Name, g.Description, g.CreatedOn, g.ModifiedOn };
static object MapOrder(ShopOrder o) => new
{
    o.ID,
    o.OrderNumber,
    CustomerId = o.Customer?.ID,
    Items = o.Items.Select(i => new { i.BookId, i.BookTitle, i.Quantity, i.UnitPrice, i.TotalPrice }).ToList(),
    o.Status,
    o.TotalAmount,
    o.CreatedOn,
    o.ModifiedOn
};
static object MapCustomer(Customer c) => new
{
    c.ID,
    c.CustomerId,
    c.FirstName,
    c.LastName,
    c.Email,
    c.Phone,
    c.ShippingAddress,
    c.IsDeleted,
    c.CreatedOn,
    c.ModifiedOn
};

// ==================== REQUEST DTOS ====================
public record CreateBookRequest(string Title, string? ISBN, string Description, decimal Price, int Stock, int PageCount, string? AuthorId);
public record UpdateBookRequest(string? Title, string? Description, decimal? Price, int? Stock);
public record CreateAuthorRequest(string Name, string Biography, string? Website);
public record UpdateAuthorRequest(string? Name, string? Biography, string? Website);
public record CreateGenreRequest(string Name, string Description);
public record CreateOrderRequest(string CustomerId, List<OrderItemRequest> Items);
public record OrderItemRequest(string BookId, int Quantity);
public record UpdateOrderStatusRequest(OrderStatus Status);
public record CreateCustomerRequest(string FirstName, string LastName, string Email, string? Phone, Address? ShippingAddress);

public partial class Program;
