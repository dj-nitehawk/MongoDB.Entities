# Delete entities

deleting entities can be achieved in any of the following ways:

### Delete a single entity
```csharp
  await book.DeleteAsync();
```
### Delete a single entity for a specific db instance
```csharp
  await book.DeleteAsync(dbInstance);
```
### Delete embeded entities
```csharp
  await book.OtherAuthors.DeleteAllAsync();
```
### Delete by ID from default instance
```csharp
  await DB.Instance().DeleteAsync<Author>("ID");
```
### Delete by ID from a specific db instance
```csharp
  await dbInstance.DeleteAsync<Author>("ID");
```
### Delete by multiple IDs
```csharp
  await DB.Instance().DeleteAsync<Book>(new[] { "ID1", "ID2" });
```
### Delete by lambda expression
```csharp
  await DB.Instance().DeleteAsync<Book>(b => b.Title.Contains("Trump")); 
```
