#!/usr/bin/env python3
"""
Gameplay Developer Node for the Alif Agent System.
Enforces C# modular boundaries, assembly separation (pure models vs runtime),
coding style, and Yarn Spinner dialogue formatting.
"""

import re
from typing import Dict, Any, List, Tuple


class GameplayDevNode:
    def __init__(self, memory_manager=None):
        self.memory = memory_manager

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

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
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
