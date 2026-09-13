#!/usr/bin/env python3
"""
Alif Asset Finder & Modification Engine (find-assets).
Enables searching, inspecting, fetching, and modifying existing game assets
(2D sprites, tilesets, UI icons, and audio) instead of creating from scratch.
"""

import argparse
import json
import os
import sys
import uuid
from pathlib import Path
from typing import Dict, Any, List, Optional, Tuple

# Optional Pillow import for image operations
try:
    from PIL import Image, ImageOps, ImageEnhance
except ImportError:
    Image = None

# Import meta_helper if available
REPO_ROOT = Path(__file__).resolve().parent.parent.parent
sys.path.insert(0, str(REPO_ROOT / "Tools" / "alif-asset-gen"))
try:
    from meta_helper import ensure_meta_for_file, create_texture_meta
except ImportError:
    ensure_meta_for_file = None
    create_texture_meta = None


# ==============================================================================
# 1. Curated Open Asset Catalog (CC0 / Public Domain)
# ==============================================================================

CC0_CATALOG = [
    {
        "id": "kenney-rpg-urban",
        "name": "Kenney RPG Urban & Town Pack",
        "category": "tileset",
        "theme": "town",
        "license": "CC0 1.0 Universal",
        "description": "2D RPG tiles for town, roads, buildings, houses, and market stalls (16x16 / 32x32).",
        "url": "https://raw.githubusercontent.com/iwenzhou/kenney/master/Art%20(5190%20files)/RPG%20pack%20(230%20assets)/Spritesheet/RPGpack_sheet_2X.png",
        "sample_elements": ["rpgTile000.png (dirt/grass)", "rpgTile010.png (signpost)", "rpgTile025.png (stone wall)"]
    },
    {
        "id": "kenney-ui-base",
        "name": "Kenney UI Base Pack",
        "category": "ui",
        "theme": "blue/gray",
        "license": "CC0 1.0 Universal",
        "description": "Buttons, panels, checkmarks, sliders, dialog frames, and navigation arrows.",
        "url": "https://raw.githubusercontent.com/iwenzhou/kenney/master/Art%20(5190%20files)/UI%20pack%20(220%20assets)/Base%20pack%20(140%20assets)/Spritesheet/blueSheet.png",
        "sample_elements": ["blue_button00.png", "blue_panel.png", "blue_boxCheckmark.png"]
    },
    {
        "id": "kenney-roguelike-dungeon",
        "name": "Kenney Roguelike / 1-Bit Pack",
        "category": "tileset",
        "theme": "dungeon/retro",
        "license": "CC0 1.0 Universal",
        "description": "Compact 16x16 dungeon, characters, items, weapons, doors, chests, and creatures.",
        "url": "https://raw.githubusercontent.com/iwenzhou/kenney/master/Art%20(5190%20files)/Roguelike%20pack%20(1700%20assets)/Spritesheet/roguelikeSheet_transparent.png",
        "sample_elements": ["character_warrior", "character_mage", "dungeon_wall", "chest_closed"]
    },
    {
        "id": "kenney-game-icons",
        "name": "Kenney Game Icons Pack",
        "category": "icons",
        "theme": "general",
        "license": "CC0 1.0 Universal",
        "description": "Clean vector / raster icons for inventory items, potions, quest markers, and spells.",
        "url": "https://raw.githubusercontent.com/iwenzhou/kenney/master/Art%20(5190%20files)/Game%20icons%20(50%20assets)/Spritesheet/sheet.png",
        "sample_elements": ["potion", "shield", "sword", "coin", "scroll"]
    },
    {
        "id": "kenney-impact-audio",
        "name": "Kenney Impact & Interaction SFX",
        "category": "audio",
        "theme": "sfx",
        "license": "CC0 1.0 Universal",
        "description": "Short UI clicks, button hits, pickup sounds, footstep thuds, and interaction chimes.",
        "url": "https://raw.githubusercontent.com/iwenzhou/kenney/master/Audio/Digital%20audio%20(60%20assets)/drop_001.ogg",
        "sample_elements": ["click.ogg", "pickup.ogg", "door_open.ogg", "dialog_blip.ogg"]
    }
]


# ==============================================================================
# 2. Local Asset Scanner & Indexer
# ==============================================================================

def scan_local_assets(root_dir: Path) -> List[Dict[str, Any]]:
    """Recursively scans repository Assets for sprites, textures, and audio files."""
    results = []
    search_dirs = [
        root_dir / "Assets" / "Sprites",
        root_dir / "Assets" / "Audio",
        root_dir / "Assets" / "AI"
    ]

    for base in search_dirs:
        if not base.exists():
            continue
        for p in base.rglob("*"):
            if p.is_file() and not p.name.endswith(".meta") and not p.name.startswith("."):
                ext = p.suffix.lower()
                if ext in [".png", ".jpg", ".jpeg", ".tga"]:
                    cat = "sprite"
                    if "tile" in p.name.lower() or "tileset" in p.name.lower():
                        cat = "tile"
                    elif "button" in p.name.lower() or "panel" in p.name.lower() or "ui" in str(p).lower():
                        cat = "ui"
                    elif "emote" in p.name.lower():
                        cat = "emote"
                    results.append({
                        "path": str(p.relative_to(root_dir)),
                        "absolute_path": str(p),
                        "name": p.name,
                        "stem": p.stem,
                        "type": cat,
                        "extension": ext,
                        "size_bytes": p.stat().st_size,
                        "has_meta": p.with_suffix(p.suffix + ".meta").exists()
                    })
                elif ext in [".wav", ".ogg", ".mp3"]:
                    results.append({
                        "path": str(p.relative_to(root_dir)),
                        "absolute_path": str(p),
                        "name": p.name,
                        "stem": p.stem,
                        "type": "audio",
                        "extension": ext,
                        "size_bytes": p.stat().st_size,
                        "has_meta": p.with_suffix(p.suffix + ".meta").exists()
                    })
    return results


def search_assets(query: str, root_dir: Path, category: Optional[str] = None, include_external: bool = True) -> Dict[str, Any]:
    """Search across local project assets and open CC0 catalog."""
    q_lower = query.lower().strip()
    query_tokens = [t for t in q_lower.replace("-", " ").replace("_", " ").split() if len(t) > 1]
    
    local_assets = scan_local_assets(root_dir)
    matched_local = []

    for item in local_assets:
        if category and item["type"] != category.lower():
            continue
        
        name_lower = item["name"].lower()
        path_lower = item["path"].lower()

        # Score relevance
        score = 0
        if q_lower and q_lower in name_lower:
            score += 50
        elif q_lower and q_lower in path_lower:
            score += 30

        for tok in query_tokens:
            if tok in name_lower:
                score += 20
            elif tok in path_lower:
                score += 10

        if not query_tokens: # Empty query lists all
            score = 10

        if score > 0:
            item_copy = dict(item)
            item_copy["relevance_score"] = score
            matched_local.append(item_copy)

    matched_local.sort(key=lambda x: x["relevance_score"], reverse=True)

    # Search CC0 Catalog
    matched_catalog = []
    if include_external:
        for cat_item in CC0_CATALOG:
            if category and cat_item["category"] != category.lower():
                continue
            cat_score = 0
            text_haystack = f"{cat_item['name']} {cat_item['theme']} {cat_item['description']} {' '.join(cat_item['sample_elements'])}".lower()
            if q_lower and q_lower in text_haystack:
                cat_score += 40
            for tok in query_tokens:
                if tok in text_haystack:
                    cat_score += 15
            if not query_tokens:
                cat_score = 10
            if cat_score > 0:
                cat_copy = dict(cat_item)
                cat_copy["relevance_score"] = cat_score
                matched_catalog.append(cat_copy)
        matched_catalog.sort(key=lambda x: x["relevance_score"], reverse=True)

    return {
        "query": query,
        "category_filter": category,
        "local_matches_count": len(matched_local),
        "catalog_matches_count": len(matched_catalog),
        "local_results": matched_local[:25],
        "catalog_results": matched_catalog[:10]
    }


# ==============================================================================
# 3. Asset Inspector
# ==============================================================================

def inspect_asset(asset_path: Path) -> Dict[str, Any]:
    """Inspects an image or audio asset: dimensions, color palette, alpha, and metadata."""
    if not asset_path.exists():
        raise FileNotFoundError(f"Asset not found: {asset_path}")

    ext = asset_path.suffix.lower()
    stat = asset_path.stat()
    meta_path = asset_path.with_suffix(asset_path.suffix + ".meta")

    info: Dict[str, Any] = {
        "file": asset_path.name,
        "path": str(asset_path),
        "size_bytes": stat.st_size,
        "extension": ext,
        "has_unity_meta": meta_path.exists()
    }

    if ext in [".png", ".jpg", ".jpeg"]:
        if Image is None:
            info["error"] = "PIL/Pillow is not installed"
            return info

        with Image.open(asset_path) as img:
            info["dimensions"] = {"width": img.width, "height": img.height}
            info["mode"] = img.mode
            info["has_alpha"] = "A" in img.mode or img.mode == "RGBA"

            # Compute dominant colors
            rgb_img = img.convert("RGBA")
            colors = rgb_img.getcolors(maxcolors=256 * 256)
            palette = []
            if colors:
                # Sort by pixel count descending, exclude transparent
                non_transparent = [(count, col) for count, col in colors if col[3] > 10]
                non_transparent.sort(key=lambda c: c[0], reverse=True)
                for count, (r, g, b, a) in non_transparent[:8]:
                    hex_code = f"#{r:02x}{g:02x}{b:02x}"
                    palette.append({
                        "hex": hex_code,
                        "rgba": (r, g, b, a),
                        "pixel_count": count
                    })
            info["dominant_palette"] = palette
            info["unique_colors_count"] = len(colors) if colors else "> 65536"
    elif ext in [".wav", ".ogg", ".mp3"]:
        info["type"] = "audio"

    return info


# ==============================================================================
# 4. Asset Modification Pipeline (modify)
# ==============================================================================

def parse_hex_color(hex_str: str) -> Tuple[int, int, int]:
    """Converts #RRGGBB or RRGGBB to (R, G, B) tuple."""
    h = hex_str.strip().lstrip("#")
    if len(h) == 3:
        h = "".join(c * 2 for c in h)
    if len(h) != 6:
        raise ValueError(f"Invalid hex color: {hex_str}")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16))


def recolor_image(img: Image.Image, color_map: Dict[str, str], tolerance: int = 25) -> Image.Image:
    """
    Replaces source colors with target colors based on RGB Euclidean distance.
    color_map: {from_hex: to_hex}
    """
    result = img.convert("RGBA")
    pixels = result.load()
    w, h = result.size

    parsed_map = []
    for src_hex, dst_hex in color_map.items():
        src_rgb = parse_hex_color(src_hex)
        dst_rgb = parse_hex_color(dst_hex)
        parsed_map.append((src_rgb, dst_rgb))

    tol_sq = tolerance * tolerance

    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a < 10:
                continue
            for (sr, sg, sb), (dr, dg, db) in parsed_map:
                dist_sq = (r - sr) ** 2 + (g - sg) ** 2 + (b - sb) ** 2
                if dist_sq <= tol_sq:
                    pixels[x, y] = (dr, dg, db, a)
                    break
    return result


def tint_image(img: Image.Image, tint_hex: str, strength: float = 0.5) -> Image.Image:
    """
    Applies a color tint to an image while preserving pixel luminance.
    strength: 0.0 (no effect) to 1.0 (full replacement)
    """
    result = img.convert("RGBA")
    pixels = result.load()
    w, h = result.size
    tr, tg, tb = parse_hex_color(tint_hex)
    strength = max(0.0, min(1.0, strength))

    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a < 10:
                continue
            nr = int(r * (1.0 - strength) + tr * strength)
            ng = int(g * (1.0 - strength) + tg * strength)
            nb = int(b * (1.0 - strength) + tb * strength)
            pixels[x, y] = (nr, ng, nb, a)
    return result


def scale_pixel_art(img: Image.Image, scale_spec: str) -> Image.Image:
    """
    Scales an image using nearest-neighbor interpolation to preserve crisp pixels.
    scale_spec: e.g. "2x", "4x", "0.5x" or "64x64", "32x48"
    """
    w, h = img.size
    spec = scale_spec.lower().strip()

    if spec.endswith("x"):
        factor = float(spec[:-1])
        new_w = max(1, int(w * factor))
        new_h = max(1, int(h * factor))
    elif "x" in spec:
        parts = spec.split("x")
        new_w = int(parts[0])
        new_h = int(parts[1])
    else:
        factor = float(spec)
        new_w = max(1, int(w * factor))
        new_h = max(1, int(h * factor))

    return img.resize((new_w, new_h), Image.Resampling.NEAREST)


def slice_image(img: Image.Image, x: int, y: int, w: int, h: int) -> Image.Image:
    """Crops a specific sub-rectangle from an image/spritesheet."""
    return img.crop((x, y, x + w, y + h))


def composite_images(base_img: Image.Image, overlay_img: Image.Image, position: str = "center") -> Image.Image:
    """Overlays one image onto another with alpha compositing."""
    base = base_img.convert("RGBA").copy()
    overlay = overlay_img.convert("RGBA")

    bw, bh = base.size
    ow, oh = overlay.size

    pos = position.lower().strip()
    if pos == "center":
        x = (bw - ow) // 2
        y = (bh - oh) // 2
    elif pos == "top-left":
        x, y = 0, 0
    elif pos == "top-right":
        x, y = bw - ow, 0
    elif pos == "bottom-left":
        x, y = 0, bh - oh
    elif pos == "bottom-right":
        x, y = bw - ow, bh - oh
    elif "," in pos:
        px, py = pos.split(",")
        x, y = int(px.strip()), int(py.strip())
    else:
        x, y = (bw - ow) // 2, (bh - oh) // 2

    base.alpha_composite(overlay, (x, y))
    return base


def ensure_unity_meta(target_file: Path, pixels_per_unit: int = 100):
    """Generates a valid Unity .meta file for the output asset and its parent directories."""
    # Ensure parent folders inside Assets have .meta
    parent = target_file.parent
    if "Assets" in parent.parts and ensure_meta_for_file is not None:
        curr = parent
        while curr and "Assets" in curr.parts and curr.name != "Assets":
            ensure_meta_for_file(curr)
            curr = curr.parent

    if ensure_meta_for_file is not None:
        ensure_meta_for_file(target_file, pixels_per_unit=pixels_per_unit)
        return

    # Fallback if meta_helper not importable
    meta_path = target_file.with_suffix(target_file.suffix + ".meta")
    if meta_path.exists():
        return

    guid = uuid.uuid4().hex
    content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
  textureSettings:
    filterMode: 0
  spriteMode: 1
  spritePixelsToUnits: {pixels_per_unit}
  alphaUsage: 1
  alphaIsTransparency: 1
"""
    meta_path.write_text(content, encoding="utf-8")



def modify_asset(
    input_path: Path,
    output_path: Path,
    recolor_pairs: Optional[List[str]] = None,
    recolor_tolerance: int = 25,
    tint_color: Optional[str] = None,
    tint_strength: float = 0.5,
    scale_spec: Optional[str] = None,
    slice_rect: Optional[Tuple[int, int, int, int]] = None,
    flip: Optional[str] = None,
    rotate_deg: Optional[int] = None,
    overlay_path: Optional[Path] = None,
    composite_pos: str = "center",
    pixels_per_unit: int = 100
) -> Dict[str, Any]:
    """Applies a pipeline of transformations to an asset and writes result with paired .meta."""
    if Image is None:
        raise RuntimeError("Pillow is required for image modification.")
    if not input_path.exists():
        raise FileNotFoundError(f"Input asset not found: {input_path}")

    img = Image.open(input_path).convert("RGBA")
    operations_applied = []

    # 1. Slice / Crop
    if slice_rect:
        x, y, w, h = slice_rect
        img = slice_image(img, x, y, w, h)
        operations_applied.append(f"slice({x},{y},{w}x{h})")

    # 2. Recolor (palette swap)
    if recolor_pairs:
        color_map = {}
        for pair in recolor_pairs:
            if ":" in pair:
                s, d = pair.split(":", 1)
                color_map[s.strip()] = d.strip()
        if color_map:
            img = recolor_image(img, color_map, tolerance=recolor_tolerance)
            operations_applied.append(f"recolor({len(color_map)} pairs, tol={recolor_tolerance})")

    # 3. Tint
    if tint_color:
        img = tint_image(img, tint_color, strength=tint_strength)
        operations_applied.append(f"tint({tint_color}, str={tint_strength})")

    # 4. Scale
    if scale_spec:
        img = scale_pixel_art(img, scale_spec)
        operations_applied.append(f"scale({scale_spec})")

    # 5. Flip & Rotate
    if flip:
        f = flip.lower().strip()
        if f in ["h", "horizontal"]:
            img = ImageOps.mirror(img)
            operations_applied.append("flip_horizontal")
        elif f in ["v", "vertical"]:
            img = ImageOps.flip(img)
            operations_applied.append("flip_vertical")
        elif f in ["both", "hv"]:
            img = ImageOps.mirror(ImageOps.flip(img))
            operations_applied.append("flip_both")

    if rotate_deg:
        if rotate_deg in [90, 180, 270]:
            img = img.rotate(-rotate_deg, expand=True) # Clockwise
            operations_applied.append(f"rotate({rotate_deg}deg)")

    # 6. Overlay / Composite
    if overlay_path and overlay_path.exists():
        overlay_img = Image.open(overlay_path)
        img = composite_images(img, overlay_img, position=composite_pos)
        operations_applied.append(f"composite({overlay_path.name}, pos={composite_pos})")

    # Ensure output directory exists
    output_path.parent.mkdir(parents=True, exist_ok=True)
    img.save(output_path, "PNG")
    ensure_unity_meta(output_path, pixels_per_unit=pixels_per_unit)

    return {
        "source": str(input_path),
        "output": str(output_path),
        "dimensions": {"width": img.width, "height": img.height},
        "operations": operations_applied,
        "meta_generated": output_path.with_suffix(output_path.suffix + ".meta").exists()
    }


# ==============================================================================
# 5. Non-interactive Fetch / Import
# ==============================================================================

def fetch_catalog_asset(catalog_id: str, dest_dir: Path, alif_root: Path) -> Dict[str, Any]:
    """Downloads or copies an asset pack or file from the CC0 catalog into the project."""
    import urllib.request

    item = next((c for c in CC0_CATALOG if c["id"] == catalog_id), None)
    if not item:
        raise ValueError(f"Unknown catalog asset ID: '{catalog_id}'. Use 'catalog' command to see options.")

    dest_dir.mkdir(parents=True, exist_ok=True)
    file_name = Path(item["url"]).name
    target_file = dest_dir / file_name

    print(f"⬇️  Fetching catalog item '{item['name']}' from {item['url']}...")
    urllib.request.urlretrieve(item["url"], target_file)

    if not target_file.exists() or target_file.stat().st_size == 0:
        raise RuntimeError(f"Download failed for {item['url']}")

    ensure_unity_meta(target_file)

    return {
        "status": "success",
        "catalog_id": catalog_id,
        "name": item["name"],
        "license": item["license"],
        "saved_path": str(target_file.relative_to(alif_root)),
        "size_bytes": target_file.stat().st_size,
        "meta_generated": target_file.with_suffix(target_file.suffix + ".meta").exists()
    }


# ==============================================================================
# 6. CLI Entrypoint & Formatters
# ==============================================================================

def format_search_markdown(data: Dict[str, Any]) -> str:
    lines = []
    lines.append(f"### 🔍 Asset Search Results for `{data['query']}`")
    if data["category_filter"]:
        lines.append(f"**Filter:** Category `{data['category_filter']}`")
    lines.append("")

    lines.append(f"#### 📁 Local Project Assets ({data['local_matches_count']} found)")
    if data["local_results"]:
        lines.append("| Name | Type | Path | Size | Meta |")
        lines.append("|---|---|---|---|---|")
        for r in data["local_results"]:
            meta_flag = "✅" if r["has_meta"] else "⚠️ Missing"
            lines.append(f"| **{r['name']}** | `{r['type']}` | `{r['path']}` | {r['size_bytes']} B | {meta_flag} |")
    else:
        lines.append("_No matching local assets found in Assets/Sprites or Assets/Audio._")

    lines.append("")
    lines.append(f"#### 🌐 Open CC0 Asset Packs ({data['catalog_matches_count']} found)")
    if data["catalog_results"]:
        lines.append("| ID | Name | Category | License | Description |")
        lines.append("|---|---|---|---|---|")
        for c in data["catalog_results"]:
            lines.append(f"| `{c['id']}` | **{c['name']}** | `{c['category']}` | {c['license']} | {c['description']} |")
    else:
        lines.append("_No matching external catalog packs found._")

    return "\n".join(lines)


def format_inspect_markdown(data: Dict[str, Any]) -> str:
    lines = []
    lines.append(f"### 🧐 Asset Inspection: `{data['file']}`")
    lines.append(f"- **Path:** `{data['path']}`")
    lines.append(f"- **Size:** {data['size_bytes']} bytes")
    lines.append(f"- **Unity .meta paired:** {'✅ Yes' if data.get('has_unity_meta') else '❌ No'}")

    if "dimensions" in data:
        dims = data["dimensions"]
        lines.append(f"- **Dimensions:** {dims['width']} x {dims['height']} px")
        lines.append(f"- **Color Mode:** `{data.get('mode')}` (Alpha: {data.get('has_alpha')})")

    if data.get("dominant_palette"):
        lines.append("\n**Dominant Color Palette:**")
        lines.append("| Hex Code | Color Preview | Pixel Count | RGBA |")
        lines.append("|---|---|---|---|")
        for p in data["dominant_palette"]:
            lines.append(f"| `{p['hex']}` | `{p['hex']}` | {p['pixel_count']} | `{p['rgba']}` |")

    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(
        description="Alif Asset Finder & Modification Engine - search, inspect, fetch, and modify 2D game assets."
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    # 1. Search
    p_search = subparsers.add_parser("search", help="Search local project assets and CC0 asset catalogs.")
    p_search.add_argument("query", nargs="?", default="", help="Keyword or semantic query (e.g. 'crate', 'button', 'market').")
    p_search.add_argument("-c", "--category", choices=["tile", "sprite", "ui", "emote", "audio", "icons"], help="Filter by asset category.")
    p_search.add_argument("--no-external", action="store_true", help="Exclude external CC0 catalog from search results.")
    p_search.add_argument("--json", action="store_true", help="Output raw JSON.")

    # 2. Inspect
    p_inspect = subparsers.add_parser("inspect", help="Inspect dimensions, palette, and metadata of an asset.")
    p_inspect.add_argument("path", help="Path to asset file (e.g. Assets/Sprites/External/UI_Pack/blue_button00.png).")
    p_inspect.add_argument("--json", action="store_true", help="Output raw JSON.")

    # 3. Modify
    p_modify = subparsers.add_parser("modify", help="Transform an existing asset (recolor, tint, scale, slice, composite).")
    p_modify.add_argument("input", help="Source asset path.")
    p_modify.add_argument("-o", "--output", required=True, help="Destination asset path.")
    p_modify.add_argument("--recolor", action="append", help="Color replacement pair 'FROM_HEX:TO_HEX' (can be specified multiple times).")
    p_modify.add_argument("--tolerance", type=int, default=25, help="Recolor color matching tolerance (default 25).")
    p_modify.add_argument("--tint", help="Hex color to tint with (e.g. '#2ecc71').")
    p_modify.add_argument("--tint-strength", type=float, default=0.5, help="Tint strength between 0.0 and 1.0 (default 0.5).")
    p_modify.add_argument("--scale", help="Nearest-neighbor scale factor (e.g. '2x', '0.5x') or target dimensions ('32x32').")
    p_modify.add_argument("--slice", help="Crop rectangle as 'X,Y,WIDTH,HEIGHT' (e.g. '0,0,32,32').")
    p_modify.add_argument("--flip", choices=["h", "v", "both"], help="Flip horizontally, vertically, or both.")
    p_modify.add_argument("--rotate", type=int, choices=[90, 180, 270], help="Rotate clockwise in degrees.")
    p_modify.add_argument("--overlay", help="Path to overlay sprite to composite onto base asset.")
    p_modify.add_argument("--position", default="center", help="Composite position: center, top-left, bottom-right, or 'X,Y'.")
    p_modify.add_argument("--ppu", type=int, default=100, help="Unity sprite pixels-per-unit for .meta (default 100).")
    p_modify.add_argument("--json", action="store_true", help="Output raw JSON.")

    # 4. Fetch
    p_fetch = subparsers.add_parser("fetch", help="Fetch a package or sprite from the CC0 catalog.")
    p_fetch.add_argument("catalog_id", help="Catalog ID (e.g. 'kenney-rpg-urban', 'kenney-ui-base').")
    p_fetch.add_argument("-d", "--dest", default="Assets/Sprites/External", help="Target directory (default Assets/Sprites/External).")
    p_fetch.add_argument("--json", action="store_true", help="Output raw JSON.")

    # 5. Catalog
    p_catalog = subparsers.add_parser("catalog", help="List bundled CC0 asset packs.")
    p_catalog.add_argument("--json", action="store_true", help="Output raw JSON.")

    args = parser.parse_args()
    root_dir = REPO_ROOT

    if args.command == "search":
        data = search_assets(args.query, root_dir, category=args.category, include_external=not args.no_external)
        if args.json:
            print(json.dumps(data, indent=2))
        else:
            print(format_search_markdown(data))

    elif args.command == "inspect":
        target = Path(args.path)
        if not target.is_absolute():
            target = root_dir / target
        data = inspect_asset(target)
        if args.json:
            print(json.dumps(data, indent=2))
        else:
            print(format_inspect_markdown(data))

    elif args.command == "modify":
        src = Path(args.input)
        if not src.is_absolute():
            src = root_dir / src
        dst = Path(args.output)
        if not dst.is_absolute():
            dst = root_dir / dst

        slice_rect = None
        if args.slice:
            parts = [int(p.strip()) for p in args.slice.split(",")]
            if len(parts) == 4:
                slice_rect = (parts[0], parts[1], parts[2], parts[3])

        overlay = None
        if args.overlay:
            overlay = Path(args.overlay)
            if not overlay.is_absolute():
                overlay = root_dir / overlay

        res = modify_asset(
            input_path=src,
            output_path=dst,
            recolor_pairs=args.recolor,
            recolor_tolerance=args.tolerance,
            tint_color=args.tint,
            tint_strength=args.tint_strength,
            scale_spec=args.scale,
            slice_rect=slice_rect,
            flip=args.flip,
            rotate_deg=args.rotate,
            overlay_path=overlay,
            composite_pos=args.position,
            pixels_per_unit=args.ppu
        )
        if args.json:
            print(json.dumps(res, indent=2))
        else:
            print(f"✅ Modified asset created: {dst.relative_to(root_dir) if dst.is_relative_to(root_dir) else dst}")
            print(f"   Operations: {', '.join(res['operations'])}")
            print(f"   Dimensions: {res['dimensions']['width']}x{res['dimensions']['height']} px")
            print(f"   Unity .meta: {'Created' if res['meta_generated'] else 'Failed'}")

    elif args.command == "fetch":
        dest_path = Path(args.dest)
        if not dest_path.is_absolute():
            dest_path = root_dir / dest_path
        res = fetch_catalog_asset(args.catalog_id, dest_path, root_dir)
        if args.json:
            print(json.dumps(res, indent=2))
        else:
            print(f"✅ Fetched asset pack: {res['name']} ({res['license']})")
            print(f"   Saved to: {res['saved_path']}")
            print(f"   Unity .meta: {'Created' if res['meta_generated'] else 'Failed'}")

    elif args.command == "catalog":
        if args.json:
            print(json.dumps(CC0_CATALOG, indent=2))
        else:
            print("### 📦 Bundled Open CC0 Asset Packs\n")
            print("| ID | Name | Category | License | Description |")
            print("|---|---|---|---|---|")
            for c in CC0_CATALOG:
                print(f"| `{c['id']}` | **{c['name']}** | `{c['category']}` | {c['license']} | {c['description']} |")


if __name__ == "__main__":
    main()
