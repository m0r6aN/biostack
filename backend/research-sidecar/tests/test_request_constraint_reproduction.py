"""Regression coverage for the executor's accepted-source boundary."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any
from unittest.mock import MagicMock

import pytest

from biostack_research_sidecar.config import Settings
from biostack_research_sidecar.contracts.models import (
    ResearchJobStatusCode,
    ScientificResearchRequest,
)
from biostack_research_sidecar.jobs.store import InMemoryJobStore
from biostack_research_sidecar.workflows.executor import execute_research_job


@dataclass
class _FakeResult:
    tool_name: str
    success: bool = True
    error_code: str | None = None
    error_message: str | None = None
    arguments: dict[str, Any] | None = None

    def __post_init__(self) -> None:
        if self.arguments is None:
            self.arguments = {}


def _claim(tool_name: str, marker: str) -> dict[str, str]:
    return {
        "claim_type": f"{marker}_type",
        "text": f"{marker}_text",
        "tool_name": tool_name,
    }


def _run(
    monkeypatch: pytest.MonkeyPatch,
    *,
    maximum_source_count: int,
    results: list[_FakeResult],
    claim_rows: list[dict[str, str]],
) -> Any:
    import biostack_research_sidecar.tooluniverse_integration.adapter as adapter_mod
    import biostack_research_sidecar.tooluniverse_integration.allowlist as allowlist_mod
    import biostack_research_sidecar.workflows.sequences as sequences_mod

    allowlist = MagicMock()
    allowlist.allowlist_version = "test-only"
    allowlist.skills_for_workflow.return_value = ("synthetic-skill",)
    monkeypatch.setattr(allowlist_mod, "load_allowlist", lambda _path=None: allowlist)
    monkeypatch.setattr(adapter_mod, "create_adapter", lambda _path=None: MagicMock())
    monkeypatch.setattr(
        sequences_mod,
        "run_workflow_sequence",
        lambda *_args, **_kwargs: (results, claim_rows, []),
    )

    settings = Settings(
        host="127.0.0.1",
        service_token="test-only",
        tooluniverse_enabled=True,
        allow_insecure_dev_auth=False,
    )
    store = InMemoryJobStore()
    request = ScientificResearchRequest.model_validate(
        {
            "subject_name": "SyntheticCompound",
            "workflow": "resolve_compound_identity",
            "known_identifiers": {"cid": "1"},
            "maximum_source_count": maximum_source_count,
            "data_classification": "public_scientific",
        }
    )
    return execute_research_job(store, store.create(request), settings)


def test_maximum_source_count_bounds_accepted_results(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """The retained reproduction binds every accepted-result collection."""
    results = [
        _FakeResult("Synthetic_source_one", arguments={"marker": "accepted_arg"}),
        _FakeResult("Synthetic_source_two", arguments={"marker": "discarded_arg"}),
        _FakeResult(
            "Synthetic_source_failed",
            success=False,
            error_code="discarded_error_code",
            error_message="discarded_error_message",
            arguments={"marker": "discarded_failure_arg"},
        ),
    ]
    updated = _run(
        monkeypatch,
        maximum_source_count=1,
        results=results,
        claim_rows=[
            _claim("Synthetic_source_one", "accepted_claim"),
            _claim("Synthetic_source_two", "discarded_claim"),
            _claim("Synthetic_source_failed", "discarded_failure_claim"),
        ],
    )

    artifact = updated.artifact
    assert artifact is not None
    assert updated.tools_invoked == ["Synthetic_source_one"]
    assert artifact.tools_invoked == ["Synthetic_source_one"]
    assert artifact.provenance["tool_results"] == [
        {
            "tool": "Synthetic_source_one",
            "success": True,
            "error_code": None,
            "error_message": None,
            "arguments": {"marker": "accepted_arg"},
        }
    ]
    assert [claim.text for claim in artifact.normalized_claims] == [
        "accepted_claim_text"
    ]
    assert [claim.source_ids for claim in artifact.normalized_claims] == [
        ["Synthetic_source_one"]
    ]
    governed_payload = artifact.model_dump_json()
    for discarded_sentinel in (
        "Synthetic_source_two",
        "Synthetic_source_failed",
        "discarded_arg",
        "discarded_failure_arg",
        "discarded_error_code",
        "discarded_error_message",
        "discarded_claim",
        "discarded_failure_claim",
    ):
        assert discarded_sentinel not in governed_payload
    assert updated.status == ResearchJobStatusCode.PENDING_REVIEW
    assert artifact.status == ResearchJobStatusCode.PENDING_REVIEW
    assert updated.partial is False


def test_source_cap_preserves_two_ordered_occurrences(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    results = [
        _FakeResult("Synthetic_order_one", arguments={"order": 1}),
        _FakeResult("Synthetic_order_two", arguments={"order": 2}),
        _FakeResult("Synthetic_order_three", arguments={"order": 3}),
    ]
    updated = _run(
        monkeypatch,
        maximum_source_count=2,
        results=results,
        claim_rows=[
            _claim("Synthetic_order_one", "ordered_one"),
            _claim("Synthetic_order_two", "ordered_two"),
            _claim("Synthetic_order_three", "ordered_three"),
        ],
    )

    artifact = updated.artifact
    assert artifact is not None
    expected_tools = ["Synthetic_order_one", "Synthetic_order_two"]
    assert updated.tools_invoked == expected_tools
    assert artifact.tools_invoked == expected_tools
    assert [row["tool"] for row in artifact.provenance["tool_results"]] == expected_tools
    assert [claim.text for claim in artifact.normalized_claims] == [
        "ordered_one_text",
        "ordered_two_text",
    ]
    assert [claim.source_ids for claim in artifact.normalized_claims] == [
        ["Synthetic_order_one"],
        ["Synthetic_order_two"],
    ]
    assert "Synthetic_order_three" not in artifact.model_dump_json()


def test_repeated_tool_names_consume_accepted_occurrences(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    updated = _run(
        monkeypatch,
        maximum_source_count=1,
        results=[
            _FakeResult("Synthetic_repeat", arguments={"occurrence": "first"}),
            _FakeResult("Synthetic_repeat", arguments={"occurrence": "second"}),
        ],
        claim_rows=[
            _claim("Synthetic_repeat", "repeat_first"),
            _claim("Synthetic_repeat", "repeat_second"),
        ],
    )

    artifact = updated.artifact
    assert artifact is not None
    assert updated.tools_invoked == ["Synthetic_repeat"]
    assert artifact.tools_invoked == ["Synthetic_repeat"]
    assert artifact.provenance["tool_results"][0]["arguments"] == {
        "occurrence": "first"
    }
    assert [claim.text for claim in artifact.normalized_claims] == [
        "repeat_first_text"
    ]
    assert [claim.source_ids for claim in artifact.normalized_claims] == [
        ["Synthetic_repeat"]
    ]
    assert "repeat_second" not in artifact.model_dump_json()


@pytest.mark.parametrize("maximum_source_count", [0, -1])
def test_non_positive_source_caps_accept_nothing(
    monkeypatch: pytest.MonkeyPatch,
    maximum_source_count: int,
) -> None:
    updated = _run(
        monkeypatch,
        maximum_source_count=maximum_source_count,
        results=[
            _FakeResult("Synthetic_nonpositive", arguments={"marker": "must_not_leak"})
        ],
        claim_rows=[_claim("Synthetic_nonpositive", "must_not_materialize")],
    )

    artifact = updated.artifact
    assert artifact is not None
    assert updated.tools_invoked == []
    assert artifact.tools_invoked == []
    assert artifact.provenance["tool_results"] == []
    assert artifact.normalized_claims == []
    assert artifact.source_manifest == []
    assert artifact.raw_artifact_hashes == []
    assert updated.status == ResearchJobStatusCode.FAILED
    assert artifact.status == ResearchJobStatusCode.FAILED
    assert updated.partial is False
    assert artifact.partial is False
    assert artifact.failure_details == (
        "no tool steps executed (missing identifiers or empty sequence)"
    )
    assert "must_not_leak" not in artifact.model_dump_json()
    assert "must_not_materialize" not in artifact.model_dump_json()
