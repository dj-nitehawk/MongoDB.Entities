### BREAKING CHANGES
- upgrade TFM to `netstandard2.1` (.NET Framework is no longer supported)
- enable nullable reference type support #194
- remove implicit opertators from `Date`,`FuzzyString` & `One<T>` types due to incompatibility with LINQ3

### FIXES
- fix string concatenation issue with LINQ3 and `FuzzyString` and `Date` serializers

### IMPROVEMENTS
- make watcher compatible with linq v3 engine
- add parameterless ctor to `Coordinates2D` class #201
- upgrade mongodb driver to v2.19.2