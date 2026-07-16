# Delete entities

Deleting entities can be achieved in any of the following ways:

## Delete a single entity

```csharp
  await db.DeleteAsync(book);
```

## Delete by ID

```csharp
  await db.DeleteAsync<Author>("ID");
```

## Delete by multiple IDs

```csharp
  await db.DeleteAsync<Book>(["ID1", "ID2"]);

  // value-type ID sequences (Guid, long, ObjectId, ...) use the typed overload
  Guid[] ids = [id1, id2];
  await db.DeleteAsync<MyGuidEntity, Guid>(ids);
```

## Delete by lambda expression

```csharp
  await db.DeleteAsync<Book>(b => b.Title.Contains("Trump")); 
```