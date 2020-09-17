# The 'Prop' Class
this static class has several handy methods for getting string property paths from lambda expressions. which can help to eliminate magic strings from your code during advanced scenarios.

#### Prop.Path()
returns the full dotted path for a given member expression.
> Authors[0].Books[0].Title > Authors.Books.Title
```csharp
    var path = Prop.Path<Book>(b => b.Authors[0].Books[0].Title);
```

#### Prop.Property()
returns the last property name for a given member expression.
> Authors[0].Books[0].Title > Title
```csharp
    var propName = Prop.Property<Book>(b => b.Authors[0].Books[0].Title);
```

#### Prop.Collection()
returns the collection/entity name for a given entity type.
```csharp
    var collectionName = Prop.Collection<Book>();
```

#### Prop.PosAll()
returns a path with the all positional operator $[] for a given expression.
> Authors[0].Name > Authors.$[].Name
```csharp
    var path = Prop.PosAll<Book>(b => b.Authors[0].Name);
```

#### Prop.PosFirst()
returns a path with the first positional operator $ for a given expression.
> Authors[0].Name > Authors.$.Name
```csharp
    var path = Prop.PosFirst<Book>(b => b.Authors[0].Name);
```

#### Prop.PosFiltered()
returns a path with filtered positional identifiers $[x] for a given expression.
> Authors[0].Name > Authors.$[a].Name

> Authors[1].Age > Authors.$[b].Age

> Authors[2].Books[3].Title > Authors.$[c].Books.$[d].Title

index positions start from [0] which is converted to $[a] and so on.
```csharp
    var path = Prop.PosFiltered<Book>(b => b.Authors[2].Books[3].Title);
```

#### Prop.Elements(index, expression)
returns a path with the filtered positional identifier prepended to the property path.
> (0, x => x.Rating) > a.Rating

> (1, x => x.Rating) > b.Rating

index positions start from '0' which is converted to 'a' and so on.
```csharp
    var res = Prop.Elements<Book>(0, x => x.Rating);
```

#### Prop.Elements()
returns a path without any filtered positional identifier prepended to it.
> b => b.Tags > Tags
```csharp
    var path = Prop.Elements<Book>(b => b.Tags);
```