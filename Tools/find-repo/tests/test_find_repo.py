#!/usr/bin/env python3
"""
Unit tests for find_repo.py
"""

import unittest
import sys
import os

# Add scripts directory to path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

import find_repo


class TestFindRepoQueryDecomposition(unittest.TestCase):
    def test_decompose_query_language_detection(self):
        query = "Build a lightweight web server in Rust"
        res = find_repo.decompose_query(query)
        self.assertEqual(res["detected_language"], "Rust")
        self.assertIn("server", res["core_tokens"])
        self.assertIn("web", res["core_tokens"])
        self.assertIn("lightweight", res["core_tokens"])
        self.assertNotIn("in", res["core_tokens"])
        self.assertNotIn("a", res["core_tokens"])

    def test_decompose_template_intent(self):
        query = "Next.js starter template with supabase and tailwind"
        res = find_repo.decompose_query(query)
        self.assertTrue(res["is_template_intent"])
        self.assertIn("supabase", res["core_tokens"])
        self.assertIn("tailwind", res["core_tokens"])

    def test_decompose_synonym_expansion(self):
        query = "kanban task board"
        res = find_repo.decompose_query(query)
        # Should expand kanban or board into trello, task-manager, etc.
        self.assertTrue(any(term in res["expanded_terms"] for term in ["trello", "task-manager", "issue-tracker", "board"]))

    def test_compute_repo_score(self):
        repo_sample = {
            "name": "nimbo",
            "full_name": "sereneblue/nimbo",
            "description": "A nimble offline Kanban board",
            "topics": ["kanban", "trello", "offline"],
            "stargazers_count": 150,
            "pushed_at": "2026-08-01T12:00:00Z",
            "license": {"spdx_id": "MIT"},
            "has_issues": True
        }
        score = find_repo.compute_repo_score(repo_sample, ["kanban", "offline"], ["trello"])
        self.assertGreater(score, 20.0)

    def test_formatting_search_markdown(self):
        mock_data = {
            "query": "test query",
            "query_analysis": {
                "detected_language": "TypeScript",
                "core_tokens": ["test", "query"],
                "expanded_terms": [],
                "is_template_intent": False
            },
            "total_candidates_analyzed": 5,
            "results": [
                {
                    "full_name": "user/repo",
                    "url": "https://github.com/user/repo",
                    "description": "A test repository",
                    "language": "TypeScript",
                    "stars": 120,
                    "forks": 15,
                    "open_issues": 2,
                    "topics": ["test"],
                    "license": "MIT",
                    "pushed_at": "2026-01-01T00:00:00Z",
                    "is_template": False,
                    "match_score": 25.5
                }
            ]
        }
        md = find_repo.format_search_markdown(mock_data)
        self.assertIn("user/repo", md)
        self.assertIn("TypeScript", md)
        self.assertIn("120", md)


if __name__ == "__main__":
    unittest.main()
