---
okf_version: "0.1"
---

# OKF Knowledge Set

This directory contains compact operational knowledge for agents working in MongoDB.Entities. Read relevant files before editing. Keep these files synchronized with code, tests, docs, and configuration.

## Core reading order

- [Project Overview](project-overview.md) - Project purpose, audience, capabilities, and glossary.
- [Architecture](architecture.md) - Library architecture, boundaries, persistence model, and invariants.
- [Code Map](code-map.md) - Repository layout and edit guidance.
- [Conventions](conventions.md) - Coding, API, and documentation conventions.

## Workflow and validation

- [Workflows](workflows.md) - Build, test, documentation, benchmark, package, and release workflows.
- [Testing](testing.md) - Test framework, layout, database dependencies, and commands.

## Task-specific references

- [Dependencies](dependencies.md) - Runtime targets, package management, and key libraries.
- [Operations](operations.md) - CI, docs publishing, NuGet release, and local MongoDB service notes.
- [Gotchas](gotchas.md) - Practical traps and non-obvious constraints.
- [Maintenance](maintenance.md) - OKF update rules and conformance checks.

## Authority rule

If OKF conflicts with source code, tests, generated artifacts, or project manifests, verify current behavior from those authoritative sources, then update OKF.

## Maintenance rule

Before finishing work, update OKF if the change affects architecture, behavior, commands, dependencies, tests, deployment, or conventions. If no update is needed, state why in the final response.
