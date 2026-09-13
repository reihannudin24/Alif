#!/usr/bin/env python3
"""
CLI interface for the Alif Agent System.
Provides developer and agent commands: run, status, evolve, memory.
"""

import sys
import argparse
from pathlib import Path

# Add Tools/alif-agent and Tools/find-repo to sys.path
AGENT_DIR = Path(__file__).parent
if str(AGENT_DIR) not in sys.path:
    sys.path.insert(0, str(AGENT_DIR))

from alif_graph import AlifGraph, AlifAgentState, NODE_ORDER
from memory.memory_manager import MemoryManager
from evolve import EvolutionEngine
from glm_client import GlmClient


def cmd_run(args):
    graph = AlifGraph()
    print(f"🚀 Executing Alif Agent Graph for task: '{args.task}'")
    if args.dry_run:
        print("ℹ️  Mode: DRY-RUN (Planning only)")

    result = graph.run(args.task, dry_run=args.dry_run,
                       run_tests=args.with_tests, with_vision=not args.no_vision)
    print(f"\n✅ Finished! Task Intent: {result.get('intent')} (planner: {result.get('planner_mode')})")
    print("Execution Steps:")
    for h in result.get("history", []):
        print(f"  - [{h['node']}]: {h['action']}")

    if result.get("repo_references"):
        print("\nDiscovered Reference Repositories:")
        for r in result["repo_references"]:
            name = r.get("full_name") or r.get("name")
            desc = r.get("description") or "No description"
            print(f"  ⭐ {name}: {desc}")

    if result.get("level_spec"):
        print(f"\nLevel Spec Generated: {result['level_spec'].get('name')} ({'Valid' if result.get('level_spec_valid') else 'Invalid'})")

    if result.get("vision_findings"):
        print("\n👁  Vision Findings (GLM-5.3-Flash):")
        for f in result["vision_findings"]:
            print(f"  - [{f['severity']}] {f['image']} ({f['area']}): {f['issue']} -> {f['suggestion']}")

    if result.get("errors"):
        print("\n⚠️  Warnings / Unresolved Errors:")
        for err in result["errors"]:
            print(f"  - {err}")


def cmd_status(args):
    mm = MemoryManager()
    data = mm.data
    symbols = data.get("discovered_symbols", {})
    rules = data.get("architectural_rules", [])
    errors = data.get("error_registry", [])
    repos = data.get("discovered_repos", [])
    cycles = data.get("evolution_cycles", [])

    glm = GlmClient()
    config = glm.config_summary()

    print("========================================")
    print("🤖 Alif Agent System Status")
    print("========================================")
    print(f"Project: {data.get('project', 'Alif')} (Unity {data.get('unity_version', '6000.6.0f1')})")
    print(f"Planner Model: {config['planner_model']}")
    print(f"Worker/Vision Model: {config['worker_model']}")
    print(f"GLM API Key: {'present (' + config['base_url'] + ')' if config['api_key_present'] else 'MISSING - deterministic keyword fallback active'}")
    print(f"Architectural Rules: {len(rules)}")
    print(f"Known Error Resolutions: {len(errors)}")
    print(f"Saved Reference Repos: {len(repos)}")
    print(f"Evolution Cycles Logged: {len(cycles)}")
    print(f"Discovered C# Symbols: {len(symbols.get('core_classes', []))}")
    print(f"Yarn Dialogue Nodes: {len(symbols.get('yarn_nodes', []))}")
    print(f"TileRPG Specs: {len(symbols.get('tile_specs', []))}")
    print("\nActive Graph Nodes:")
    for node in NODE_ORDER:
        print(f"  - {node}")
    print("========================================")


def cmd_vision(args):
    graph = AlifGraph()
    state = AlifAgentState(task=args.context or "Standalone vision QA pass", skip_capture=args.no_capture)
    state = graph.nodes["vision_qa"].execute(state)

    for h in state.get("history", []):
        print(f"  - [{h['node']}]: {h['action']}")

    findings = state.get("vision_findings", [])
    if findings:
        print("\n👁  Vision Findings:")
        for f in findings:
            print(f"  - [{f['severity']}] {f['image']} ({f['area']}): {f['issue']} -> {f['suggestion']}")
    elif state.get("errors"):
        print("\n⚠️  Errors:")
        for err in state["errors"]:
            print(f"  - {err}")


def cmd_evolve(args):
    engine = EvolutionEngine()
    engine.evolve()


def cmd_memory(args):
    mm = MemoryManager()
    print(mm.get_context_summary())


def main():
    parser = argparse.ArgumentParser(description="Alif Agentic Graph CLI")
    subparsers = parser.add_subparsers(dest="command", help="Agent command")

    # Run
    p_run = subparsers.add_parser("run", help="Run the Alif agent graph on a task")
    p_run.add_argument("task", type=str, help="Developer task description")
    p_run.add_argument("--dry-run", action="store_true", help="Perform planning without mutating files")
    p_run.add_argument("--with-tests", action="store_true",
                       help="qa_verifier also runs EditMode tests via the unity CLI")
    p_run.add_argument("--no-vision", action="store_true",
                       help="Skip the vision_qa screenshot evaluation step")

    # Vision QA
    p_vision = subparsers.add_parser("vision", help="Capture screenshots and evaluate them with GLM-5.3-Flash vision")
    p_vision.add_argument("--no-capture", action="store_true",
                          help="Evaluate existing Logs/Screenshots/*.png without re-capturing")
    p_vision.add_argument("--context", type=str, default="", help="Optional context for the evaluator")

    # Status
    subparsers.add_parser("status", help="Show agent system status and memory")

    # Evolve
    subparsers.add_parser("evolve", help="Run recursive evolutionary learning cycle")

    # Memory
    subparsers.add_parser("memory", help="Show current prompt-ready knowledge summary")

    args = parser.parse_args()
    if not args.command:
        parser.print_help()
        sys.exit(1)

    if args.command == "run":
        cmd_run(args)
    elif args.command == "vision":
        cmd_vision(args)
    elif args.command == "status":
        cmd_status(args)
    elif args.command == "evolve":
        cmd_evolve(args)
    elif args.command == "memory":
        cmd_memory(args)


if __name__ == "__main__":
    main()
