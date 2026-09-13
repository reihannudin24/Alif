#!/usr/bin/env python3
"""
Alif Design Asset & TileRPG Validator.
Strictly validates TileRPG specifications against Tools/ai-tile-rpg/world.schema.json
and Unity's C# AiTileRpgBuilder.cs invariants (BFS reachability, layer enum, solid constraints).
Also verifies sprite existence and Unity .meta file pairing.
"""

import json
import re
from collections import deque
from pathlib import Path
from typing import Dict, Any, List, Tuple, Optional


class SpecValidator:
    NAME_PATTERN = re.compile(r"^[A-Za-z][A-Za-z0-9_-]{0,47}$")
    VALID_LAYERS = {"ground", "detail", "collision"}

    def __init__(self, alif_root: Optional[Path] = None):
        self.alif_root = alif_root or Path(__file__).resolve().parent.parent.parent

    def validate_spec_dict(
        self,
        spec: Dict[str, Any],
        require_assets: bool = True
    ) -> Tuple[bool, List[str]]:
        """Validates a world spec dictionary against all schema and runtime constraints."""
        errors: List[str] = []

        # 1. Required fields
        required_fields = ["name", "width", "height", "cellSize", "palette", "rows", "spawn", "exit"]
        for field in required_fields:
            if field not in spec:
                errors.append(f"Missing required field: '{field}'")

        if errors:
            return False, errors

        # 2. Name format
        name = spec.get("name", "")
        if not self.NAME_PATTERN.match(name):
            errors.append(f"Invalid name '{name}': must match ^[A-Za-z][A-Za-z0-9_-]{{0,47}}$")

        # 3. Dimensions
        width = spec.get("width", 0)
        height = spec.get("height", 0)
        if not (3 <= width <= 256):
            errors.append(f"Width {width} out of range [3..256]")
        if not (3 <= height <= 256):
            errors.append(f"Height {height} out of range [3..256]")

        # 4. Cell Size
        cell_size = spec.get("cellSize", 1.0)
        if not (0 < cell_size <= 16.0):
            errors.append(f"cellSize {cell_size} must be > 0 and <= 16.0")

        # 5. Palette validation
        palette_list = spec.get("palette", [])
        if not (1 <= len(palette_list) <= 64):
            errors.append(f"Palette count ({len(palette_list)}) must be in range [1..64]")

        palette: Dict[str, Dict[str, Any]] = {}
        for entry in palette_list:
            sym = entry.get("symbol")
            if not sym or len(sym) != 1:
                errors.append(f"Palette entry symbol must be exactly 1 character: '{sym}'")
                continue
            if sym in palette:
                errors.append(f"Duplicate palette symbol: '{sym}'")
            palette[sym] = entry

            layer = entry.get("layer")
            if layer not in self.VALID_LAYERS:
                errors.append(f"Symbol '{sym}' has invalid layer '{layer}'. Must be one of {sorted(list(self.VALID_LAYERS))}")

            solid = entry.get("solid", False)
            if solid and layer != "collision":
                errors.append(f"Solid symbol '{sym}' must use layer 'collision', but found '{layer}'")

            sprite = entry.get("sprite", "")
            if not sprite.startswith("Assets/"):
                errors.append(f"Sprite path for '{sym}' must start with 'Assets/': '{sprite}'")
            elif require_assets:
                sprite_path = self.alif_root / sprite
                if not sprite_path.exists():
                    errors.append(f"Sprite file not found on disk: '{sprite}'")
                else:
                    meta_path = sprite_path.with_suffix(sprite_path.suffix + ".meta")
                    if not meta_path.exists():
                        errors.append(f"Missing .meta file for sprite: '{sprite}'")

        # 6. Rows validation
        rows = spec.get("rows", [])
        if len(rows) != height:
            errors.append(f"Row count ({len(rows)}) does not match height ({height})")

        for y, row in enumerate(rows):
            if len(row) != width:
                errors.append(f"Row {y} length ({len(row)}) does not match width ({width})")
            for x, char in enumerate(row):
                if char not in palette:
                    errors.append(f"Unknown symbol '{char}' at ({x}, {y}) not in palette")

        # 7. Spawn & Exit validation
        spawn = spec.get("spawn", {})
        sx, sy = spawn.get("x", -1), spawn.get("y", -1)
        if not (0 <= sx < width and 0 <= sy < height):
            errors.append(f"Spawn point ({sx}, {sy}) is outside bounds ({width}x{height})")
        elif rows and len(rows) > sy and len(rows[sy]) > sx:
            spawn_sym = rows[sy][sx]
            if spawn_sym in palette and palette[spawn_sym].get("solid"):
                errors.append(f"Spawn point ({sx}, {sy}) is on solid tile '{spawn_sym}'")

        exit_pt = spec.get("exit", {})
        ex, ey = exit_pt.get("x", -1), exit_pt.get("y", -1)
        if not (0 <= ex < width and 0 <= ey < height):
            errors.append(f"Exit point ({ex}, {ey}) is outside bounds ({width}x{height})")
        elif rows and len(rows) > ey and len(rows[ey]) > ex:
            exit_sym = rows[ey][ex]
            if exit_sym in palette and palette[exit_sym].get("solid"):
                errors.append(f"Exit point ({ex}, {ey}) is on solid tile '{exit_sym}'")

        # 8. BFS 4-Directional Reachability from Spawn to Exit
        if not errors and 0 <= sx < width and 0 <= sy < height and 0 <= ex < width and 0 <= ey < height:
            reachable = self._check_reachability(rows, palette, width, height, sx, sy, ex, ey)
            if not reachable:
                errors.append(f"No walkable 4-direction path exists from spawn ({sx}, {sy}) to exit ({ex}, {ey})")

        return len(errors) == 0, errors

    def _check_reachability(
        self,
        rows: List[str],
        palette: Dict[str, Dict[str, Any]],
        width: int,
        height: int,
        start_x: int,
        start_y: int,
        exit_x: int,
        exit_y: int
    ) -> bool:
        """Exact 4-direction BFS path traversal matching AiTileRpgBuilder.cs Reachable()."""
        start = (start_x, start_y)
        goal = (exit_x, exit_y)
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
                    sym = rows[ny][nx]
                    is_solid = palette.get(sym, {}).get("solid", False)
                    if not is_solid:
                        visited.add((nx, ny))
                        queue.append((nx, ny))

        return False

    def validate_spec_file(self, file_path: Path, require_assets: bool = True) -> Tuple[bool, List[str]]:
        """Parses and validates a spec JSON file."""
        if not file_path.exists():
            return False, [f"File not found: {file_path}"]
        try:
            data = json.loads(file_path.read_text(encoding="utf-8"))
        except Exception as e:
            return False, [f"Invalid JSON in {file_path}: {e}"]
        return self.validate_spec_dict(data, require_assets=require_assets)

    def validate_all_specs(self, specs_dir: Optional[Path] = None) -> Dict[str, Any]:
        """Validates all specs in the given or default directory."""
        dir_to_check = specs_dir or (self.alif_root / "Assets/AI/TileRpg/Specs")
        results = {"total": 0, "passed": 0, "failed": 0, "details": {}}
        if not dir_to_check.exists():
            return results

        for f in dir_to_check.glob("*.json"):
            results["total"] += 1
            valid, errors = self.validate_spec_file(f, require_assets=True)
            if valid:
                results["passed"] += 1
                results["details"][f.name] = {"status": "PASS", "errors": []}
            else:
                results["failed"] += 1
                results["details"][f.name] = {"status": "FAIL", "errors": errors}

        return results

    def find_missing_meta_files(self, search_dir: Optional[Path] = None) -> List[str]:
        """Finds any asset files under Assets/ that are missing .meta files."""
        dir_to_scan = search_dir or (self.alif_root / "Assets")
        missing = []
        for p in dir_to_scan.rglob("*"):
            if p.is_file() and not p.name.endswith(".meta") and not p.name.startswith("."):
                meta = p.with_suffix(p.suffix + ".meta")
                if not meta.exists():
                    missing.append(str(p.relative_to(self.alif_root)))
        return missing
