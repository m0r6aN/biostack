"""S6: terminal status mapping for tool-sequence outcomes."""

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any
from unittest.mock import MagicMock

from biostack_research_sidecar.config import Settings
from biostack_research_sidecar.contracts.models import (
    ResearchJobStatusCode,
    ScientificResearchRequest,
)
from biostack_research_sidecar.jobs.store import InMemoryJobStore
from biostack_research_sidecar.workflows import executor as executor_mod


@dataclass
class _FakeResult:
    tool_name: str
    success: bool
    error_code: str | None = None
    error_message: str | None = None
    arguments: dict[str, Any] | None = None

    def __post_init__(self) -> None:
        if self.arguments is None:
            self.arguments = {}


def _settings() -> Settings:
    return Settings(
        host="127.0.0.1",
        service_token="t",
        tooluniverse_enabled=True,
        allow_insecure_dev_auth=False,
    )


def _record(store: InMemoryJobStore) -> Any:
    request = ScientificResearchRequest.model_validate(
        {
            "subject_name": "semaglutide",
            "workflow": "resolve_compound_identity",
            "known_identifiers": {"cid": "1"},
            "data_classification": "public_scientific",
        }
    )
    return store.create(request)


def _run_with_results(
    results: list[_FakeResult],
    claim_rows: list[dict[str, Any]] | None = None,
    skips: list[str] | None = None,
) -> Any:
    store = InMemoryJobStore()
    record = _record(store)
    settings = _settings()

    allowlist = MagicMock()
    allowlist.allowlist_version = "1.0.0"
    allowlist.skills_for_workflow.return_value = ("skill-a",)

    adapter = MagicMock()

    def fake_load_allowlist(_path: str | None = None) -> Any:
        return allowlist

    def fake_create_adapter(_path: str | None = None) -> Any:
        return adapter

    def fake_run_sequence(*_args: object, **_kwargs: object) -> tuple:
        return results, claim_rows or [], skips or []

    # Patch the late imports inside _execute_with_tooluniverse via module attributes
    # by monkeypatching the functions the executor imports at call time.
    import biostack_research_sidecar.tooluniverse_integration.allowlist as allowlist_mod
    import biostack_research_sidecar.tooluniverse_integration.adapter as adapter_mod
    import biostack_research_sidecar.workflows.sequences as sequences_mod

    original_load = allowlist_mod.load_allowlist
    original_create = adapter_mod.create_adapter
    original_run = sequences_mod.run_workflow_sequence

    allowlist_mod.load_allowlist = fake_load_allowlist  # type: ignore[assignment]
    adapter_mod.create_adapter = fake_create_adapter  # type: ignore[assignment]
    sequences_mod.run_workflow_sequence = fake_run_sequence  # type: ignore[assignment]
    try:
        return executor_mod.execute_research_job(store, record, settings)
    finally:
        allowlist_mod.load_allowlist = original_load  # type: ignore[assignment]
        adapter_mod.create_adapter = original_create  # type: ignore[assignment]
        sequences_mod.run_workflow_sequence = original_run  # type: ignore[assignment]


def test_all_tools_success_is_pending_review() -> None:
    updated = _run_with_results(
        [
            _FakeResult("PubChem_get_CID_by_compound_name", True),
            _FakeResult("PubChem_get_compound_synonyms_by_CID", True),
        ],
        claim_rows=[
            {
                "claim_type": "identity_pubchem_cid",
                "text": "cid candidate",
                "tool_name": "PubChem_get_CID_by_compound_name",
            }
        ],
    )
    assert updated.status == ResearchJobStatusCode.PENDING_REVIEW
    assert updated.partial is False


def test_mixed_tool_outcomes_is_partial() -> None:
    updated = _run_with_results(
        [
            _FakeResult("PubChem_get_CID_by_compound_name", True),
            _FakeResult(
                "PubChem_get_compound_synonyms_by_CID",
                False,
                error_code="timeout",
                error_message="slow",
            ),
        ],
        claim_rows=[
            {
                "claim_type": "identity_pubchem_cid",
                "text": "cid candidate",
                "tool_name": "PubChem_get_CID_by_compound_name",
            }
        ],
    )
    assert updated.status == ResearchJobStatusCode.PARTIAL
    assert updated.partial is True


def test_all_tools_failed_is_failed() -> None:
    updated = _run_with_results(
        [
            _FakeResult(
                "PubChem_get_CID_by_compound_name",
                False,
                error_code="error",
                error_message="boom",
            ),
        ]
    )
    assert updated.status == ResearchJobStatusCode.FAILED
    assert updated.partial is False


def test_no_steps_executed_is_failed() -> None:
    updated = _run_with_results([], claim_rows=[], skips=["missing identifier"])
    assert updated.status == ResearchJobStatusCode.FAILED
    assert updated.partial is False
