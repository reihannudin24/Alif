# Asset Modification Recipes & Color Palettes

Practical workflows for recoloring, scaling, and adapting existing assets in the Alif adventure game.

---

## 🎨 Alif Indonesian Adventure Palette

Standard warm earth/tropical color tokens for seamless harmonization:

| Token Name | Hex Code | Usage |
|---|---|---|
| `grass_dark` | `#2d641e` | Shaded ground, dense foliage |
| `grass_light` | `#468c2d` | Sunlit grass, bushes |
| `stone_dark` | `#464b55` | Dungeon walls, stone boundaries |
| `stone_light` | `#787d87` | Paved walkways, foundation |
| `wood_teak` | `#733c19` | Wooden stalls, crates, benches |
| `wood_light` | `#a05f2d` | Planks, fence posts |
| `brick_terracotta` | `#af3723` | Indonesian roof tiles (genteng) |
| `bazaar_orange` | `#e6781e` | Canopies, flags, market fabric |
| `water_deep` | `#235fa0` | River, deep ponds |
| `water_light` | `#3c91d7` | Flowing stream, fountain |
| `yellow_alert` | `#f0c828` | Exclamation / quest indicators |
| `red_heart` | `#dc2832` | Affection / health |
| `cyan_question` | `#28b4d2` | Mystery / dialogue hint |

---

## 🍳 Transformation Recipes

### Recipe 1: Night / Moonlight Variant
Convert daytime tiles into cool moonlit night tiles:
```bash
./Tools/alif find-assets modify Assets/Sprites/External/RPG_Pack/rpgTile000.png \
    -o Assets/Sprites/Modified/rpgTile000_night.png \
    --tint "#1a2550" --tint-strength 0.45
```

### Recipe 2: Autumn / Dry Season Vegetation
Turn lush green foliage into dry teak forest leaves:
```bash
./Tools/alif find-assets modify Assets/Sprites/Generated/Tiles/tile_tree.png \
    -o Assets/Sprites/Modified/tile_tree_autumn.png \
    --recolor "#2d641e:#a05f2d" \
    --recolor "#468c2d:#e6781e" \
    --tolerance 35
```

### Recipe 3: Golden / Emerald UI Button
Transform default blue UI buttons to match dialog actions:
```bash
# Emerald Confirmation Button
./Tools/alif find-assets modify Assets/Sprites/External/UI_Pack/blue_button00.png \
    -o Assets/Sprites/Modified/emerald_button.png \
    --tint "#2ecc71" --tint-strength 0.75

# Golden Quest Button
./Tools/alif find-assets modify Assets/Sprites/External/UI_Pack/blue_button00.png \
    -o Assets/Sprites/Modified/gold_button.png \
    --tint "#f1c40f" --tint-strength 0.8
```

### Recipe 4: Sprite Extraction & 2X Upscale
Extract a specific building prop from `RPGpack_sheet_2X.png` and scale it:
```bash
./Tools/alif find-assets modify Assets/Sprites/External/RPG_Pack/RPGpack_sheet_2X.png \
    -o Assets/Sprites/Modified/market_crate_large.png \
    --slice "0,0,64,64" \
    --scale 2x
```
