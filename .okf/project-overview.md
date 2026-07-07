---
type: Reference
title: Project Overview
description: Purpose, scope, capabilities, audience, and glossary for MongoDB.Entities.
tags: [overview, product]
---

# Project Overview

## Purpose

MongoDB.Entities is a lightweight .NET Standard data-access library that wraps the official MongoDB driver with an async, entity-oriented API. It aims to reduce boilerplate around MongoDB access while preserving access to driver concepts when needed.

## Scope and consumers

- Public NuGet package: `MongoDB.Entities`.
- Target consumers: .NET applications using MongoDB as a document database.
- Primary documentation site: `https://mongodb-entities.com`, generated from `Documentation/`.
- Minimum library target framework: `netstandard2.1`.

## Major capabilities

- Static/instance database initialization through `DB.InitAsync`, `DB.Default`, and named `DB.Instance(...)` lookups.
- Entity persistence with `Entity`/`IEntity`, custom ID support, optional audit fields, and custom collection/field attributes.
- Query builders for find, LINQ/queryable, aggregation pipelines, distinct, count, sorting, projection, paging, and fuzzy/full-text search.
- Save, replace, delete, update, update-and-return, and partial save/update helpers.
- Referenced relationships through `One<T>` and `Many<TChild,TParent>` plus embedded relationship patterns in docs.
- Multi-database and multi-client support.
- Multi-document transactions via `DB.Transaction()` / `Transaction`.
- Data migrations via `IMigration`, numbered migration classes, and `DB.MigrateAsync`/`DB.MigrationsAsync`.
- File storage using `FileEntity<T>` and chunked upload/download as a GridFS alternative.
- Index management, change streams, global filters, geospatial helpers, string templates, and sequence counters.

## Status

- Active package version in `MongoDB.Entities/MongoDB.Entities.csproj`: `25.1.0`.
- CI and releases run from tags: Azure Pipelines tests on `v*`, GitHub NuGet publishing on `v*`, GitHub Pages docs publishing on `doc-*`.
- `changelog.md` currently notes migration activator support and MongoDB driver upgrade work.

## Glossary

- **Entity**: Persistable model implementing `IEntity`, usually by inheriting `Entity`.
- **DB**: Main API facade and database instance wrapper; implemented as partial class files under `MongoDB.Entities/DB/`.
- **Default database**: First initialized database for the default client settings.
- **Join collection**: Internal collection storing `Many<TChild,TParent>` relationship records.
- **FileEntity**: Entity subtype whose binary content is stored in chunks.
- **Migration**: Numbered class implementing `IMigration` and executed in ascending order.

## Sources

- `README.md`
- `MongoDB.Entities/MongoDB.Entities.csproj`
- `Documentation/wiki/index.md`
- `Documentation/wiki/Get-Started.md`
- `Documentation/wiki/Entities.md`
- `Documentation/wiki/Relationships-Referenced.md`
- `Documentation/wiki/Data-Migrations.md`
- `Documentation/wiki/File-Storage.md`
- `Documentation/wiki/Transactions.md`
- `changelog.md`
