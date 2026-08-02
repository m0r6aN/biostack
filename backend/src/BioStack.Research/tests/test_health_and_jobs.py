"""Foundation contract tests for the research sidecar."""

from __future__ import annotations

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
    }
    values.update(overrides)
    settings = Settings(**values)  # type: ignore[arg-type]
    return TestClient(create_app(settings))


def _auth_headers(token: str = "test-token") -> dict[str, str]:
    return {"Authorization": f"Bearer {token}"}


def test_health_ok() -> None:
    client = _client()
    response = client.get("/health")
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "ok"
    assert body["service"] == "biostack-research-sidecar"
    assert body["tooluniverse_enabled"] is False


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
    assert detail["code"] == "privacy_value_scan_violation"
    assert "subject_name" in detail["fields"]


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


def test_submit_job_returns_partial_when_tooluniverse_disabled() -> None:
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
    assert handle["status"] == "partial"
    job_id = handle["job_id"]

    status = client.get(f"/internal/v1/research/jobs/{job_id}", headers=_auth_headers())
    assert status.status_code == 200
    assert status.json()["partial"] is True

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
    assert response.json()["status"] == "rejected_by_policy"


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
    assert response.json()["status"] == "rejected_by_policy"


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
