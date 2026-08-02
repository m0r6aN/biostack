"""ToolUniverse allowlist loading and enforcement."""

from __future__ import annotations

import json
from dataclasses import dataclass
from functools import lru_cache
from pathlib import Path


class AllowlistViolation(ValueError):
    def __init__(self, code: str, message: str) -> None:
        super().__init__(message)
        self.code = code
        self.message = message


@dataclass(frozen=True)
class ToolUniverseAllowlist:
    allowlist_version: str
    package_version: str
    approved_skills: frozenset[str]
    approved_tools: frozenset[str]
    approved_categories: frozenset[str]
    workflow_to_skills: dict[str, tuple[str, ...]]

    def assert_tool_allowed(self, tool_name: str) -> None:
        if tool_name not in self.approved_tools:
            raise AllowlistViolation(
                "tool_not_allowlisted",
                f"Tool '{tool_name}' is not on the BioStack ToolUniverse allowlist "
                f"(v{self.allowlist_version}).",
            )

    def assert_skill_allowed(self, skill_name: str) -> None:
        if skill_name not in self.approved_skills:
            raise AllowlistViolation(
                "skill_not_allowlisted",
                f"Skill '{skill_name}' is not on the BioStack ToolUniverse allowlist "
                f"(v{self.allowlist_version}).",
            )

    def skills_for_workflow(self, workflow: str) -> tuple[str, ...]:
        return self.workflow_to_skills.get(workflow, ())


def _default_allowlist_path() -> Path:
    here = Path(__file__).resolve()
    candidates = [
        # Editable/repo layout
        here.parents[3] / "config" / "tooluniverse_allowlist.v1.json",
        Path.cwd() / "config" / "tooluniverse_allowlist.v1.json",
        # Packaged wheel layout
        here.parents[1] / "data" / "tooluniverse_allowlist.v1.json",
    ]
    for path in candidates:
        if path.is_file():
            return path
    raise FileNotFoundError(
        "tooluniverse_allowlist.v1.json not found under config/ or package data/."
    )


@lru_cache
def load_allowlist(path: str | None = None) -> ToolUniverseAllowlist:
    allowlist_path = Path(path) if path else _default_allowlist_path()
    raw = json.loads(allowlist_path.read_text(encoding="utf-8"))
    workflow_map = {
        key: tuple(values)
        for key, values in (raw.get("workflowToSkills") or {}).items()
    }
    return ToolUniverseAllowlist(
        allowlist_version=str(raw["allowlistVersion"]),
        package_version=str(raw["tooluniversePackageVersion"]),
        approved_skills=frozenset(raw.get("approvedSkills") or []),
        approved_tools=frozenset(raw.get("approvedTools") or []),
        approved_categories=frozenset(raw.get("approvedCategoriesForPreload") or []),
        workflow_to_skills=workflow_map,
    )
