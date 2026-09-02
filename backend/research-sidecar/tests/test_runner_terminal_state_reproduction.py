"""Reproduce timeout terminal-state regression without external services."""

from __future__ import annotations

import threading
import time
from dataclasses import dataclass
from typing import Any
from unittest.mock import MagicMock

import pytest

from biostack_research_sidecar.config import Settings
from biostack_research_sidecar.contracts.models import (
    ResearchJobStatusCode,
    ScientificResearchRequest,
)
from biostack_research_sidecar.jobs.runner import JobRunner
from biostack_research_sidecar.jobs.store import InMemoryJobStore


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


def _wait_until(predicate: Any, *, timeout: float = 4.0) -> None:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        if predicate():
            return
        time.sleep(0.01)
    raise AssertionError("condition was not reached before the deterministic test deadline")


def test_timed_out_job_cannot_be_overwritten_by_late_worker(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A timeout must remain terminal after the already-running worker returns."""
    import biostack_research_sidecar.tooluniverse_integration.adapter as adapter_mod
    import biostack_research_sidecar.tooluniverse_integration.allowlist as allowlist_mod
    import biostack_research_sidecar.workflows.sequences as sequences_mod

    entered_sequence = threading.Event()
    release_sequence = threading.Event()

    allowlist = MagicMock()
    allowlist.allowlist_version = "test-only"
    allowlist.skills_for_workflow.return_value = ("synthetic-skill",)

    monkeypatch.setattr(allowlist_mod, "load_allowlist", lambda _path=None: allowlist)
    monkeypatch.setattr(adapter_mod, "create_adapter", lambda _path=None: MagicMock())

    def blocked_sequence(*_args: object, **_kwargs: object) -> tuple:
        entered_sequence.set()
        assert release_sequence.wait(timeout=4.0)
        return [_FakeResult("Synthetic_lookup")], [], []

    monkeypatch.setattr(sequences_mod, "run_workflow_sequence", blocked_sequence)

    settings = Settings(
        host="127.0.0.1",
        service_token="test-only",
        tooluniverse_enabled=True,
        allow_insecure_dev_auth=False,
        max_concurrent_research_jobs=1,
    )
    store = InMemoryJobStore()
    request = ScientificResearchRequest.model_validate(
        {
            "subject_name": "SyntheticCompound",
            "workflow": "resolve_compound_identity",
            "known_identifiers": {"cid": "1"},
            "maximum_execution_time_seconds": 1,
            "execution": {"maximum_execution_duration_seconds": 1},
            "data_classification": "public_scientific",
        }
    )
    record = store.create(request)
    runner = JobRunner(store, settings)

    try:
        assert runner.try_reserve_slot()
        runner.submit(record.job_id)
        assert entered_sequence.wait(timeout=2.0)

        _wait_until(
            lambda: (
                (current := store.get(record.job_id)) is not None
                and current.error_code == "execution_timeout"
                and current.status == ResearchJobStatusCode.FAILED
            )
        )

        release_sequence.set()
        _wait_until(
            lambda: (
                (current := store.get(record.job_id)) is not None
                and current.status != ResearchJobStatusCode.FAILED
            )
        )

        final = store.get(record.job_id)
        assert final is not None
        assert final.status == ResearchJobStatusCode.FAILED, (
            "The late worker overwrote the timeout terminal state with "
            f"{final.status.value}."
        )
        assert final.error_code == "execution_timeout"
    finally:
        release_sequence.set()
        runner.shutdown(wait=True)
