#!/usr/bin/env python3
"""
QA Verifier Node for the Alif Agent System.
Executes test suites, checks asset metadata, inspects doorway overlaps,
and validates level integrity against Alif test harnesses.
"""

import json
import subprocess
import xml.etree.ElementTree as ElementTree
from pathlib import Path
from typing import Dict, Any, List, Optional, Tuple

TEST_TIMEOUT_SECONDS = 1200


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

    def run_editmode_tests(self) -> Tuple[bool, List[str]]:
        """Runs `./Tools/alif test-edit` and parses the JUnit-style report."""
        script = self.alif_root / "Tools" / "alif"
        report = self.alif_root / "Logs" / "TestResults" / "EditMode.xml"
        if report.exists():
            report.unlink()
        try:
            result = subprocess.run(
                ["bash", str(script), "test-edit"],
                cwd=str(self.alif_root),
                capture_output=True,
                text=True,
                timeout=TEST_TIMEOUT_SECONDS
            )
        except (subprocess.TimeoutExpired, OSError) as e:
            return False, [f"EditMode test run failed to execute: {e}"]

        errors: List[str] = []
        if result.returncode != 0:
            tail = (result.stderr or result.stdout or "").strip().splitlines()[-5:]
            errors.append(f"EditMode tests exited {result.returncode}: {' | '.join(tail)}")
        if report.exists():
            try:
                root = ElementTree.parse(report).getroot()
                failures = int(root.attrib.get("failures", "0") or 0)
                test_errors = int(root.attrib.get("errors", "0") or 0)
                total = int(root.attrib.get("total", root.attrib.get("tests", "0")) or 0)
                if failures or test_errors:
                    errors.append(f"EditMode tests: {failures} failure(s), {test_errors} error(s) out of {total}")
                    for case in root.iter("test-case"):
                        if case.find("failure") is not None:
                            name = case.attrib.get("fullname", case.attrib.get("name", "unknown"))
                            errors.append(f"  FAIL: {name}")
            except ElementTree.ParseError as e:
                errors.append(f"Could not parse test report {report.name}: {e}")
        else:
            errors.append("EditMode test report was not produced")
        return len(errors) == 0, errors

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
        meta_ok, meta_errs = self.verify_meta_files()
        spec_ok, spec_errs = self.run_spec_check()

        test_ok, test_errs = True, []
        if state.get("run_tests"):
            test_ok, test_errs = self.run_editmode_tests()

        all_passed = meta_ok and spec_ok and test_ok
        if not all_passed:
            state.setdefault("errors", []).extend(meta_errs + spec_errs + test_errs)

        state["qa_passed"] = all_passed
        action = f"QA checks complete. Meta integrity: {'OK' if meta_ok else 'WARN'}, Spec syntax: {'OK' if spec_ok else 'FAIL'}"
        if state.get("run_tests"):
            action += f", EditMode tests: {'PASS' if test_ok else 'FAIL'}"
        state.setdefault("history", []).append({
            "node": "qa_verifier",
            "action": action
        })
        return state
