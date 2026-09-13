---
name: alif-agent
description: "Specialized multi-agent graph orchestrator for the Alif educational 2D adventure game (Unity 6000 URP, pure C# models, Yarn Spinner, TileRPG specs). Coordinates task decomposition, GitHub reference discovery (find-repo), level generation, architecture linting, and recursive self-improvement."
---

# Alif Agent: Specialized Agentic Graph & Self-Improvement Engine

The `alif-agent` skill provides a coordinated multi-agent workflow tailored specifically to the architecture of **Alif** (Unity 6000.6.0f1, 2D URP, Input System, Yarn Spinner, decoupled pure C# models, TileRPG procedural worlds).

---

## 🎯 When to Use

Activate and consult this skill whenever:
- Designing or modifying **TileRPG world specifications** (`Assets/AI/TileRpg/Specs/*.json`).
- Authoring pure C# domain models (`Alif.Adventure.Model`, `Alif.Battle.Model`) or runtime MonoBehaviours (`Alif.Runtime`).
- Writing or refactoring **Yarn Spinner dialogue trees** (`Assets/Dialogue/*.yarn`).
- Searching GitHub for reference implementations or open-source libraries via embedded **`find-repo`**.
- Running validation, EditMode tests, or checking doorway overlap integrity.
- Triggering recursive self-improvement to upgrade the agent's knowledge base as the game develops.

---

## 🤖 Subagents & Graph Roles

The Alif Agentic Graph orchestrates specialized roles:

| Node | Responsibilities |
|---|---|
| **`orchestrator`** | Analyzes developer intent, decomposes tasks into sub-problems, and schedules graph execution. |
| **`repo_hunter`** | Leverages embedded `find-repo` to search GitHub for reference mechanics, pixel art shaders, or Yarn tools. |
| **`design_asset_agent`** | Plans, generates, and validates 2D pixel art tiles, props, EmoteBubble sprites, and TileRPG specs with paired `.meta`. |
| **`world_architect`** | Authors and validates 2D TileRPG world specs against `world.schema.json` and ensures BFS path reachability. |
| **`gameplay_dev`** | Enforces C# PascalCase, 4-space indentation, pure model isolation (zero UnityEngine in models), and Yarn syntax. |
| **`qa_verifier`** | Executes test suites (`./Tools/alif test-edit`), inspects doorway overlaps (`>= 0.2f`), and flags missing `.meta` files. |
| **`self_healer`** | Diagnoses validation errors and automatically applies surgical repairs to level specs or code. |
| **`evolution`** | Scans recent code and commits, extracts newly introduced symbols, and recursively updates agent memory. |

---

## 💻 CLI Commands

Run from the Alif repository root:

```bash
# Execute the agent graph on a task
./Tools/alif agent run "Design a bazaar market with 2 food stalls"

# Generate specific design assets and world specs
./Tools/alif asset-gen generate-spec MarketBazaar --theme market
./Tools/alif asset-gen create-tile stall_fruit --type market_stall
./Tools/alif asset-gen create-emote emote_sparkle --type sparkle
./Tools/alif asset-gen validate-all

# Perform planning without mutating files
./Tools/alif agent run "Search reference repos for turn-based combat" --dry-run

# Inspect agent system status and discovered symbols
./Tools/alif agent status

# View current prompt-ready architectural knowledge
./Tools/alif agent memory

# Run the recursive evolutionary learning engine
./Tools/alif agent evolve

# Search GitHub repositories using embedded find-repo
./Tools/alif find-repo "unity 2d combat dialogue"
```

---

## 📐 Non-Negotiable Invariants

1. **Decoupled Domain Models**: `Alif.Adventure.Model` and `Alif.Battle.Model` must remain pure C#. Never import `UnityEngine` or inherit from `MonoBehaviour`.
2. **Floor Whitelist Authority**: Doorways between `WalkableArea` boxes must overlap by at least `0.2f` units.
3. **Prop Feet Colliders**: Solid colliders on props must only occupy ground-level feet area to prevent player snagging.
4. **Dialogue Separation**: All branching dialogue must reside in `Assets/Dialogue/*.yarn` (using plain text `->` choices and `<<jump>>`), never hardcoded into C#.
5. **Asset Metadata**: Every newly added asset must have a paired `.meta` file.

---

## 🔄 Recursive Evolution Workflow

Whenever significant features, new classes, or dialogue trees are added to Alif:
1. Run `./Tools/alif agent evolve`
2. The engine scans git logs, extracts public classes, Yarn nodes, and tile specs.
3. It updates `Tools/alif-agent/memory/knowledge_base.json` and `Docs/agent_memory/README.md`.
4. Subsequent agent sessions automatically inherit the expanded project knowledge.
