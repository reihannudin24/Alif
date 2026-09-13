#!/usr/bin/env python3
"""
Alif Agentic Graph: Core State Machine and Execution Engine.
Orchestrates specialized subagents (RepoHunter, WorldArchitect, GameplayDev, QAVerifier, SelfHealer, Evolution)
to build and maintain the Alif educational 2D adventure game.
"""

import sys
from pathlib import Path
from typing import Dict, Any, Optional

from memory.memory_manager import MemoryManager
from nodes.orchestrator import OrchestratorNode
from nodes.repo_hunter import RepoHunterNode
from nodes.world_architect import WorldArchitectNode
from nodes.gameplay_dev import GameplayDevNode
from nodes.qa_verifier import QAVerifierNode
from nodes.self_healer import SelfHealerNode
from nodes.evolution import EvolutionNode


class AlifAgentState(dict):
    """Encapsulates the state dictionary across graph nodes."""
    def __init__(self, task: str, **kwargs):
        super().__init__(
            task=task,
            intent="",
            plan=[],
            repo_references=[],
            level_spec=None,
            level_spec_valid=True,
            code_changes={},
            code_valid=True,
            qa_passed=True,
            errors=[],
            healing_attempts=0,
            history=[],
            **kwargs
        )


class AlifGraph:
    def __init__(self, kb_path: Optional[Path] = None, alif_root: Optional[Path] = None):
        self.alif_root = alif_root or Path(__file__).parent.parent.parent
        self.memory = MemoryManager(kb_path)
        self.nodes = {
            "orchestrator": OrchestratorNode(self.memory),
            "repo_hunter": RepoHunterNode(self.memory),
            "world_architect": WorldArchitectNode(self.memory),
            "gameplay_dev": GameplayDevNode(self.memory),
            "qa_verifier": QAVerifierNode(self.memory, self.alif_root),
            "self_healer": SelfHealerNode(self.memory),
            "evolution": EvolutionNode(self.memory)
        }

    def execute_node(self, node_name: str, state: Dict[str, Any]) -> Dict[str, Any]:
        node = self.nodes.get(node_name)
        if not node:
            raise ValueError(f"Unknown node: {node_name}")
        return node.execute(state)

    def run(self, task: str, dry_run: bool = False) -> Dict[str, Any]:
        state = AlifAgentState(task=task)

        # 1. Orchestration & Planning
        state = self.execute_node("orchestrator", state)
        plan = list(state.get("plan", []))

        if dry_run:
            state.setdefault("history", []).append({
                "node": "graph_runner",
                "action": f"[DRY RUN] Plan finalized: {' -> '.join(plan)}"
            })
            return state

        # 2. Sequential / Conditional Graph Execution
        for step in plan:
            state = self.execute_node(step, state)

            # Check for failures triggering self-healing loop
            if state.get("errors") and step in ["world_architect", "gameplay_dev", "qa_verifier"]:
                state = self.execute_node("self_healer", state)
                # If healed, re-verify
                if state.get("can_heal"):
                    state = self.execute_node("qa_verifier", state)

        return state


if __name__ == "__main__":
    task_input = sys.argv[1] if len(sys.argv) > 1 else "Find dialogue tools and build a market level"
    graph = AlifGraph()
    res = graph.run(task_input)
    print(f"\n[Task: {res['task']}]")
    print(f"Intent: {res['intent']}")
    print("\nExecution History:")
    for h in res.get("history", []):
        print(f"  - [{h['node']}]: {h['action']}")
