#!/usr/bin/env python3
"""
World Architect Node for the Alif Agent System.
Designs, validates, and optimizes 2D TileRPG world specifications
conforming to Tools/ai-tile-rpg/world.schema.json and WalkableArea invariants.
"""

import json
from pathlib import Path
from typing import Dict, Any, List, Tuple


class WorldArchitectNode:
    DEFAULT_PALETTE = [
        {"symbol": ".", "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile000.png", "layer": "ground", "solid": False},
        {"symbol": "#", "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile025.png", "layer": "collision", "solid": True},
        {"symbol": "S", "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile163.png", "layer": "props", "solid": True},
        {"symbol": "T", "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile184.png", "layer": "props", "solid": True}
    ]

    def __init__(self, memory_manager=None):
        self.memory = memory_manager

    def validate_spec(self, spec: Dict[str, Any]) -> Tuple[bool, List[str]]:
        errors = []
        for field in ["name", "width", "height", "palette", "rows", "spawn"]:
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
            spawn_char = rows[sy][sx]
            if spawn_char in solid_symbols:
                errors.append(f"Spawn point ({sx}, {sy}) is on a solid collision tile ('{spawn_char}')")

        return len(errors) == 0, errors

    def generate_level_spec(self, name: str, width: int = 12, height: int = 8, theme: str = "market") -> Dict[str, Any]:
        """Procedurally authors a compliant TileRPG world spec."""
        rows = []
        for y in range(height):
            if y == 0 or y == height - 1:
                rows.append("#" * width)
            else:
                row_chars = ["#"]
                for x in range(1, width - 1):
                    # Add thematic stalls or props
                    if theme == "market" and ((x == 3 and y == 3) or (x == 7 and y == 3)):
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
            "spawn": {"x": 1, "y": 1}
        }
        return spec

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
        task = state.get("task", "")
        # Determine theme & dimensions from task
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
