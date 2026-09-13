# Curated Open CC0 & Public Domain Game Asset Sources

This guide catalogs high-quality, verified permissive (CC0 / Public Domain / MIT) game assets suitable for Alif and 2D adventure games.

---

## 1. Kenney.nl (Public Domain / CC0 1.0)

All Kenney assets are completely free for personal and commercial projects without attribution requirements.

| Pack | Focus | Content |
|---|---|---|
| **Kenney RPG Urban** | Towns, buildings, roads, roofs, doors | 230 tiles (16x16 / 32x32) |
| **Kenney UI Base** | Buttons, panels, frames, checkmarks, arrows | 220 UI elements & sheets |
| **Kenney Roguelike** | 1-bit / 16-color characters, weapons, items | 1,700 sprites |
| **Kenney Game Icons** | Potions, scrolls, keys, chests, books | 50 clean icons |
| **Kenney Digital Audio** | Clicks, drops, chimes, UI blips, interaction sounds | 60 sound effects |

**Direct Access via Alif Tool:**
```bash
./Tools/alif find-assets fetch kenney-rpg-urban
./Tools/alif find-assets fetch kenney-ui-base
./Tools/alif find-assets fetch kenney-roguelike-dungeon
```

---

## 2. OpenGameArt.org (CC0 Filtered)

Open community portal with extensive pixel art and sound effects:
- **LPC (Liberated Pixel Cup)**: 32x32 characters, clothes, and animations (GPL/CC-BY-SA, ensure license compatibility).
- **DawnBringer 16 / 32 Palettes**: Standard pixel art palettes ideal for harmonizing color schemes across diverse artists.
- **Freesound.org (CC0)**: Atmospheric sounds (market murmurs, river streams, footsteps on gravel).

---

## 3. GitHub Public Repositories (Discovered via `find-repo`)

Use `./Tools/alif find-repo` to discover open-source game repositories containing clean pixel art assets:
```bash
./Tools/alif find-repo "pixel art rpg 2d cc0 assets"
./Tools/alif find-repo "indonesian 2d game assets"
```

---

## 4. Local Project Asset Hierarchy

Before downloading external packages, check these local locations:
- `Assets/Sprites/External/RPG_Pack/`: Base RPG terrain and tiles.
- `Assets/Sprites/External/UI_Pack/`: Base UI panels and buttons.
- `Assets/Sprites/Generated/Tiles/`: Procedural market, garden, and station props.
- `Assets/Sprites/Modified/`: Adapted, recolored, and scaled assets.
- `Assets/Audio/`: Background music and sound effects.
