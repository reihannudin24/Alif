#!/usr/bin/env python3
"""
Orchestrator / Director Node for the Alif Agent System.
Decomposes user prompts, identifies intent, and schedules execution routes.
Planning is model-driven when the GLM planner (glm-5.3) is configured, and falls
back to deterministic keyword routing when no API key is available or the model
response is unusable.
"""

from typing import Dict, Any, List, Optional

from glm_client import GlmClient, GlmError
from roles import Planner


class OrchestratorNode:
    INTENTS = ["RESEARCH", "DESIGN_ASSETS", "WORLD_DESIGN", "GAMEPLAY_DEV", "QA_VALIDATION", "FULL_FEATURE"]

    def __init__(self, memory_manager=None, allowed_nodes: Optional[List[str]] = None,
                 client: Optional[GlmClient] = None):
        self.memory = memory_manager
        self.allowed_nodes = allowed_nodes or []
        self.client = client
        self.planner = Planner(client, self.allowed_nodes) if client else None

    def classify_intent(self, task: str) -> str:
        t = task.lower()
        if any(w in t for w in ["find repo", "search github", "reference repo", "starter kit", "library for"]):
            return "RESEARCH"
        if any(w in t for w in ["test", "lint", "validate", "check", "verify", "ci"]):
            return "QA_VALIDATION"
        if any(w in t for w in ["asset", "sprite", "emote", "art", "tileset", "texture", "props", "manifest", "modify", "recolor", "tint", "button", "icon"]):
            return "DESIGN_ASSETS"
        if any(w in t for w in ["tile", "level", "room", "map", "world", "spawn", "schema", "market", "garden", "station"]):
            return "WORLD_DESIGN"
        if any(w in t for w in ["dialogue", "yarn", "combat", "battle", "controller", "player", "model", "script", "c#"]):
            return "GAMEPLAY_DEV"
        return "FULL_FEATURE"

    def plan_steps(self, task: str, intent: str) -> List[str]:
        if intent == "RESEARCH":
            return ["repo_hunter", "evolution"]
        elif intent == "DESIGN_ASSETS":
            return ["design_asset_agent", "qa_verifier", "evolution"]
        elif intent == "WORLD_DESIGN":
            return ["design_asset_agent", "world_architect", "qa_verifier", "evolution"]
        elif intent == "QA_VALIDATION":
            return ["qa_verifier", "vision_qa", "evolution"]
        elif intent == "GAMEPLAY_DEV":
            return ["gameplay_dev", "qa_verifier", "evolution"]
        else: # FULL_FEATURE
            return ["repo_hunter", "design_asset_agent", "world_architect", "gameplay_dev", "qa_verifier", "vision_qa", "evolution"]

    def _plan_with_model(self, task: str) -> Optional[Dict[str, Any]]:
        if not (self.planner and self.client and self.client.available()):
            return None
        try:
            memory_summary = self.memory.get_context_summary() if self.memory else ""
            return self.planner.plan(task, memory_summary=memory_summary)
        except GlmError as e:
            return {"error": str(e)}
        except Exception:
            return None

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
        task = state.get("task", "")
        model_plan = self._plan_with_model(task)

        if model_plan and "steps" in model_plan:
            state["intent"] = model_plan.get("intent", "FULL_FEATURE")
            state["plan"] = [s["node"] for s in model_plan["steps"]]
            state["plan_instructions"] = {s["node"]: s.get("instruction", "") for s in model_plan["steps"]}
            state["planner_mode"] = f"glm ({self.client.planner_model})"
            state.setdefault("history", []).append({
                "node": "orchestrator",
                "action": f"Planned via {self.client.planner_model}: {state['intent']} -> {' -> '.join(state['plan'])}"
            })
            return state

        intent = self.classify_intent(task)
        steps = self.plan_steps(task, intent)
        state["intent"] = intent
        state["plan"] = steps
        state["planner_mode"] = "keyword-fallback"
        if model_plan and "error" in model_plan:
            state.setdefault("history", []).append({
                "node": "orchestrator",
                "action": f"Planner model failed ({model_plan['error']}); using keyword routing"
            })
        state.setdefault("history", []).append({
            "node": "orchestrator",
            "action": f"Classified task as {intent} (keyword), planned pipeline: {' -> '.join(steps)}"
        })
        return state
