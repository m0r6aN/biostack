"""Background research job runner.

Keeps the HTTP event loop free: submit returns 202 with a QUEUED handle while
work runs off-request. Enforces max concurrency and per-job timeouts that were
previously define-only settings.
"""

from __future__ import annotations

import logging
import threading
from concurrent.futures import ThreadPoolExecutor, TimeoutError as FuturesTimeoutError
from datetime import datetime, timezone

from biostack_research_sidecar import __version__
from biostack_research_sidecar.config import Settings
from biostack_research_sidecar.contracts.models import (
    ResearchJobStatusCode,
    ScientificResearchArtifact,
)
from biostack_research_sidecar.jobs.store import InMemoryJobStore, JobRecord
from biostack_research_sidecar.workflows.executor import execute_research_job

logger = logging.getLogger(__name__)


class JobRunner:
    """Bounded worker pool + external waiters for timeout without pool deadlock."""

    def __init__(self, store: InMemoryJobStore, settings: Settings) -> None:
        self._store = store
        self._settings = settings
        max_workers = max(1, int(settings.max_concurrent_research_jobs))
        self._max_workers = max_workers
        self._semaphore = threading.BoundedSemaphore(max_workers)
        # Only execute_research_job runs on this pool. Timeout waiters are outside it.
        self._executor = ThreadPoolExecutor(
            max_workers=max_workers,
            thread_name_prefix="research-job",
        )
        self._lock = threading.Lock()
        self._in_flight = 0

    @property
    def max_workers(self) -> int:
        return self._max_workers

    @property
    def in_flight(self) -> int:
        with self._lock:
            return self._in_flight

    def try_reserve_slot(self) -> bool:
        """Non-blocking reservation used at submit time to fail closed with 429."""
        acquired = self._semaphore.acquire(blocking=False)
        if acquired:
            with self._lock:
                self._in_flight += 1
        return acquired

    def release_slot(self) -> None:
        with self._lock:
            self._in_flight = max(0, self._in_flight - 1)
        self._semaphore.release()

    def submit(self, job_id: str) -> None:
        """Schedule execution. Caller must have already reserved a slot."""
        # Waiter thread sits outside the pool so fut.result(timeout=...) cannot
        # deadlock when the pool is saturated.
        threading.Thread(
            target=self._run_job,
            args=(job_id,),
            name=f"research-wait-{job_id[:8]}",
            daemon=True,
        ).start()

    def _run_job(self, job_id: str) -> None:
        try:
            record = self._store.get(job_id)
            if record is None:
                return
            if record.cancel_requested or record.status == ResearchJobStatusCode.CANCELLED:
                return

            timeout_seconds = _resolve_timeout_seconds(record, self._settings)
            future = self._executor.submit(
                execute_research_job, self._store, record, self._settings
            )
            try:
                future.result(timeout=timeout_seconds)
            except FuturesTimeoutError:
                logger.warning(
                    "research job %s timed out after %ss", job_id, timeout_seconds
                )
                # Best-effort: mark failed. The worker thread may still be running;
                # cooperative cancel is checked at job start only in the foundation.
                self._store.request_cancel(job_id)
                self._mark_timeout(job_id, record, timeout_seconds)
            except Exception as exc:  # noqa: BLE001 — last-line defence for worker
                logger.exception("research job %s failed", job_id)
                self._mark_internal_failure(job_id, record, exc)
        finally:
            self.release_slot()

    def _mark_timeout(
        self, job_id: str, record: JobRecord, timeout_seconds: int
    ) -> None:
        current = self._store.get(job_id)
        if current is None:
            return
        if current.finished_at_utc is not None and current.artifact is not None:
            # Executor finished racing the timeout; leave its result.
            return
        finished = datetime.now(timezone.utc)
        message = f"Job exceeded maximum execution time ({timeout_seconds}s)."
        artifact = ScientificResearchArtifact(
            research_artifact_id=f"artifact-{job_id}",
            job_id=job_id,
            research_request_id=record.request.research_request_id,
            provider_version=__version__,
            workflow=record.request.workflow,
            status=ResearchJobStatusCode.FAILED,
            partial=False,
            started_at_utc=record.submitted_at_utc,
            finished_at_utc=finished,
            failure_details=message,
            warnings=[message],
            provenance={
                "timeout_seconds": timeout_seconds,
                "correlation_id": record.request.correlation_id,
            },
        )
        self._store.update(
            job_id,
            status=ResearchJobStatusCode.FAILED,
            partial=False,
            finished_at_utc=finished,
            error_code="execution_timeout",
            error_message=message,
            progress_message=message,
            artifact=artifact,
        )

    def _mark_internal_failure(
        self, job_id: str, record: JobRecord, exc: BaseException
    ) -> None:
        current = self._store.get(job_id)
        if current is None or current.artifact is not None:
            return
        finished = datetime.now(timezone.utc)
        message = f"Unhandled worker error: {exc.__class__.__name__}"
        artifact = ScientificResearchArtifact(
            research_artifact_id=f"artifact-{job_id}",
            job_id=job_id,
            research_request_id=record.request.research_request_id,
            provider_version=__version__,
            workflow=record.request.workflow,
            status=ResearchJobStatusCode.FAILED,
            partial=False,
            started_at_utc=record.submitted_at_utc,
            finished_at_utc=finished,
            failure_details=message,
            warnings=[message],
            provenance={"correlation_id": record.request.correlation_id},
        )
        self._store.update(
            job_id,
            status=ResearchJobStatusCode.FAILED,
            partial=False,
            finished_at_utc=finished,
            error_code="worker_error",
            error_message=message,
            progress_message=message,
            artifact=artifact,
        )

    def shutdown(self, wait: bool = False) -> None:
        self._executor.shutdown(wait=wait, cancel_futures=True)


def _resolve_timeout_seconds(record: JobRecord, settings: Settings) -> int:
    """Honour the tighter of request and execution-profile limits (floor 1s)."""
    del settings  # reserved for a future global ceiling
    req = record.request
    candidates = [
        int(req.maximum_execution_time_seconds),
        int(req.execution.maximum_execution_duration_seconds),
    ]
    timeout = min(c for c in candidates if c > 0)
    return max(1, timeout)
