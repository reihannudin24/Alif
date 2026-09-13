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
from glm_client import GlmClient
from nodes.orchestrator import OrchestratorNode
from nodes.repo_hunter import RepoHunterNode
from nodes.world_architect import WorldArchitectNode
from nodes.gameplay_dev import GameplayDevNode
from nodes.qa_verifier import QAVerifierNode
from nodes.self_healer import SelfHealerNode
from nodes.vision_qa import VisionQANode
from nodes.evolution import EvolutionNode
from nodes.design_asset_agent import DesignAssetAgentNode

NODE_ORDER = [
    "orchestrator", "repo_hunter", "design_asset_agent", "world_architect",
    "gameplay_dev", "qa_verifier", "vision_qa", "self_healer", "evolution"
]


class AlifAgentState(dict):
    """Encapsulates the state dictionary across graph nodes."""
    def __init__(self, task: str, **kwargs):
        defaults = dict(
            task=task,
            intent="",
            plan=[],
            plan_instructions={},
            planner_mode="keyword-fallback",
            repo_references=[],
            generated_assets=[],
            level_spec=None,
            level_spec_valid=True,
            code_changes={},
            code_valid=True,
            qa_passed=True,
            vision_findings=[],
            run_tests=False,
            skip_vision=False,
            skip_capture=False,
            errors=[],
            healing_attempts=0,
            history=[]
        )
        defaults.update(kwargs)
        super().__init__(**defaults)


class AlifGraph:
    def __init__(self, kb_path: Optional[Path] = None, alif_root: Optional[Path] = None):
        self.alif_root = alif_root or Path(__file__).parent.parent.parent
        self.memory = MemoryManager(kb_path)
        self.glm = GlmClient()
        self.nodes = {
            "orchestrator": OrchestratorNode(self.memory, allowed_nodes=NODE_ORDER, client=self.glm),
            "repo_hunter": RepoHunterNode(self.memory),
            "design_asset_agent": DesignAssetAgentNode(self.memory, self.alif_root),
            "world_architect": WorldArchitectNode(self.memory, self.alif_root),
            "gameplay_dev": GameplayDevNode(self.memory, self.alif_root, self.glm),
            "qa_verifier": QAVerifierNode(self.memory, self.alif_root),
            "vision_qa": VisionQANode(self.memory, self.alif_root, self.glm),
            "self_healer": SelfHealerNode(self.memory),
            "evolution": EvolutionNode(self.memory)
        }

    def execute_node(self, node_name: str, state: Dict[str, Any]) -> Dict[str, Any]:
        node = self.nodes.get(node_name)
        if not node:
            raise ValueError(f"Unknown node: {node_name}")
        return node.execute(state)

    def run(self, task: str, dry_run: bool = False, run_tests: bool = False,
            with_vision: bool = True) -> Dict[str, Any]:
        state = AlifAgentState(task=task, run_tests=run_tests, skip_vision=not with_vision)

        # 1. Orchestration & Planning (GLM-5.3 planner, keyword fallback)
        state = self.execute_node("orchestrator", state)
        plan = list(state.get("plan", []))

        if dry_run:
            state.setdefault("history", []).append({
                "node": "graph_runner",
                "action": f"[DRY RUN] Plan finalized ({state.get('planner_mode')}): {' -> '.join(plan)}"
            })
            return state

        # 2. Sequential / Conditional Graph Execution
        for step in plan:
            state = self.execute_node(step, state)

            # Check for failures triggering self-healing loop
            if state.get("errors") and step in ["world_architect", "gameplay_dev", "qa_verifier", "vision_qa", "design_asset_agent"]:
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
