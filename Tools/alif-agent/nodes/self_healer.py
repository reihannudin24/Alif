#!/usr/bin/env python3
"""
Self-Healing Node for the Alif Agent System.
Analyzes error diagnostics, applies automated repairs to level specs or code,
and records the diagnosis in memory for recursive learning.
"""

from typing import Dict, Any, List


class SelfHealerNode:
    MAX_ATTEMPTS = 3

    def __init__(self, memory_manager=None):
        self.memory = memory_manager

    def repair_level_spec(self, spec: Dict[str, Any], errors: List[str]) -> Dict[str, Any]:
        """Automatically repairs common level spec issues (e.g. spawn on collision)."""
        repaired = dict(spec)
        rows = repaired.get("rows", [])
        solid_symbols = {p.get("symbol") for p in repaired.get("palette", []) if p.get("solid")}

        for err in errors:
            if "Spawn point" in err and "is on a solid collision tile" in err:
                # Find first non-solid tile
                for y, row in enumerate(rows):
                    for x, char in enumerate(row):
                        if char not in solid_symbols:
                            repaired["spawn"] = {"x": x, "y": y}
                            if self.memory:
                                self.memory.add_error_resolution(
                                    "SPAWN_ON_COLLISION",
                                    err,
                                    "Spawn placed on solid tile",
                                    f"Relocated spawn point to walkable tile ({x}, {y})"
                                )
                            return repaired
        return repaired

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
        errors = state.get("errors", [])
        attempts = state.get("healing_attempts", 0)

        if not errors or attempts >= self.MAX_ATTEMPTS:
            state["can_heal"] = False
            return state

        state["healing_attempts"] = attempts + 1
        healed_items = []

        if "level_spec" in state and not state.get("level_spec_valid", True):
            repaired_spec = self.repair_level_spec(state["level_spec"], errors)
            state["level_spec"] = repaired_spec
            state["errors"] = [] # Cleared for re-verification
            state["level_spec_valid"] = True
            healed_items.append("level_spec")

        state["can_heal"] = len(healed_items) > 0
        state.setdefault("history", []).append({
            "node": "self_healer",
            "action": f"Attempt {state['healing_attempts']}: Repaired {', '.join(healed_items) if healed_items else 'none'}"
        })
        return state
