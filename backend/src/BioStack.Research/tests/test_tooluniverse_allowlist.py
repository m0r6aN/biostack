"""Allowlist and pin enforcement tests (no network required)."""

from __future__ import annotations

from pathlib import Path

import pytest

from biostack_research_sidecar.tooluniverse_integration.adapter import ToolUniverseAdapter
from biostack_research_sidecar.tooluniverse_integration.allowlist import (
    AllowlistViolation,
    load_allowlist,
)

ALLOWLIST_PATH = (
    Path(__file__).resolve().parents[1] / "config" / "tooluniverse_allowlist.v1.json"
)


def test_allowlist_loads_pin_version() -> None:
    allowlist = load_allowlist(str(ALLOWLIST_PATH))
    assert allowlist.package_version == "1.4.0"
    assert allowlist.allowlist_version == "1.0.0"
    assert "tooluniverse-chemical-compound-retrieval" in allowlist.approved_skills
    assert "PubMed_search_articles" in allowlist.approved_tools
    assert "ExecuteAnyTool" not in allowlist.approved_tools


def test_workflow_skill_mapping() -> None:
    allowlist = load_allowlist(str(ALLOWLIST_PATH))
    skills = allowlist.skills_for_workflow("research_adverse_events")
    assert "tooluniverse-adverse-event-detection" in skills
    assert "tooluniverse-pharmacovigilance" in skills


def test_unlisted_tool_rejected_without_network() -> None:
    allowlist = load_allowlist(str(ALLOWLIST_PATH))
    adapter = ToolUniverseAdapter(allowlist, expected_package_version="1.4.0")
    result = adapter.run_tool("Definitely_Not_A_Real_Tool", {"q": "x"})
    assert result.success is False
    assert result.error_code == "tool_not_allowlisted"


def test_assert_tool_allowed_raises() -> None:
    allowlist = load_allowlist(str(ALLOWLIST_PATH))
    with pytest.raises(AllowlistViolation) as exc:
        allowlist.assert_tool_allowed("shell_exec")
    assert exc.value.code == "tool_not_allowlisted"


def test_assert_skill_allowed_raises() -> None:
    allowlist = load_allowlist(str(ALLOWLIST_PATH))
    with pytest.raises(AllowlistViolation) as exc:
        allowlist.assert_skill_allowed("tooluniverse-precision-oncology")
    assert exc.value.code == "skill_not_allowlisted"
