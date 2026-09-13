#!/usr/bin/env python3
"""
Repo Hunter Node for the Alif Agent System.
Uses the embedded find-repo tool to discover reference implementations,
libraries, and architecture patterns relevant to Alif.
"""

import sys
from pathlib import Path
from typing import Dict, Any, List

# Locate find-repo
TOOLS_DIR = Path(__file__).parent.parent.parent
FIND_REPO_DIR = TOOLS_DIR / "find-repo"
if str(FIND_REPO_DIR) not in sys.path:
    sys.path.insert(0, str(FIND_REPO_DIR))

try:
    import find_repo
except ImportError:
    find_repo = None


class RepoHunterNode:
    def __init__(self, memory_manager=None):
        self.memory = memory_manager

    def search(self, query: str, limit: int = 3) -> List[Dict[str, Any]]:
        if not find_repo:
            return [{"full_name": "mock/unity-reference", "description": "Fallback reference (find_repo unimported)", "stars": 100}]
        try:
            # Query decomposition & semantic execution
            data = find_repo.search_repositories(query=query, limit=limit)
            return data.get("results", [])
        except Exception as e:
            return [{"error": str(e), "query": query}]

    def execute(self, state: Dict[str, Any]) -> Dict[str, Any]:
        task = state.get("task", "")
        results = self.search(task, limit=3)
        valid_repos = [r for r in results if "full_name" in r or "name" in r]
        state["repo_references"] = valid_repos

        # Persist top discovered repositories into memory
        if self.memory:
            for repo in valid_repos:
                self.memory.add_discovered_repo({
                    "name": repo.get("full_name") or repo.get("name"),
                    "description": repo.get("description", ""),
                    "stars": repo.get("stars", 0),
                    "url": repo.get("url", ""),
                    "relevant_to_task": task
                })

        state.setdefault("history", []).append({
            "node": "repo_hunter",
            "action": f"Discovered {len(valid_repos)} reference repositories for: '{task}'"
        })
        return state
