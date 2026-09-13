#!/usr/bin/env python3
"""
GitHub Semantic Repo Finder (find_repo.py)
------------------------------------------
Performs semantic and natural language search for GitHub repositories,
discovers reference implementations and peer projects, and evaluates
repository architecture and health for app development.
"""

import sys
import os
import re
import json
import math
import subprocess
import urllib.request
import urllib.parse
from datetime import datetime, timezone
import argparse

# Stop words to filter out during query decomposition
STOP_WORDS = {
    "a", "about", "above", "after", "again", "against", "all", "am", "an", "and",
    "any", "app", "application", "apps", "are", "aren't", "as", "at", "be", "because",
    "been", "before", "being", "below", "between", "both", "build", "building", "but",
    "by", "can", "cannot", "could", "couldn't", "create", "creating", "did", "didn't",
    "do", "does", "doesn't", "doing", "don't", "down", "during", "each", "few",
    "find", "for", "from", "further", "get", "had", "hadn't", "has", "hasn't", "have",
    "haven't", "having", "he", "help", "how", "i", "i'd", "i'll", "i'm", "i've",
    "if", "in", "into", "is", "isn't", "it", "its", "itself", "just", "like", "look",
    "looking", "make", "making", "me", "more", "most", "my", "myself", "need", "no",
    "nor", "not", "of", "off", "on", "once", "only", "or", "other", "ought", "our",
    "ours", "ourselves", "out", "over", "own", "project", "repo", "repository", "same",
    "search", "show", "so", "some", "such", "than", "that", "the", "their", "theirs",
    "them", "themselves", "then", "there", "these", "they", "this", "those", "through",
    "to", "too", "under", "until", "up", "very", "want", "was", "wasn't", "we",
    "were", "weren't", "what", "when", "where", "which", "while", "who", "whom",
    "why", "with", "won't", "would", "wouldn't", "you", "your", "yours", "yourself"
}

# Common languages and technologies mapping
KNOWN_LANGUAGES = {
    "typescript": "TypeScript",
    "javascript": "JavaScript",
    "python": "Python",
    "rust": "Rust",
    "go": "Go",
    "golang": "Go",
    "swift": "Swift",
    "kotlin": "Kotlin",
    "java": "Java",
    "c++": "C++",
    "cpp": "C++",
    "c#": "C#",
    "csharp": "C#",
    "ruby": "Ruby",
    "php": "PHP",
    "dart": "Dart",
    "elixir": "Elixir",
    "zig": "Zig",
    "lua": "Lua",
    "svelte": "Svelte",
    "vue": "Vue",
    "html": "HTML",
    "css": "CSS"
}

# Framework and topic synonyms / associations
SYNONYM_MAP = {
    "auth": ["authentication", "oauth", "jwt", "login", "sso"],
    "kanban": ["trello", "board", "task-manager", "issue-tracker"],
    "chat": ["messaging", "messenger", "conversational", "llm-chat", "chatbot"],
    "llm": ["ai", "gpt", "large-language-model", "local-llm", "ollama", "rag"],
    "desktop": ["tauri", "electron", "wails", "gui"],
    "mobile": ["react-native", "flutter", "ios", "android"],
    "ui": ["components", "design-system", "tailwind", "radix", "component-library"],
    "database": ["db", "sqlite", "postgres", "vector-database", "embedded-db"],
    "api": ["rest", "graphql", "grpc", "backend", "fastapi"],
    "dashboard": ["admin", "admin-panel", "analytics", "portal"],
    "boilerplate": ["starter", "template", "starter-kit", "skeleton"],
    "workflow": ["pipeline", "automation", "orchestration", "dag"]
}


def call_github_api(endpoint, params=None):
    """
    Calls GitHub API using gh CLI if available, otherwise falls back to urllib.
    """
    query_str = ""
    if params:
        query_str = "?" + urllib.parse.urlencode(params)
    full_path = endpoint.lstrip("/") + query_str

    # 1. Try gh api CLI first (has keyring auth and high rate limit)
    try:
        cmd = ["gh", "api", full_path]
        res = subprocess.run(cmd, capture_output=True, text=True, timeout=15)
        if res.returncode == 0:
            return json.loads(res.stdout)
    except (subprocess.SubprocessError, FileNotFoundError, json.JSONDecodeError):
        pass

    # 2. Fallback to direct HTTP request
    url = f"https://api.github.com/{full_path}"
    headers = {
        "Accept": "application/vnd.github+json",
        "User-Agent": "Antigravity-Find-Repo-Skill/1.0"
    }
    token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
    if token:
        headers["Authorization"] = f"Bearer {token}"

    req = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        error_body = e.read().decode("utf-8", errors="ignore")
        try:
            err_json = json.loads(error_body)
            msg = err_json.get("message", str(e))
        except Exception:
            msg = str(e)
        return {"error": f"GitHub API error ({e.code}): {msg}"}
    except Exception as e:
        return {"error": f"Failed to connect to GitHub API: {str(e)}"}


def decompose_query(user_query, explicit_lang=None):
    """
    Decomposes natural language query into intent concepts, detected language,
    keywords, and candidate GitHub search queries.
    """
    normalized = user_query.lower()
    tokens = re.findall(r"[a-z0-9+#.-]+", normalized)

    detected_lang = explicit_lang
    if not detected_lang:
        for token in tokens:
            if token in KNOWN_LANGUAGES:
                detected_lang = KNOWN_LANGUAGES[token]
                break

    # Check for template/starter intent
    is_template_intent = any(t in tokens for t in ["template", "starter", "boilerplate", "skeleton"])

    # Meaningful core tokens (excluding stop words and detected language name)
    core_tokens = []
    for t in tokens:
        if t in STOP_WORDS:
            continue
        if detected_lang and t == detected_lang.lower():
            continue
        if len(t) > 1:
            core_tokens.append(t)

    # Collect topic/synonym expansions
    expanded_terms = set()
    for token in core_tokens:
        for key, syns in SYNONYM_MAP.items():
            if token == key or token in syns:
                expanded_terms.update(syns[:2])

    search_variations = []

    # Variation 1: Strict core keywords + language
    q1_parts = core_tokens[:4]
    if detected_lang:
        q1_parts.append(f"language:{detected_lang}")
    if is_template_intent:
        q1_parts.append("topic:template")
    q1_parts.append("archived:false")
    search_variations.append(" ".join(q1_parts))

    # Variation 2: Topic-oriented query
    primary_topic = core_tokens[0] if core_tokens else "awesome"
    q2_parts = [f"topic:{primary_topic}"]
    if len(core_tokens) > 1:
        q2_parts.extend(core_tokens[1:3])
    if detected_lang:
        q2_parts.append(f"language:{detected_lang}")
    q2_parts.append("archived:false")
    search_variations.append(" ".join(q2_parts))

    # Variation 3: In description & name search
    if core_tokens:
        main_phrase = "+".join(core_tokens[:3])
        q3 = f"{main_phrase} in:name,description"
        if detected_lang:
            q3 += f" language:{detected_lang}"
        q3 += " archived:false"
        search_variations.append(q3)

    # Variation 4: Synonyms / related topic query
    if expanded_terms:
        syn_term = list(expanded_terms)[0]
        q4_parts = [syn_term] + core_tokens[:2]
        if detected_lang:
            q4_parts.append(f"language:{detected_lang}")
        q4_parts.append("archived:false")
        search_variations.append(" ".join(q4_parts))

    return {
        "original_query": user_query,
        "detected_language": detected_lang,
        "core_tokens": core_tokens,
        "expanded_terms": list(expanded_terms),
        "is_template_intent": is_template_intent,
        "search_variations": search_variations
    }


def compute_repo_score(repo, core_tokens, expanded_terms):
    """
    Computes a composite semantic and quality score for ranking repositories.
    """
    score = 0.0
    name = (repo.get("name") or "").lower()
    full_name = (repo.get("full_name") or "").lower()
    description = (repo.get("description") or "").lower()
    topics = [t.lower() for t in repo.get("topics", [])]
    stars = repo.get("stargazers_count", 0)
    pushed_at = repo.get("pushed_at")

    # 1. Textual relevance to core tokens
    for token in core_tokens:
        if token in name:
            score += 8.0
        elif token in full_name:
            score += 4.0
        if token in topics:
            score += 6.0
        if token in description:
            score += 3.0

    # 2. Match with expanded synonyms
    for term in expanded_terms:
        if term in topics:
            score += 3.0
        elif term in description:
            score += 1.5

    # 3. Popularity score (log scaled)
    if stars > 0:
        score += min(15.0, round(math.log10(stars + 1) * 3.5, 1))

    # 4. Freshness / Maintenance score
    if pushed_at:
        try:
            # ISO timestamp e.g. 2026-02-18T06:41:37Z
            pushed_dt = datetime.fromisoformat(pushed_at.replace("Z", "+00:00"))
            now_dt = datetime.now(timezone.utc)
            days_ago = (now_dt - pushed_dt).days
            if days_ago <= 30:
                score += 8.0
            elif days_ago <= 90:
                score += 6.0
            elif days_ago <= 180:
                score += 4.0
            elif days_ago <= 365:
                score += 2.0
            elif days_ago > 730:
                score -= 4.0
        except Exception:
            pass

    # 5. Health & Quality signals
    if repo.get("license"):
        score += 2.0
    if len(topics) >= 3:
        score += 2.0
    if repo.get("has_issues", False):
        score += 1.0
    if repo.get("is_template", False):
        score += 2.0

    return round(score, 2)


def search_repositories(query, language=None, min_stars=0, limit=10, is_template=False, sort_by="best-match"):
    """
    Executes semantic search over GitHub repositories using query decomposition,
    multi-query fanout, deduplication, and composite reranking.
    """
    analysis = decompose_query(query, explicit_lang=language)
    seen_repos = {}

    for var_query in analysis["search_variations"]:
        q_clean = var_query
        if min_stars > 0 and "stars:" not in q_clean:
            q_clean += f" stars:>={min_stars}"
        if is_template and "is:template" not in q_clean:
            q_clean += " is:template"

        params = {
            "q": q_clean,
            "per_page": min(30, max(15, limit * 2))
        }
        if sort_by in ["stars", "updated", "forks"]:
            params["sort"] = sort_by
            params["order"] = "desc"

        data = call_github_api("search/repositories", params)
        if "items" in data:
            for item in data["items"]:
                repo_id = item["full_name"]
                if repo_id not in seen_repos:
                    score = compute_repo_score(item, analysis["core_tokens"], analysis["expanded_terms"])
                    seen_repos[repo_id] = {
                        "repo": item,
                        "score": score
                    }

    # Sort results
    if sort_by == "stars":
        ranked = sorted(seen_repos.values(), key=lambda x: x["repo"].get("stargazers_count", 0), reverse=True)
    elif sort_by == "updated":
        ranked = sorted(seen_repos.values(), key=lambda x: x["repo"].get("pushed_at", ""), reverse=True)
    else:
        ranked = sorted(seen_repos.values(), key=lambda x: x["score"], reverse=True)

    results = []
    for entry in ranked[:limit]:
        item = entry["repo"]
        results.append({
            "full_name": item.get("full_name"),
            "url": item.get("html_url"),
            "description": item.get("description") or "No description provided.",
            "language": item.get("language") or "Unknown",
            "stars": item.get("stargazers_count", 0),
            "forks": item.get("forks_count", 0),
            "open_issues": item.get("open_issues_count", 0),
            "topics": item.get("topics", []),
            "license": item.get("license", {}).get("spdx_id") if item.get("license") else None,
            "pushed_at": item.get("pushed_at"),
            "is_template": item.get("is_template", False),
            "match_score": entry["score"]
        })

    return {
        "query": query,
        "query_analysis": analysis,
        "total_candidates_analyzed": len(seen_repos),
        "results": results
    }


def find_similar_repositories(target_repo, language=None, limit=10):
    """
    Finds repositories similar or alternative to a given target repository.
    """
    target_data = call_github_api(f"repos/{target_repo}")
    if "error" in target_data or "full_name" not in target_data:
        return {"error": f"Could not retrieve target repository '{target_repo}': {target_data.get('error', 'Not found')}"}

    target_name = target_data.get("name", "")
    target_topics = target_data.get("topics", [])
    target_lang = language or target_data.get("language")
    target_desc = target_data.get("description") or ""

    # Build similarity queries
    queries = []

    # 1. Topic overlap
    if len(target_topics) >= 2:
        top_topics = " ".join([f"topic:{t}" for t in target_topics[:3]])
        q = f"{top_topics} archived:false"
        if language:
            q += f" language:{language}"
        queries.append(q)

    # 2. Named alternatives ("alternative to X" or "X clone" or "X like")
    q_alt = f'"{target_name}" in:name,description archived:false'
    if language:
        q_alt += f" language:{language}"
    queries.append(q_alt)

    # 3. Domain description tokens + target language
    desc_tokens = [t for t in re.findall(r"[a-z0-9]+", target_desc.lower()) if t not in STOP_WORDS and len(t) > 2]
    if desc_tokens:
        q_domain = " ".join(desc_tokens[:3]) + " archived:false"
        if target_lang:
            q_domain += f" language:{target_lang}"
        queries.append(q_domain)

    seen = {}
    target_full_name = target_data["full_name"].lower()

    for q_str in queries:
        params = {"q": q_str, "sort": "stars", "order": "desc", "per_page": 20}
        data = call_github_api("search/repositories", params)
        if "items" in data:
            for item in data["items"]:
                fn = item["full_name"].lower()
                if fn == target_full_name:
                    continue  # skip self
                if fn not in seen:
                    # Calculate similarity overlap
                    item_topics = [t.lower() for t in item.get("topics", [])]
                    shared_topics = set(t.lower() for t in target_topics).intersection(item_topics)
                    score = len(shared_topics) * 5.0
                    if target_name.lower() in (item.get("description") or "").lower():
                        score += 6.0
                    if item.get("language") == target_lang:
                        score += 3.0
                    stars = item.get("stargazers_count", 0)
                    if stars > 0:
                        score += min(10.0, math.log10(stars + 1) * 2.5)

                    seen[fn] = {
                        "repo": item,
                        "shared_topics": list(shared_topics),
                        "score": round(score, 2)
                    }

    ranked = sorted(seen.values(), key=lambda x: x["score"], reverse=True)

    results = []
    for entry in ranked[:limit]:
        item = entry["repo"]
        results.append({
            "full_name": item.get("full_name"),
            "url": item.get("html_url"),
            "description": item.get("description") or "No description provided.",
            "language": item.get("language") or "Unknown",
            "stars": item.get("stargazers_count", 0),
            "shared_topics": entry["shared_topics"],
            "license": item.get("license", {}).get("spdx_id") if item.get("license") else None,
            "pushed_at": item.get("pushed_at"),
            "similarity_score": entry["score"]
        })

    return {
        "target_repo": {
            "full_name": target_data.get("full_name"),
            "stars": target_data.get("stargazers_count", 0),
            "language": target_data.get("language"),
            "topics": target_topics,
            "description": target_desc
        },
        "results": results
    }


def inspect_repository(repo_path):
    """
    Fetches comprehensive architectural and health overview of a specific repository.
    """
    repo_data = call_github_api(f"repos/{repo_path}")
    if "error" in repo_data or "full_name" not in repo_data:
        return {"error": f"Could not inspect '{repo_path}': {repo_data.get('error', 'Not found')}"}

    # Languages breakdown
    languages_data = call_github_api(f"repos/{repo_path}/languages")
    if isinstance(languages_data, dict) and "error" not in languages_data:
        total_bytes = sum(languages_data.values())
        lang_percent = {}
        if total_bytes > 0:
            for l_name, b_count in sorted(languages_data.items(), key=lambda x: x[1], reverse=True)[:5]:
                lang_percent[l_name] = f"{round((b_count / total_bytes) * 100, 1)}%"
    else:
        lang_percent = {repo_data.get("language") or "Unknown": "100%"}

    # Top-level directory contents
    contents_data = call_github_api(f"repos/{repo_path}/contents")
    top_level_files = []
    if isinstance(contents_data, list):
        for item in contents_data:
            icon = "📁" if item.get("type") == "dir" else "📄"
            top_level_files.append(f"{icon} {item.get('name')}")

    # Fetch README snippet
    readme_excerpt = "No README found."
    try:
        # Use gh repo view if available for clean plain-text markdown
        res = subprocess.run(["gh", "repo", "view", repo_path], capture_output=True, text=True, timeout=10)
        if res.returncode == 0 and res.stdout.strip():
            lines = res.stdout.strip().splitlines()
            readme_excerpt = "\n".join(lines[:40])
            if len(lines) > 40:
                readme_excerpt += "\n... (truncated)"
    except Exception:
        pass

    # Health assessment
    pushed_at = repo_data.get("pushed_at")
    status = "Active"
    if repo_data.get("archived", False):
        status = "Archived (Read-Only)"
    elif pushed_at:
        try:
            pushed_dt = datetime.fromisoformat(pushed_at.replace("Z", "+00:00"))
            days_ago = (datetime.now(timezone.utc) - pushed_dt).days
            if days_ago > 730:
                status = "Dormant (> 2 years inactive)"
            elif days_ago > 365:
                status = "Stale (1-2 years inactive)"
        except Exception:
            pass

    license_info = repo_data.get("license")
    license_name = license_info.get("name") if license_info else "No license detected"

    return {
        "full_name": repo_data.get("full_name"),
        "url": repo_data.get("html_url"),
        "description": repo_data.get("description") or "No description.",
        "stars": repo_data.get("stargazers_count", 0),
        "forks": repo_data.get("forks_count", 0),
        "open_issues": repo_data.get("open_issues_count", 0),
        "default_branch": repo_data.get("default_branch", "main"),
        "pushed_at": pushed_at,
        "health_status": status,
        "is_template": repo_data.get("is_template", False),
        "license": license_name,
        "homepage": repo_data.get("homepage"),
        "topics": repo_data.get("topics", []),
        "languages": lang_percent,
        "top_level_files": top_level_files[:15],
        "readme_excerpt": readme_excerpt
    }


def format_search_markdown(data):
    """Formats search results as Markdown."""
    out = []
    out.append(f"## 🔍 GitHub Semantic Search: \"{data['query']}\"\n")
    analysis = data["query_analysis"]
    meta_parts = []
    if analysis["detected_language"]:
        meta_parts.append(f"**Detected Language:** `{analysis['detected_language']}`")
    if analysis["core_tokens"]:
        meta_parts.append(f"**Core Concepts:** {', '.join([f'`{t}`' for t in analysis['core_tokens']])}")
    meta_parts.append(f"**Candidates Analyzed:** {data['total_candidates_analyzed']}")
    out.append(" | ".join(meta_parts) + "\n")

    if not data["results"]:
        out.append("No matching repositories found. Try broadening your keywords or removing filters.")
        return "\n".join(out)

    out.append("| Repository | Stars | Language | License | Last Updated | Description |")
    out.append("| :--- | :--- | :--- | :--- | :--- | :--- |")
    for r in data["results"]:
        date_str = r['pushed_at'][:10] if r.get('pushed_at') else "N/A"
        lic = r['license'] or "None"
        desc = (r['description'][:90] + "...") if len(r['description']) > 90 else r['description']
        desc = desc.replace("|", "/")
        out.append(f"| [{r['full_name']}]({r['url']}) | ⭐ {r['stars']:,} | `{r['language']}` | {lic} | {date_str} | {desc} |")

    out.append("\n### Key Takeaways for Developers:")
    for idx, r in enumerate(data["results"][:3], 1):
        out.append(f"{idx}. **[{r['full_name']}]({r['url']})** ({r['language']}, ⭐ {r['stars']:,})")
        if r['topics']:
            out.append(f"   - **Topics:** {', '.join([f'`{t}`' for t in r['topics'][:6]])}")
        out.append(f"   - **Relevance:** Match score `{r['match_score']}` — {r['description']}")

    return "\n".join(out)


def format_similar_markdown(data):
    """Formats similarity search results as Markdown."""
    if "error" in data:
        return f"❌ Error: {data['error']}"

    target = data["target_repo"]
    out = []
    out.append(f"## 🔄 Repositories Similar to [{target['full_name']}](https://github.com/{target['full_name']})\n")
    out.append(f"> **Target Stack:** `{target['language']}` | ⭐ {target['stars']:,} stars")
    if target["topics"]:
        out.append(f"> **Target Topics:** {', '.join([f'`{t}`' for t in target['topics'][:6]])}\n")

    if not data["results"]:
        out.append("No peer or alternative repositories found.")
        return "\n".join(out)

    out.append("| Similar Repository | Stars | Language | Overlapping Topics | Last Updated | Description |")
    out.append("| :--- | :--- | :--- | :--- | :--- | :--- |")
    for r in data["results"]:
        date_str = r['pushed_at'][:10] if r.get('pushed_at') else "N/A"
        shared = ", ".join([f"`{t}`" for t in r['shared_topics'][:3]]) or "Domain match"
        desc = (r['description'][:80] + "...") if len(r['description']) > 80 else r['description']
        desc = desc.replace("|", "/")
        out.append(f"| [{r['full_name']}]({r['url']}) | ⭐ {r['stars']:,} | `{r['language']}` | {shared} | {date_str} | {desc} |")

    return "\n".join(out)


def format_inspect_markdown(data):
    """Formats repository inspection as Markdown."""
    if "error" in data:
        return f"❌ Error: {data['error']}"

    out = []
    out.append(f"# 📦 Repository Architecture Overview: [{data['full_name']}]({data['url']})\n")
    out.append(f"> {data['description']}\n")

    out.append("### 📊 Health & Metrics")
    out.append(f"- **Stars:** ⭐ {data['stars']:,} | **Forks:** 🍴 {data['forks']:,} | **Open Issues:** ❗ {data['open_issues']:,}")
    out.append(f"- **Maintenance Status:** `{data['health_status']}` (Last pushed: {data['pushed_at'][:10] if data['pushed_at'] else 'N/A'})")
    out.append(f"- **License:** `{data['license']}`")
    if data["is_template"]:
        out.append("- **Repository Template:** Yes (Can be used via *Use this template*)")
    if data["homepage"]:
        out.append(f"- **Homepage/Demo:** [{data['homepage']}]({data['homepage']})")

    out.append("\n### 💻 Language Breakdown")
    for lang, pct in data["languages"].items():
        out.append(f"- `{lang}`: {pct}")

    if data["topics"]:
        out.append("\n### 🏷️ Topics")
        out.append(", ".join([f"`{t}`" for t in data["topics"]]))

    if data["top_level_files"]:
        out.append("\n### 🗂️ Top-Level Structure")
        out.append("```text")
        out.append("\n".join(data["top_level_files"]))
        out.append("```")

    if data["readme_excerpt"] and data["readme_excerpt"] != "No README found.":
        out.append("\n### 📖 README Excerpt")
        out.append("```markdown")
        out.append(data["readme_excerpt"])
        out.append("```")

    return "\n".join(out)


def main():
    parser = argparse.ArgumentParser(description="GitHub Semantic Repo Finder for App Developers")
    subparsers = parser.add_subparsers(dest="command", help="Command to run")

    # Search subcommand
    search_p = subparsers.add_parser("search", help="Search repositories with natural language intent")
    search_p.add_argument("query", help="Natural language search prompt or requirement")
    search_p.add_argument("-l", "--language", help="Filter by programming language")
    search_p.add_argument("--min-stars", type=int, default=0, help="Minimum stars threshold")
    search_p.add_argument("-n", "--limit", type=int, default=10, help="Number of results to return")
    search_p.add_argument("--template", action="store_true", help="Only return template repositories")
    search_p.add_argument("--sort", choices=["best-match", "stars", "updated"], default="best-match", help="Sort order")
    search_p.add_argument("--json", action="store_true", help="Output raw JSON format")

    # Similar subcommand
    similar_p = subparsers.add_parser("similar", help="Find repositories similar to a reference repo")
    similar_p.add_argument("repo", help="Target repository in 'owner/repo' format")
    similar_p.add_argument("-l", "--language", help="Filter similar repos by specific language")
    similar_p.add_argument("-n", "--limit", type=int, default=10, help="Number of results to return")
    similar_p.add_argument("--json", action="store_true", help="Output raw JSON format")

    # Inspect subcommand
    inspect_p = subparsers.add_parser("inspect", help="Deeply inspect a repository's architecture and health")
    inspect_p.add_argument("repo", help="Target repository in 'owner/repo' format")
    inspect_p.add_argument("--json", action="store_true", help="Output raw JSON format")

    args = parser.parse_args()

    if not args.command:
        parser.print_help()
        sys.exit(1)

    if args.command == "search":
        res = search_repositories(
            args.query,
            language=args.language,
            min_stars=args.min_stars,
            limit=args.limit,
            is_template=args.template,
            sort_by=args.sort
        )
        if args.json:
            print(json.dumps(res, indent=2))
        else:
            print(format_search_markdown(res))

    elif args.command == "similar":
        res = find_similar_repositories(args.repo, language=args.language, limit=args.limit)
        if args.json:
            print(json.dumps(res, indent=2))
        else:
            print(format_similar_markdown(res))

    elif args.command == "inspect":
        res = inspect_repository(args.repo)
        if args.json:
            print(json.dumps(res, indent=2))
        else:
            print(format_inspect_markdown(res))


if __name__ == "__main__":
    main()
