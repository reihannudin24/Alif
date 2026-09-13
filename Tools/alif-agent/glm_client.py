#!/usr/bin/env python3
"""
GLM API client for the Alif Agent System.
Dual-model routing: GLM-5.3 plans, GLM-5.3-Flash works and evaluates vision input.

Stdlib-only (urllib) OpenAI-compatible client against the Z.ai chat completions
endpoint. All credentials come from the environment and are never logged:
- ZAI_API_KEY / ZHIPUAI_API_KEY  -> bearer token
- ALIF_GLM_BASE_URL              -> API root (default https://api.z.ai/api/paas/v4)
- ALIF_PLANNER_MODEL             -> planner model (default glm-5.3)
- ALIF_WORKER_MODEL              -> worker/vision model (default glm-5.3-flash)

When no key is present `available()` is False and callers fall back to the
deterministic keyword pipeline, so the graph always runs.
"""

import base64
import json
import os
import time
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any, Dict, List, Optional

DEFAULT_BASE_URL = "https://api.z.ai/api/paas/v4"
DEFAULT_PLANNER_MODEL = "glm-5.3"
DEFAULT_WORKER_MODEL = "glm-5.3-flash"
REQUEST_TIMEOUT_SECONDS = 120
MAX_RETRIES = 2
MAX_IMAGE_BYTES = 8 * 1024 * 1024  # per image, after base64 encoding


class GlmError(RuntimeError):
    """Raised when the GLM API call cannot be completed or parsed."""


def _env(name: str, default: str) -> str:
    value = os.environ.get(name, "").strip()
    return value or default


class GlmClient:
    """Minimal OpenAI-compatible chat client with JSON and vision helpers."""

    def __init__(self, api_key: Optional[str] = None, base_url: Optional[str] = None,
                 planner_model: Optional[str] = None, worker_model: Optional[str] = None):
        self.api_key = (api_key or os.environ.get("ZAI_API_KEY") or
                        os.environ.get("ZHIPUAI_API_KEY") or "").strip()
        self.base_url = (base_url or _env("ALIF_GLM_BASE_URL", DEFAULT_BASE_URL)).rstrip("/")
        self.planner_model = planner_model or _env("ALIF_PLANNER_MODEL", DEFAULT_PLANNER_MODEL)
        self.worker_model = worker_model or _env("ALIF_WORKER_MODEL", DEFAULT_WORKER_MODEL)

    def available(self) -> bool:
        return bool(self.api_key)

    def config_summary(self) -> Dict[str, Any]:
        return {
            "base_url": self.base_url,
            "planner_model": self.planner_model,
            "worker_model": self.worker_model,
            "api_key_present": self.available()
        }

    def chat(self, messages: List[Dict[str, Any]], model: Optional[str] = None,
             temperature: float = 0.3, max_tokens: int = 4096) -> str:
        if not self.available():
            raise GlmError("GLM API key missing (set ZAI_API_KEY)")
        payload = {
            "model": model or self.worker_model,
            "messages": messages,
            "temperature": temperature,
            "max_tokens": max_tokens
        }
        body = json.dumps(payload).encode("utf-8")
        last_error: Optional[Exception] = None
        for attempt in range(MAX_RETRIES):
            try:
                return self._post(body)
            except GlmError as e:
                last_error = e
                if "HTTP 4" in str(e) and "429" not in str(e):
                    raise  # client errors other than rate limit are not retryable
                time.sleep(1.5 * (attempt + 1))
        raise GlmError(f"GLM API failed after {MAX_RETRIES} attempts: {last_error}")

    def _post(self, body: bytes) -> str:
        request = urllib.request.Request(
            f"{self.base_url}/chat/completions",
            data=body,
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {self.api_key}"
            },
            method="POST"
        )
        try:
            with urllib.request.urlopen(request, timeout=REQUEST_TIMEOUT_SECONDS) as response:
                data = json.loads(response.read().decode("utf-8"))
        except urllib.error.HTTPError as e:
            raise GlmError(f"GLM API HTTP {e.code}: {e.reason}") from e
        except (urllib.error.URLError, TimeoutError, json.JSONDecodeError) as e:
            raise GlmError(f"GLM API unreachable: {e}") from e
        try:
            return data["choices"][0]["message"]["content"]
        except (KeyError, IndexError, TypeError) as e:
            raise GlmError(f"GLM API unexpected response shape: {e}") from e

    def chat_json(self, messages: List[Dict[str, Any]], model: Optional[str] = None,
                  temperature: float = 0.2, max_tokens: int = 4096) -> Any:
        """Chat expecting a JSON response; strips code fences and re-asks once on bad JSON."""
        raw = self.chat(messages, model=model, temperature=temperature, max_tokens=max_tokens)
        parsed = _parse_json_lenient(raw)
        if parsed is not None:
            return parsed
        retry_messages = messages + [
            {"role": "assistant", "content": raw},
            {"role": "user", "content": "That was not valid JSON. Respond again with valid JSON only, no prose, no code fences."}
        ]
        raw = self.chat(retry_messages, model=model, temperature=0.0, max_tokens=max_tokens)
        parsed = _parse_json_lenient(raw)
        if parsed is None:
            raise GlmError("GLM API returned unparseable JSON after retry")
        return parsed

    def vision(self, prompt: str, images: List[Path], model: Optional[str] = None,
               temperature: float = 0.1, max_tokens: int = 4096) -> str:
        """Sends text plus PNG screenshots as image_url content blocks (worker/vision model)."""
        if not images:
            raise GlmError("vision() requires at least one image")
        content: List[Dict[str, Any]] = [{"type": "text", "text": prompt}]
        for image in images:
            data_uri = _image_data_uri(image)
            if data_uri:
                content.append({"type": "image_url", "image_url": {"url": data_uri}})
        if len(content) == 1:
            raise GlmError("No readable images provided to vision()")
        return self.chat(
            [{"role": "user", "content": content}],
            model=model or self.worker_model,
            temperature=temperature,
            max_tokens=max_tokens
        )

    def vision_json(self, prompt: str, images: List[Path], model: Optional[str] = None) -> Any:
        raw = self.vision(prompt, images, model=model)
        parsed = _parse_json_lenient(raw)
        if parsed is not None:
            return parsed
        raise GlmError("GLM vision response was not valid JSON")


def _parse_json_lenient(raw: str) -> Optional[Any]:
    """Parses JSON possibly wrapped in markdown fences or leading prose."""
    if not raw:
        return None
    text = raw.strip()
    if text.startswith("```"):
        text = text.strip("`")
        if text.lower().startswith("json"):
            text = text[4:]
        text = text.strip()
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        pass
    # Fall back to the outermost JSON object/array embedded in prose.
    for opener, closer in (("{", "}"), ("[", "]")):
        start = text.find(opener)
        end = text.rfind(closer)
        if start != -1 and end > start:
            try:
                return json.loads(text[start:end + 1])
            except json.JSONDecodeError:
                continue
    return None


def _image_data_uri(path: Path) -> Optional[str]:
    try:
        raw = path.read_bytes()
    except OSError:
        return None
    if len(raw) * 4 // 3 > MAX_IMAGE_BYTES:
        return None  # caller filters oversized images before evaluation
    encoded = base64.b64encode(raw).decode("ascii")
    return f"data:image/png;base64,{encoded}"


if __name__ == "__main__":
    client = GlmClient()
    summary = client.config_summary()
    print("GLM client config:")
    for key, value in summary.items():
        print(f"  {key}: {value}")
    print(f"\navailable: {client.available()}")
