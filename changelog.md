### BREAKING CHANGES
- upgrade TFM to `netstandard2.1`
- enable nullable reference type support #194
- remove implicit opertators from `Date` & `FuzzyString` types due to incompatibility with LINQ3

### IMPROVEMENTS
- make watcher compatible with linq v3 engine
- upgrade mongodb driver to v2.19