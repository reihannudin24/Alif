#!/usr/bin/env python3
"""
Gameplay Developer Node for the Alif Agent System.
Enforces C# modular boundaries, assembly separation (pure models vs runtime),
coding style, and Yarn Spinner dialogue formatting.
"""

import re
from pathlib import Path
from typing import Dict, Any, List, Optional, Tuple

from glm_client import GlmClient, GlmError
from roles import Worker

MAX_WORKER_FILES = 4


class GameplayDevNode:
    def __init__(self, memory_manager=None, alif_root: Optional[Path] = None,
                 client: Optional[GlmClient] = None):
        self.memory = memory_manager
        self.alif_root = alif_root or Path(__file__).parent.parent.parent
        self.client = client
        self.worker = Worker(client) if client else None

    def validate_csharp_script(self, filename: str, content: str) -> Tuple[bool, List[str]]:
        errors = []
        is_domain_model = "Alif.Adventure.Model" in filename or "Alif.Battle.Model" in filename

        # 1. Pure C# domain model check
        if is_domain_model:
            if re.search(r"using\s+UnityEngine", content):
                errors.append(f"Domain model violation in {filename}: Must not import UnityEngine (pure C# only)")
            if re.search(r"using\s+UnityEditor", content):
                errors.append(f"Domain model violation in {filename}: Must not import UnityEditor")
            if re.search(r":\s*MonoBehaviour", content):
                errors.append(f"Domain model violation in {filename}: Cannot inherit from MonoBehaviour")

        # 2. Indentation check (reject tab indentation, recommend 4 spaces)
        lines = content.split("\n")
        for i, line in enumerate(lines[:100]):
            if line.startswith("\t"):
                errors.append(f"Style violation at {filename}:{i+1}: Found tab indentation (Alif requires 4 spaces)")
                break

        return len(errors) == 0, errors

    def validate_yarn_script(self, filename: str, content: str) -> Tuple[bool, List[str]]:
        errors = []
        nodes = content.split("===")
        declared_titles = set()
        jumps = []

        for chunk in nodes:
            chunk = chunk.strip()
            if not chunk:
                continue
            title_match = re.search(r"title:\s*(\w+)", chunk)
            if not title_match:
                errors.append(f"Yarn error in {filename}: Node missing 'title:' declaration")
            else:
                declared_titles.add(title_match.group(1))

            if "---" not in chunk:
                errors.append(f"Yarn error in {filename}: Node missing '---' header separator")

            for jm in re.finditer(r"<<jump\s+(\w+)>>", chunk):
                jumps.append((jm.group(1), filename))

        for target, fname in jumps:
            if target not in declared_titles:
                # Warning or error for unknown jump target within single file
                errors.append(f"Yarn jump warning in {fname}: Jump to undeclared node '<<jump {target}>>'")

        return len(errors) == 0, errors

    def _select_worker_files(self, instruction: str) -> List[str]:
        """Finds up to MAX_WORKER_FILES project files explicitly named in the instruction."""
        candidates = set(re.findall(r"Assets/[\w/.-]+\.(?:cs|yarn)", instruction))
        candidates |= set(re.findall(r"\b([\w/.-]+\.(?:cs|yarn))\b", instruction))
        resolved = []
        for name in sorted(candidates):
            for base in (self.alif_root, self.alif_root / "Assets" / "Scripts",
                         self.alif_root / "Assets" / "Dialogue"):
                path = base / name
                if path.is_file() and str(path.resolve()).startswith(str(self.alif_root.resolve())):
                    resolved.append(str(path.relative_to(self.alif_root)))
                    break
            if len(resolved) >= MAX_WORKER_FILES:
                break
        return resolved

    def _apply_worker_edits(self, state: Dict[str, Any]) -> None:
        """GLM-5.3-Flash worker applies the planner instruction to named files."""
        instruction = (state.get("plan_instructions") or {}).get("gameplay_dev", "")
        if not instruction or not (self.worker and self.client and self.client.available()):
            return
        targets = self._select_worker_files(instruction)
        if not targets:
            state.setdefault("history", []).append({
                "node": "gameplay_dev",
                "action": "Worker model available but instruction named no .cs/.yarn files; lint-only pass"
            })
            return
        files = {}
        for rel in targets:
            try:
                files[rel] = (self.alif_root / rel).read_text(encoding="utf-8")
            except OSError:
                continue
        try:
            changed = self.worker.edit(instruction, files)
        except GlmError as e:
            state.setdefault("history", []).append({
                "node": "gameplay_dev",
                "action": f"Worker model failed ({e}); no edits applied"
            })
            return
        for rel, content in changed.items():
            (self.alif_root / rel).write_text(content, encoding="utf-8")
            state.setdefault("code_changes", {})[rel] = content
        state.setdefault("history", []).append({
            "node": "gameplay_dev",
            "action": f"Worker {self.client.worker_model} edited {len(changed)} file(s): {', '.join(changed)}"
        })

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
        self._apply_worker_edits(state)
        task = state.get("task", "")
        code_changes = state.get("code_changes", {})

        all_valid = True
        all_errors = []

        for fname, content in code_changes.items():
            if fname.endswith(".cs"):
                valid, errs = self.validate_csharp_script(fname, content)
                if not valid:
                    all_valid = False
                    all_errors.extend(errs)
            elif fname.endswith(".yarn"):
                valid, errs = self.validate_yarn_script(fname, content)
                if not valid:
                    all_valid = False
                    all_errors.extend(errs)

        state["code_valid"] = all_valid
        if all_errors:
            state.setdefault("errors", []).extend(all_errors)

        state.setdefault("history", []).append({
            "node": "gameplay_dev",
            "action": f"Validated {len(code_changes)} code artifacts against Alif invariants. Status: {'PASS' if all_valid else 'FAIL'}"
        })
        return state
