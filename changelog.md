[//]: # (### NEW)

[//]: # (### IMPROVEMENTS)

# ⚠️ Major Version With Breaking Changes ⚠️

Version 25 is a major version jump with many breaking changes. It is not recommended to upgrade to v25 unless you have the time to go through the [latest documentation](https://mongodb-entities.com/) and spend some effort to upgrade your existing project code. It's not that complicated to upgrade but will involve a lot of manual work if your codebase is quite large. AI could possibly help with that.

v25 modernizes the API and brings proper multi-database support. The old way of doing things via `DB` static calls is gone. All operations must now be performed via a `DB` instance. The extension methods targeting entities such as `something.SaveAsync()` are no longer available. The features that were tied to `DBContext` has also been merged in to the `DB` class.