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
    # Which file this was actually loaded from. A security control whose location is ambiguous
    # is a security control you cannot audit, so the resolved path travels with the allowlist.
    source_path: str = ""

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


CANONICAL_ALLOWLIST_FILENAME = "tooluniverse_allowlist.v1.json"


def packaged_allowlist_path() -> Path:
    """The single canonical allowlist: the copy inside this package.

    It is present in the editable/repo layout and ships in the wheel (hatchling includes the
    whole package directory), so one path is correct in every deployment.
    """
    return Path(__file__).resolve().parents[1] / "data" / CANONICAL_ALLOWLIST_FILENAME


def resolve_allowlist_path(explicit_path: str | None = None) -> Path:
    """Resolve the allowlist location. Exactly two cases, and neither consults the CWD.

    Previously three candidates were tried in order, the second being
    ``Path.cwd() / "config" / ...``. In a packaged deployment the first candidate misses, so a
    file in whatever directory the process happened to start in would silently override the
    vetted packaged allowlist — for the control that decides which external tools may execute.
    Operators who genuinely need a different allowlist set BIOSTACK_RESEARCH_TOOLUNIVERSE_ALLOWLIST_PATH
    explicitly; there is no implicit discovery.
    """
    if explicit_path:
        path = Path(explicit_path).expanduser().resolve()
        if not path.is_file():
            raise FileNotFoundError(
                f"Configured tooluniverse_allowlist_path does not exist: {path}"
            )
        return path

    packaged = packaged_allowlist_path()
    if not packaged.is_file():
        raise FileNotFoundError(
            f"Packaged allowlist missing at {packaged}. The sidecar refuses to run without "
            "an allowlist rather than falling back to an unvetted one."
        )
    return packaged


@lru_cache
def load_allowlist(path: str | None = None) -> ToolUniverseAllowlist:
    allowlist_path = resolve_allowlist_path(path)
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
        source_path=str(allowlist_path),
    )
