#!/usr/bin/env python3
"""
Unit tests for the Alif Agentic Graph and Evolutionary Subsystems.
"""

import unittest
import sys
import os
import json
from pathlib import Path

# Add Tools/alif-agent to path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from nodes.orchestrator import OrchestratorNode
from nodes.world_architect import WorldArchitectNode
from nodes.gameplay_dev import GameplayDevNode
from nodes.self_healer import SelfHealerNode
from memory.memory_manager import MemoryManager
from alif_graph import AlifGraph


class TestAlifOrchestrator(unittest.TestCase):
    def setUp(self):
        self.node = OrchestratorNode()

    def test_intent_classification(self):
        self.assertEqual(self.node.classify_intent("Find repo for 2D platformer physics"), "RESEARCH")
        self.assertEqual(self.node.classify_intent("Generate a tile map for desert level"), "WORLD_DESIGN")
        self.assertEqual(self.node.classify_intent("Validate and test level collisions"), "QA_VALIDATION")
        self.assertEqual(self.node.classify_intent("Write Yarn dialogue for merchant quest"), "GAMEPLAY_DEV")


class TestWorldArchitect(unittest.TestCase):
    def setUp(self):
        self.node = WorldArchitectNode()

    def test_generate_and_validate_level(self):
        spec = self.node.generate_level_spec("TestLevel", width=10, height=6, theme="market")
        valid, errors = self.node.validate_spec(spec)
        self.assertTrue(valid, f"Validation errors: {errors}")

    def test_invalid_spawn_on_solid(self):
        spec = self.node.generate_level_spec("SolidSpawn", width=6, height=6)
        # Force spawn onto collision wall (0, 0 is #)
        spec["spawn"] = {"x": 0, "y": 0}
        valid, errors = self.node.validate_spec(spec)
        self.assertFalse(valid)
        self.assertTrue(any("solid collision tile" in e for e in errors))


class TestSelfHealer(unittest.TestCase):
    def setUp(self):
        self.node = SelfHealerNode()
        self.architect = WorldArchitectNode()

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
        finally:
            if temp_kb.exists():
                temp_kb.unlink()


class TestGraphDryRun(unittest.TestCase):
    def test_dry_run_pipeline(self):
        graph = AlifGraph()
        state = graph.run("Design a new garden world map", dry_run=True)
        self.assertEqual(state["intent"], "WORLD_DESIGN")
        self.assertIn("world_architect", state["plan"])
        self.assertTrue(any("[DRY RUN]" in h["action"] for h in state.get("history", [])))


if __name__ == "__main__":
    unittest.main()
