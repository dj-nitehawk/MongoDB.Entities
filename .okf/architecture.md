---
type: Architecture
title: Architecture
description: High-level library architecture, module boundaries, persistence model, and invariants.
tags: [architecture, boundaries, persistence]
---

# Architecture

## Style

MongoDB.Entities is a single .NET library package. The public API is concentrated in the `MongoDB.Entities` namespace and organized as small partial classes, builders, extension methods, base entities, and relationship abstractions over `MongoDB.Driver`.

```text
consumer app
  -> MongoDB.Entities public API (`DB`, builders, entities, relationships)
      -> MongoDB.Driver / BSON serialization
          -> MongoDB server
```

## Components

| Component | Location | Role |
| --- | --- | --- |
| `DB` facade | `MongoDB.Entities/DB/` | Initialization, database instance lookup, collections, CRUD, query, migrations, transactions, change streams, files, and helper factories. |
| Builders | `MongoDB.Entities/Builders/` | Fluent command/query builders such as `Find`, `Update`, `Replace`, `PagedSearch`, and `Index`. |
| Core entities and utilities | `MongoDB.Entities/Core/` | `Entity`, `IEntity`, `FileEntity<T>`, serializers, attributes, transactions, templates, watchers, audit helpers, and utility logic. |
| Relationships | `MongoDB.Entities/Relationships/` | `One<T>`, `Many<TChild,TParent>`, join records, and relationship manipulation extensions. |
| Migrations | `MongoDB.Entities/Migrations/` and `DB/DB.Migrate.cs` | Migration contracts, metadata, discovery, ordering, and execution. |
| Extensions | `MongoDB.Entities/Extensions/` | Extension methods for collections, entities, dates, delete/update helpers, identity, reflection, and relationships. |
| Documentation | `Documentation/wiki/`, `Documentation/api/` | User docs and DocFX API metadata/site output. |
| Tests | `Tests/` | MSTest coverage against MongoDB local service or Testcontainers. |

## Dependency rules

- Library code depends on `MongoDB.Driver` and BSON APIs; do not introduce unrelated runtime dependencies without a clear package need.
- Public APIs should stay in the `MongoDB.Entities` namespace unless existing layout says otherwise.
- `DB` is intentionally partial by capability. Add new database facade behavior in the relevant `DB/DB.*.cs` file rather than growing unrelated files.
- Builders encapsulate fluent operation construction; avoid duplicating builder logic in tests or docs-only samples.
- Tests and benchmarks may reference the library project; the library project must not reference test or benchmark projects.

## Persistence model

- Entities are MongoDB documents; the base `Entity` supplies string `ID` values generated from MongoDB `ObjectId` by default.
- Custom IDs are supported through `IEntity`/`GenerateNewID`, but referenced relationships only support selected ID types documented in `Documentation/wiki/Entities.md`.
- `ICreatedOn` and `IModifiedOn` are opt-in audit interfaces that the library updates automatically.
- Referenced one-to-many and many-to-many relationships use separate join collections, not embedded child mutation.
- File storage writes metadata to the file entity document and binary data to chunk records managed by `DataStreamer<T>`.
- Migrations persist migration state and run numbered migration classes in order.

## Communication and runtime model

- APIs are async-first; documentation describes async-only use for scalable applications.
- `DB.InitAsync` creates/reuses `MongoClient` instances keyed by `MongoClientSettings` and `DB` instances keyed by database name.
- The first initialized database for the default client settings becomes `DB.Default`.
- Transactions wrap MongoDB client sessions. Transactional relationship and file operations require the session to be passed to lower-level calls where documented.

## Security/auth model

- The library delegates authentication and authorization to MongoDB connection settings supplied by consumers.
- Repository test infrastructure uses local/test MongoDB credentials in `docker-compose.ci.yml` and `Tests/Init.cs`; do not copy credential values into documentation beyond pointing to those files.

## Invariants to preserve

- Keep the library package compatible with `netstandard2.1` unless the project intentionally changes its public support matrix.
- Preserve async APIs and `ConfigureAwait(false)` patterns where already used in library internals.
- Preserve public API compatibility unless a breaking change is intentional and documented.
- Keep generated documentation/API output separate from source edits; update source XML/docs and regenerate generated docs when required by the docs workflow.
- Do not bypass MongoDB driver serialization conventions with ad-hoc string/BSON manipulation unless existing builders or docs require it.

## Sources

- `MongoDB.Entities/MongoDB.Entities.csproj`
- `MongoDB.Entities/DB/`
- `MongoDB.Entities/Builders/`
- `MongoDB.Entities/Core/`
- `MongoDB.Entities/Relationships/`
- `MongoDB.Entities/Migrations/`
- `Documentation/wiki/Entities.md`
- `Documentation/wiki/Relationships-Referenced.md`
- `Documentation/wiki/File-Storage.md`
- `Documentation/wiki/Transactions.md`
- `Tests/Init.cs`
