# Delete entities

deleting entities can be achieved in any of the following ways:

### Delete a single entity
```csharp
  await book.DeleteAsync();
```
### Delete embeded entities
```csharp
  await book.OtherAuthors.DeleteAllAsync();
```
### Delete by ID
```csharp
  await DB.DeleteAsync<Author>("ID");
```
### Delete by multiple IDs
```csharp
  await DB.DeleteAsync<Book>(new[] { "ID1", "ID2" });
```
### Delete by lambda expression
```csharp
  await DB.DeleteAsync<Book>(b => b.Title.Contains("Trump")); 
```
