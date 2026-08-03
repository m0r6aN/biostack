"""Foundation contract tests for the research sidecar."""

from __future__ import annotations

import time
from typing import Any

import pytest
from fastapi.testclient import TestClient

from biostack_research_sidecar.app import create_app
from biostack_research_sidecar.config import Settings


def _client(**overrides: object) -> TestClient:
    values: dict[str, object] = {
        "host": "127.0.0.1",
        "service_token": "test-token",
        "allow_insecure_dev_auth": False,
        "tooluniverse_enabled": False,
        "global_kill_switch": False,
        "hosted_fallback_enabled": False,
        "max_concurrent_research_jobs": 4,
    }
    values.update(overrides)
    settings = Settings(**values)  # type: ignore[arg-type]
    return TestClient(create_app(settings))


def _auth_headers(token: str = "test-token") -> dict[str, str]:
    return {"Authorization": f"Bearer {token}"}


_IN_FLIGHT = frozenset(
    {
        "queued",
        "resolving_identity",
        "gathering_evidence",
        "normalizing",
    }
)


def _wait_for_terminal(
    client: TestClient, job_id: str, *, timeout: float = 5.0
) -> dict[str, Any]:
    deadline = time.time() + timeout
    last: dict[str, Any] | None = None
    while time.time() < deadline:
        response = client.get(
            f"/internal/v1/research/jobs/{job_id}", headers=_auth_headers()
        )
        assert response.status_code == 200
        last = response.json()
        if last["status"] not in _IN_FLIGHT:
            return last
        time.sleep(0.02)
    raise AssertionError(f"job {job_id} did not reach terminal status; last={last}")


def test_health_ok() -> None:
    client = _client()
    response = client.get("/health")
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "ok"
    assert body["service"] == "biostack-research-sidecar"
    assert body["tooluniverse_enabled"] is False
    assert body["max_concurrent_research_jobs"] == 4
    assert body["jobs_in_flight"] == 0


def test_workflows_require_token_by_default() -> None:
    client = _client()
    denied = client.get("/internal/v1/workflows")
    assert denied.status_code == 401
    allowed = client.get("/internal/v1/workflows", headers=_auth_headers())
    assert allowed.status_code == 200
    body = allowed.json()
    assert "resolve_compound_identity" in body["allowed_workflows"]
    assert "execute_any_tool" not in body["allowed_workflows"]


def test_privacy_boundary_rejects_user_fields() -> None:
    client = _client()
    response = client.post(
        "/internal/v1/research/jobs",
        headers=_auth_headers(),
        json={
            "subject_name": "semaglutide",
            "workflow": "resolve_compound_identity",
            "user_id": "should-not-pass",
            "age": 42,
        },
    )
    assert response.status_code == 422
    detail = response.json()["detail"]
    assert detail["code"] in {
        "privacy_boundary_violation",
        "unknown_request_fields",
    }
    assert any("user_id" in field or "age" in field for field in detail["fields"])


def test_privacy_rejects_health_content_in_subject_name() -> None:
    client = _client()
    response = client.post(
        "/internal/v1/research/jobs",
        headers=_auth_headers(),
        json={
            "subject_name": "retatrutide patient diagnosis type 2 diabetes BMI 34",
            "workflow": "resolve_compound_identity",
            "data_classification": "public_scientific",
        },
    )
    assert response.status_code == 422
    detail = response.json()["detail"]
    # Shape or value scan — either is a correct fail-closed outcome.
    assert detail["code"] in {
        "privacy_value_scan_violation",
        "subject_name_invalid",
    }
    assert "subject_name" in detail["fields"]


def test_privacy_rejects_prose_subject_name_shape() -> None:
    """S2: free-text health prose must not pass even without denylist keywords."""
    client = _client()
    response = client.post(
        "/internal/v1/research/jobs",
        headers=_auth_headers(),
        json={
            "subject_name": "47yo M 92kg on tirzepatide with ongoing nausea notes",
            "workflow": "resolve_compound_identity",
            "data_classification": "public_scientific",
        },
    )
    assert response.status_code == 422
    detail = response.json()["detail"]
    assert detail["code"] in {
        "subject_name_invalid",
        "privacy_value_scan_violation",
    }


def test_privacy_rejects_unknown_identifier_keys() -> None:
    client = _client()
    response = client.post(
        "/internal/v1/research/jobs",
        headers=_auth_headers(),
        json={
            "subject_name": "semaglutide",
            "workflow": "resolve_compound_identity",
            "known_identifiers": {
                "patient_mrn": "MRN-12345",
                "cid": "56842117",
            },
            "data_classification": "public_scientific",
        },
    )
    assert response.status_code == 422
    detail = response.json()["detail"]
    assert detail["code"] == "known_identifiers_key_not_allowlisted"
    assert any("patient_mrn" in field for field in detail["fields"])


def test_privacy_accepts_allowlisted_identifier_keys() -> None:
    client = _client()
    response = client.post(
        "/internal/v1/research/jobs",
        headers=_auth_headers(),
        json={
            "subject_name": "semaglutide",
            "workflow": "resolve_compound_identity",
            "known_identifiers": {"cid": "56842117", "chembl_id": "CHEMBL1201759"},
            "data_classification": "public_scientific",
        },
    )
    assert response.status_code == 202
    handle = response.json()
    assert handle["status"] == "queued"
    terminal = _wait_for_terminal(client, handle["job_id"])
    assert terminal["status"] == "partial"  # ToolUniverse disabled scaffold


def test_privacy_rejects_unknown_top_level_fields() -> None:
    client = _client()
    response = client.post(
        "/internal/v1/research/jobs",
        headers=_auth_headers(),
        json={
            "subject_name": "semaglutide",
            "workflow": "resolve_compound_identity",
            "dob": "1990-01-01",
        },
    )
    assert response.status_code == 422
    assert response.json()["detail"]["code"] in {
        "unknown_request_fields",
        "privacy_boundary_violation",
    }


def test_submit_job_returns_202_queued_then_partial_when_tooluniverse_disabled() -> None:
    client = _client()
    response = client.post(
        "/internal/v1/research/jobs",
        headers=_auth_headers(),
        json={
            "subject_name": "semaglutide",
            "workflow": "research_compound_evidence",
            "correlation_id": "corr-1",
            "data_classification": "public_scientific",
        },
    )
    assert response.status_code == 202
    handle = response.json()
    assert handle["status"] == "queued"
    job_id = handle["job_id"]

    status_body = _wait_for_terminal(client, job_id)
    assert status_body["partial"] is True
    assert status_body["status"] == "partial"

    result = client.get(
        f"/internal/v1/research/jobs/{job_id}/result",
        headers=_auth_headers(),
    )
    assert result.status_code == 200
    artifact = result.json()
    assert artifact["partial"] is True
    assert artifact["normalized_claims"]
    assert artifact["provenance"]["scaffold"] is True


def test_global_kill_switch_rejects_policy() -> None:
    client = _client(global_kill_switch=True)
    response = client.post(
        "/internal/v1/research/jobs",
        headers=_auth_headers(),
        json={
            "subject_name": "semaglutide",
            "workflow": "resolve_compound_identity",
        },
    )
    assert response.status_code == 202
    assert response.json()["status"] == "queued"
    terminal = _wait_for_terminal(client, response.json()["job_id"])
    assert terminal["status"] == "rejected_by_policy"


def test_partial_hosted_flags_fail_closed() -> None:
    client = _client()
    response = client.post(
        "/internal/v1/research/jobs",
        headers=_auth_headers(),
        json={
            "subject_name": "semaglutide",
            "workflow": "resolve_compound_identity",
            "hosted_inference_permitted": True,
            "execution": {
                "mode": "auto",
                "allow_hosted_fallback": False,
            },
            "data_classification": "public_scientific",
        },
    )
    assert response.status_code == 202
    terminal = _wait_for_terminal(client, response.json()["job_id"])
    assert terminal["status"] == "rejected_by_policy"


def test_service_token_required_when_configured() -> None:
    settings = Settings(
        host="127.0.0.1",
        service_token="secret-token",
        tooluniverse_enabled=False,
    )
    client = TestClient(create_app(settings))
    denied = client.get("/internal/v1/workflows")
    assert denied.status_code == 401
    allowed = client.get(
        "/internal/v1/workflows",
        headers={"Authorization": "Bearer secret-token"},
    )
    assert allowed.status_code == 200


def test_non_loopback_without_token_rejected_at_settings() -> None:
    with pytest.raises(ValueError, match="SERVICE_TOKEN is required"):
        Settings(host="0.0.0.0", service_token="", allow_insecure_dev_auth=False)


def test_health_stays_responsive_while_job_runs(monkeypatch: pytest.MonkeyPatch) -> None:
    """S5: long jobs must not freeze /health (event-loop honesty)."""
    import biostack_research_sidecar.workflows.executor as executor_mod

    original = executor_mod.execute_research_job

    def slow_execute(store, record, settings):  # type: ignore[no-untyped-def]
        time.sleep(0.3)
        return original(store, record, settings)

    monkeypatch.setattr(executor_mod, "execute_research_job", slow_execute)
    # JobRunner imported execute_research_job at module load — patch runner binding too.
    import biostack_research_sidecar.jobs.runner as runner_mod

    monkeypatch.setattr(runner_mod, "execute_research_job", slow_execute)

    client = _client()
    submit = client.post(
        "/internal/v1/research/jobs",
        headers=_auth_headers(),
        json={
            "subject_name": "semaglutide",
            "workflow": "resolve_compound_identity",
            "data_classification": "public_scientific",
        },
    )
    assert submit.status_code == 202
    assert submit.json()["status"] == "queued"

    health = client.get("/health")
    assert health.status_code == 200
    assert health.json()["status"] == "ok"

    _wait_for_terminal(client, submit.json()["job_id"], timeout=5.0)


def test_max_concurrent_jobs_returns_429(monkeypatch: pytest.MonkeyPatch) -> None:
    """S5: concurrency cap is enforced at submit, not define-only."""
    import threading

    import biostack_research_sidecar.jobs.runner as runner_mod

    gate = threading.Event()

    def blocking_execute(store, record, settings):  # type: ignore[no-untyped-def]
        gate.wait(timeout=5.0)
        from biostack_research_sidecar.workflows.executor import (
            execute_research_job as real,
        )

        return real(store, record, settings)

    monkeypatch.setattr(runner_mod, "execute_research_job", blocking_execute)

    client = _client(max_concurrent_research_jobs=1)

    first = client.post(
        "/internal/v1/research/jobs",
        headers=_auth_headers(),
        json={
            "subject_name": "semaglutide",
            "workflow": "resolve_compound_identity",
            "data_classification": "public_scientific",
        },
    )
    assert first.status_code == 202

    second = client.post(
        "/internal/v1/research/jobs",
        headers=_auth_headers(),
        json={
            "subject_name": "tirzepatide",
            "workflow": "resolve_compound_identity",
            "data_classification": "public_scientific",
        },
    )
    assert second.status_code == 429
    assert second.json()["detail"]["code"] == "max_concurrent_jobs"

    gate.set()
    _wait_for_terminal(client, first.json()["job_id"])
