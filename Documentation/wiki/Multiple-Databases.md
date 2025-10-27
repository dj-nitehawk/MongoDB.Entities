# Multiple database support
you can store and retrieve Entities in multiple databases on either a single server or multiple servers. the only requirement is to have unique names for each database. the following example demonstrates how to use multiple databases.

### Test Case Usage example, 1 customer per database
It save the same Entity (Auto) to two different databases.

```csharp
var _db1 = await DB.InitAsync("Customer1");
var _db2 = await DB.InitAsync("Customer1");

    var auto1 = new Auto
    {
        Make = "Toyota",
        Model = "Corolla",
        Year = 2020
    };
    await auto1.SaveAsync(_db1);
    
    var auto2 = new Auto
    {
        Make = "Honda",
        Model = "Civic",
        Year = 2021
    };
    await auto2.SaveAsync(_db2);
    
    var res1 = await _db1.Find<Auto>().MatchID(auto1.ID).ExecuteSingleAsync();
    Assert.IsNotNull(res1);
    Assert.AreEqual(auto1.Make, res1.Make);
    Assert.AreEqual(auto1.Model, res1.Model);
    Assert.AreEqual(auto1.Year, res1.Year);

    var res2 = await _db2.Find<Auto>().MatchID(auto2.ID).ExecuteSingleAsync();
    Assert.IsNotNull(res2);
    Assert.AreEqual(auto2.Make, res2.Make);
    Assert.AreEqual(auto2.Model, res2.Model);
    Assert.AreEqual(auto2.Year, res2.Year);

    res1 = await _db1.Find<Auto>().MatchID(auto2.ID).ExecuteSingleAsync();
    res2 = await _db2.Find<Auto>().MatchID(auto1.ID).ExecuteSingleAsync();
    
    Assert.IsNull(res1);
    Assert.IsNull(res2);
```

### Limitations
- cross-database relationships with `Many<T>` is not supported.
- no cross-database joins/ look-ups as the driver doesn't support it.