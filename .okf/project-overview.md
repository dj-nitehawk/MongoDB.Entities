---
type: Reference
title: Project Overview
description: MongoDB.Entities is a lightweight .NET data-access library over the official MongoDB driver.
tags: [overview]
resource: README.md
---

# Project Overview

## Purpose
.NET Standard library that abstracts `MongoDB.Driver` with an async-first, entity-oriented API: save/find/update/delete builders, relationships, migrations, indexes, transactions, change streams, file chunks, and multi-database support.

## Scope
- **In:** NuGet library `MongoDB.Entities` (library project), MSTest suite, BenchmarkDotNet harness, DocFX site under `Documentation/`.
- **Out:** Host applications, server provisioning, ODM for non-Mongo stores.

## Capabilities
- Entity model via `IEntity` / `Entity` / `FileEntity<T>`; default IDs are ObjectId-formatted strings stored as plain strings, but any ID type/representation is supported (incl. relationships)
- Fluent ops: `Save`/`Insert`/`Find`/`Update`/`UpdateAndGet`/`Replace`/`Delete`/`PagedSearch`/`Distinct`/`Count`
- LINQ (`Queryable`), aggregation pipelines + string `Template`s, GeoNear
- Relationships: `One<T>`, `Many<TChild,TParent>` (1-1 / 1-N / N-N via join collections)
- Indexes (incl. fuzzy text), global filters, audit (`ICreatedOn`/`IModifiedOn`/`ModifiedBy`)
- Transactions (`Transaction`), change streams (`Watcher`), file chunk streaming
- Ordered data migrations (`IMigration`), multi-client / multi-DB (`DB.InitAsync` / `DB.Instance` / `DB.Default`)

## Status
- Published NuGet package; library version from `MongoDB.Entities/MongoDB.Entities.csproj` (`Version`, currently 26.0.0-beta.1)
- TFM: library `netstandard2.1`; tests/benchmarks `net10.0`
- Canonical user docs: https://mongodb-entities.com (built from `Documentation/`)

## Non-goals
- Not a full EF-style change tracker / unit-of-work beyond explicit save and transactions
- Not a server or admin tool

## Glossary
| Term | Meaning |
| --- | --- |
| `DB` | Main entry type; partial class split across `MongoDB.Entities/DB/` |
| Default DB | First `DB.InitAsync` instance for a client; `DB.Default` |
| Join collection | Backing collection for `Many<>` relationships (`JoinRecord`) |
| Migration | Type implementing `IMigration` named `_NNN_description` |

## Sources
- `README.md`
- `MongoDB.Entities/MongoDB.Entities.csproj`
- `Documentation/wiki/index.md`
