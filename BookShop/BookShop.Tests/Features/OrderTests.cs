using System.Net;
using System.Net.Http.Json;
using BookShop.Api.Entities;
using Xunit;

namespace BookShop.Tests.Features;

[Collection("BookShop")]
public class OrderTests
{
    private readonly HttpClient _client;

    public OrderTests(BookShopFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    private async Task<(string CustomerId, string BookId)> SetupTestDataAsync()
    {
        // Create a customer
        var customerResponse = await _client.PostAsJsonAsync("/api/customers", new
        {
            FirstName = "Order",
            LastName = "Test",
            Email = $"order.test.{Guid.NewGuid():N}@example.com"
        });
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create a book with stock
        var bookResponse = await _client.PostAsJsonAsync("/api/books", new
        {
            Title = $"Order Test Book {Guid.NewGuid():N}",
            Description = "A book for order testing",
            Price = 25.99m,
            Stock = 50,
            PageCount = 200
        });
        var book = await bookResponse.Content.ReadFromJsonAsync<BookResponse>();

        return (customer!.Id, book!.Id);
    }

    [Fact]
    public async Task CreateOrder_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var (customerId, bookId) = await SetupTestDataAsync();

        var request = new
        {
            CustomerId = customerId,
            Items = new[]
            {
                new { BookId = bookId, Quantity = 2 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(result);
        Assert.StartsWith("ORD-", result!.OrderNumber);
        Assert.Equal(OrderStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetOrder_ExistingOrder_ShouldReturnOk()
    {
        // Arrange
        var (customerId, bookId) = await SetupTestDataAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customerId,
            Items = new[] { new { BookId = bookId, Quantity = 1 } }
        });
        var createResult = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        // Act
        var response = await _client.GetAsync($"/api/orders/{createResult!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListOrders_ShouldReturnResults()
    {
        // Arrange
        var (customerId, bookId) = await SetupTestDataAsync();
        await _client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customerId,
            Items = new[] { new { BookId = bookId, Quantity = 1 } }
        });

        // Act
        var response = await _client.GetAsync("/api/orders?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOrderStatus_ShouldUpdateStatus()
    {
        // Arrange
        var (customerId, bookId) = await SetupTestDataAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customerId,
            Items = new[] { new { BookId = bookId, Quantity = 1 } }
        });
        var createResult = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/orders/{createResult!.Id}/status", new
        {
            Status = OrderStatus.Confirmed
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(OrderStatus.Confirmed, result!.Status);
    }

    [Fact]
    public async Task CancelOrder_ShouldCancelOrder()
    {
        // Arrange
        var (customerId, bookId) = await SetupTestDataAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customerId,
            Items = new[] { new { BookId = bookId, Quantity = 3 } }
        });
        var createResult = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        // Act
        var response = await _client.PostAsync($"/api/orders/{createResult!.Id}/cancel", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cancelResult = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(OrderStatus.Cancelled, cancelResult!.Status);
    }

    record CustomerResponse(string Id, string CustomerId, string FirstName, string LastName, string Email);
    record BookResponse(string Id, string Title, decimal Price, int Stock);
    record OrderResponse(string Id, string OrderNumber, OrderStatus Status, decimal TotalAmount);
}
