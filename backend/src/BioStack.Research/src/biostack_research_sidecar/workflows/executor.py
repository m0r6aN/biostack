"""Bounded workflow executor.

Foundation behavior:
- Reject arbitrary tools
- Honor kill switches
- Call ToolUniverse only when enabled, pinned, and allowlist-bound
- Emit structured candidate artifacts suitable for BioStack review staging
"""

from __future__ import annotations

from datetime import datetime, timezone
from typing import Any

from biostack_research_sidecar import __version__
from biostack_research_sidecar.config import Settings
from biostack_research_sidecar.contracts.models import (
    NormalizedClaim,
    ResearchJobStatusCode,
    ScientificResearchArtifact,
)
from biostack_research_sidecar.inference_policy import (
    InferencePolicyError,
    assert_no_silent_hosted_escalation,
)
from biostack_research_sidecar.jobs.store import InMemoryJobStore, JobRecord
from biostack_research_sidecar.kill_switches import KillSwitchError, assert_research_allowed


def execute_research_job(
    store: InMemoryJobStore,
    record: JobRecord,
    settings: Settings,
) -> JobRecord:
    job_id = record.job_id
    request = record.request
    started = datetime.now(timezone.utc)

    try:
        assert_research_allowed(settings, request.workflow)
        # Fail closed on partial hosted flags before any work (including future inference).
        assert_no_silent_hosted_escalation(settings, request)
    except (KillSwitchError, InferencePolicyError) as exc:
        code = getattr(exc, "code", "policy_denied")
        message = getattr(exc, "message", str(exc))
        artifact = _artifact(
            record=record,
            settings=settings,
            status=ResearchJobStatusCode.REJECTED_BY_POLICY,
            started=started,
            finished=datetime.now(timezone.utc),
            warnings=[message],
            failure_details=message,
        )
        return store.update(
            job_id,
            status=ResearchJobStatusCode.REJECTED_BY_POLICY,
            finished_at_utc=artifact.finished_at_utc,
            error_code=code,
            error_message=message,
            progress_message=message,
            artifact=artifact,
        )  # type: ignore[return-value]

    if record.cancel_requested:
        return store.update(
            job_id,
            status=ResearchJobStatusCode.CANCELLED,
            finished_at_utc=datetime.now(timezone.utc),
            progress_message="cancelled before execution",
        )  # type: ignore[return-value]

    store.update(
        job_id,
        status=ResearchJobStatusCode.GATHERING_EVIDENCE,
        progress_message="workflow accepted",
    )

    if not settings.tooluniverse_enabled:
        message = (
            "ToolUniverse is disabled. Pin is tooluniverse==1.4.0 "
            "(optional extra). Set BIOSTACK_RESEARCH_TOOLUNIVERSE_ENABLED=true "
            "after installing with uv sync --extra tooluniverse."
        )
        artifact = _artifact(
            record=record,
            settings=settings,
            status=ResearchJobStatusCode.PARTIAL,
            started=started,
            finished=datetime.now(timezone.utc),
            partial=True,
            warnings=[message],
            claims=[
                NormalizedClaim(
                    claim_id=f"{job_id}-scaffold",
                    claim_type="scaffold_notice",
                    text=(
                        f"Research job accepted for subject '{request.subject_name}' "
                        f"workflow '{request.workflow}'. No external scientific tools were invoked."
                    ),
                    evidence_class="unknown",
                    review_status="not_promotable",
                )
            ],
            provenance_extra={"scaffold": True},
        )
        return store.update(
            job_id,
            status=ResearchJobStatusCode.PARTIAL,
            partial=True,
            finished_at_utc=artifact.finished_at_utc,
            progress_message=message,
            artifact=artifact,
            tools_invoked=[],
        )  # type: ignore[return-value]

    return _execute_with_tooluniverse(store, record, settings, started)


def _execute_with_tooluniverse(
    store: InMemoryJobStore,
    record: JobRecord,
    settings: Settings,
    started: datetime,
) -> JobRecord:
    from biostack_research_sidecar.tooluniverse_integration.adapter import create_adapter
    from biostack_research_sidecar.tooluniverse_integration.allowlist import load_allowlist
    from biostack_research_sidecar.workflows.sequences import run_workflow_sequence

    job_id = record.job_id
    request = record.request
    allowlist_path = settings.tooluniverse_allowlist_path or None
    allowlist = load_allowlist(allowlist_path)
    adapter = create_adapter(allowlist_path)
    skills = allowlist.skills_for_workflow(request.workflow)

    store.update(
        job_id,
        status=ResearchJobStatusCode.GATHERING_EVIDENCE,
        progress_message=f"running allowlisted sequence for {request.workflow}",
    )

    results, claim_rows, skips = run_workflow_sequence(
        adapter,
        request.workflow,
        request.subject_name,
        dict(request.known_identifiers or {}),
    )

    tools_invoked = [item.tool_name for item in results]
    warnings: list[str] = [
        f"ToolUniverse pin={settings.tooluniverse_version}",
        f"allowlist={allowlist.allowlist_version}",
        f"skills={','.join(skills) if skills else 'none'}",
        "Results are candidate evidence only; never canonical.",
        *skips,
    ]
    claims: list[NormalizedClaim] = []
    provenance_extra: dict[str, Any] = {
        "scaffold": False,
        "allowlist_version": allowlist.allowlist_version,
        "approved_skills_for_workflow": list(skills),
        "tool_results": [],
        "sequence_skips": skips,
    }

    for item in results:
        provenance_extra["tool_results"].append(
            {
                "tool": item.tool_name,
                "success": item.success,
                "error_code": item.error_code,
                "error_message": item.error_message,
                "arguments": item.arguments,
            }
        )
        if not item.success:
            warnings.append(
                f"{item.tool_name}: {item.error_code or 'error'} — {item.error_message}"
            )

    for index, row in enumerate(claim_rows):
        claims.append(
            NormalizedClaim(
                claim_id=f"{job_id}-{row['claim_type']}-{index}",
                claim_type=str(row["claim_type"]),
                text=str(row["text"]),
                evidence_class="unknown",
                source_ids=[str(row.get("tool_name") or "")],
                review_status="candidate",
            )
        )

    any_success = any(item.success for item in results)
    if tools_invoked and not any_success:
        status = ResearchJobStatusCode.PARTIAL
        partial = True
        progress = "allowlisted tools invoked; all returned errors"
    elif tools_invoked and any_success:
        status = ResearchJobStatusCode.PARTIAL
        partial = True
        progress = "allowlisted sequence returned candidate payloads (pending review)"
    else:
        status = ResearchJobStatusCode.PARTIAL
        partial = True
        progress = "no tool steps executed (missing identifiers or empty sequence)"

    finished = datetime.now(timezone.utc)
    artifact = _artifact(
        record=record,
        settings=settings,
        status=status,
        started=started,
        finished=finished,
        partial=partial,
        warnings=warnings,
        claims=claims,
        tools_invoked=tools_invoked,
        provenance_extra=provenance_extra,
        failure_details=None if tools_invoked else progress,
    )
    return store.update(
        job_id,
        status=status,
        partial=partial,
        finished_at_utc=finished,
        progress_message=progress,
        artifact=artifact,
        tools_invoked=tools_invoked,
    )  # type: ignore[return-value]


def _artifact(
    *,
    record: JobRecord,
    settings: Settings,
    status: ResearchJobStatusCode,
    started: datetime,
    finished: datetime,
    partial: bool = False,
    warnings: list[str] | None = None,
    claims: list[NormalizedClaim] | None = None,
    failure_details: str | None = None,
    tools_invoked: list[str] | None = None,
    provenance_extra: dict[str, Any] | None = None,
) -> ScientificResearchArtifact:
    provenance: dict[str, Any] = {
        "scaffold": False,
        "tooluniverse_enabled": settings.tooluniverse_enabled,
        "tooluniverse_pin": settings.tooluniverse_version,
        "data_classification": record.request.data_classification,
        "correlation_id": record.request.correlation_id,
        "guidance": "candidate_only_never_canonical",
    }
    if provenance_extra:
        provenance.update(provenance_extra)

    return ScientificResearchArtifact(
        research_artifact_id=f"artifact-{record.job_id}",
        job_id=record.job_id,
        research_request_id=record.request.research_request_id,
        provider_version=__version__,
        workflow=record.request.workflow,
        tooluniverse_version=settings.tooluniverse_version
        if settings.tooluniverse_enabled
        else None,
        status=status,
        partial=partial,
        started_at_utc=started,
        finished_at_utc=finished,
        tools_invoked=tools_invoked or [],
        normalized_claims=claims or [],
        warnings=warnings or [],
        failure_details=failure_details,
        execution_device="cpu",
        provenance=provenance,
    )
