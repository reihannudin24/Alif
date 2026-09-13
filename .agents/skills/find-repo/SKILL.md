---
name: find-repo
description: >-
  Find GitHub repositories through natural semantic search, discover reference
  implementations, starter templates, and peer projects, or compare similar
  repositories to help developers build applications. Use whenever a developer
  asks to find GitHub repos, boilerplates, libraries, open-source alternatives,
  or architectural references for their tech stack.
---

# GitHub Semantic Repo Finder

Empower developers to find the best GitHub repositories, reference architectures, starter templates, and peer projects using natural semantic search.

GitHub's native search uses strict token intersection matching, which often returns zero results for natural language developer queries (e.g. *"lightweight desktop chat app with local LLMs in Rust"*). This skill bridges that gap by decomposing developer intent into multi-angle search vectors, ranking repositories by relevance and health, and surfacing actionable insights.

---

## Capabilities

1. **Semantic Search**: Natural language intent search for boilerplates, libraries, architecture patterns, and full-stack reference apps.
2. **Similarity & Peer Discovery**: Given an existing repo or technology (e.g., `shadcn/ui`, `pocketbase`), find peer repositories, direct alternatives, or cross-framework ports (e.g., Svelte/Vue equivalents).
3. **Architecture & Health Inspection**: Deeply inspect repository structure, language breakdown, recent commit activity, maintenance status, license, and README features.

---

## Workflow Guide

### Step 1: Deconstruct Developer Intent

When a developer asks for a repository or reference implementation:
1. **Identify the core application domain**: e.g., `kanban`, `auth`, `video-streaming`, `vector-search`, `chat`.
2. **Identify the target tech stack**: e.g., `Next.js`, `Svelte`, `Rust`, `FastAPI`, `Flutter`, `Tauri`.
3. **Determine the target repository type**:
   - **Template / Boilerplate**: Starting a new app from scratch (`--template`).
   - **Library / Component**: Looking for a package to integrate.
   - **Full-Stack Reference**: Looking for an existing production-grade project to study design patterns and folder structures.

### Step 2: Execute the Search Engine

Use the helper script [find_repo.py](./scripts/find_repo.py):

#### Semantic Search
```bash
python3 .agents/skills/find-repo/scripts/find_repo.py search "<natural_language_prompt>" [options]
```
Common options:
- `-l, --language <Lang>`: Restrict to a specific language (e.g. `Rust`, `TypeScript`, `Python`, `Svelte`).
- `--min-stars <N>`: Filter out repositories below a community trust threshold (e.g. `--min-stars 20`).
- `-n, --limit <N>`: Return top $N$ ranked candidates (default 10).
- `--template`: Filter for official GitHub template repositories.
- `--sort <best-match|stars|updated>`: Sorting strategy.
- `--json`: Output raw JSON for programmatic handling.

*Example:*
```bash
python3 .agents/skills/find-repo/scripts/find_repo.py search "real-time whiteboard collaboration with webrtc" -l TypeScript -n 5
```

#### Finding Similar Repositories & Alternatives
```bash
python3 .agents/skills/find-repo/scripts/find_repo.py similar "<owner/repo>" [options]
```
*Example:*
```bash
# Find Svelte alternatives/equivalents to shadcn-ui
python3 .agents/skills/find-repo/scripts/find_repo.py similar "shadcn-ui/ui" -l Svelte -n 5
```

#### Inspecting Repository Architecture & Health
```bash
python3 .agents/skills/find-repo/scripts/find_repo.py inspect "<owner/repo>"
```
Fetches commit recency, health status, licensing, language breakdown, top-level directory structure, and README summary.

---

### Step 3: Evaluate and Synthesize Results

Before presenting repositories to the developer, evaluate them against the [Repository Evaluation Criteria](./references/evaluation_criteria.md):
- **Maintenance Health**: Is the repository actively maintained (`Active`), stale (`> 1 year`), or dormant (`> 2 years`)?
- **License Permissiveness**: Is it safe for the developer's project (`MIT`, `Apache-2.0`) or copyleft (`GPL`)?
- **Architectural Fit**: Does the folder structure and setup match the developer's project setup?

### Step 4: Present Actionable Recommendations

Format your response clearly:
1. **Summary Table**: Name, Stars, Language, License, Last Updated, and One-line Description.
2. **Top Recommendations with Context**:
   - Why this repository matches their specific problem.
   - Key architectural features (e.g., uses WebSockets + IndexedDB, Clean Architecture, Tailwind v4).
   - How to use it (as a starter template vs code reference vs library).
3. **Quick Commands**: Direct `git clone` or inspection command to get started immediately.

---

## Reference Guides

- [GitHub Advanced Search Syntax Guide](./references/search_syntax_guide.md): Reference qualifiers (`topic:`, `in:readme`, `stars:`, `is:template`).
- [Repository Evaluation Criteria](./references/evaluation_criteria.md): Checklist for evaluating open-source projects for production apps.
