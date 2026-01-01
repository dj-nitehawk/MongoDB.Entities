using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BookShop.Tests.Features;

public class BookTests : IClassFixture<BookShopFixture>
{
    private readonly HttpClient _client;

    public BookTests(BookShopFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task CreateBook_ShouldReturnCreated()
    {
        // Arrange
        var request = new
        {
            Title = "Test Book Title",
            Description = "A test book description",
            Price = 29.99m,
            Stock = 100,
            PageCount = 300
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/books", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BookResponse>();
        Assert.NotNull(result);
        Assert.Equal("Test Book Title", result!.Title);
    }

    [Fact]
    public async Task GetBook_ExistingBook_ShouldReturnOk()
    {
        // Arrange - create a book first
        var createResponse = await _client.PostAsJsonAsync("/api/books", new
        {
            Title = "Book To Get",
            Description = "Description",
            Price = 19.99m,
            Stock = 50,
            PageCount = 200
        });
        var createResult = await createResponse.Content.ReadFromJsonAsync<BookResponse>();

        // Act
        var response = await _client.GetAsync($"/api/books/{createResult!.ID}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBook_NonExistingBook_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/books/000000000000000000000000");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListBooks_ShouldReturnResults()
    {
        // Arrange
        await _client.PostAsJsonAsync("/api/books", new
        {
            Title = "List Test Book",
            Description = "Description",
            Price = 10.00m,
            Stock = 10,
            PageCount = 100
        });

        // Act
        var response = await _client.GetAsync("/api/books?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBook_ExistingBook_ShouldReturnOk()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/books", new
        {
            Title = "Book To Update",
            Description = "Original Description",
            Price = 15.99m,
            Stock = 25,
            PageCount = 150
        });
        var createResult = await createResponse.Content.ReadFromJsonAsync<BookResponse>();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/books/{createResult!.ID}", new
        {
            Title = "Updated Book Title",
            Price = 19.99m
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BookResponse>();
        Assert.Equal("Updated Book Title", result!.Title);
    }

    [Fact]
    public async Task DeleteBook_ExistingBook_ShouldReturnNoContent()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/books", new
        {
            Title = "Book To Delete",
            Description = "To be deleted",
            Price = 9.99m,
            Stock = 5,
            PageCount = 100
        });
        var createResult = await createResponse.Content.ReadFromJsonAsync<BookResponse>();

        // Act
        var response = await _client.DeleteAsync($"/api/books/{createResult!.ID}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    record BookResponse(string ID, string Title, string Description, decimal Price, int Stock, int PageCount);
}
