# GitHub Advanced Search Qualifiers for Developers

A reference cheat sheet for querying GitHub repositories, reference architectures, boilerplates, and code patterns.

## 1. Core Metadata Qualifiers

| Qualifier | Example | Purpose |
| :--- | :--- | :--- |
| `language:<LANG>` | `language:rust` | Restricts to primary programming language. |
| `topic:<TOPIC>` | `topic:kanban` | Matches repos tagged with GitHub topic tags. Highly effective for semantic discovery. |
| `stars:<OP><NUM>` | `stars:>500`, `stars:100..500` | Filters by community adoption and trust. |
| `pushed:<OP><DATE>` | `pushed:>2025-01-01` | Filters by freshness. Essential for modern stacks (e.g. Next.js App Router vs Pages). |
| `archived:false` | `archived:false` | Eliminates abandoned/read-only repositories. |
| `is:template` | `is:template` | Identifies repos explicitly marked as starter templates. |
| `license:<KEY>` | `license:mit`, `license:apache-2.0` | Filters for commercially-friendly open source licenses. |

## 2. Text Scope Qualifiers

- `in:name`: Matches term only in the repository slug/name.
- `in:description`: Matches term in the repo summary line.
- `in:readme`: Searches inside the full README.md content (useful for architecture terms like "CQRS", "Event Sourcing", or "multi-tenant").

## 3. Query Decomposition Strategy

When developers ask natural questions like:
> *"I want to build a real-time collaborative canvas like Figma in React"*

Do **not** search GitHub with that full sentence. Exact token matching will return 0 results. Instead, decompose into:

1. **Core Concept & Topics**:
   - `topic:canvas topic:collaborative`
   - `topic:whiteboard language:typescript`
2. **Ecosystem & Synonyms**:
   - `webrtc yjs tldraw react`
   - `realtime canvas language:typescript`
3. **Quality & Freshness**:
   - `archived:false stars:>50`
