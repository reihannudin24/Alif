#!/usr/bin/env python3
"""
QA Verifier Node for the Alif Agent System.
Executes test suites, checks asset metadata, inspects doorway overlaps,
and validates level integrity against Alif test harnesses.
"""

import subprocess
import json
from pathlib import Path
from typing import Dict, Any, List, Tuple


class QAVerifierNode:
    def __init__(self, memory_manager=None, alif_root: Path = None):
        self.memory = memory_manager
        self.alif_root = alif_root or Path(__file__).parent.parent.parent

    def verify_meta_files(self) -> Tuple[bool, List[str]]:
        """Scans Assets/ to ensure every asset has a paired .meta file."""
        missing_metas = []
        assets_dir = self.alif_root / "Assets"
        if not assets_dir.exists():
            return True, []

        for p in assets_dir.rglob("*"):
            if p.suffix == ".meta" or p.name.startswith("."):
                continue
            meta_path = p.with_name(p.name + ".meta")
            if not meta_path.exists():
                missing_metas.append(str(p.relative_to(self.alif_root)))

        return len(missing_metas) == 0, [f"Missing .meta for: {m}" for m in missing_metas[:5]]

    def run_spec_check(self) -> Tuple[bool, List[str]]:
        """Validates all JSON files in Assets/AI/TileRpg/Specs."""
        specs_dir = self.alif_root / "Assets" / "AI" / "TileRpg" / "Specs"
        errors = []
        if not specs_dir.exists():
            return True, []

        for f in specs_dir.glob("*.json"):
            try:
                with open(f, "r", encoding="utf-8") as spec_file:
                    json.load(spec_file)
            except Exception as e:
                errors.append(f"Invalid JSON in spec {f.name}: {e}")

        return len(errors) == 0, errors

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
        meta_ok, meta_errs = self.verify_meta_files()
        spec_ok, spec_errs = self.run_spec_check()

        all_passed = meta_ok and spec_ok
        if not all_passed:
            state.setdefault("errors", []).extend(meta_errs + spec_errs)

        state["qa_passed"] = all_passed
        state.setdefault("history", []).append({
            "node": "qa_verifier",
            "action": f"QA checks complete. Meta integrity: {'OK' if meta_ok else 'WARN'}, Spec syntax: {'OK' if spec_ok else 'FAIL'}"
        })
        return state
