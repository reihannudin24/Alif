# Repository Guidelines

## Project Structure

Alif is a Unity 6000.6.0f1 2D educational adventure game using URP, the Input System, uGUI/TextMeshPro, and Unity Test Framework.

- `Assets/Scripts/` — runtime systems, adventure/campaign logic, battle models, UI, player, and world code.
- `Assets/Editor/` — scene/content builders, campaign validation, and platform build entry points.
- `Assets/Scenes/` — menus, cutscenes, chapter gameplay, and adventure scenes.
- `Assets/ScriptableObjects/`, `Assets/Sprites/`, `Assets/Audio/`, and `Assets/Animations/` — authored game data and media.
- `Assets/Tests/Adventure/` and `Assets/Tests/Editor/` — NUnit EditMode tests.
- `ProjectSettings/` and `Packages/` — Unity configuration and locked dependencies. Keep matching `.meta` files with assets.

## Build, Test, and Development

Use the official `unity` CLI with Unity 6000.6.0f1. Run `./Tools/alif` for the supported workflows:

- `./Tools/alif validate` validates content and updates Build Settings.
- `./Tools/alif build-web` writes `Build/WebGL`; `build-mac` writes `Build/macOS/Alif.app`.
- `./Tools/alif test-edit` and `./Tools/alif test-play` write reports under `Logs/TestResults/`.
- `./Tools/alif generate-tile-rpg [spec]` validates an AI-authored world spec and creates an isolated preview scene.
- Preview WebGL with the repository’s Brotli-aware server when available; plain `python3 -m http.server` does not provide the required encoding headers.
- Deploy existing builds with `./deploy_to_itch.sh <username>/<game-slug>` after authenticating Butler.

Prefix shell commands with `rtk` in this environment (for example, `rtk git status`).

## Coding Style

Use four-space indentation, braces on the same line, PascalCase for public types/methods, and camelCase or `_camelCase` for locals/private fields. Follow nearby Unity serialization patterns, avoid unnecessary allocations in per-frame code, and keep scene, asset, and script names descriptive.

## Testing Guidelines

Add focused NUnit tests beside the relevant suite. Name tests by behavior, such as `ThreeCorrectResponsesWinWithoutMutatingDefinition`. Cover invalid input, save/reload behavior, and progression boundaries when changing campaign logic.

## Commits and Pull Requests

Existing commits use short imperative descriptions such as `Update Chapter 2 story`. Keep commits similarly focused. Pull requests should explain the player-facing change, list validation performed, identify changed scenes/assets, and include screenshots or a short capture for visible changes. Mention known Unity Editor, build, or PlayMode limitations explicitly.

## Configuration and Safety

Do not commit credentials or generated caches/build intermediates. Preserve existing authored scenes and uncommitted work. Validate the campaign before building, and inspect the Unity Console after script or scene changes.

---

## AI Agent Workflows & Recursive Self-Improvement

Alif includes an embedded multi-agent graph system and semantic repository finder to accelerate development:

### 1. Embedded Tools & Skills
- **`find-repo`** ([`.agents/skills/find-repo/`](./.agents/skills/find-repo/)): Embedded semantic search engine for finding open-source Unity mechanics, shaders, dialogue examples, and peer repositories. Run via `./Tools/alif find-repo "<query>"`.
- **`alif-agent`** ([`.agents/skills/alif-agent/`](./.agents/skills/alif-agent/)): Specialized multi-agent state graph (`Tools/alif-agent/alif_graph.py`) with nodes for Orchestration, Repo Hunting, Level Architecture, Gameplay Dev, QA Verification, Self-Healing, and Evolution.

### 2. Supported Agent Commands
```bash
./Tools/alif agent run "<task>"      # Execute the specialized agentic graph
./Tools/alif agent run "<task>" --dry-run # Plan without mutating files
./Tools/alif agent status            # Inspect system health and learned symbols
./Tools/alif agent memory            # Display prompt-ready architecture rules
./Tools/alif agent evolve            # Run recursive self-improvement cycle
./Tools/alif find-repo "<query>"     # Discover GitHub references
```

### 3. Recursive Learning Protocol
Whenever you introduce new C# classes, assemblies, Yarn dialogue nodes, or TileRPG world specs, run:
```bash
./Tools/alif agent evolve
```
The evolution engine will scan project diffs, register newly created symbols in `Tools/alif-agent/memory/knowledge_base.json`, update `Docs/agent_memory/README.md`, and refine agent operational heuristics over time.
