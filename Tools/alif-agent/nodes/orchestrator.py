#!/usr/bin/env python3
"""
Orchestrator / Director Node for the Alif Agent System.
Decomposes user prompts, identifies intent, and schedules execution routes.
"""

from typing import Dict, Any, List


class OrchestratorNode:
    INTENTS = ["RESEARCH", "DESIGN_ASSETS", "WORLD_DESIGN", "GAMEPLAY_DEV", "QA_VALIDATION", "FULL_FEATURE"]

    def __init__(self, memory_manager=None):
        self.memory = memory_manager

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
            return ["qa_verifier", "evolution"]
        elif intent == "GAMEPLAY_DEV":
            return ["gameplay_dev", "qa_verifier", "evolution"]
        else: # FULL_FEATURE
            return ["repo_hunter", "design_asset_agent", "world_architect", "gameplay_dev", "qa_verifier", "evolution"]

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
        task = state.get("task", "")
        intent = self.classify_intent(task)
        steps = self.plan_steps(task, intent)
        state["intent"] = intent
        state["plan"] = steps
        state.setdefault("history", []).append({
            "node": "orchestrator",
            "action": f"Classified task as {intent}, planned pipeline: {' -> '.join(steps)}"
        })
        return state
