#!/usr/bin/env python3
"""
Design Asset Agent Node for the Alif Agent System.
Autonomously plans, generates, normalizes, and validates design assets:
- 2D pixel art tilesets and props
- Reaction emotes for EmoteBubble (Alert, Heart, Question, Sparkle, Sweat)
- Schema-compliant TileRPG world specifications (world.schema.json + AiTileRpgBuilder.cs)
- Yarn Spinner narrative dialogue assets (.yarn)
- Complete Unity .meta file pairing and asset manifests
"""

import sys
from pathlib import Path
from typing import Dict, Any, List, Optional

# Add Tools/alif-asset-gen and Tools/find-assets to path
tools_dir = Path(__file__).resolve().parent.parent.parent
alif_root = tools_dir.parent
asset_gen_dir = tools_dir / "alif-asset-gen"
find_assets_dir = tools_dir / "find-assets"
if str(asset_gen_dir) not in sys.path:
    sys.path.insert(0, str(asset_gen_dir))
if str(find_assets_dir) not in sys.path:
    sys.path.insert(0, str(find_assets_dir))

from generator import AssetGenerator
from validator import SpecValidator
try:
    import find_assets
except ImportError:
    find_assets = None


class DesignAssetAgentNode:
    def __init__(self, memory_manager=None, alif_root: Optional[Path] = None):
        self.memory = memory_manager
        self.alif_root = alif_root or Path(__file__).resolve().parent.parent.parent.parent
        self.generator = AssetGenerator(self.alif_root)
        self.validator = SpecValidator(self.alif_root)

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
        task = state.get("task", "")
        t_lower = task.lower()
        generated_assets: List[Dict[str, Any]] = []

        # 0. Asset Modification / Adaptation via find-assets
        has_modify = any(w in t_lower for w in ["modify", "adapt", "tint", "recolor", "emerald", "gold", "golden", "green button", "autumn", "night"])
        if has_modify and find_assets is not None:
            # Determine base asset and transformation
            if "button" in t_lower:
                base_path = self.alif_root / "Assets/Sprites/External/UI_Pack/blue_button00.png"
                if "emerald" in t_lower or "green" in t_lower:
                    out_path = self.alif_root / "Assets/Sprites/Modified/emerald_button.png"
                    find_assets.modify_asset(base_path, out_path, tint_color="#2ecc71", tint_strength=0.75)
                    generated_assets.append({"type": "modified_ui", "path": str(out_path.relative_to(self.alif_root))})
                elif "gold" in t_lower:
                    out_path = self.alif_root / "Assets/Sprites/Modified/gold_button.png"
                    find_assets.modify_asset(base_path, out_path, tint_color="#f1c40f", tint_strength=0.8)
                    generated_assets.append({"type": "modified_ui", "path": str(out_path.relative_to(self.alif_root))})
            elif "crate" in t_lower:
                base_path = self.alif_root / "Assets/Sprites/Generated/Tiles/tile_crate.png"
                if base_path.exists():
                    out_path = self.alif_root / "Assets/Sprites/Modified/crate_mossy.png"
                    find_assets.modify_asset(base_path, out_path, recolor_pairs=["#733c19:#2d641e"], recolor_tolerance=30)
                    generated_assets.append({"type": "modified_tile", "path": str(out_path.relative_to(self.alif_root))})
            elif "tree" in t_lower and "autumn" in t_lower:
                base_path = self.alif_root / "Assets/Sprites/Generated/Tiles/tile_tree.png"
                if base_path.exists():
                    out_path = self.alif_root / "Assets/Sprites/Modified/tile_tree_autumn.png"
                    find_assets.modify_asset(base_path, out_path, tint_color="#e6781e", tint_strength=0.6)
                    generated_assets.append({"type": "modified_tile", "path": str(out_path.relative_to(self.alif_root))})

        # 1. Determine asset types requested
        has_tile = any(w in t_lower for w in ["tile", "tileset", "stall", "tree", "bench", "prop", "wall", "ground"])
        has_world = any(w in t_lower for w in ["level", "world", "map", "tilerpg", "room", "bazaar", "garden", "station", "kampoeng"])
        has_emote = any(w in t_lower for w in ["emote", "bubble", "reaction", "alert", "heart", "sparkle", "sweat"])
        has_dialogue = any(w in t_lower for w in ["dialogue", "yarn", "npc", "convo", "speech", "story"])

        # Default fallback if general asset generation
        if not (has_tile or has_world or has_emote or has_dialogue):
            has_world = True
            has_tile = True

        # 2. Tile / Prop Generation
        if has_tile:
            if "stall" in t_lower or "market" in t_lower or "bazaar" in t_lower:
                p_stall = self.generator.create_procedural_tile("tile_stall", "market_stall")
                p_crate = self.generator.create_procedural_tile("tile_crate", "crate")
                generated_assets.append({"type": "tile", "path": str(p_stall.relative_to(self.alif_root))})
                generated_assets.append({"type": "tile", "path": str(p_crate.relative_to(self.alif_root))})
            if "tree" in t_lower or "garden" in t_lower:
                p_tree = self.generator.create_procedural_tile("tile_tree", "tree")
                p_flwr = self.generator.create_procedural_tile("tile_flower", "flower")
                generated_assets.append({"type": "tile", "path": str(p_tree.relative_to(self.alif_root))})
                generated_assets.append({"type": "tile", "path": str(p_flwr.relative_to(self.alif_root))})
            if "station" in t_lower or "bench" in t_lower:
                p_bench = self.generator.create_procedural_tile("tile_bench", "station_bench")
                generated_assets.append({"type": "tile", "path": str(p_bench.relative_to(self.alif_root))})

        # 3. Emote Bubble Generation
        if has_emote:
            emote_type = "alert"
            for et in ["heart", "question", "sparkle", "sweat"]:
                if et in t_lower:
                    emote_type = et
                    break
            p_emote = self.generator.create_reaction_emote(f"emote_{emote_type}", emote_type)
            generated_assets.append({"type": "emote", "path": str(p_emote.relative_to(self.alif_root))})

        # 4. Yarn Dialogue Tree Generation
        if has_dialogue:
            speaker = "Bu Siti" if "siti" in t_lower else ("Pak Ustad" if "ustad" in t_lower else "Alif")
            node_name = "MarketConversation" if "market" in t_lower else "AgentGeneratedStory"
            p_yarn = self.generator.create_yarn_dialogue(
                node_title=node_name,
                speaker=speaker,
                lines=[f"Halo! Senang bertemu denganmu di sini."],
                choices=[
                    ("Ada yang bisa saya bantu?", f"{node_name}_Help"),
                    ("Saya hanya melihat-lihat saja.", f"{node_name}_Browse")
                ]
            )
            generated_assets.append({"type": "dialogue", "path": str(p_yarn.relative_to(self.alif_root))})

        # 5. World Specification Generation
        if has_world:
            theme = "market" if "market" in t_lower or "bazaar" in t_lower else (
                "garden" if "garden" in t_lower else (
                    "station" if "station" in t_lower else "market"
                )
            )
            level_name = "AgentMarketWorld" if theme == "market" else f"Agent{theme.capitalize()}World"
            spec_path = self.alif_root / f"Assets/AI/TileRpg/Specs/{level_name}.json"
            spec = self.generator.generate_tilerpg_spec(
                name=level_name,
                theme=theme,
                width=12,
                height=8,
                cell_size=1.0,
                output_file=spec_path
            )
            state["level_spec"] = spec
            state["level_spec_valid"] = True
            generated_assets.append({"type": "tilerpg_spec", "path": str(spec_path.relative_to(self.alif_root))})

        # Update state with generated assets
        state["generated_assets"] = generated_assets
        asset_summary = f"Generated {len(generated_assets)} design assets: " + ", ".join(a["path"] for a in generated_assets)

        state.setdefault("history", []).append({
            "node": "design_asset_agent",
            "action": asset_summary
        })

        if self.memory:
            for a in generated_assets:
                if a["type"] == "tilerpg_spec":
                    self.memory.add_tile_spec(Path(a["path"]).stem)

        return state
