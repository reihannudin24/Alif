# Repository Evaluation & Reference Guide

When evaluating GitHub repositories to recommend to developers building apps, grade them across these 4 pillars:

## 1. Relevance to Developer Architecture
- **Type of Project**:
  - **Starter / Boilerplate**: Good when starting a project from scratch with predefined routing, auth, and tooling.
  - **Library / SDK**: Good when adding a specific capability into an existing codebase without rewriting it.
  - **Showcase / Reference Architecture**: Full-fledged app showing how production patterns (auth, DB migrations, state management) interact.
  - **Educational / Demo**: Useful for conceptual understanding, but often lacks edge-case handling.

## 2. Maintenance & Health Indicators
- **Recent Push Date**:
  - `< 3 months`: Highly active; matches current framework versions.
  - `3 - 12 months`: Stable; review open PRs and issues.
  - `> 12 months`: Use with caution; check if framework breaking changes (e.g. Next.js 15, Svelte 5, React 19) affect it.
  - `> 24 months` / `Archived`: Only useful as a conceptual/algorithmic reference.
- **Open Issues / PR Ratio**: High ratio of unresolved bug reports with no maintainer comments indicates abandonment.

## 3. License Suitability
- **Permissive (Safe for Commercial & Closed Apps)**:
  - `MIT`, `Apache-2.0`, `BSD-2-Clause`, `BSD-3-Clause`, `ISC`
- **Copyleft (Requires sharing derivative source code)**:
  - `GPL-3.0`, `AGPL-3.0`, `LGPL`
- **Unlicensed / Proprietary**:
  - Treat as copyrighted code unless explicitly released by the author.

## 4. Code Quality & Ergonomics
- Does it have clear directory separation (e.g., `src/`, `components/`, `lib/`, `tests/`)?
- Does it include automated tests or CI workflows (`.github/workflows`)?
- Is there a clear configuration file (`tsconfig.json`, `Cargo.toml`, `pyproject.toml`, `go.mod`)?
