# BookShop API Demo

A demonstration Web API showcasing MongoDB.Entities library features using ASP.NET Core Minimal APIs.

## Features Demonstrated

This demo project showcases the following MongoDB.Entities features:

### Core Operations
- **Save entities** - Create and persist entities to MongoDB
- **Update entities** - Various update patterns including `Modify` and `UpdateAndGet`
- **Delete entities** - Hard delete and soft delete patterns

### Queries
- **Find queries** - Find by ID, filter expressions, and match criteria
- **LINQ queries** - Use familiar LINQ syntax for querying
- **Paged search** - Built-in pagination support with total counts
- **Fuzzy text search** - Search with misspellings using `FuzzyString`

### Relationships
- **One-to-One** - `One<T>` references (Book → Author)
- **One-to-Many** - `Many<TChild, TParent>` (Author → Books)
- **Many-to-Many** - Bidirectional relationships (Book ↔ Genre)
- **Embedded documents** - Nested objects within entities (Customer → Address)

### Advanced Features
- **Transactions** - ACID-compliant multi-document transactions (Order creation)
- **Soft delete** - Using filter expressions (Customer delete)
- **Sequential numbers** - Generate unique sequential IDs (`NextSequentialNumberAsync`)
- **Indexes** - Text indexes for search
- **Date type** - Precision date/time handling with `Date` class
- **ICreatedOn/IModifiedOn** - Automatic timestamp management

## Project Structure

```
BookShop/
├── BookShop.Api/
│   ├── Entities/          # MongoDB entity definitions
│   ├── Infrastructure/    # Database setup
│   └── Program.cs         # Minimal API endpoints
├── BookShop.Tests/        # Integration tests with TestContainers
├── docker-compose.yml     # MongoDB replica set setup
└── README.md
```

## Running the Application

### Prerequisites
- .NET 10 SDK
- Docker (for MongoDB)

### Start MongoDB

```bash
cd BookShop
docker-compose up -d
```

Wait for MongoDB to initialize (the `mongodb-init` service will configure the replica set).

### Run the API

```bash
cd BookShop/BookShop.Api
dotnet run
```

The API will be available at:
- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

### Run Tests

Tests use TestContainers to spin up MongoDB automatically:

```bash
cd BookShop/BookShop.Tests
dotnet test
```

## API Endpoints

### Books
- `GET /api/books` - List books with pagination and fuzzy search
- `GET /api/books/{id}` - Get a book by ID
- `POST /api/books` - Create a new book
- `PUT /api/books/{id}` - Update a book
- `DELETE /api/books/{id}` - Delete a book

### Authors
- `GET /api/authors` - List authors with LINQ query
- `GET /api/authors/{id}` - Get an author
- `POST /api/authors` - Create an author
- `PUT /api/authors/{id}` - Update an author
- `DELETE /api/authors/{id}` - Delete an author

### Genres
- `GET /api/genres` - List all genres
- `GET /api/genres/{id}` - Get a genre
- `POST /api/genres` - Create a genre
- `DELETE /api/genres/{id}` - Delete a genre

### Orders (Demonstrates Transactions)
- `GET /api/orders` - List orders with filtering
- `GET /api/orders/{id}` - Get an order
- `POST /api/orders` - Create an order (uses transactions to reduce stock)
- `PATCH /api/orders/{id}/status` - Update order status (uses UpdateAndGet)
- `POST /api/orders/{id}/cancel` - Cancel order (restores stock atomically)

### Customers (Demonstrates Soft Delete & Sequential IDs)
- `GET /api/customers` - List customers (excludes soft-deleted)
- `GET /api/customers/{id}` - Get a customer
- `POST /api/customers` - Create customer with sequential ID (CUST-00000001)
- `DELETE /api/customers/{id}` - Soft delete a customer

## Entity Relationships

```
┌─────────────────┐
│     Genre       │
└────────┬────────┘
         │ Many-to-Many
┌────────┴────────┐
│      Book       │
└────────┬────────┘
         │ One-to-One (Reference)
         ▼
┌─────────────────┐
│     Author      │
└─────────────────┘

┌─────────────────┐         ┌─────────────────┐
│    Customer     │◄────────│    ShopOrder    │
└─────────────────┘ One-to-One └───────────────┘
    (Soft Delete)              │
    (Sequential ID)            │ Embedded
                               ▼
                        ┌─────────────────┐
                        │    OrderItem    │
                        └─────────────────┘
```

## MongoDB.Entities Features Used

| Feature | Location |
|---------|----------|
| `FuzzyString` | Book.Title |
| `One<T>` | Book.MainAuthor, ShopOrder.Customer |
| `Many<T,P>` | Author.Books, Genre.Books, Book.Genres |
| `ICreatedOn` | All entities |
| `IModifiedOn` | All entities |
| `Transaction()` | Order creation and cancellation |
| `NextSequentialNumberAsync()` | Customer ID, Order number |
| `PagedSearch()` | Book listing |
| `UpdateAndGet()` | Order status update |
| `Index()` | Book title text index |
| Soft delete pattern | Customer.IsDeleted |
| `Date` type | Book.PublishedDate |
| `[Field]` attribute | Book.Description |

## License

MIT
