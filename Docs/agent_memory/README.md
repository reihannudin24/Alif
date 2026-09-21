# Alif Agent Evolving Memory Ledger

> Auto-updated by the Alif Recursive Evolution Engine (`./Tools/alif agent evolve`).
> Last updated: 2026-09-21 11:57:56

## 📐 Architectural Rules & Invariants

### `ASM_SEPARATION`: Decoupled Domain Models
Alif.Adventure.Model and Alif.Battle.Model must be pure C# with 0 UnityEngine dependencies. MonoBehaviours live in Alif.Runtime.

### `WALKABLE_OVERLAP`: Floor Whitelist Doorway Overlap
Ambang pintu (doorways) between WalkableArea boxes must overlap by at least 0.2f units to prevent player snagging.

### `FEET_COLLIDERS`: Prop Feet Colliders
Props standing on the floor use small feet colliders at ground level, never full-body solid blockers.

### `YARN_DIALOGUE`: Plaintext Yarn Narrative
Dialogue scripts reside in Assets/Dialogue/*.yarn. Node headers: title, tags. Choices use -> and <<jump>>.

### `TILERPG_SCHEMA`: TileRPG Schema Conformance
TileRPG world specs must adhere to Tools/ai-tile-rpg/world.schema.json with valid ground and collision layers.

### `CODE_STYLE`: C# PascalCase & 4-Space Indent
Four-space indentation, braces on the same line, PascalCase public types/methods, camelCase locals.

## 🔍 Discovered Project Symbols
- **Total Core C# Classes**: 145
- **Yarn Dialogue Nodes**: AgentGeneratedStory, BuSiti_Intro, BuSiti_MarketIntro, Station_NoticeBoard
- **Authored Tile Specs**: AgentGardenWorld, AgentMarketWorld, MarketBazaar, sample-world

## 🛡️ Error & Resolution Registry
- **DOORWAY_SNAG**: Player stuck at room transition or doorway threshold
  - *Cause*: WalkableArea doorway overlap is less than 0.2f
  - *Solution*: Expand adjoining WalkableArea box boundaries so they overlap by >= 0.2f in world units

- **MISSING_META**: Asset unassigned or GUID lost after reload
  - *Cause*: Asset file added without corresponding .meta file
  - *Solution*: Run unity CLI to generate .meta or commit paired .meta file

- **ASSEMBLY_DEPENDENCY_LEAK**: Compile error: UnityEngine not found in Alif.Adventure.Model
  - *Cause*: Using UnityEngine types (Vector3, GameObject, Transform) inside domain models
  - *Solution*: Use pure C# representations or domain coordinates in Alif.Adventure.Model

## 🔄 Evolution Cycles History
- `[2026-09-21T10:15:56.813254]` Evolved knowledge base: 139 C# symbols, 4 Yarn nodes, 4 tile specs. Discovered 0 new symbols.
- `[2026-09-21T11:17:52.551357]` Evolved knowledge base: 140 C# symbols, 4 Yarn nodes, 4 tile specs. Discovered 1 new symbols.
- `[2026-09-21T11:34:12.845180]` Evolved knowledge base: 140 C# symbols, 4 Yarn nodes, 4 tile specs. Discovered 0 new symbols.
- `[2026-09-21T11:52:08.622156]` Evolved knowledge base: 144 C# symbols, 4 Yarn nodes, 4 tile specs. Discovered 4 new symbols.
- `[2026-09-21T11:57:56.427805]` Evolved knowledge base: 145 C# symbols, 4 Yarn nodes, 4 tile specs. Discovered 1 new symbols.
