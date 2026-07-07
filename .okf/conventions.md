---
type: Reference
title: Conventions
description: Coding style, API design, documentation, and repository conventions to preserve.
tags: [conventions, style]
---

# Conventions

## Formatting and style

- Follow `.editorconfig`.
- Use UTF-8, CRLF line endings, spaces, 4-space indentation for most files.
- JSON/HAR files use 2-space indentation.
- C# nullable reference types are enabled in library, tests, and benchmark projects.
- C# language version for the library is `13` because DocFX does not yet support C# 14 in this repo.
- Prefer `var` where `.editorconfig` suggests it.
- Accessibility modifiers are not required by style (`dotnet_style_require_accessibility_modifiers = never:error`).
- ReSharper settings prefer expression-bodied methods/operators and target-typed object creation when the type is evident.

## Naming

- Parameters use lower camel case.
- Private constants use PascalCase.
- Private static readonly fields use lower camel case with `_` prefix.
- Keep public API names aligned with existing API vocabulary: `DB`, `Entity`, `Find`, `Update`, `Many`, `One`, `Migration`, `Transaction`.

## API design

- Public library APIs live under `MongoDB.Entities`.
- Async methods should use `Async` suffix and return `Task`/`Task<T>`.
- Preserve async-first API behavior; avoid adding sync database I/O wrappers.
- Add database facade methods to the relevant partial `DB/DB.*.cs` file.
- Add fluent operation state/behavior to the appropriate builder under `Builders/`.
- Keep consumer-facing examples simple and entity-oriented; avoid exposing raw `BsonDocument` or magic strings unless the feature requires it.
- Preserve access to MongoDB driver constructs where existing APIs accept them (`MongoClientSettings`, `MongoDatabaseSettings`, sessions, filters, projections, pipelines).

## Error handling and validation

- Validate obvious invalid inputs early with clear exceptions, matching existing patterns such as `ArgumentNullException` for empty database names.
- Let MongoDB driver exceptions surface when they convey the authoritative server/driver failure.
- In transaction flows, commit explicitly with `CommitAsync`; disposal/abort should leave no partial writes.

## Persistence and model conventions

- Prefer inheriting `Entity` for standard string IDs generated from MongoDB `ObjectId`.
- Use `IEntity` only when inheritance is not possible or custom ID handling is required.
- Use attributes for persistence customization: collection names, field names, ignore/default-ignore, ID treatment, reference sides, and preserve/don't-preserve behavior.
- Use `ICreatedOn`/`IModifiedOn` for auto-managed audit fields rather than ad-hoc timestamp handling.
- For referenced relationships, initialize `Many<TChild,TParent>` properties in entity constructors using the existing `InitOneToMany`/`InitManyToMany` helpers.

## Documentation conventions

- User docs live in `Documentation/wiki/*.md` and are included by `Documentation/docfx.json`.
- Update `Documentation/toc.yml` when adding pages that should appear in navigation.
- API reference YAML in `Documentation/api/` is generated from the library project.
- Keep README concise; it points users to the official docs site.

## Dependency and simplicity rules

- Keep the runtime library small. New dependencies should have direct value for the NuGet package, not just implementation convenience.
- Preserve public API compatibility unless a breaking change is intentional and documented.
- Prefer existing builders/extensions over new abstraction layers.

## Sources

- `.editorconfig`
- `MongoDB.Entities/MongoDB.Entities.csproj`
- `MongoDB.Entities/DB/DB.cs`
- `MongoDB.Entities/Builders/`
- `MongoDB.Entities/Extensions/`
- `Documentation/docfx.json`
- `Documentation/toc.yml`
- `Documentation/wiki/Entities.md`
- `Documentation/wiki/Relationships-Referenced.md`
