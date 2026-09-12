# AI Tile RPG pipeline

AI authors a small, reviewable JSON world; Unity CLI validates connectivity and asset references, then generates a prefab and an isolated preview scene. It never edits authored scenes or hand-writes Unity YAML.

```bash
rtk ./Tools/alif generate-tile-rpg
rtk ./Tools/alif test-edit
rtk ./Tools/alif build-web
```

Copy `Assets/AI/TileRpg/Specs/sample-world.json`, keep it valid against `world.schema.json`, and reference sprites already under `Assets/`. Each palette symbol paints one of `ground`, `detail`, or `collision`; solid tiles must use `collision`. The generator rejects unknown symbols, duplicate symbols, missing sprites, blocked spawn/exit cells, and maps without a four-direction path from spawn to exit.

For cohesive pixel art, have the image model produce a single orthographic tileset at one tile size and restricted palette, with no antialiasing, text, perspective, or baked characters. Slice and inspect the sheet before referencing sprites. Reuse Alif’s original art where it already fits; generated previews stay under `Assets/Generated/AiTileRpg/`.

Useful upstream projects evaluated:

- [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills) — official Unity CLI workflows.
- [gamedev-skills/awesome-gamedev-agent-skills](https://github.com/gamedev-skills/awesome-gamedev-agent-skills) — Tilemap, RPG, procedural-generation, and build guidance.
- [Cammin/LDtkToUnity](https://github.com/Cammin/LDtkToUnity) — mature LDtk importer if visual level editing becomes necessary.
- [deepnight/ldtk](https://github.com/deepnight/ldtk) — optional external level editor; deliberately not added because the current JSON-to-Tilemap path already covers AI generation.
