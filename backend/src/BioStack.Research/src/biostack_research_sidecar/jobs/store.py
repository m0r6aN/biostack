"""In-memory research job store for foundation scaffold.

Production will replace this with durable storage coordinated by BioStack.
"""

from __future__ import annotations

import threading
from dataclasses import dataclass, field
from datetime import datetime, timezone
from uuid import uuid4

from biostack_research_sidecar.contracts.models import (
    ResearchJobStatusCode,
    ScientificResearchArtifact,
    ScientificResearchRequest,
)


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
    def __init__(self) -> None:
        self._lock = threading.RLock()
        self._jobs: dict[str, JobRecord] = {}

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
            self._jobs[record.job_id] = record
        return record

    def get(self, job_id: str) -> JobRecord | None:
        with self._lock:
            return self._jobs.get(job_id)

    def update(self, job_id: str, **kwargs: object) -> JobRecord | None:
        with self._lock:
            record = self._jobs.get(job_id)
            if record is None:
                return None
            for key, value in kwargs.items():
                if not hasattr(record, key):
                    raise AttributeError(key)
                setattr(record, key, value)
            record.updated_at_utc = datetime.now(timezone.utc)
            return record

    def request_cancel(self, job_id: str) -> JobRecord | None:
        with self._lock:
            record = self._jobs.get(job_id)
            if record is None:
                return None
            record.cancel_requested = True
            if record.status in {
                ResearchJobStatusCode.QUEUED,
                ResearchJobStatusCode.RESOLVING_IDENTITY,
                ResearchJobStatusCode.GATHERING_EVIDENCE,
                ResearchJobStatusCode.NORMALIZING,
            }:
                record.status = ResearchJobStatusCode.CANCELLED
                record.finished_at_utc = datetime.now(timezone.utc)
                record.progress_message = "cancelled"
            record.updated_at_utc = datetime.now(timezone.utc)
            return record
