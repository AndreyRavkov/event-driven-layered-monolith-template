---
paths:
  - "solution/AR/**/*.cs"
---

## Code style (build-breaking)

`Directory.Build.props` sets `TreatWarningsAsErrors=true`, `EnableNETAnalyzers=true`, and `EnforceCodeStyleInBuild=true`, so **the build fails on any analyzer or code-style violation**. Rules promoted to errors via `.editorconfig` (see also `.claude/rules/code-style.md`):

- `IDE0005` — unused `using` directives are errors; remove them.
- File-scoped namespaces (`namespace X;`), `using` directives **outside** the namespace.
- `var` required when the type is clear; expression-bodied members required.
- Private fields must be `_underscorePrefixed`.
- Async methods returning `Task`/`ValueTask` must end with `Async`.

Target `net10.0`, nullable + implicit usings enabled, XML doc generation on (`CS1591` suppressed).

# Code Development Rules

- each class should follow the solution naming convention.
- each class (or object) is located in the right folder (or layer) and be in the right namespace.
- each class should have a single responsibility.
- each class should have a clear and concise name.
- each class should have a clear and concise purpose.