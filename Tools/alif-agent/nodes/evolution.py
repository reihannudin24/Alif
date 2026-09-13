#!/usr/bin/env python3
"""
Evolution Node for the Alif Agent System.
Performs recursive knowledge crystallization, updating agent memory
and recording learned patterns for subsequent runs.
"""

from typing import Dict, Any


class EvolutionNode:
    def __init__(self, memory_manager=None):
        self.memory = memory_manager

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
        task = state.get("task", "")
        intent = state.get("intent", "")
        healed = state.get("healing_attempts", 0) > 0
        new_repos = len(state.get("repo_references", []))

        summary = f"Task '{task}' ({intent}) completed. Healed: {healed}, Repos linked: {new_repos}."

        level_spec = state.get("level_spec")
        new_syms = [level_spec.get("name", "")] if isinstance(level_spec, dict) and level_spec.get("name") else []

        if self.memory:
            self.memory.record_evolution_cycle(
                summary=summary,
                changes_found=len(state.get("code_changes", {})),
                new_symbols=new_syms
            )

        state.setdefault("history", []).append({
            "node": "evolution",
            "action": f"Crystallized execution memory into knowledge base: {summary}"
        })
        return state
