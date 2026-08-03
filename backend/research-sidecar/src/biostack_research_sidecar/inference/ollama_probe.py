"""Ollama inventory probe (read-only capability discovery)."""

from __future__ import annotations

from datetime import datetime, timezone

import httpx

from biostack_research_sidecar.config import Settings
from biostack_research_sidecar.contracts.models import (
    InferenceCapabilityManifest,
    ModelCapabilityProfile,
)


def detect_inference_capability(settings: Settings) -> InferenceCapabilityManifest:
    notes: list[str] = []
    if not settings.local_inference_enabled:
        notes.append("Local inference administratively disabled.")
        return InferenceCapabilityManifest(
            ollama_reachable=False,
            ollama_base_url=settings.ollama_base_url,
            local_inference_enabled=False,
            hosted_fallback_enabled=settings.hosted_fallback_enabled,
            detection_notes=notes,
        )

    base = settings.ollama_base_url.rstrip("/")
    models: list[ModelCapabilityProfile] = []
    version: str | None = None
    reachable = False

    try:
        with httpx.Client(timeout=settings.ollama_timeout_seconds) as client:
            version_resp = client.get(f"{base}/api/version")
            if version_resp.status_code == 200:
                reachable = True
                payload = version_resp.json()
                version = str(payload.get("version") or payload.get("Version") or "")
            tags_resp = client.get(f"{base}/api/tags")
            if tags_resp.status_code == 200:
                reachable = True
                for item in tags_resp.json().get("models", []):
                    name = str(item.get("name") or item.get("model") or "")
                    digest = item.get("digest")
                    details = item.get("details") or {}
                    models.append(
                        ModelCapabilityProfile(
                            provider="ollama",
                            canonical_model_name=name,
                            model_digest=str(digest) if digest else None,
                            quantization=details.get("quantization_level"),
                            advertised_context=None,
                            approval_status="candidate",
                            known_weaknesses=[
                                "Not BioStack-benchmarked; digest-only inventory."
                            ],
                        )
                    )
            else:
                notes.append(f"Ollama /api/tags returned HTTP {tags_resp.status_code}.")
    except httpx.HTTPError as exc:
        notes.append(f"Ollama unreachable: {exc}")
        reachable = False

    # Flag cloud-tagged models as prohibited for default local routes.
    for model in models:
        if ":cloud" in model.canonical_model_name or model.canonical_model_name.endswith(
            "cloud"
        ):
            model.approval_status = "prohibited_default"
            model.known_weaknesses.append(
                "Cloud-tagged model must not be used on local-first routes without explicit approval."
            )

    if reachable and not models:
        notes.append("Ollama reachable but no models listed.")

    return InferenceCapabilityManifest(
        ollama_reachable=reachable,
        ollama_base_url=settings.ollama_base_url,
        ollama_version=version or None,
        local_inference_enabled=settings.local_inference_enabled,
        hosted_fallback_enabled=settings.hosted_fallback_enabled,
        models=models,
        detection_notes=notes,
        detected_at_utc=datetime.now(timezone.utc),
    )
