"""Global and per-workflow kill switches."""

from __future__ import annotations

from biostack_research_sidecar.config import Settings


class KillSwitchError(RuntimeError):
    def __init__(self, code: str, message: str) -> None:
        super().__init__(message)
        self.code = code
        self.message = message


def assert_research_allowed(settings: Settings, workflow: str) -> None:
    if settings.global_kill_switch:
        raise KillSwitchError(
            "global_kill_switch",
            "Scientific research sidecar is globally disabled.",
        )
    if workflow in settings.killed_workflows():
        raise KillSwitchError(
            "workflow_kill_switch",
            f"Workflow '{workflow}' is administratively disabled.",
        )
