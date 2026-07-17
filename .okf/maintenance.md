---
type: Reference
title: Maintenance
description: How to keep the OKF knowledge set conformant and synchronized.
tags: [maintain]
---

# Maintenance

Day-to-day finish gate and reading budget: repo `AGENTS.md`. This file is the detailed trigger inventory and conformance checklist.

## Conformance
- OKF v0.1: non-reserved `.md` files need YAML frontmatter with non-empty `type` (allowed list), `title`, and `description`.
- Bundle-root `index.md` is a listing; only it may set `okf_version: "0.1"`.
- Allowed types: `Reference`, `Architecture`, `Playbook`, `API Endpoint`, `Database`, `Service`, `Event`, `Security`, `Deployment`, `Generated`, `ADR`. Do not invent types; use `Reference` if unsure.
- No empty placeholders; omit inapplicable files rather than stubbing.
- Optional `log.md` is append-only notes — not a concept file; do not create unless requested.

## Update triggers
Sync `.okf/` when changes affect:
- architecture / module boundaries
- public APIs, entity contracts, builders, migrations framework
- persistence / collection / relationship behavior
- dependencies, TFMs, package versions, package manager layout
- build / test / pack / docs / deploy commands or CI
- testing strategy, Mongo fixtures, parallelization
- security/auth connection assumptions
- config/env/ports/ops
- conventions, layout, gotchas

If unaffected, state why in the final response (`OKF unaffected (non-behavioral edit)` for pure comment/typo/format).

## Conflicts
1. Prefer source, tests, generated artifacts, lockfiles/manifests over OKF prose.
2. Fix OKF to match verified behavior.
3. Mention the correction in the agent’s final response.
