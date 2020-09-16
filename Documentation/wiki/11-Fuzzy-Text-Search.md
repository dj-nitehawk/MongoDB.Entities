fuzzy text matching is done using the [double metaphone](https://en.wikipedia.org/wiki/Metaphone) algorythm. with it you can find non-exact matches that sounds similar to your search term.

fuzzy matching will only work on properties that are of the type `FuzzyString` which is supplied by this library. it also requires adding these properties to a text index. 

here's how you'd typically get the fuzzy search to work:

#### 1. define entity class
```csharp
    public class Book : Entity
    {
        public FuzzyString AuthorName { get; set; }
    }
```
#### 2. create text index
```csharp
    await DB.Index<Book>()
            .Key(b => b.AuthorName, KeyType.Text)
            .CreateAsync();
```
#### 3. store the entity
```csharp
    new Book { AuthorName = "Eckhart Tolle" }.Save();
```
#### 4. do a fuzzy search on the index
```csharp
    var results = await DB.Find<Book>()
                          .Match(Search.Fuzzy, "ekard tole")
                          .ExecuteAsync();
```
that's all there's to it...

in case you need to start a flunt aggregation pipeline with fuzzy text matching, you can do it like so:
```csharp
    DB.FluentTextSearch<Book>(Search.Fuzzy, "ekard tole")
```
### How it works
when you store text using `FuzzyString` class, the resulting mongodb document will look like this:
```java
{
    ...
    "AuthorName": {
        "Value": "Eckhart Tolle",
        "Hash": "AKRT TL"
    }
    ...
}
```
the text is stored in both the original form and also a hash consisting of double metaphone key codes for each word. when you perform a fuzzy search, your search term is converted to double metaphone key codes on the fly and matched against the stored hash to find results using standard mongodb full text functionality.

#### limitations:
you are only allowed to store strings of up to 250 characters in length, which is roughly about 25 to 30 words max.

### Sorting Fuzzy Results:
if you'd like to sort the results by relevence (closeness to the original search term) you can use the following utility method:
```csharp
    var sortedResults = results.SortByRelevance("ekard tole", b => b.AuthorName);
```
this sorting is done client-side after the fuzzy search retrieves the entities from mongodb. what this extension method does is; it will compare your search term with the value of the property you specify as the second argument to see how close it is using [levenshtein distance](https://en.wikipedia.org/wiki/Levenshtein_distance) algorythm. then it will return a new list with the closest matches at the top.

you can also exclude items from the resulting list that has a greater edit distance than a given value by specifiying the `maxDistance` optional parameter like so:
```csharp
    var sortedResults = results.SortByRelevance("ekard tole", b => b.AuthorName, 10);
```
### [Next Page >>](https://github.com/dj-nitehawk/MongoDB.Entities/wiki/12.-File-Storage)