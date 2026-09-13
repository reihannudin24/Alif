#!/usr/bin/env python3
"""
Vision QA Node for the Alif Agent System.
Captures rendered screenshots of the key scenes via the Unity CLI
(`./Tools/alif capture-screens`) and evaluates them with the GLM-5.3-Flash
vision model. Without an API key the capture still runs and the PNGs are left
in Logs/Screenshots/ for manual review.
"""

import subprocess
from pathlib import Path
from typing import Dict, Any, List, Optional

from glm_client import GlmClient
from roles import VisionEvaluator

SCREENSHOTS_DIR = "Logs/Screenshots"
CAPTURE_TIMEOUT_SECONDS = 900
ERROR_SEVERITIES = ("medium", "high")


class VisionQANode:
    def __init__(self, memory_manager=None, alif_root: Optional[Path] = None,
                 client: Optional[GlmClient] = None):
        self.memory = memory_manager
        self.alif_root = alif_root or Path(__file__).parent.parent.parent
        self.client = client
        self.evaluator = VisionEvaluator(client) if client else None

    def capture_screenshots(self) -> bool:
        """Runs the Unity batch capture; returns True when it succeeds."""
        script = self.alif_root / "Tools" / "alif"
        try:
            result = subprocess.run(
                ["bash", str(script), "capture-screens"],
                cwd=str(self.alif_root),
                capture_output=True,
                text=True,
                timeout=CAPTURE_TIMEOUT_SECONDS
            )
        except (subprocess.TimeoutExpired, OSError) as e:
            print(f"[WARN] Screenshot capture failed: {e}")
            return False
        if result.returncode != 0:
            tail = (result.stderr or result.stdout or "").strip().splitlines()[-5:]
            print(f"[WARN] Screenshot capture exited {result.returncode}: {' | '.join(tail)}")
            return False
        return True

    def collect_screenshots(self) -> List[Path]:
        shots_dir = self.alif_root / SCREENSHOTS_DIR
        if not shots_dir.exists():
            return []
        return sorted(shots_dir.glob("*.png"))

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
        if state.get("skip_vision"):
            state.setdefault("history", []).append({
                "node": "vision_qa",
                "action": "Skipped (--no-vision)"
            })
            return state

        captured = True if state.get("skip_capture") else self.capture_screenshots()
        screenshots = self.collect_screenshots()
        if not screenshots:
            state.setdefault("errors", []).append(
                "vision_qa: no screenshots produced by capture-screens"
            )
            state["qa_passed"] = False
            state.setdefault("history", []).append({
                "node": "vision_qa",
                "action": "Capture produced no screenshots"
            })
            return state

        if not (self.evaluator and self.client and self.client.available()):
            state["vision_findings"] = []
            state.setdefault("history", []).append({
                "node": "vision_qa",
                "action": (f"{len(screenshots)} screenshots captured to {SCREENSHOTS_DIR}/ "
                           "for manual review; vision evaluation skipped (no GLM API key)")
            })
            return state

        context = state.get("task", "")
        findings = self.evaluator.evaluate(screenshots, context=context)
        state["vision_findings"] = findings
        blocking = [f for f in findings if f.get("severity") in ERROR_SEVERITIES]
        for f in blocking:
            state.setdefault("errors", []).append(
                f"vision_qa [{f['severity']}] {f['image']} ({f['area']}): {f['issue']} -> {f['suggestion']}"
            )
        if blocking:
            state["qa_passed"] = False
        state.setdefault("history", []).append({
            "node": "vision_qa",
            "action": (f"Evaluated {len(screenshots)} screenshots with "
                       f"{self.client.worker_model}: {len(findings)} findings "
                       f"({len(blocking)} blocking)")
        })
        if captured is False:
            state.setdefault("history", []).append({
                "node": "vision_qa",
                "action": "Note: capture command reported an issue; evaluated existing screenshots"
            })
        return state
