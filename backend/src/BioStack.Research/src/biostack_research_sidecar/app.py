"""FastAPI application factory for the scientific research sidecar."""

from __future__ import annotations

from contextlib import asynccontextmanager
from datetime import datetime, timezone
from typing import Any, AsyncIterator

from fastapi import FastAPI, Header, HTTPException, Request, status
from fastapi.responses import JSONResponse

from biostack_research_sidecar import __version__
from biostack_research_sidecar.auth import require_service_auth
from biostack_research_sidecar.config import Settings, get_settings
from biostack_research_sidecar.contracts.models import (
    ALLOWED_WORKFLOWS,
    CancelJobResponse,
    ResearchJobHandle,
    ResearchJobStatus,
    ResearchJobStatusCode,
    ScientificResearchArtifact,
    ScientificResearchRequest,
)
from biostack_research_sidecar.gpu.capability import detect_gpu_capability
from biostack_research_sidecar.inference.ollama_probe import detect_inference_capability
from biostack_research_sidecar.jobs.runner import JobRunner
from biostack_research_sidecar.jobs.store import InMemoryJobStore
from biostack_research_sidecar.privacy import PrivacyViolation, validate_research_payload


def create_app(settings: Settings | None = None) -> FastAPI:
    settings = settings or get_settings()
    store = InMemoryJobStore(job_ttl_seconds=settings.job_ttl_seconds)
    runner = JobRunner(store, settings)

    @asynccontextmanager
    async def lifespan(_app: FastAPI) -> AsyncIterator[None]:
        yield
        runner.shutdown(wait=False)

    app = FastAPI(
        title="BioStack Scientific Research Sidecar",
        version=__version__,
        description=(
            "Internal BioStack research operations only. "
            "Does not expose unrestricted ToolUniverse execution."
        ),
        lifespan=lifespan,
    )
    app.state.settings = settings
    app.state.job_store = store
    app.state.job_runner = runner

    async def enforce_auth(
        authorization: str | None = Header(default=None),
    ) -> None:
        await require_service_auth(app.state.settings, authorization)

    @app.get("/health")
    def health() -> dict[str, Any]:
        current: Settings = app.state.settings
        current_runner: JobRunner = app.state.job_runner
        return {
            "status": "ok" if not current.global_kill_switch else "disabled",
            "service": "biostack-research-sidecar",
            "version": __version__,
            "global_kill_switch": current.global_kill_switch,
            "tooluniverse_enabled": current.tooluniverse_enabled,
            "jobs_in_flight": current_runner.in_flight,
            "max_concurrent_research_jobs": current_runner.max_workers,
            "timestamp_utc": datetime.now(timezone.utc).isoformat(),
        }

    @app.get("/internal/v1/capabilities/gpu")
    async def gpu_capabilities(
        authorization: str | None = Header(default=None),
    ):
        await enforce_auth(authorization)
        return detect_gpu_capability(app.state.settings)

    @app.get("/internal/v1/capabilities/inference")
    async def inference_capabilities(
        authorization: str | None = Header(default=None),
    ):
        await enforce_auth(authorization)
        return detect_inference_capability(app.state.settings)

    @app.get("/internal/v1/workflows")
    async def list_workflows(
        authorization: str | None = Header(default=None),
    ) -> dict[str, Any]:
        await enforce_auth(authorization)
        current: Settings = app.state.settings
        killed = current.killed_workflows()
        return {
            "allowed_workflows": sorted(ALLOWED_WORKFLOWS),
            "killed_workflows": sorted(killed),
            "tooluniverse_enabled": current.tooluniverse_enabled,
            "note": "No ExecuteAnyTool endpoint exists by design.",
        }

    @app.get("/internal/v1/capabilities/tooluniverse")
    async def tooluniverse_capabilities(
        authorization: str | None = Header(default=None),
    ) -> dict[str, Any]:
        await enforce_auth(authorization)
        current: Settings = app.state.settings
        payload: dict[str, Any] = {
            "enabled": current.tooluniverse_enabled,
            "expected_version": current.tooluniverse_version,
            "pin_document": "docs/pins/TOOLUNIVERSE-PIN.md",
            "install_extra": "tooluniverse",
            "installs_all_extras": False,
            "execute_any_tool": False,
        }
        try:
            from biostack_research_sidecar.tooluniverse_integration.allowlist import (
                load_allowlist,
            )
            from biostack_research_sidecar.tooluniverse_integration.adapter import (
                ToolUniverseAdapter,
            )

            allowlist = load_allowlist(current.tooluniverse_allowlist_path or None)
            adapter = ToolUniverseAdapter(
                allowlist,
                expected_package_version=current.tooluniverse_version,
            )
            try:
                installed = adapter.package_version()
                payload["installed_version"] = installed
                payload["installed"] = True
                payload["version_matches_pin"] = installed == current.tooluniverse_version
            except Exception as exc:
                payload["installed"] = False
                payload["installed_version"] = None
                payload["version_matches_pin"] = False
                payload["install_error"] = str(exc)

            payload["allowlist_version"] = allowlist.allowlist_version
            # Which file was actually loaded — an allowlist you cannot locate is one you
            # cannot audit. Resolution is deterministic and never reads the CWD.
            payload["allowlist_path"] = allowlist.source_path
            payload["approved_tool_count"] = len(allowlist.approved_tools)
            payload["approved_skill_count"] = len(allowlist.approved_skills)
            payload["approved_tools"] = sorted(allowlist.approved_tools)
            payload["approved_skills"] = sorted(allowlist.approved_skills)
        except Exception as exc:
            payload["allowlist_error"] = str(exc)
        return payload

    @app.post(
        "/internal/v1/research/jobs",
        response_model=ResearchJobHandle,
        status_code=status.HTTP_202_ACCEPTED,
    )
    async def submit_job(
        request: Request,
        authorization: str | None = Header(default=None),
    ) -> ResearchJobHandle:
        await enforce_auth(authorization)
        current: Settings = app.state.settings
        payload = await request.json()
        if not isinstance(payload, dict):
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail={"code": "invalid_body", "message": "JSON object required."},
            )

        try:
            validate_research_payload(payload)
        except PrivacyViolation as exc:
            raise HTTPException(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                detail={
                    "code": exc.code,
                    "message": exc.message,
                    "fields": exc.fields,
                },
            ) from exc

        try:
            research_request = ScientificResearchRequest.model_validate(payload)
        except Exception as exc:  # pydantic ValidationError
            raise HTTPException(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                detail={"code": "schema_validation_failed", "message": str(exc)},
            ) from exc

        if research_request.workflow not in ALLOWED_WORKFLOWS:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail={
                    "code": "workflow_not_allowlisted",
                    "message": f"Workflow '{research_request.workflow}' is not allowlisted.",
                },
            )

        if research_request.data_classification not in {
            "public_scientific",
            "public_metadata",
        }:
            raise HTTPException(
                status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
                detail={
                    "code": "data_classification_rejected",
                    "message": "Only public scientific classifications are accepted in v0.1.",
                },
            )

        runner: JobRunner = app.state.job_runner
        if not runner.try_reserve_slot():
            raise HTTPException(
                status_code=status.HTTP_429_TOO_MANY_REQUESTS,
                detail={
                    "code": "max_concurrent_jobs",
                    "message": (
                        f"At capacity ({runner.max_workers} concurrent research jobs). "
                        "Retry after an in-flight job finishes."
                    ),
                },
            )

        try:
            record = store.create(research_request)
        except Exception:
            runner.release_slot()
            raise

        # 202 is honest: work runs off the event loop; poll status/result.
        runner.submit(record.job_id)
        return ResearchJobHandle(
            job_id=record.job_id,
            research_request_id=record.request.research_request_id,
            workflow=record.request.workflow,
            status=ResearchJobStatusCode.QUEUED,
            submitted_at_utc=record.submitted_at_utc,
            correlation_id=record.request.correlation_id,
        )

    @app.get(
        "/internal/v1/research/jobs/{job_id}",
        response_model=ResearchJobStatus,
    )
    async def get_job(
        job_id: str,
        authorization: str | None = Header(default=None),
    ) -> ResearchJobStatus:
        await enforce_auth(authorization)
        record = store.get(job_id)
        if record is None:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail={"code": "job_not_found", "message": job_id},
            )
        return ResearchJobStatus(
            job_id=record.job_id,
            research_request_id=record.request.research_request_id,
            workflow=record.request.workflow,
            status=record.status,
            progress_message=record.progress_message,
            partial=record.partial,
            error_code=record.error_code,
            error_message=record.error_message,
            submitted_at_utc=record.submitted_at_utc,
            updated_at_utc=record.updated_at_utc,
            finished_at_utc=record.finished_at_utc,
            correlation_id=record.request.correlation_id,
        )

    @app.get(
        "/internal/v1/research/jobs/{job_id}/result",
        response_model=ScientificResearchArtifact,
    )
    async def get_result(
        job_id: str,
        authorization: str | None = Header(default=None),
    ) -> ScientificResearchArtifact:
        await enforce_auth(authorization)
        record = store.get(job_id)
        if record is None:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail={"code": "job_not_found", "message": job_id},
            )
        if record.artifact is None:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail={
                    "code": "result_not_ready",
                    "message": f"Job status is {record.status.value}.",
                },
            )
        return record.artifact

    @app.post(
        "/internal/v1/research/jobs/{job_id}/cancel",
        response_model=CancelJobResponse,
    )
    async def cancel_job(
        job_id: str,
        authorization: str | None = Header(default=None),
    ) -> CancelJobResponse:
        await enforce_auth(authorization)
        record = store.request_cancel(job_id)
        if record is None:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail={"code": "job_not_found", "message": job_id},
            )
        return CancelJobResponse(
            job_id=record.job_id,
            status=record.status,
            message="cancel requested"
            if record.status != ResearchJobStatusCode.CANCELLED
            else "cancelled",
        )

    @app.exception_handler(Exception)
    async def unhandled_exception_handler(_request: Request, exc: Exception) -> JSONResponse:
        return JSONResponse(
            status_code=500,
            content={
                "code": "internal_error",
                "message": "Unhandled sidecar error.",
                "detail": str(exc.__class__.__name__),
            },
        )

    return app
