using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BookShop.Tests.Features;

public class AuthorTests : IClassFixture<BookShopFixture>
{
    private readonly HttpClient _client;

    public AuthorTests(BookShopFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task CreateAuthor_ShouldReturnCreated()
    {
        // Arrange
        var request = new
        {
            Name = "Test Author",
            Biography = "A test author biography",
            Website = "https://testauthor.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/authors", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthorResponse>();
        Assert.NotNull(result);
        Assert.Equal("Test Author", result!.Name);
    }

    [Fact]
    public async Task GetAuthor_ExistingAuthor_ShouldReturnOk()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/authors", new
        {
            Name = "Author To Get",
            Biography = "Biography"
        });
        var createResult = await createResponse.Content.ReadFromJsonAsync<AuthorResponse>();

        // Act
        var response = await _client.GetAsync($"/api/authors/{createResult!.ID}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListAuthors_ShouldReturnResults()
    {
        // Arrange
        await _client.PostAsJsonAsync("/api/authors", new
        {
            Name = "List Test Author",
            Biography = "Biography"
        });

        // Act
        var response = await _client.GetAsync("/api/authors?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuthor_ExistingAuthor_ShouldReturnOk()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/authors", new
        {
            Name = "Author To Update",
            Biography = "Original Bio"
        });
        var createResult = await createResponse.Content.ReadFromJsonAsync<AuthorResponse>();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/authors/{createResult!.ID}", new
        {
            Name = "Updated Author Name"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthorResponse>();
        Assert.Equal("Updated Author Name", result!.Name);
    }

    [Fact]
    public async Task DeleteAuthor_ExistingAuthor_ShouldReturnNoContent()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/authors", new
        {
            Name = "Author To Delete",
            Biography = "To be deleted"
        });
        var createResult = await createResponse.Content.ReadFromJsonAsync<AuthorResponse>();

        // Act
        var response = await _client.DeleteAsync($"/api/authors/{createResult!.ID}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    record AuthorResponse(string ID, string Name, string Biography, string? Website);
}
