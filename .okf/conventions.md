---
type: Reference
title: Conventions
description: Coding, API, and entity design conventions used in this repository.
tags: [conventions]
---

# Conventions

## Naming
- Namespace: `MongoDB.Entities` for library; tests `MongoDB.Entities.Tests` / `…Tests.Models`.
- `DB` partial class name kept (ReSharper inconsistent-naming suppressed).
- Entity types often suffix `Entity` in tests (`BookEntity`); library base is `Entity`.
- Migration classes: `_NNN_snake_description` implementing `IMigration`.
- Private fields: editorconfig/ReSharper lean camelCase with `_` prefix for private static readonly; parameters camelCase.
- Prefer PascalCase for public members; avoid `this.` qualification (editorconfig suggestions).

## Style
- `.editorconfig`: UTF-8, **CRLF**, spaces, indent 4 for C#.
- `LangVersion` 13 on library (DocFX C# 14 support lag noted in csproj comment).
- Nullable enabled on library and tests.
- Accessibility modifiers: editorconfig sets `dotnet_style_require_accessibility_modifiers = never:error` — match existing file style.
- `DB` feature surface: new file `DB/<Area>.cs` as `public partial class DB`, not a separate service class.
- XML docs on public API (`GenerateDocumentationFile`); `CS1591` suppressed at project level.

## Errors and validation
- Fail fast on empty database names, uninitialized `DB.Instance`/`Default`, bad migration naming.
- `InitAsync` pings unless `skipNetworkPing: true`; failed init removes partial cache entries.
- Prefer `InvalidOperationException` / `ArgumentNullException` consistent with existing methods.

## APIs and data
- Async methods named `*Async`; accept `CancellationToken cancellation = default` where I/O occurs.
- Generic entity constraints: `where T : IEntity` (sometimes `new()`).
- Fluent builders returned from `DB` methods; execution methods on builders (`ExecuteAsync`, etc.).
- Default ID: ObjectId-formatted plain string via `[BsonId]` on `Entity.ID` (stored as BSON string). Any ID type/representation works (`long`, `Guid`, `ObjectId`, `[BsonRepresentation]`/`[AsObjectId]` strings); relationships store/query the ID's stored representation (`BsonValue` join records, `Cache<T>.IdToBsonValue` / `GetBsonId()`). Batch raw-ID APIs expose `IReadOnlyList<TId> where TId : struct` overloads for value-type ID arrays (avoid `IEnumerable<TId>` — `string` is `IEnumerable<char>`). Inherited `Entity.ID` cannot be re-attributed — use a class map or implement `IEntity` to keep BSON ObjectId storage. Internal `[BINARY_CHUNKS].FileID` retains `[AsObjectId]` for legacy file data.
- Collection name: type name or `[Collection("name")]`.
- Ignore persistence: `[Ignore]`, `[IgnoreDefault]`; field rename/order: `[Field]`.
- Relationships: initialize in entity constructor with `InitOneToMany` / `InitManyToMany`; sides via `[OwnerSide]` / `[InverseSide]` where needed.
- Subclass `DB` for hooks: `OnBeforeSave`, `OnBeforeUpdate`, global filters (`SetGlobalFilter*`).

## Config and DI
- No appsettings in library; all connection config via `MongoClientSettings` / connection strings.
- Optional: `db.SetServiceProvider(IServiceProvider)` and `db.SetMigrationActivator(Func<Type,IMigration>)` (activator wins over SP).

## Sources
- `.editorconfig`
- `MongoDB.Entities/MongoDB.Entities.csproj`
- `MongoDB.Entities/DB/DB.cs`
- `Documentation/wiki/Entities.md`
