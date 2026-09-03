"""In-memory research job store for foundation scaffold.

Production will replace this with durable storage coordinated by BioStack.
"""

from __future__ import annotations

import threading
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from uuid import uuid4

from biostack_research_sidecar.contracts.models import (
    ResearchJobStatusCode,
    ScientificResearchArtifact,
    ScientificResearchRequest,
)

_ACTIVE_STATUSES = {
    ResearchJobStatusCode.QUEUED,
    ResearchJobStatusCode.RESOLVING_IDENTITY,
    ResearchJobStatusCode.GATHERING_EVIDENCE,
    ResearchJobStatusCode.NORMALIZING,
}

_TERMINAL_STATUSES = {
    ResearchJobStatusCode.PENDING_REVIEW,
    ResearchJobStatusCode.COMPLETED,
    ResearchJobStatusCode.FAILED,
    ResearchJobStatusCode.CANCELLED,
    ResearchJobStatusCode.PARTIAL,
    ResearchJobStatusCode.REJECTED_BY_POLICY,
}


@dataclass
class JobRecord:
    job_id: str
    request: ScientificResearchRequest
    status: ResearchJobStatusCode
    submitted_at_utc: datetime
    updated_at_utc: datetime
    finished_at_utc: datetime | None = None
    progress_message: str | None = None
    partial: bool = False
    error_code: str | None = None
    error_message: str | None = None
    artifact: ScientificResearchArtifact | None = None
    cancel_requested: bool = False
    tools_invoked: list[str] = field(default_factory=list)


class InMemoryJobStore:
    def __init__(self, job_ttl_seconds: int = 86_400) -> None:
        self._lock = threading.RLock()
        self._jobs: dict[str, JobRecord] = {}
        self._job_ttl_seconds = max(60, int(job_ttl_seconds))

    def create(self, request: ScientificResearchRequest) -> JobRecord:
        now = datetime.now(timezone.utc)
        record = JobRecord(
            job_id=str(uuid4()),
            request=request,
            status=ResearchJobStatusCode.QUEUED,
            submitted_at_utc=now,
            updated_at_utc=now,
            progress_message="queued",
        )
        with self._lock:
            self._purge_expired_unlocked(now)
            self._jobs[record.job_id] = record
        return record

    def get(self, job_id: str) -> JobRecord | None:
        with self._lock:
            self._purge_expired_unlocked(datetime.now(timezone.utc))
            return self._jobs.get(job_id)

    def _purge_expired_unlocked(self, now: datetime) -> None:
        cutoff = now - timedelta(seconds=self._job_ttl_seconds)
        expired = [
            job_id
            for job_id, record in self._jobs.items()
            if record.updated_at_utc < cutoff
            and record.status not in _ACTIVE_STATUSES
        ]
        for job_id in expired:
            del self._jobs[job_id]

    def update(self, job_id: str, **kwargs: object) -> JobRecord | None:
        with self._lock:
            record = self._jobs.get(job_id)
            if record is None:
                return None
            if record.status in _TERMINAL_STATUSES:
                return record
            for key in kwargs:
                if not hasattr(record, key):
                    raise AttributeError(key)
            for key, value in kwargs.items():
                setattr(record, key, value)
            record.updated_at_utc = datetime.now(timezone.utc)
            return record

    def request_cancel(self, job_id: str) -> JobRecord | None:
        with self._lock:
            record = self._jobs.get(job_id)
            if record is None:
                return None
            if record.status in _TERMINAL_STATUSES:
                return record
            record.cancel_requested = True
            if record.status in _ACTIVE_STATUSES:
                record.status = ResearchJobStatusCode.CANCELLED
                record.finished_at_utc = datetime.now(timezone.utc)
                record.progress_message = "cancelled"
            record.updated_at_utc = datetime.now(timezone.utc)
            return record
