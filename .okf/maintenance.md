---
type: Reference
title: OKF Maintenance
description: Rules for keeping OKF synchronized and conformant.
tags: [okf, maintenance]
---

# OKF Maintenance

Preserve OKF v0.1 conformance:

- every non-reserved `.md` file needs YAML frontmatter with a non-empty `type` field;
- `index.md` and `log.md` are reserved filenames;
- only the bundle-root `.okf/index.md` may include frontmatter, and only for `okf_version`;
- do not create empty placeholder files.

## Update OKF when changing

- architecture, module boundaries, or public API shape;
- public docs, examples, API reference, package metadata, or release workflow;
- entity, relationship, file-storage, migration, transaction, or query semantics;
- build, test, benchmark, docs, package, or release commands;
- dependencies, runtime versions, target frameworks, or tool versions;
- test framework, test layout, database setup, fixtures, or validation expectations;
- CI, Docker, environment variables, ports, or operational assumptions;
- coding conventions, generated-file rules, or repository layout;
- known gotchas or common failure modes.

## Resolve conflicts

If OKF conflicts with code, tests, generated artifacts, or manifests:

1. Verify current behavior from authoritative sources.
2. Update the stale OKF file.
3. Mention the correction in the final response.

Source code, tests, `.csproj` files, solution files, CI workflows, generated artifacts, and lock/config files are more authoritative than OKF prose.

## Review expectations

- Keep OKF concise and operational; link to canonical docs rather than duplicating long pages.
- Cite source paths in each concept file where practical.
- Do not copy secrets, private keys, tokens, production data, or local credential values.
- Prefer targeted updates over regenerating the whole knowledge set.
- Before finishing any task, either update affected OKF files or explicitly state why OKF was unaffected.

## Sources

- `.okf/index.md`
- `AGENTS.md`
