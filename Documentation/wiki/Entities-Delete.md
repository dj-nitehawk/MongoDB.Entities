# Delete entities

deleting entities can be achieved in any of the following ways:

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
```

## Delete by lambda expression

```csharp
  await db.DeleteAsync<Book>(b => b.Title.Contains("Trump")); 
```