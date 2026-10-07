"""Single choke point for local vs hosted inference policy.

Today the executor does not run model inference. This module exists so any
future inference path fails closed unless policy is explicitly satisfied.

Flags (all must be considered together):
- Settings.hosted_fallback_enabled (global operator switch, default false)
- Settings.local_inference_enabled
- Request.local_inference_permitted
- Request.hosted_inference_permitted
- Request.execution.allow_hosted_fallback
- Request.execution.mode == hosted_fallback_allowed
"""

from __future__ import annotations

from dataclasses import dataclass

from biostack_research_sidecar.config import Settings
from biostack_research_sidecar.contracts.models import ScientificResearchRequest


class InferencePolicyError(RuntimeError):
    def __init__(self, code: str, message: str) -> None:
        super().__init__(message)
        self.code = code
        self.message = message


@dataclass(frozen=True)
class InferencePolicyDecision:
    allow_local: bool
    allow_hosted: bool
    reasons: tuple[str, ...]


def evaluate_inference_policy(
    settings: Settings,
    request: ScientificResearchRequest,
) -> InferencePolicyDecision:
    """Return whether local/hosted inference is permitted. Does not execute models."""
    reasons: list[str] = []

    allow_local = bool(settings.local_inference_enabled and request.local_inference_permitted)
    if not allow_local:
        reasons.append("local_inference_disabled_or_not_permitted")

    # Hosted requires EVERY explicit enablement flag. Default is fail-closed.
    request_wants_hosted = bool(
        request.hosted_inference_permitted
        or request.execution.allow_hosted_fallback
        or request.execution.mode == "hosted_fallback_allowed"
    )
    allow_hosted = bool(
        settings.hosted_fallback_enabled
        and request.hosted_inference_permitted
        and request.execution.allow_hosted_fallback
        and request.execution.mode == "hosted_fallback_allowed"
    )

    if request_wants_hosted and not allow_hosted:
        reasons.append("hosted_inference_requested_but_not_fully_authorized")
    if not settings.hosted_fallback_enabled:
        reasons.append("hosted_fallback_globally_disabled")

    return InferencePolicyDecision(
        allow_local=allow_local,
        allow_hosted=allow_hosted,
        reasons=tuple(reasons),
    )


def assert_hosted_inference_allowed(
    settings: Settings,
    request: ScientificResearchRequest,
) -> None:
    """Choke point: call before any hosted model invocation.

    Raises InferencePolicyError unless hosted is fully authorized.
    """
    decision = evaluate_inference_policy(settings, request)
    if decision.allow_hosted:
        return
    raise InferencePolicyError(
        "hosted_inference_forbidden",
        "Hosted inference is not authorized. "
        "Require BIOSTACK_RESEARCH_HOSTED_FALLBACK_ENABLED=true, "
        "request.hosted_inference_permitted=true, "
        "execution.allow_hosted_fallback=true, and "
        "execution.mode=hosted_fallback_allowed. "
        f"Reasons: {', '.join(decision.reasons) or 'policy_denied'}.",
    )


def assert_local_inference_allowed(
    settings: Settings,
    request: ScientificResearchRequest,
) -> None:
    """Choke point: call before any local model invocation."""
    decision = evaluate_inference_policy(settings, request)
    if decision.allow_local:
        return
    raise InferencePolicyError(
        "local_inference_forbidden",
        "Local inference is not authorized. "
        "Require BIOSTACK_RESEARCH_LOCAL_INFERENCE_ENABLED=true and "
        "request.local_inference_permitted=true. "
        f"Reasons: {', '.join(decision.reasons) or 'policy_denied'}.",
    )


def assert_no_silent_hosted_escalation(
    settings: Settings,
    request: ScientificResearchRequest,
) -> None:
    """Fail closed if the request asks for hosted paths that policy would deny.

    Call at job start so misconfigured clients fail early rather than later
    when inference is wired.
    """
    wants_hosted = bool(
        request.hosted_inference_permitted
        or request.execution.allow_hosted_fallback
        or request.execution.mode == "hosted_fallback_allowed"
    )
    if not wants_hosted:
        return
    # If any hosted flag is set, require full authorization now.
    assert_hosted_inference_allowed(settings, request)
