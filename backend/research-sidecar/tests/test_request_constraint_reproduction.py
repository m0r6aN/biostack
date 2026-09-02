"""Reproduce request constraints that are declared but not enforced."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any
from unittest.mock import MagicMock

import pytest

from biostack_research_sidecar.config import Settings
from biostack_research_sidecar.contracts.models import ScientificResearchRequest
from biostack_research_sidecar.jobs.store import InMemoryJobStore
from biostack_research_sidecar.workflows.executor import execute_research_job


@dataclass
class _FakeResult:
    tool_name: str
    success: bool = True
    error_code: str | None = None
    error_message: str | None = None
    arguments: dict[str, Any] | None = None

    def __post_init__(self) -> None:
        if self.arguments is None:
            self.arguments = {}


def test_maximum_source_count_bounds_accepted_results(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    """The executor must not accept more sources than the request permits."""
    import biostack_research_sidecar.tooluniverse_integration.adapter as adapter_mod
    import biostack_research_sidecar.tooluniverse_integration.allowlist as allowlist_mod
    import biostack_research_sidecar.workflows.sequences as sequences_mod

    allowlist = MagicMock()
    allowlist.allowlist_version = "test-only"
    allowlist.skills_for_workflow.return_value = ("synthetic-skill",)
    monkeypatch.setattr(allowlist_mod, "load_allowlist", lambda _path=None: allowlist)
    monkeypatch.setattr(adapter_mod, "create_adapter", lambda _path=None: MagicMock())
    monkeypatch.setattr(
        sequences_mod,
        "run_workflow_sequence",
        lambda *_args, **_kwargs: (
            [_FakeResult("Synthetic_source_one"), _FakeResult("Synthetic_source_two")],
            [],
            [],
        ),
    )

    settings = Settings(
        host="127.0.0.1",
        service_token="test-only",
        tooluniverse_enabled=True,
        allow_insecure_dev_auth=False,
    )
    store = InMemoryJobStore()
    request = ScientificResearchRequest.model_validate(
        {
            "subject_name": "SyntheticCompound",
            "workflow": "resolve_compound_identity",
            "known_identifiers": {"cid": "1"},
            "maximum_source_count": 1,
            "data_classification": "public_scientific",
        }
    )
    updated = execute_research_job(store, store.create(request), settings)

    assert len(updated.tools_invoked) <= request.maximum_source_count, (
        f"The request allowed {request.maximum_source_count} source, but the executor "
        f"accepted {len(updated.tools_invoked)} results."
    )
