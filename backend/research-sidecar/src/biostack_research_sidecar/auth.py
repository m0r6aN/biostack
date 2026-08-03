"""Service authentication for internal callers.

Default is fail-closed: a bearer token is required unless insecure loopback
dev auth is explicitly enabled.
"""

from __future__ import annotations

import hmac

from fastapi import Header, HTTPException, status

from biostack_research_sidecar.config import Settings


async def require_service_auth(
    settings: Settings,
    authorization: str | None = Header(default=None),
) -> None:
    expected = settings.service_token.strip()

    if not expected:
        if settings.allow_insecure_dev_auth and settings.is_loopback_host():
            # Explicit local-only test/dev escape hatch.
            return
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail={
                "code": "service_token_not_configured",
                "message": (
                    "Service token is not configured. Set BIOSTACK_RESEARCH_SERVICE_TOKEN, "
                    "or enable BIOSTACK_RESEARCH_ALLOW_INSECURE_DEV_AUTH only on loopback."
                ),
            },
        )

    if not authorization or not authorization.startswith("Bearer "):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail={"code": "missing_bearer_token", "message": "Bearer token required."},
        )
    token = authorization.removeprefix("Bearer ").strip()
    if not hmac.compare_digest(token, expected):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail={"code": "invalid_bearer_token", "message": "Invalid service token."},
        )
