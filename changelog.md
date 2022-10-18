### CHANGELOG
- switched to LINQ3 translation engine [#info](https://mongodb.github.io/mongo-csharp-driver/2.18/reference/driver/crud/linq3/)
- deprecate `Many<T>.ParentsQueryable()` method due to incompatibility with LINQ3
- new `SaveOnlyAsync` and `SaveExceptAsync` methods that accept an `IEnumerable<string>` of property names #180
- enable sourcelink/symbol packages
- upgrade mongo driver to latest