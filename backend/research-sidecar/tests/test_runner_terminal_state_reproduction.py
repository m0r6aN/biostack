"""Reproduce timeout terminal-state regression without external services."""

from __future__ import annotations

import threading
import time
from copy import deepcopy
from dataclasses import dataclass
from datetime import UTC, datetime, timedelta
from typing import Any
from unittest.mock import MagicMock

import pytest

from biostack_research_sidecar.config import Settings
from biostack_research_sidecar.contracts.models import (
    ResearchJobStatusCode,
    ScientificResearchArtifact,
    ScientificResearchRequest,
)
from biostack_research_sidecar.jobs.runner import JobRunner
from biostack_research_sidecar.jobs.store import InMemoryJobStore, JobRecord


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


@dataclass(frozen=True)
class _TerminalCase:
    status: ResearchJobStatusCode
    error_code: str | None
    partial: bool = False
    via_cancel: bool = False
    cancel_requested: bool = False


_TERMINAL_CASES = (
    pytest.param(
        _TerminalCase(
            ResearchJobStatusCode.FAILED,
            "execution_timeout",
            cancel_requested=True,
        ),
        id="timeout-failure",
    ),
    pytest.param(
        _TerminalCase(ResearchJobStatusCode.FAILED, "worker_error"),
        id="ordinary-failure",
    ),
    pytest.param(
        _TerminalCase(
            ResearchJobStatusCode.CANCELLED,
            None,
            via_cancel=True,
            cancel_requested=True,
        ),
        id="cancellation",
    ),
    pytest.param(
        _TerminalCase(ResearchJobStatusCode.PARTIAL, None, partial=True),
        id="partial",
    ),
    pytest.param(
        _TerminalCase(ResearchJobStatusCode.REJECTED_BY_POLICY, "policy_rejection"),
        id="policy-rejection",
    ),
    pytest.param(
        _TerminalCase(ResearchJobStatusCode.PENDING_REVIEW, None),
        id="pending-review",
    ),
    pytest.param(
        _TerminalCase(ResearchJobStatusCode.COMPLETED, None),
        id="completion",
    ),
)


def _request() -> ScientificResearchRequest:
    return ScientificResearchRequest.model_validate(
        {
            "subject_name": "SyntheticCompound",
            "workflow": "resolve_compound_identity",
            "known_identifiers": {"cid": "1"},
            "maximum_execution_time_seconds": 1,
            "execution": {"maximum_execution_duration_seconds": 1},
            "data_classification": "public_scientific",
        }
    )


def _artifact(
    record: JobRecord,
    status: ResearchJobStatusCode,
    marker: str,
    finished: datetime,
) -> ScientificResearchArtifact:
    return ScientificResearchArtifact(
        research_artifact_id=f"artifact-{marker}",
        job_id=record.job_id,
        research_request_id=record.request.research_request_id,
        provider_version="test-only",
        workflow=record.request.workflow,
        status=status,
        partial=status == ResearchJobStatusCode.PARTIAL,
        started_at_utc=record.submitted_at_utc,
        finished_at_utc=finished,
        warnings=[marker],
        provenance={"marker": marker},
    )


def _wait_until(predicate: Any, *, timeout: float = 4.0) -> None:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        if predicate():
            return
        time.sleep(0.01)
    raise AssertionError("condition was not reached before the deterministic test deadline")


def _assert_snapshot(store: InMemoryJobStore, job_id: str, expected: JobRecord) -> None:
    current = store.get(job_id)
    assert current is not None
    assert deepcopy(current) == expected


def test_timed_out_job_cannot_be_overwritten_by_late_worker(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """A timeout must remain terminal after the already-running worker returns."""
    import biostack_research_sidecar.jobs.runner as runner_mod
    import biostack_research_sidecar.tooluniverse_integration.adapter as adapter_mod
    import biostack_research_sidecar.tooluniverse_integration.allowlist as allowlist_mod
    import biostack_research_sidecar.workflows.sequences as sequences_mod

    entered_sequence = threading.Event()
    release_sequence = threading.Event()
    worker_completed = threading.Event()

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

    execute_research_job = runner_mod.execute_research_job

    def tracked_execute_research_job(*args: object, **kwargs: object) -> object:
        try:
            return execute_research_job(*args, **kwargs)
        finally:
            worker_completed.set()

    monkeypatch.setattr(runner_mod, "execute_research_job", tracked_execute_research_job)

    settings = Settings(
        host="127.0.0.1",
        service_token="test-only",
        tooluniverse_enabled=True,
        allow_insecure_dev_auth=False,
        max_concurrent_research_jobs=1,
    )
    store = InMemoryJobStore()
    record = store.create(_request())
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
        timeout_record = store.get(record.job_id)
        assert timeout_record is not None
        timeout_snapshot = deepcopy(timeout_record)
        timeout_message = "Job exceeded maximum execution time (1s)."
        assert timeout_snapshot.status == ResearchJobStatusCode.FAILED
        assert timeout_snapshot.cancel_requested is True
        assert timeout_snapshot.error_code == "execution_timeout"
        assert timeout_snapshot.error_message == timeout_message
        assert timeout_snapshot.progress_message == timeout_message
        assert timeout_snapshot.finished_at_utc is not None
        assert timeout_snapshot.artifact is not None
        assert timeout_snapshot.artifact.status == ResearchJobStatusCode.FAILED
        assert timeout_snapshot.artifact.failure_details == timeout_message
        assert timeout_snapshot.artifact.warnings == [timeout_message]
        assert timeout_snapshot.artifact.finished_at_utc == timeout_snapshot.finished_at_utc

        release_sequence.set()
        assert worker_completed.wait(timeout=4.0)
        _assert_snapshot(store, record.job_id, timeout_snapshot)
    finally:
        release_sequence.set()
        runner.shutdown(wait=True)


@pytest.mark.parametrize("case", _TERMINAL_CASES)
def test_first_terminal_snapshot_is_immutable(case: _TerminalCase) -> None:
    store = InMemoryJobStore()
    record = store.create(_request())
    finished = datetime(2026, 9, 2, 12, 0, tzinfo=UTC)

    if case.via_cancel:
        first = store.request_cancel(record.job_id)
    else:
        first = store.update(
            record.job_id,
            status=case.status,
            finished_at_utc=finished,
            progress_message=f"first-{case.status.value}",
            partial=case.partial,
            error_code=case.error_code,
            error_message=f"error-{case.error_code}" if case.error_code else None,
            artifact=_artifact(record, case.status, "first", finished),
            cancel_requested=case.cancel_requested,
            tools_invoked=["first-tool"],
        )
    assert first is not None
    snapshot = deepcopy(first)

    store.update(
        record.job_id,
        status=snapshot.status,
        finished_at_utc=snapshot.finished_at_utc,
        progress_message=snapshot.progress_message,
        partial=snapshot.partial,
        error_code=snapshot.error_code,
        error_message=snapshot.error_message,
        artifact=deepcopy(snapshot.artifact),
        cancel_requested=snapshot.cancel_requested,
        tools_invoked=deepcopy(snapshot.tools_invoked),
    )
    _assert_snapshot(store, record.job_id, snapshot)

    store.update(record.job_id, progress_message="late-progress")
    _assert_snapshot(store, record.job_id, snapshot)

    hostile_status = (
        ResearchJobStatusCode.FAILED
        if case.status == ResearchJobStatusCode.COMPLETED
        else ResearchJobStatusCode.COMPLETED
    )
    hostile_finished = finished + timedelta(hours=1)
    store.update(
        record.job_id,
        status=hostile_status,
        finished_at_utc=hostile_finished,
        progress_message="hostile-progress",
        partial=not snapshot.partial,
        error_code="hostile-error",
        error_message="hostile-message",
        artifact=_artifact(record, hostile_status, "hostile", hostile_finished),
        cancel_requested=not snapshot.cancel_requested,
        tools_invoked=["hostile-tool"],
    )
    _assert_snapshot(store, record.job_id, snapshot)

    store.request_cancel(record.job_id)
    _assert_snapshot(store, record.job_id, snapshot)


def test_all_active_states_accept_progress_updates() -> None:
    store = InMemoryJobStore()
    record = store.create(_request())
    active_snapshot = deepcopy(record)
    with pytest.raises(AttributeError, match="invalid_terminal_field"):
        store.update(
            record.job_id,
            status=ResearchJobStatusCode.COMPLETED,
            finished_at_utc=datetime(2026, 9, 2, 12, 0, tzinfo=UTC),
            progress_message="must-not-stick",
            invalid_terminal_field="rejected",
        )
    _assert_snapshot(store, record.job_id, active_snapshot)

    active_states = (
        ResearchJobStatusCode.QUEUED,
        ResearchJobStatusCode.RESOLVING_IDENTITY,
        ResearchJobStatusCode.GATHERING_EVIDENCE,
        ResearchJobStatusCode.NORMALIZING,
    )

    for index, status in enumerate(active_states):
        progress = f"active-{index}-{status.value}"
        updated = store.update(record.job_id, status=status, progress_message=progress)
        assert updated is not None
        assert updated.status == status
        assert updated.progress_message == progress


def test_simultaneous_terminal_writers_preserve_one_complete_snapshot() -> None:
    store = InMemoryJobStore()
    record = store.create(_request())
    before = deepcopy(record)
    barrier = threading.Barrier(3)
    errors: list[BaseException] = []
    results: list[JobRecord] = []
    first_finished = datetime(2026, 9, 2, 12, 0, tzinfo=UTC)
    second_finished = first_finished + timedelta(hours=1)
    payloads = (
        {
            "status": ResearchJobStatusCode.PARTIAL,
            "finished_at_utc": first_finished,
            "progress_message": "contender-a",
            "partial": True,
            "error_code": "partial-a",
            "error_message": "message-a",
            "artifact": _artifact(
                record, ResearchJobStatusCode.PARTIAL, "contender-a", first_finished
            ),
            "cancel_requested": False,
            "tools_invoked": ["tool-a"],
        },
        {
            "status": ResearchJobStatusCode.FAILED,
            "finished_at_utc": second_finished,
            "progress_message": "contender-b",
            "partial": False,
            "error_code": "failure-b",
            "error_message": "message-b",
            "artifact": _artifact(
                record, ResearchJobStatusCode.FAILED, "contender-b", second_finished
            ),
            "cancel_requested": True,
            "tools_invoked": ["tool-b"],
        },
    )

    def write_terminal(payload: dict[str, object]) -> None:
        try:
            barrier.wait(timeout=2.0)
            result = store.update(record.job_id, **payload)
            assert result is not None
            results.append(deepcopy(result))
        except BaseException as exc:  # noqa: BLE001 - surfaced on the test thread
            errors.append(exc)

    threads = [
        threading.Thread(target=write_terminal, args=(payload,), daemon=True)
        for payload in payloads
    ]
    for thread in threads:
        thread.start()
    barrier.wait(timeout=2.0)
    for thread in threads:
        thread.join(timeout=2.0)
        assert not thread.is_alive(), "terminal writer did not complete within its deadline"

    assert not errors
    final = store.get(record.job_id)
    assert final is not None
    final_snapshot = deepcopy(final)
    assert results == [final_snapshot, final_snapshot]

    expected_snapshots = []
    for payload in payloads:
        expected = deepcopy(before)
        for key, value in payload.items():
            setattr(expected, key, deepcopy(value))
        expected.updated_at_utc = final_snapshot.updated_at_utc
        expected_snapshots.append(expected)
    assert final_snapshot in expected_snapshots

    store.request_cancel(record.job_id)
    _assert_snapshot(store, record.job_id, final_snapshot)
