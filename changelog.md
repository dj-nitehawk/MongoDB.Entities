
### NEW
- wip: ability to use any type for `ID` property and ability to name it based on mongodb conventions.
- todo: update docs about no support for relationships with custom id types.

### IMPROVEMENTS
- `Entity.ID` property has been made non-nullable
- support for dictionary based index keys #206
- upgrade mongodb driver to v2.21.0

### MINOR BREAKING
- `IEntity.HasDefaultID()` method must be implemented by entities if implementing `IEntity` directly.