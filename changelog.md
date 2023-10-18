### NEW

- wip: ability to use any type for `ID` property and ability to name it based on mongodb conventions.
- todo: update docs about no support for relationships with custom id types except for `ObjectId` & `long`

### IMPROVEMENTS

- `Entity.ID` property has been made non-nullable
- support for dictionary based index keys #206
- various internal code refactors and optimizations
- upgrade mongodb driver to v2.22

### BREAKING CHANGES

- `Many<TParent>` is now `Many<TChild,TParent>`.
- `IEntity.HasDefaultID()` method must be implemented by entities if implementing `IEntity` directly.