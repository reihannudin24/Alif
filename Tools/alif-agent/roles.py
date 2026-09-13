#!/usr/bin/env python3
"""
Role wrappers for the Alif dual-model pipeline.
GLM-5.3 (planner) decomposes tasks into graph steps; GLM-5.3-Flash (worker)
edits code and dialogue, and evaluates rendered screenshots with vision input.
"""

from pathlib import Path
from typing import Any, Dict, List, Optional

from glm_client import GlmClient, GlmError

ALLOWED_EDIT_PREFIXES = ("Assets/Scripts/", "Assets/Dialogue/", "Assets/AI/")
MAX_EDIT_FILE_BYTES = 256 * 1024
MAX_VISION_IMAGES = 8
SEVERITIES = ("low", "medium", "high")


class Planner:
    """GLM-5.3 planner: turns a natural-language task into a validated graph plan."""

    def __init__(self, client: GlmClient, allowed_nodes: List[str]):
        self.client = client
        self.allowed_nodes = allowed_nodes

    def plan(self, task: str, memory_summary: str = "", audit_context: str = "") -> Dict[str, Any]:
        """Returns {"intent": str, "steps": [{"node", "instruction"}]} with node names
        filtered to the registered graph nodes. Raises GlmError when unusable."""
        nodes_doc = "\n".join(f"- {n}" for n in self.allowed_nodes)
        system = (
            "You are the planning model for the Alif Unity 2D educational adventure game.\n"
            "Decompose the task into an execution plan over these graph nodes only:\n"
            f"{nodes_doc}\n\n"
            "Respond with JSON only: {\"intent\": \"<GAMEPLAY_DEV|QA_VALIDATION|DESIGN_ASSETS|"
            "WORLD_DESIGN|RESEARCH|FULL_FEATURE>\", \"steps\": [{\"node\": \"<node>\", "
            "\"instruction\": \"<specific instruction for that node>\"}]}.\n"
            "Rules: use only listed nodes; 'qa_verifier' or 'vision_qa' must be the last "
            "verification step before 'evolution'; keep plans under 8 steps; instructions "
            "must be concrete and reference files/systems by name when known.\n"
        )
        context = memory_summary or ""
        if audit_context:
            context += ("\n\n## Known Polish Backlog:\n" + audit_context)
        messages = [
            {"role": "system", "content": system},
            {"role": "user", "content": (f"# Project knowledge\n{context}\n\n# Task\n{task}" if context else task)}
        ]
        data = self.client.chat_json(messages, model=self.client.planner_model, max_tokens=2048)
        steps = []
        for step in data.get("steps", []) if isinstance(data, dict) else []:
            node = step.get("node", "")
            instruction = str(step.get("instruction", "")).strip()
            if node in self.allowed_nodes:
                steps.append({"node": node, "instruction": instruction})
        if not steps:
            raise GlmError("planner produced no valid steps")
        intent = data.get("intent", "FULL_FEATURE") if isinstance(data, dict) else "FULL_FEATURE"
        return {"intent": intent, "steps": steps}


class Worker:
    """GLM-5.3-Flash worker: applies bounded code/dialogue edits."""

    def __init__(self, client: GlmClient):
        self.client = client

    def edit(self, instruction: str, files: Dict[str, str]) -> Dict[str, str]:
        """Given {relative_path: current_content}, returns {relative_path: new_content}
        for changed files only. Paths outside ALLOWED_EDIT_PREFIXES are rejected."""
        for path in files:
            if not path.startswith(ALLOWED_EDIT_PREFIXES):
                raise GlmError(f"worker edit out of bounds: {path}")
            if len(files[path].encode("utf-8")) > MAX_EDIT_FILE_BYTES:
                raise GlmError(f"worker edit file too large: {path}")
        system = (
            "You are the implementation worker for the Alif Unity project "
            "(Unity 6000.6, URP, Input System, uGUI/TextMeshPro, four-space indent, "
            "PascalCase public members). Apply the instruction to the provided files.\n"
            "Respond with JSON only: {\"files\": {\"<same relative path>\": \"<full updated "
            "file content>\"}} for changed files only. Omit unchanged files. Preserve "
            "existing style and usings; never rename public APIs unless instructed."
        )
        payload = {"instruction": instruction, "files": files}
        messages = [
            {"role": "system", "content": system},
            {"role": "user", "content": _dumps(payload)}
        ]
        data = self.client.chat_json(messages, model=self.client.worker_model, max_tokens=8192)
        changed = {}
        if isinstance(data, dict):
            for path, content in data.get("files", {}).items():
                if not path.startswith(ALLOWED_EDIT_PREFIXES) or not isinstance(content, str):
                    continue
                if content != files.get(path):
                    changed[path] = content
        return changed


class VisionEvaluator:
    """GLM-5.3-Flash vision evaluator: judges rendered screenshots of the game."""

    def __init__(self, client: GlmClient):
        self.client = client

    def evaluate(self, screenshots: List[Path], context: str = "") -> List[Dict[str, Any]]:
        """Returns findings [{image, severity(low|medium|high), area, issue, suggestion}]."""
        usable: List[Path] = []
        for shot in screenshots:
            if shot.exists() and shot.suffix.lower() == ".png":
                usable.append(shot)
        usable = usable[:MAX_VISION_IMAGES]
        if not usable:
            return []
        names = ", ".join(s.name for s in usable)
        system = (
            "You are the visual QA evaluator for Alif, a 2D educational adventure game "
            "(pixel-art, uGUI/TextMeshPro HUD). You receive rendered screenshots.\n"
            "Judge: layout/alignment, text legibility and overflow, contrast, HUD "
            "occlusion of gameplay, art consistency, obvious rendering glitches.\n"
            "Ignore stylistic taste; report player-facing problems only.\n"
            "Respond with JSON only: {\"findings\": [{\"image\": \"<filename>\", "
            "\"severity\": \"low|medium|high\", \"area\": \"<UI area/scene part>\", "
            "\"issue\": \"<what is wrong>\", \"suggestion\": \"<concrete fix>\"}]}. "
            "Return an empty findings list when a screenshot is fine."
        )
        user = f"Screenshots: {names}."
        if context:
            user += f"\nContext: {context}"
        messages = [
            {"role": "system", "content": system},
            {"role": "user", "content": user}
        ]
        # chat_json first so the text contract is established, then send images.
        prompt = system + "\n\n" + user
        data = self.client.vision_json(prompt, usable, model=self.client.worker_model)
        findings: List[Dict[str, Any]] = []
        known = {s.name for s in usable}
        if isinstance(data, dict):
            for finding in data.get("findings", []):
                if not isinstance(finding, dict):
                    continue
                severity = str(finding.get("severity", "low")).lower()
                if severity not in SEVERITIES:
                    severity = "low"
                image = finding.get("image", "")
                findings.append({
                    "image": image if image in known else names.split(",")[0].strip(),
                    "severity": severity,
                    "area": str(finding.get("area", "general")),
                    "issue": str(finding.get("issue", "")),
                    "suggestion": str(finding.get("suggestion", ""))
                })
        return findings


def _dumps(payload: Any) -> str:
    import json
    return json.dumps(payload, ensure_ascii=False)
