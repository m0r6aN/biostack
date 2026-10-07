"""Runtime configuration for the research sidecar."""

from __future__ import annotations

from functools import lru_cache
from typing import Literal

from pydantic import field_validator, model_validator
from pydantic_settings import BaseSettings, SettingsConfigDict

ExecutionMode = Literal[
    "auto",
    "gpu_preferred",
    "gpu_required",
    "cpu_only",
    "hosted_fallback_allowed",
]

_LOOPBACK_HOSTS = frozenset({"127.0.0.1", "::1", "localhost"})


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_prefix="BIOSTACK_RESEARCH_",
        env_file=".env",
        extra="ignore",
    )

    # Bind loopback by default. Non-loopback requires a service token (see validator).
    host: str = "127.0.0.1"
    port: int = 8080
    log_level: str = "info"

    # Service auth: required unless allow_insecure_dev_auth is explicitly true on loopback.
    service_token: str = ""
    # Dev/test only. Never enable with a non-loopback host.
    allow_insecure_dev_auth: bool = False

    global_kill_switch: bool = False
    # Comma-separated workflow ids
    workflow_kills: str = ""

    default_execution_mode: ExecutionMode = "auto"
    gpu_enabled: bool = True
    max_concurrent_gpu_jobs: int = 1
    max_concurrent_research_jobs: int = 4
    job_ttl_seconds: int = 86_400

    ollama_base_url: str = "http://127.0.0.1:11434"
    ollama_timeout_seconds: float = 30.0
    local_inference_enabled: bool = True
    # Global kill for any hosted inference path. Default fail-closed.
    hosted_fallback_enabled: bool = False

    # Runtime enablement is separate from package install (uv --extra tooluniverse).
    tooluniverse_enabled: bool = False
    # Must match docs/pins/TOOLUNIVERSE-PIN.md and allowlist package version.
    tooluniverse_version: str = "1.4.0"
    tooluniverse_allowlist_path: str = ""

    @field_validator("default_execution_mode", mode="before")
    @classmethod
    def _normalize_mode(cls, value: object) -> object:
        if isinstance(value, str):
            return value.strip().lower()
        return value

    @field_validator("host", mode="before")
    @classmethod
    def _strip_host(cls, value: object) -> object:
        if isinstance(value, str):
            return value.strip()
        return value

    @model_validator(mode="after")
    def _enforce_bind_auth_policy(self) -> Settings:
        host = (self.host or "").lower()
        loopback = host in _LOOPBACK_HOSTS
        token = self.service_token.strip()

        if not loopback and not token:
            raise ValueError(
                "BIOSTACK_RESEARCH_SERVICE_TOKEN is required when host is not loopback "
                f"(host={self.host!r}). Refusing unauthenticated non-loopback bind."
            )

        if not loopback and self.allow_insecure_dev_auth:
            raise ValueError(
                "BIOSTACK_RESEARCH_ALLOW_INSECURE_DEV_AUTH cannot be true when host is "
                "not loopback."
            )

        if not token and not self.allow_insecure_dev_auth:
            # Explicit empty token without opt-in is only a config object construction risk;
            # auth layer also enforces. Documented for operators.
            pass

        return self

    def is_loopback_host(self) -> bool:
        return (self.host or "").lower() in _LOOPBACK_HOSTS

    def killed_workflows(self) -> set[str]:
        if not self.workflow_kills.strip():
            return set()
        return {part.strip() for part in self.workflow_kills.split(",") if part.strip()}


@lru_cache
def get_settings() -> Settings:
    return Settings()
