#!/usr/bin/env python3
"""
Alif Design Asset & World Spec Generator.
Produces schema-compliant TileRPG worlds (world.schema.json + AiTileRpgBuilder.cs),
pixel art sprites, reaction emotes, tileset slicing, and Yarn Spinner dialogue trees
complete with paired Unity .meta files.
"""

import json
from pathlib import Path
from typing import Dict, Any, List, Optional, Tuple
from PIL import Image, ImageDraw

try:
    from .meta_helper import create_texture_meta, create_text_script_meta, ensure_meta_for_file
    from .validator import SpecValidator
except (ImportError, ValueError):
    from meta_helper import create_texture_meta, create_text_script_meta, ensure_meta_for_file
    from validator import SpecValidator


class AssetGenerator:
    # Standard 16-color educational adventure palette (Indonesian warm/earth tones)
    PALETTE_COLORS = {
        "transparent": (0, 0, 0, 0),
        "grass_dark": (45, 100, 30, 255),
        "grass_light": (70, 140, 45, 255),
        "stone_dark": (70, 75, 85, 255),
        "stone_light": (120, 125, 135, 255),
        "earth_warm": (165, 125, 80, 255),
        "earth_light": (205, 170, 125, 255),
        "wood_teak": (115, 60, 25, 255),
        "wood_light": (160, 95, 45, 255),
        "brick_terracotta": (175, 55, 35, 255),
        "brick_dark": (125, 35, 20, 255),
        "bazaar_orange": (230, 120, 30, 255),
        "water_deep": (35, 95, 160, 255),
        "water_light": (60, 145, 215, 255),
        "white": (245, 245, 245, 255),
        "yellow_alert": (240, 200, 40, 255),
        "red_heart": (220, 40, 50, 255),
        "cyan_question": (40, 180, 210, 255),
    }

    def __init__(self, alif_root: Optional[Path] = None):
        self.alif_root = alif_root or Path(__file__).resolve().parent.parent.parent
        self.validator = SpecValidator(self.alif_root)

    # --------------------------------------------------------------------------
    # 1. TileRPG World Specification Generation
    # --------------------------------------------------------------------------
    def generate_tilerpg_spec(
        self,
        name: str,
        theme: str = "market",
        width: int = 12,
        height: int = 8,
        cell_size: float = 1.0,
        output_file: Optional[Path] = None
    ) -> Dict[str, Any]:
        """Generates a guaranteed valid TileRPG spec meeting world.schema.json & reachability."""
        # Ensure base sprites exist
        self._ensure_default_sprites()

        # Build palette
        palette = [
            {
                "symbol": ".",
                "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile000.png",
                "layer": "ground",
                "solid": False
            },
            {
                "symbol": "#",
                "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile025.png",
                "layer": "collision",
                "solid": True
            },
            {
                "symbol": ",",
                "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile001.png",
                "layer": "ground",
                "solid": False
            },
            {
                "symbol": "D",
                "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile010.png",
                "layer": "detail",
                "solid": False
            }
        ]

        # Add theme-specific detail / collision props
        if theme == "market":
            # Add stall & crate
            self.create_procedural_tile("tile_stall", "market_stall")
            self.create_procedural_tile("tile_crate", "crate")
            palette.append({
                "symbol": "S",
                "sprite": "Assets/Sprites/Generated/Tiles/tile_stall.png",
                "layer": "collision",
                "solid": True
            })
            palette.append({
                "symbol": "C",
                "sprite": "Assets/Sprites/Generated/Tiles/tile_crate.png",
                "layer": "detail",
                "solid": False
            })
        elif theme == "garden":
            self.create_procedural_tile("tile_tree", "tree")
            self.create_procedural_tile("tile_flower", "flower")
            palette.append({
                "symbol": "T",
                "sprite": "Assets/Sprites/Generated/Tiles/tile_tree.png",
                "layer": "collision",
                "solid": True
            })
            palette.append({
                "symbol": "F",
                "sprite": "Assets/Sprites/Generated/Tiles/tile_flower.png",
                "layer": "detail",
                "solid": False
            })
        elif theme == "station":
            self.create_procedural_tile("tile_bench", "station_bench")
            palette.append({
                "symbol": "B",
                "sprite": "Assets/Sprites/Generated/Tiles/tile_bench.png",
                "layer": "collision",
                "solid": True
            })

        # Generate map rows
        spawn = {"x": 1, "y": 1}
        exit_pt = {"x": width - 2, "y": height - 2}

        # Clear grid surrounded by collision border
        grid = [["." for _ in range(width)] for _ in range(height)]
        for x in range(width):
            grid[0][x] = "#"
            grid[height - 1][x] = "#"
        for y in range(height):
            grid[y][0] = "#"
            grid[y][width - 1] = "#"

        # Safe walkway along boundary from spawn to exit:
        # spawn is (1,1), walkway along y=1 to x=width-2, then along x=width-2 to y=height-2
        guaranteed_walkable = set()
        for x in range(1, width - 1):
            guaranteed_walkable.add((x, 1))
        for y in range(1, height - 1):
            guaranteed_walkable.add((width - 2, y))

        # Place thematic props in non-guaranteed areas
        if theme == "market":
            # Stalls in middle
            if height > 5 and width > 6:
                for mx in range(3, width - 3, 3):
                    if (mx, 3) not in guaranteed_walkable:
                        grid[3][mx] = "S"
                        if (mx + 1, 3) not in guaranteed_walkable and mx + 1 < width - 1:
                            grid[3][mx + 1] = "C"
        elif theme == "garden":
            if height > 4 and width > 5:
                for gy in range(2, height - 2):
                    for gx in range(2, width - 3, 2):
                        if (gx, gy) not in guaranteed_walkable:
                            grid[gy][gx] = "T" if (gx + gy) % 3 == 0 else "F"
        elif theme == "station":
            if height > 4 and width > 5:
                for bx in range(3, width - 3, 2):
                    if (bx, 3) not in guaranteed_walkable:
                        grid[3][bx] = "B"

        rows = ["".join(row) for row in grid]

        spec = {
            "name": name,
            "width": width,
            "height": height,
            "cellSize": cell_size,
            "palette": palette,
            "rows": rows,
            "spawn": spawn,
            "exit": exit_pt
        }

        # Validate with SpecValidator
        valid, errors = self.validator.validate_spec_dict(spec, require_assets=True)
        if not valid:
            raise ValueError(f"Generated spec failed internal validation: {errors}")

        if output_file:
            output_file.parent.mkdir(parents=True, exist_ok=True)
            output_file.write_text(json.dumps(spec, indent=2), encoding="utf-8")
            ensure_meta_for_file(output_file)

        return spec

    # --------------------------------------------------------------------------
    # 2. Procedural Pixel Art Tile Generation
    # --------------------------------------------------------------------------
    def create_procedural_tile(self, name: str, tile_type: str, output_path: Optional[Path] = None) -> Path:
        """Generates a 64x64 pixel art tile adhering to Alif's art direction."""
        target_path = output_path or (self.alif_root / f"Assets/Sprites/Generated/Tiles/{name}.png")
        if target_path.exists() and target_path.with_suffix(".png.meta").exists():
            return target_path

        target_path.parent.mkdir(parents=True, exist_ok=True)
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)

        c = self.PALETTE_COLORS
        if tile_type == "market_stall":
            # Wooden counter with striped cloth awning
            draw.rectangle([4, 28, 59, 63], fill=c["wood_teak"])
            draw.rectangle([8, 32, 55, 59], fill=c["wood_light"])
            # Awning (red/white stripes)
            for i in range(4, 60, 8):
                draw.rectangle([i, 6, min(i + 4, 59), 24], fill=c["brick_terracotta"])
                draw.rectangle([i + 4, 6, min(i + 8, 59), 24], fill=c["white"])
            # Stall poles
            draw.rectangle([6, 20, 10, 32], fill=c["wood_teak"])
            draw.rectangle([53, 20, 57, 32], fill=c["wood_teak"])
        elif tile_type == "crate":
            # Wooden fruit crate
            draw.rectangle([10, 18, 53, 61], fill=c["wood_light"])
            draw.rectangle([14, 22, 49, 57], fill=c["wood_teak"])
            # Fruit inside
            draw.ellipse([20, 26, 32, 38], fill=c["bazaar_orange"])
            draw.ellipse([32, 28, 44, 40], fill=c["yellow_alert"])
            draw.ellipse([26, 36, 38, 48], fill=c["red_heart"])
        elif tile_type == "tree":
            # Trunk
            draw.rectangle([26, 38, 37, 63], fill=c["wood_teak"])
            # Foliage layers
            draw.ellipse([8, 6, 55, 48], fill=c["grass_dark"])
            draw.ellipse([14, 10, 49, 42], fill=c["grass_light"])
        elif tile_type == "flower":
            # Ground patch
            draw.ellipse([16, 20, 47, 52], fill=c["grass_dark"])
            # Blossoms
            draw.ellipse([20, 24, 28, 32], fill=c["red_heart"])
            draw.ellipse([36, 26, 44, 34], fill=c["yellow_alert"])
            draw.ellipse([28, 38, 36, 46], fill=c["white"])
        elif tile_type == "station_bench":
            # Wood slat bench
            draw.rectangle([6, 28, 57, 38], fill=c["wood_light"])
            draw.rectangle([6, 42, 57, 52], fill=c["wood_teak"])
            # Iron legs
            draw.rectangle([10, 52, 16, 63], fill=c["stone_dark"])
            draw.rectangle([47, 52, 53, 63], fill=c["stone_dark"])
        else: # Generic cobblestone floor
            draw.rectangle([0, 0, 63, 63], fill=c["stone_light"])
            for y in range(0, 64, 16):
                draw.line([(0, y), (63, y)], fill=c["stone_dark"], width=2)
            for y in range(0, 64, 16):
                offset = 16 if (y // 16) % 2 == 1 else 0
                for x in range(offset, 64, 32):
                    draw.line([(x, y), (x, y + 16)], fill=c["stone_dark"], width=2)

        img.save(target_path, "PNG")
        create_texture_meta(target_path.with_suffix(".png.meta"), pixels_per_unit=64, filter_mode=0)
        return target_path

    # --------------------------------------------------------------------------
    # 3. Emote Bubble Asset Generation (Variant C UI)
    # --------------------------------------------------------------------------
    def create_reaction_emote(self, name: str, emote_type: str, output_path: Optional[Path] = None) -> Path:
        """Generates a 64x64 reaction emote bubble for Alif's EmoteBubble component."""
        target_path = output_path or (self.alif_root / f"Assets/Sprites/Generated/Emotes/{name}.png")
        target_path.parent.mkdir(parents=True, exist_ok=True)

        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        c = self.PALETTE_COLORS

        # White bubble backdrop with dark shadow outline
        draw.ellipse([6, 6, 57, 53], fill=(20, 20, 30, 180))
        draw.ellipse([8, 8, 55, 51], fill=c["white"])
        # Pointer tail
        draw.polygon([(26, 48), (38, 48), (32, 60)], fill=c["white"])

        if emote_type == "alert":
            # Yellow/Orange exclamation mark
            draw.rectangle([28, 16, 35, 34], fill=c["bazaar_orange"])
            draw.rectangle([28, 38, 35, 44], fill=c["bazaar_orange"])
        elif emote_type == "heart":
            # Red heart
            draw.polygon([(32, 44), (16, 28), (22, 18), (32, 24), (42, 18), (48, 28)], fill=c["red_heart"])
        elif emote_type == "question":
            # Cyan question mark
            draw.arc([24, 16, 40, 30], 180, 360, fill=c["cyan_question"], width=4)
            draw.line([(40, 23), (32, 28), (32, 34)], fill=c["cyan_question"], width=4)
            draw.rectangle([30, 38, 34, 42], fill=c["cyan_question"])
        elif emote_type == "sparkle":
            # Golden 4-point star
            draw.polygon([(32, 12), (36, 26), (50, 30), (36, 34), (32, 48), (28, 34), (14, 30), (28, 26)], fill=c["yellow_alert"])
        elif emote_type == "sweat":
            # Teardrop sweat
            draw.polygon([(32, 14), (20, 36), (26, 44), (38, 44), (44, 36)], fill=c["water_light"])

        img.save(target_path, "PNG")
        create_texture_meta(target_path.with_suffix(".png.meta"), pixels_per_unit=64, filter_mode=0)
        return target_path

    # --------------------------------------------------------------------------
    # 4. Yarn Spinner Dialogue Tree Generation
    # --------------------------------------------------------------------------
    def create_yarn_dialogue(
        self,
        node_title: str,
        speaker: str,
        lines: List[str],
        choices: List[Tuple[str, str]],
        output_path: Optional[Path] = None
    ) -> Path:
        """Generates a Yarn Spinner narrative node with branching choices."""
        target_path = output_path or (self.alif_root / f"Assets/Dialogue/{node_title}.yarn")
        target_path.parent.mkdir(parents=True, exist_ok=True)

        content_lines = [
            f"title: {node_title}",
            f"tags: character:{speaker.lower()} portrait:{speaker.lower()}",
            "---"
        ]
        for line in lines:
            content_lines.append(f"{speaker}: {line}")

        if choices:
            for text, target_node in choices:
                content_lines.append(f"-> {text}")
                content_lines.append(f"    <<jump {target_node}>>")

        content_lines.append("===")
        content_lines.append("")

        target_path.write_text("\n".join(content_lines), encoding="utf-8")
        create_text_script_meta(target_path.with_suffix(".yarn.meta"))
        return target_path

    # --------------------------------------------------------------------------
    # 5. Helper to guarantee base sprites exist with valid .meta
    # --------------------------------------------------------------------------
    def _ensure_default_sprites(self):
        """Ensures RPG_Pack individual tiles have paired .meta files."""
        rpg_pack = self.alif_root / "Assets/Sprites/External/RPG_Pack"
        for p in rpg_pack.glob("*.png"):
            ensure_meta_for_file(p)
