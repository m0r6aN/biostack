"""Hosted/local inference policy choke-point tests."""

from __future__ import annotations

import pytest

from biostack_research_sidecar.config import Settings
from biostack_research_sidecar.contracts.models import (
    ScientificExecutionProfile,
    ScientificResearchRequest,
)
from biostack_research_sidecar.inference_policy import (
    InferencePolicyError,
    assert_hosted_inference_allowed,
    assert_local_inference_allowed,
    assert_no_silent_hosted_escalation,
    evaluate_inference_policy,
)


def _request(**overrides: object) -> ScientificResearchRequest:
    base = {
        "subject_name": "semaglutide",
        "workflow": "resolve_compound_identity",
        "local_inference_permitted": True,
        "hosted_inference_permitted": False,
        "execution": ScientificExecutionProfile(
            mode="auto",
            allow_gpu=True,
            allow_cpu_fallback=True,
            allow_hosted_fallback=False,
        ),
    }
    base.update(overrides)
    return ScientificResearchRequest.model_validate(base)


def test_default_policy_allows_local_denies_hosted() -> None:
    settings = Settings(
        host="127.0.0.1",
        service_token="t",
        local_inference_enabled=True,
        hosted_fallback_enabled=False,
    )
    decision = evaluate_inference_policy(settings, _request())
    assert decision.allow_local is True
    assert decision.allow_hosted is False


def test_hosted_requires_all_four_authorizations() -> None:
    settings = Settings(
        host="127.0.0.1",
        service_token="t",
        hosted_fallback_enabled=True,
        local_inference_enabled=True,
    )
    partial = _request(
        hosted_inference_permitted=True,
        execution=ScientificExecutionProfile(
            mode="auto",
            allow_hosted_fallback=True,
        ),
    )
    with pytest.raises(InferencePolicyError) as exc:
        assert_hosted_inference_allowed(settings, partial)
    assert exc.value.code == "hosted_inference_forbidden"

    full = _request(
        hosted_inference_permitted=True,
        execution=ScientificExecutionProfile(
            mode="hosted_fallback_allowed",
            allow_hosted_fallback=True,
        ),
    )
    assert_hosted_inference_allowed(settings, full)


def test_silent_hosted_escalation_rejected_early() -> None:
    settings = Settings(
        host="127.0.0.1",
        service_token="t",
        hosted_fallback_enabled=False,
    )
    request = _request(hosted_inference_permitted=True)
    with pytest.raises(InferencePolicyError):
        assert_no_silent_hosted_escalation(settings, request)


def test_local_inference_can_be_disabled() -> None:
    settings = Settings(
        host="127.0.0.1",
        service_token="t",
        local_inference_enabled=False,
    )
    with pytest.raises(InferencePolicyError) as exc:
        assert_local_inference_allowed(settings, _request())
    assert exc.value.code == "local_inference_forbidden"
