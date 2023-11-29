### NEW

ability to use any type for primary key property and ability to name it based on mongodb conventions when implementing `IEntity` interface.

**NOTE:** due to a technical constraint, only the following primary key types are supported with referenced relationships.

- string
- long
- ObjectId

see #195 for more info.

### IMPROVEMENTS

- `Entity.ID` property has been made non-nullable #210
- support for dictionary based index keys #206
- upgrade mongodb driver to v2.22
- various internal code refactors and optimizations

### BREAKING CHANGES

- `Many<T>` is now `Many<TChild,TParent>` when defining referenced relationships. i.e. you now need to specify the type of the parent class that contains the property.
- `IEntity.GenerateNewID()` & `IEntity.HasDefaultID()` method must be implemented by entities if implementing `IEntity` directly.