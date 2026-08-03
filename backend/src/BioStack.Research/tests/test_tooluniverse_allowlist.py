"""Allowlist and pin enforcement tests (no network required)."""

from __future__ import annotations

import json
from pathlib import Path

import pytest

from biostack_research_sidecar.tooluniverse_integration.adapter import ToolUniverseAdapter
from biostack_research_sidecar.tooluniverse_integration.allowlist import (
    AllowlistViolation,
    load_allowlist,
    packaged_allowlist_path,
    resolve_allowlist_path,
)

# The canonical allowlist — the copy inside the package. Tests previously pointed at a second
# copy under config/, which is exactly the duplication these tests now guard against.
ALLOWLIST_PATH = packaged_allowlist_path()

LEGACY_CONFIG_COPY = (
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


# ── Resolution is deterministic and auditable ────────────────────────────────


def test_default_resolution_is_the_packaged_copy() -> None:
    assert resolve_allowlist_path() == packaged_allowlist_path()


def test_loaded_allowlist_reports_its_source() -> None:
    load_allowlist.cache_clear()
    allowlist = load_allowlist()
    assert allowlist.source_path == str(packaged_allowlist_path())
    load_allowlist.cache_clear()


def test_allowlist_resolution_ignores_working_directory(tmp_path, monkeypatch) -> None:
    """Security regression: a file in the CWD must never override the vetted allowlist.

    Resolution used to try ``Path.cwd() / "config" / tooluniverse_allowlist.v1.json`` before the
    packaged copy. In a container that meant whatever sat in the working directory decided which
    external tools could execute. This plants exactly that decoy and asserts it is ignored.
    """
    decoy_dir = tmp_path / "config"
    decoy_dir.mkdir()
    (decoy_dir / "tooluniverse_allowlist.v1.json").write_text(
        json.dumps(
            {
                "allowlistVersion": "666.0.0",
                "tooluniversePackageVersion": "0.0.0",
                "approvedSkills": [],
                "approvedTools": ["ExecuteAnyTool", "shell_exec"],
                "approvedCategoriesForPreload": [],
                "workflowToSkills": {},
            }
        ),
        encoding="utf-8",
    )

    monkeypatch.chdir(tmp_path)
    load_allowlist.cache_clear()

    allowlist = load_allowlist()

    assert allowlist.allowlist_version != "666.0.0"
    assert "ExecuteAnyTool" not in allowlist.approved_tools
    assert "shell_exec" not in allowlist.approved_tools

    load_allowlist.cache_clear()


def test_explicit_override_is_still_honoured(tmp_path) -> None:
    """Operators may point at a different allowlist — but only deliberately, never implicitly."""
    override = tmp_path / "custom_allowlist.json"
    override.write_text(packaged_allowlist_path().read_text(encoding="utf-8"), encoding="utf-8")

    assert resolve_allowlist_path(str(override)) == override.resolve()


def test_missing_explicit_override_fails_loudly(tmp_path) -> None:
    with pytest.raises(FileNotFoundError):
        resolve_allowlist_path(str(tmp_path / "nope.json"))


def test_legacy_config_copy_has_not_drifted() -> None:
    """The repo shipped a second tracked allowlist under config/. It is no longer read.

    A stale duplicate of a security control is worse than no duplicate: two environments can
    enforce different tool sets while both look correct. While the file still exists it must
    match the packaged copy byte for byte. Delete it and this test becomes a no-op.
    """
    if not LEGACY_CONFIG_COPY.is_file():
        pytest.skip("legacy config/ copy has been removed — nothing to guard")

    assert LEGACY_CONFIG_COPY.read_bytes() == packaged_allowlist_path().read_bytes(), (
        "The legacy config/ allowlist has diverged from the packaged one. Delete "
        "config/tooluniverse_allowlist.v1.json — it is no longer loaded."
    )

