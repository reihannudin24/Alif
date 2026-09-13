#!/usr/bin/env python3
"""
Alif Recursive Evolution Engine.
Recursively inspects the Alif codebase, git diffs, test logs, and symbols
to upgrade the agent's knowledge base, prompt context, and operational rules.
"""

import os
import re
import sys
import subprocess
from datetime import datetime
from pathlib import Path
from typing import Dict, Any, List, Set

from memory.memory_manager import MemoryManager


class EvolutionEngine:
    def __init__(self, alif_root: Path = None):
        self.alif_root = alif_root or Path(__file__).parent.parent.parent
        self.memory = MemoryManager()

    def scan_git_recent_changes(self) -> List[str]:
        """Collects recent commit messages and changed files."""
        try:
            cmd = ["git", "-C", str(self.alif_root), "log", "-n", "10", "--oneline", "--name-only"]
            out = subprocess.check_output(cmd, stderr=subprocess.DEVNULL, text=True)
            return out.strip().split("\n")
        except Exception:
            return []

    def scan_csharp_symbols(self) -> Set[str]:
        """Extracts public classes and structs across Assets/Scripts."""
        symbols = set()
        scripts_dir = self.alif_root / "Assets" / "Scripts"
        if not scripts_dir.exists():
            return symbols

        class_regex = re.compile(r"public\s+(?:abstract\s+|sealed\s+|partial\s+)?(?:class|struct|interface|enum)\s+(\w+)")
        for cs_file in scripts_dir.rglob("*.cs"):
            try:
                with open(cs_file, "r", encoding="utf-8", errors="ignore") as f:
                    for match in class_regex.finditer(f.read()):
                        symbols.add(match.group(1))
            except Exception:
                continue
        return symbols

    def scan_yarn_dialogue_nodes(self) -> Set[str]:
        """Extracts dialogue node titles from Assets/Dialogue."""
        nodes = set()
        yarn_dir = self.alif_root / "Assets" / "Dialogue"
        if not yarn_dir.exists():
            return nodes

        for yf in yarn_dir.rglob("*.yarn"):
            try:
                with open(yf, "r", encoding="utf-8", errors="ignore") as f:
                    for line in f:
                        if line.startswith("title:"):
                            nodes.add(line.split(":", 1)[1].strip())
            except Exception:
                continue
        return nodes

    def scan_tile_specs(self) -> List[str]:
        """Finds all tile RPG world specs in Assets/AI/TileRpg/Specs."""
        specs = []
        specs_dir = self.alif_root / "Assets" / "AI" / "TileRpg" / "Specs"
        if specs_dir.exists():
            for s in specs_dir.glob("*.json"):
                specs.append(s.stem)
        return specs

    def evolve(self) -> Dict[str, Any]:
        """Runs the evolutionary learning cycle and persists upgrades."""
        print("🌱 Running Alif Recursive Evolution Engine...")
        
        # 1. Analyze code changes & symbols
        git_log = self.scan_git_recent_changes()
        csharp_classes = self.scan_csharp_symbols()
        yarn_nodes = self.scan_yarn_dialogue_nodes()
        tile_specs = self.scan_tile_specs()

        # Update discovered symbols in memory
        discovered = self.memory.data.setdefault("discovered_symbols", {})
        existing_classes = set(discovered.get("core_classes", []))
        new_classes = list(csharp_classes - existing_classes)
        discovered["core_classes"] = sorted(list(existing_classes | csharp_classes))
        discovered["yarn_nodes"] = sorted(list(yarn_nodes))
        discovered["tile_specs"] = sorted(tile_specs)

        # 2. Record evolution cycle
        cycle_summary = (
            f"Evolved knowledge base: {len(discovered['core_classes'])} C# symbols, "
            f"{len(yarn_nodes)} Yarn nodes, {len(tile_specs)} tile specs. "
            f"Discovered {len(new_classes)} new symbols."
        )
        self.memory.record_evolution_cycle(cycle_summary, len(git_log), new_classes[:10])

        # 3. Regenerate human-readable documentation
        self.generate_docs()

        print(f"✅ {cycle_summary}")
        return {
            "status": "success",
            "summary": cycle_summary,
            "new_classes_count": len(new_classes),
            "total_classes": len(discovered["core_classes"]),
            "yarn_nodes": len(yarn_nodes),
            "tile_specs": len(tile_specs)
        }

    def generate_docs(self) -> None:
        """Writes Docs/agent_memory/README.md with current learned patterns."""
        doc_dir = self.alif_root / "Docs" / "agent_memory"
        doc_dir.mkdir(parents=True, exist_ok=True)
        doc_path = doc_dir / "README.md"

        data = self.memory.data
        lines = [
            "# Alif Agent Evolving Memory Ledger",
            "",
            "> Auto-updated by the Alif Recursive Evolution Engine (`./Tools/alif agent evolve`).",
            f"> Last updated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}",
            "",
            "## 📐 Architectural Rules & Invariants",
            ""
        ]
        for r in data.get("architectural_rules", []):
            lines.append(f"### `{r.get('id')}`: {r.get('title')}")
            lines.append(f"{r.get('rule')}")
            lines.append("")

        lines.append("## 🔍 Discovered Project Symbols")
        lines.append(f"- **Total Core C# Classes**: {len(data.get('discovered_symbols', {}).get('core_classes', []))}")
        lines.append(f"- **Yarn Dialogue Nodes**: {', '.join(data.get('discovered_symbols', {}).get('yarn_nodes', [])) or 'None'}")
        lines.append(f"- **Authored Tile Specs**: {', '.join(data.get('discovered_symbols', {}).get('tile_specs', [])) or 'None'}")
        lines.append("")

        lines.append("## 🛡️ Error & Resolution Registry")
        for err in data.get("error_registry", []):
            lines.append(f"- **{err.get('error_type')}**: {err.get('symptom')}")
            lines.append(f"  - *Cause*: {err.get('cause')}")
            lines.append(f"  - *Solution*: {err.get('solution')}")
            lines.append("")

        lines.append("## 🔄 Evolution Cycles History")
        for c in data.get("evolution_cycles", [])[-5:]:
            lines.append(f"- `[{c.get('timestamp')}]` {c.get('summary')}")

        with open(doc_path, "w", encoding="utf-8") as f:
            f.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    engine = EvolutionEngine()
    engine.evolve()
