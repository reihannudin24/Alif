#!/usr/bin/env python3
"""
Unit tests for the Alif Agentic Graph, Design Asset Agent, and Evolutionary Subsystems.
"""

import unittest
import sys
import os
import json
from pathlib import Path

# Add Tools/alif-agent and Tools/alif-asset-gen to path
alif_root = Path(__file__).resolve().parent.parent.parent.parent
sys.path.insert(0, str(alif_root / "Tools/alif-agent"))
sys.path.insert(0, str(alif_root / "Tools/alif-asset-gen"))

from nodes.orchestrator import OrchestratorNode
from nodes.world_architect import WorldArchitectNode
from nodes.design_asset_agent import DesignAssetAgentNode
from nodes.gameplay_dev import GameplayDevNode
from nodes.self_healer import SelfHealerNode
from memory.memory_manager import MemoryManager
from alif_graph import AlifGraph
from validator import SpecValidator
from generator import AssetGenerator


class TestAlifOrchestrator(unittest.TestCase):
    def setUp(self):
        self.node = OrchestratorNode()

    def test_intent_classification(self):
        self.assertEqual(self.node.classify_intent("Find repo for 2D platformer physics"), "RESEARCH")
        self.assertEqual(self.node.classify_intent("Create market stall sprite and emote asset"), "DESIGN_ASSETS")
        self.assertEqual(self.node.classify_intent("Generate a tile map for desert level"), "WORLD_DESIGN")
        self.assertEqual(self.node.classify_intent("Validate and test level collisions"), "QA_VALIDATION")
        self.assertEqual(self.node.classify_intent("Write Yarn dialogue for merchant quest"), "GAMEPLAY_DEV")


class TestDesignAssetAgent(unittest.TestCase):
    def setUp(self):
        self.node = DesignAssetAgentNode(alif_root=alif_root)

    def test_generate_market_assets(self):
        state = {"task": "Create a market stall sprite, reaction bubble, and world level"}
        res = self.node.execute(state)
        self.assertIn("generated_assets", res)
        self.assertTrue(len(res["generated_assets"]) > 0)
        
        # Verify generated files have matching .meta files
        for a in res["generated_assets"]:
            p = alif_root / a["path"]
            self.assertTrue(p.exists(), f"Asset {p} was not created")
            meta = p.with_suffix(p.suffix + ".meta")
            self.assertTrue(meta.exists(), f"Missing meta file for {p}")


class TestWorldArchitect(unittest.TestCase):
    def setUp(self):
        self.node = WorldArchitectNode(alif_root=alif_root)

    def test_generate_and_validate_level(self):
        spec = self.node.generate_level_spec("TestLevel", width=10, height=6, theme="market")
        valid, errors = self.node.validate_spec(spec)
        self.assertTrue(valid, f"Validation errors: {errors}")
        self.assertIn("exit", spec)

    def test_invalid_spawn_on_solid(self):
        spec = self.node.generate_level_spec("SolidSpawn", width=6, height=6)
        # Force spawn onto collision wall (0, 0 is #)
        spec["spawn"] = {"x": 0, "y": 0}
        valid, errors = self.node.validate_spec(spec)
        self.assertFalse(valid)
        self.assertTrue(any("solid collision tile" in e for e in errors))

    def test_unreachable_level(self):
        spec = self.node.generate_level_spec("WallBlocked", width=6, height=6)
        # Block vertical corridor with solid wall column at x=2
        rows = list(spec["rows"])
        for y in range(len(rows)):
            row_list = list(rows[y])
            row_list[2] = "#"
            rows[y] = "".join(row_list)
        spec["rows"] = rows
        valid, errors = self.node.validate_spec(spec)
        self.assertFalse(valid)
        self.assertTrue(any("No walkable path" in e for e in errors))


class TestAssetGenValidator(unittest.TestCase):
    def setUp(self):
        self.validator = SpecValidator(alif_root)

    def test_schema_conformance(self):
        valid_spec = {
            "name": "CheckRoom",
            "width": 4,
            "height": 4,
            "cellSize": 1.0,
            "palette": [
                {"symbol": ".", "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile000.png", "layer": "ground", "solid": False},
                {"symbol": "#", "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile025.png", "layer": "collision", "solid": True}
            ],
            "rows": [
                "####",
                "#..#",
                "#..#",
                "####"
            ],
            "spawn": {"x": 1, "y": 1},
            "exit": {"x": 2, "y": 2}
        }
        valid, errors = self.validator.validate_spec_dict(valid_spec, require_assets=True)
        self.assertTrue(valid, f"Unexpected validation errors: {errors}")

    def test_solid_tile_invalid_layer_fails(self):
        bad_spec = {
            "name": "BadLayer",
            "width": 3,
            "height": 3,
            "cellSize": 1.0,
            "palette": [
                {"symbol": ".", "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile000.png", "layer": "ground", "solid": False},
                # Solid but layer is ground instead of collision
                {"symbol": "#", "sprite": "Assets/Sprites/External/RPG_Pack/rpgTile025.png", "layer": "ground", "solid": True}
            ],
            "rows": ["###", "#.#", "###"],
            "spawn": {"x": 1, "y": 1},
            "exit": {"x": 1, "y": 1}
        }
        valid, errors = self.validator.validate_spec_dict(bad_spec, require_assets=False)
        self.assertFalse(valid)
        self.assertTrue(any("must use layer 'collision'" in e for e in errors))


class TestSelfHealer(unittest.TestCase):
    def setUp(self):
        self.node = SelfHealerNode()
        self.architect = WorldArchitectNode(alif_root=alif_root)

    def test_heal_spawn_collision(self):
        spec = self.architect.generate_level_spec("BrokenLevel", width=6, height=6)
        spec["spawn"] = {"x": 0, "y": 0}
        errors = ["Spawn point (0, 0) is on a solid collision tile ('#')"]
        
        repaired = self.node.repair_level_spec(spec, errors)
        # Verify spawn was moved to a non-solid tile
        valid, _ = self.architect.validate_spec(repaired)
        self.assertTrue(valid)
        self.assertNotEqual(repaired["spawn"], {"x": 0, "y": 0})


class TestGameplayDevInvariants(unittest.TestCase):
    def setUp(self):
        self.node = GameplayDevNode()

    def test_domain_model_purity_violation(self):
        bad_code = "using UnityEngine;\npublic class AdventureState : MonoBehaviour { }"
        valid, errors = self.node.validate_csharp_script("Assets/Scripts/Model/Alif.Adventure.Model/State.cs", bad_code)
        self.assertFalse(valid)
        self.assertTrue(any("Must not import UnityEngine" in e for e in errors))

    def test_tab_indentation_rejection(self):
        tabbed_code = "public class Foo\n{\n\tint bar = 1;\n}"
        valid, errors = self.node.validate_csharp_script("Assets/Scripts/Foo.cs", tabbed_code)
        self.assertFalse(valid)
        self.assertTrue(any("Found tab indentation" in e for e in errors))

    def test_yarn_syntax_validation(self):
        valid_yarn = "title: Intro\n---\nBu Siti: Hello!\n-> Help?\n    <<jump Next>>\n===\ntitle: Next\n---\nBu Siti: Thanks!\n==="
        valid, errors = self.node.validate_yarn_script("Assets/Dialogue/test.yarn", valid_yarn)
        self.assertTrue(valid, f"Yarn errors: {errors}")


class TestMemoryManager(unittest.TestCase):
    def test_memory_crud(self):
        temp_kb = Path("/tmp/alif_test_kb.json")
        try:
            mm = MemoryManager(kb_path=temp_kb)
            mm.add_rule("TEST_RULE", "Test Title", "Test rule description")
            self.assertTrue(any(r["id"] == "TEST_RULE" for r in mm.get_rules()))

            mm.add_error_resolution("TEST_ERR", "symptom", "cause", "solution")
            self.assertTrue(any(e["error_type"] == "TEST_ERR" for e in mm.data.get("error_registry", [])))
            
            mm.add_tile_spec("TestTileSpec")
            self.assertIn("TestTileSpec", mm.data.get("discovered_symbols", {}).get("tile_specs", []))
        finally:
            if temp_kb.exists():
                temp_kb.unlink()


class TestGraphDryRun(unittest.TestCase):
    def test_dry_run_pipeline(self):
        graph = AlifGraph()
        state = graph.run("Design a new garden world map", dry_run=True)
        self.assertEqual(state["intent"], "WORLD_DESIGN")
        self.assertIn("design_asset_agent", state["plan"])
        self.assertIn("world_architect", state["plan"])
        self.assertTrue(any("[DRY RUN]" in h["action"] for h in state.get("history", [])))


if __name__ == "__main__":
    unittest.main()
