### BREAKING CHANGES
- upgrade TFM to `netstandard2.1`
- enable nullable reference type support #194
- remove implicit opertators from `Date`,`FuzzyString` & `One<T>` types due to incompatibility with LINQ3

### FIXES
- fix string concatenation issue with LINQ3 and `FuzzyString` and `Date` serializers

### IMPROVEMENTS
- make watcher compatible with linq v3 engine
- upgrade mongodb driver to v2.19.1
- add parameterless ctor to `Coordinates2D` class #201