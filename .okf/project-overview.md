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

## Status
- Published NuGet package; runtime compatibility and version source: [Dependencies](dependencies.md).
- Canonical user docs: https://mongodb-entities.com (built from `Documentation/`)

## Non-goals
- Not a full EF-style change tracker / unit-of-work beyond explicit save and transactions
- Not a server or admin tool

## Sources
- `README.md`
- `MongoDB.Entities/MongoDB.Entities.csproj`
- `Documentation/wiki/index.md`
