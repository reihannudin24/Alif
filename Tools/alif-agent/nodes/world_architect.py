#!/usr/bin/env python3
"""
World Architect Node for the Alif Agent System.
Designs, validates, and optimizes 2D TileRPG world specifications
conforming to Tools/ai-tile-rpg/world.schema.json and WalkableArea invariants.
"""

import json
from collections import deque
from pathlib import Path
from typing import Dict, Any, List, Tuple, Optional


class WorldArchitectNode:
    DEFAULT_PALETTE = [
        {"symbol": ".", "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile000.png", "layer": "ground", "solid": False},
        {"symbol": "#", "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile025.png", "layer": "collision", "solid": True},
        {"symbol": "S", "sprite": "Assets/Sprites/Generated/Tiles/tile_stall.png", "layer": "collision", "solid": True},
        {"symbol": "T", "sprite": "Assets/Sprites/Generated/Tiles/tile_tree.png", "layer": "collision", "solid": True}
    ]

    def __init__(self, memory_manager=None, alif_root: Optional[Path] = None):
        self.memory = memory_manager
        self.alif_root = alif_root or Path(__file__).resolve().parent.parent.parent.parent

    def validate_spec(self, spec: Dict[str, Any]) -> Tuple[bool, List[str]]:
        errors = []
        for field in ["name", "width", "height", "palette", "rows", "spawn", "exit"]:
            if field not in spec:
                errors.append(f"Missing required field: '{field}'")

        if errors:
            return False, errors

        width = spec.get("width", 0)
        height = spec.get("height", 0)
        rows = spec.get("rows", [])

        if len(rows) != height:
            errors.append(f"Row count ({len(rows)}) does not match specified height ({height})")

        palette_symbols = {p.get("symbol"): p for p in spec.get("palette", [])}
        solid_symbols = {p.get("symbol") for p in spec.get("palette", []) if p.get("solid")}

        for p in spec.get("palette", []):
            layer = p.get("layer")
            if layer not in ["ground", "detail", "collision"]:
                errors.append(f"Invalid layer '{layer}' for symbol '{p.get('symbol')}': must be ground, detail, or collision")
            if p.get("solid") and layer != "collision":
                errors.append(f"Solid symbol '{p.get('symbol')}' must use layer 'collision'")

        for y, r in enumerate(rows):
            if len(r) != width:
                errors.append(f"Row {y} length ({len(r)}) does not match width ({width})")
            for x, char in enumerate(r):
                if char not in palette_symbols:
                    errors.append(f"Undefined palette symbol '{char}' at ({x}, {y})")

        spawn = spec.get("spawn", {})
        sx, sy = spawn.get("x", -1), spawn.get("y", -1)
        if not (0 <= sx < width and 0 <= sy < height):
            errors.append(f"Spawn point ({sx}, {sy}) out of bounds for {width}x{height} map")
        else:
            spawn_char = rows[sy][sx] if sy < len(rows) and sx < len(rows[sy]) else "#"
            if spawn_char in solid_symbols:
                errors.append(f"Spawn point ({sx}, {sy}) is on a solid collision tile ('{spawn_char}')")

        exit_pt = spec.get("exit", {})
        ex, ey = exit_pt.get("x", -1), exit_pt.get("y", -1)
        if not (0 <= ex < width and 0 <= ey < height):
            errors.append(f"Exit point ({ex}, {ey}) out of bounds for {width}x{height} map")
        else:
            exit_char = rows[ey][ex] if ey < len(rows) and ex < len(rows[ey]) else "#"
            if exit_char in solid_symbols:
                errors.append(f"Exit point ({ex}, {ey}) is on a solid collision tile ('{exit_char}')")

        # 4-Direction BFS Reachability check matching AiTileRpgBuilder.cs
        if not errors:
            if not self._check_reachability(rows, solid_symbols, width, height, sx, sy, ex, ey):
                errors.append(f"No walkable path from spawn ({sx}, {sy}) to exit ({ex}, {ey})")

        return len(errors) == 0, errors

    def _check_reachability(self, rows: List[str], solid_symbols: set, width: int, height: int, sx: int, sy: int, ex: int, ey: int) -> bool:
        start = (sx, sy)
        goal = (ex, ey)
        if start == goal:
            return True
        queue = deque([start])
        visited = {start}
        directions = [(-1, 0), (1, 0), (0, -1), (0, 1)]

        while queue:
            cx, cy = queue.popleft()
            if (cx, cy) == goal:
                return True
            for dx, dy in directions:
                nx, ny = cx + dx, cy + dy
                if 0 <= nx < width and 0 <= ny < height and (nx, ny) not in visited:
                    if rows[ny][nx] not in solid_symbols:
                        visited.add((nx, ny))
                        queue.append((nx, ny))
        return False

    def generate_level_spec(self, name: str, width: int = 12, height: int = 8, theme: str = "market") -> Dict[str, Any]:
        """Procedurally authors a compliant TileRPG world spec."""
        rows = []
        # Keep clear pathway from (1,1) to (width-2, height-2)
        guaranteed_walkable = set()
        for x in range(1, width - 1):
            guaranteed_walkable.add((x, 1))
        for y in range(1, height - 1):
            guaranteed_walkable.add((width - 2, y))

        for y in range(height):
            if y == 0 or y == height - 1:
                rows.append("#" * width)
            else:
                row_chars = ["#"]
                for x in range(1, width - 1):
                    if (x, y) in guaranteed_walkable:
                        row_chars.append(".")
                    elif theme == "market" and ((x == 3 and y == 3) or (x == 7 and y == 3)):
                        row_chars.append("S") # Stall
                    elif theme == "garden" and ((x == 4 and y == 2) or (x == 6 and y == 5)):
                        row_chars.append("T") # Tree
                    else:
                        row_chars.append(".")
                row_chars.append("#")
                rows.append("".join(row_chars))

        spec = {
            "name": name,
            "width": width,
            "height": height,
            "cellSize": 1.0,
            "palette": self.DEFAULT_PALETTE,
            "rows": rows,
            "spawn": {"x": 1, "y": 1},
            "exit": {"x": width - 2, "y": height - 2}
        }
        return spec

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
        task = state.get("task", "")
        theme = "market" if "market" in task.lower() else ("garden" if "garden" in task.lower() else "courtyard")
        level_name = "AgentGeneratedWorld"
        spec = self.generate_level_spec(level_name, width=12, height=8, theme=theme)
        valid, errors = self.validate_spec(spec)

        state["level_spec"] = spec
        state["level_spec_valid"] = valid
        if not valid:
            state.setdefault("errors", []).extend(errors)

        state.setdefault("history", []).append({
            "node": "world_architect",
            "action": f"Generated {level_name} ({theme}, 12x8). Validation status: {'PASS' if valid else 'FAIL'}"
        })
        return state
