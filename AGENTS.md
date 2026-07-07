# Agent Instructions

## OKF knowledge set

This repository uses `.okf/` as compact operational memory for AI agents.

Keep `.okf/` compliant with OKF v0.1:

- non-reserved `.md` files must start with YAML frontmatter;
- frontmatter must include a non-empty `type` field;
- `index.md` and `log.md` are reserved filenames;
- only the bundle-root `.okf/index.md` may include frontmatter, for `okf_version`.

### Before starting work

Read relevant OKF files before editing code, tests, docs, or configuration.

Start with:

- `.okf/index.md`
- `.okf/project-overview.md`
- `.okf/architecture.md`
- `.okf/code-map.md`
- `.okf/conventions.md`

Then read task-specific files, such as:

- `.okf/testing.md`
- `.okf/workflows.md`
- `.okf/dependencies.md`
- `.okf/operations.md`
- `.okf/gotchas.md`

Read only files relevant to the task. Do not treat OKF as a replacement for checking source code, tests, generated artifacts, or project manifests when exact behavior matters.

### During work

Use OKF to preserve project conventions, boundaries, and workflows.

If OKF conflicts with source code, tests, generated artifacts, or project manifests:

1. Prefer verified current behavior from authoritative sources.
2. Update OKF to match the verified behavior.
3. Mention the correction in your final response.

### Before finishing work

Check whether your change affects OKF.

Update `.okf/` when changing:

- architecture or module/service boundaries;
- public APIs, schemas, contracts, events, or message formats;
- persistence models, migrations, relationships, file storage, or data ownership;
- dependency versions, frameworks, runtime versions, or package management;
- build, run, test, lint, format, generation, benchmark, or deployment commands;
- testing strategy, test layout, fixtures, or required validation steps;
- security/auth behavior;
- configuration, environment variables, ports, or operational assumptions;
- coding conventions or repository layout;
- known gotchas or common failure modes.

If no OKF update is needed, explicitly state why in your final response.

Do not consider the task complete until OKF is synchronized or explicitly unaffected.

## General expectations

- Keep changes focused and minimal.
- Prefer existing project patterns over new abstractions.
- Do not edit generated files unless the project explicitly requires it.
- Run relevant validation commands before finishing when practical.
- Do not copy secrets, private keys, tokens, production data, or local credential values into docs or OKF.
