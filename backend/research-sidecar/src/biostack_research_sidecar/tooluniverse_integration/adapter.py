"""Bounded ToolUniverse adapter.

Only allowlisted tools may execute. No ExecuteAnyTool surface.
"""

from __future__ import annotations

import importlib.metadata
import logging
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any

from biostack_research_sidecar.tooluniverse_integration.allowlist import (
    AllowlistViolation,
    ToolUniverseAllowlist,
    load_allowlist,
)

logger = logging.getLogger(__name__)

PINNED_PACKAGE_VERSION = "1.4.0"


class ToolUniverseAdapterError(RuntimeError):
    def __init__(self, code: str, message: str) -> None:
        super().__init__(message)
        self.code = code
        self.message = message


@dataclass(frozen=True)
class ToolInvocationResult:
    tool_name: str
    arguments: dict[str, Any]
    success: bool
    result: Any
    error_code: str | None
    error_message: str | None
    started_at_utc: datetime
    finished_at_utc: datetime
    package_version: str
    allowlist_version: str


class ToolUniverseAdapter:
    def __init__(
        self,
        allowlist: ToolUniverseAllowlist,
        *,
        expected_package_version: str = PINNED_PACKAGE_VERSION,
    ) -> None:
        self._allowlist = allowlist
        self._expected_package_version = expected_package_version
        self._tu: Any | None = None
        self._loaded_tools: set[str] = set()

    @property
    def allowlist(self) -> ToolUniverseAllowlist:
        return self._allowlist

    def package_version(self) -> str:
        try:
            return importlib.metadata.version("tooluniverse")
        except importlib.metadata.PackageNotFoundError as exc:
            raise ToolUniverseAdapterError(
                "tooluniverse_not_installed",
                "tooluniverse package is not installed. "
                "Install with: uv sync --extra tooluniverse",
            ) from exc

    def assert_version_pin(self) -> str:
        installed = self.package_version()
        if installed != self._expected_package_version:
            raise ToolUniverseAdapterError(
                "tooluniverse_version_mismatch",
                f"Installed tooluniverse=={installed} does not match pin "
                f"{self._expected_package_version}.",
            )
        if installed != self._allowlist.package_version:
            raise ToolUniverseAdapterError(
                "tooluniverse_allowlist_version_mismatch",
                f"Installed tooluniverse=={installed} does not match allowlist package "
                f"{self._allowlist.package_version}.",
            )
        return installed

    def _ensure_client(self) -> Any:
        if self._tu is not None:
            return self._tu
        self.assert_version_pin()
        try:
            from tooluniverse import ToolUniverse
        except ImportError as exc:
            raise ToolUniverseAdapterError(
                "tooluniverse_import_failed",
                "Failed to import tooluniverse. Install with: uv sync --extra tooluniverse",
            ) from exc

        tu = ToolUniverse()
        # Narrow load: only approved tool names (fail closed if unknown).
        tool_names = sorted(self._allowlist.approved_tools)
        try:
            tu.load_tools(include_tools=tool_names)
        except Exception as exc:  # pragma: no cover - upstream API variance
            logger.warning("include_tools load failed (%s); trying categories", exc)
            categories = sorted(self._allowlist.approved_categories)
            tu.load_tools(categories=categories)

        self._tu = tu
        self._loaded_tools = set(tool_names)
        return tu

    def run_tool(
        self,
        tool_name: str,
        arguments: dict[str, Any] | None = None,
    ) -> ToolInvocationResult:
        started = datetime.now(timezone.utc)
        arguments = dict(arguments or {})
        try:
            self._allowlist.assert_tool_allowed(tool_name)
        except AllowlistViolation as exc:
            finished = datetime.now(timezone.utc)
            return ToolInvocationResult(
                tool_name=tool_name,
                arguments=_redact_args(arguments),
                success=False,
                result=None,
                error_code=exc.code,
                error_message=exc.message,
                started_at_utc=started,
                finished_at_utc=finished,
                package_version=self._expected_package_version,
                allowlist_version=self._allowlist.allowlist_version,
            )

        try:
            tu = self._ensure_client()
            raw = tu.run({"name": tool_name, "arguments": arguments})
            finished = datetime.now(timezone.utc)
            # Upstream sometimes returns structured error dicts.
            if isinstance(raw, dict) and raw.get("status") == "error":
                return ToolInvocationResult(
                    tool_name=tool_name,
                    arguments=_redact_args(arguments),
                    success=False,
                    result=raw,
                    error_code="tool_execution_error",
                    error_message=str(raw.get("error") or "ToolUniverse returned error status"),
                    started_at_utc=started,
                    finished_at_utc=finished,
                    package_version=self.package_version(),
                    allowlist_version=self._allowlist.allowlist_version,
                )
            return ToolInvocationResult(
                tool_name=tool_name,
                arguments=_redact_args(arguments),
                success=True,
                result=raw,
                error_code=None,
                error_message=None,
                started_at_utc=started,
                finished_at_utc=finished,
                package_version=self.package_version(),
                allowlist_version=self._allowlist.allowlist_version,
            )
        except ToolUniverseAdapterError as exc:
            finished = datetime.now(timezone.utc)
            return ToolInvocationResult(
                tool_name=tool_name,
                arguments=_redact_args(arguments),
                success=False,
                result=None,
                error_code=exc.code,
                error_message=exc.message,
                started_at_utc=started,
                finished_at_utc=finished,
                package_version=self._expected_package_version,
                allowlist_version=self._allowlist.allowlist_version,
            )
        except Exception as exc:  # pragma: no cover
            finished = datetime.now(timezone.utc)
            logger.exception("ToolUniverse tool failed: %s", tool_name)
            return ToolInvocationResult(
                tool_name=tool_name,
                arguments=_redact_args(arguments),
                success=False,
                result=None,
                error_code="tool_unhandled_exception",
                error_message=f"{type(exc).__name__}: {exc}",
                started_at_utc=started,
                finished_at_utc=finished,
                package_version=self._expected_package_version,
                allowlist_version=self._allowlist.allowlist_version,
            )

def create_adapter(allowlist_path: str | None = None) -> ToolUniverseAdapter:
    return ToolUniverseAdapter(load_allowlist(allowlist_path))


def _redact_args(arguments: dict[str, Any]) -> dict[str, Any]:
    sensitive_keys = {
        "api_key",
        "apikey",
        "token",
        "authorization",
        "password",
        "secret",
    }
    redacted: dict[str, Any] = {}
    for key, value in arguments.items():
        if key.lower().replace("-", "_") in sensitive_keys:
            redacted[key] = "[REDACTED]"
        else:
            redacted[key] = value
    return redacted
