#!/usr/bin/env python3
"""
Memory Manager for the Alif Agent System.
Maintains persistent project knowledge, architectural invariants,
discovered repositories, and learned patterns over time.
"""

import json
import os
from pathlib import Path
from typing import Any, Dict, List, Optional
from datetime import datetime

DEFAULT_KB_PATH = Path(__file__).parent / "knowledge_base.json"


class MemoryManager:
    def __init__(self, kb_path: Optional[Path] = None):
        self.kb_path = Path(kb_path) if kb_path else DEFAULT_KB_PATH
        self.data: Dict[str, Any] = self._load()

    def _load(self) -> Dict[str, Any]:
        if not self.kb_path.exists():
            return {
                "version": "1.0.0",
                "project": "Alif",
                "architectural_rules": [],
                "discovered_symbols": {},
                "error_registry": [],
                "discovered_repos": [],
                "evolution_cycles": []
            }
        try:
            with open(self.kb_path, "r", encoding="utf-8") as f:
                return json.load(f)
        except Exception as e:
            print(f"[WARN] Failed to load knowledge base: {e}")
            return {}

    def save(self) -> None:
        self.kb_path.parent.mkdir(parents=True, exist_ok=True)
        with open(self.kb_path, "w", encoding="utf-8") as f:
            json.dump(self.data, f, indent=2, ensure_ascii=False)

    def get_rules(self) -> List[Dict[str, str]]:
        return self.data.get("architectural_rules", [])

    def add_rule(self, rule_id: str, title: str, rule: str) -> None:
        rules = self.data.setdefault("architectural_rules", [])
        for r in rules:
            if r.get("id") == rule_id:
                r["title"] = title
                r["rule"] = rule
                self.save()
                return
        rules.append({"id": rule_id, "title": title, "rule": rule})
        self.save()

    def add_error_resolution(self, error_type: str, symptom: str, cause: str, solution: str) -> None:
        reg = self.data.setdefault("error_registry", [])
        for entry in reg:
            if entry.get("error_type") == error_type:
                entry.update({"symptom": symptom, "cause": cause, "solution": solution})
                self.save()
                return
        reg.append({
            "error_type": error_type,
            "symptom": symptom,
            "cause": cause,
            "solution": solution,
            "recorded_at": datetime.now().isoformat()
        })
        self.save()

    def add_discovered_repo(self, repo_info: Dict[str, Any]) -> None:
        repos = self.data.setdefault("discovered_repos", [])
        full_name = repo_info.get("full_name") or repo_info.get("name")
        for r in repos:
            if r.get("full_name") == full_name:
                r.update(repo_info)
                self.save()
                return
        repo_info["added_at"] = datetime.now().isoformat()
        repos.append(repo_info)
        self.save()

    def record_evolution_cycle(self, summary: str, changes_found: int, new_symbols: List[str]) -> None:
        cycles = self.data.setdefault("evolution_cycles", [])
        cycles.append({
            "timestamp": datetime.now().isoformat(),
            "summary": summary,
            "changes_analyzed": changes_found,
            "new_symbols": new_symbols
        })
        self.save()

    def get_context_summary(self) -> str:
        """Returns prompt-ready knowledge summary for agent nodes."""
        lines = [
            f"# Alif Project Knowledge (v{self.data.get('version', '1.0')})",
            f"- Project: {self.data.get('project', 'Alif')}",
            f"- Unity: {self.data.get('unity_version', '6000.6.0f1')} ({self.data.get('render_pipeline', 'URP')})",
            "",
            "## Core Architectural Invariants:"
        ]
        for r in self.get_rules():
            lines.append(f"- **{r.get('title')}**: {r.get('rule')}")

        lines.append("")
        lines.append("## Known Error Mitigations:")
        for err in self.data.get("error_registry", [])[:5]:
            lines.append(f"- *{err.get('error_type')}*: {err.get('symptom')} -> Fix: {err.get('solution')}")

        return "\n".join(lines)


if __name__ == "__main__":
    mm = MemoryManager()
    print(mm.get_context_summary())
