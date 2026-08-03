"""Unit tests for workflow tool sequences (no network)."""

from __future__ import annotations

from datetime import datetime, timezone

from biostack_research_sidecar.tooluniverse_integration.adapter import (
    ToolInvocationResult,
    ToolUniverseAdapter,
)
from biostack_research_sidecar.tooluniverse_integration.allowlist import (
    load_allowlist,
    packaged_allowlist_path,
)
from biostack_research_sidecar.workflows.sequences import (
    WORKFLOW_SEQUENCES,
    run_workflow_sequence,
)

ALLOWLIST = packaged_allowlist_path()


class FakeAdapter(ToolUniverseAdapter):
    def __init__(self) -> None:
        super().__init__(load_allowlist(str(ALLOWLIST)), expected_package_version="1.4.0")
        self.calls: list[tuple[str, dict]] = []

    def run_tool(self, tool_name: str, arguments: dict | None = None):  # type: ignore[override]
        arguments = dict(arguments or {})
        self.calls.append((tool_name, arguments))
        now = datetime.now(timezone.utc)
        # Synthesize identifier harvest for identity tools.
        result: object
        if tool_name == "PubChem_get_CID_by_compound_name":
            result = {"IdentifierList": {"CID": [2244]}}
        elif tool_name == "ChEMBL_search_molecules":
            result = {"molecules": [{"molecule_chembl_id": "CHEMBL25"}]}
        else:
            result = {"ok": True, "tool": tool_name}
        return ToolInvocationResult(
            tool_name=tool_name,
            arguments=arguments,
            success=True,
            result=result,
            error_code=None,
            error_message=None,
            started_at_utc=now,
            finished_at_utc=now,
            package_version="1.4.0",
            allowlist_version="1.0.0",
        )


def test_all_workflows_have_sequences() -> None:
    expected = {
        "resolve_compound_identity",
        "research_compound_evidence",
        "research_published_regimens",
        "research_adverse_events",
        "research_mechanisms_and_targets",
        "research_pathways",
        "refresh_evidence_packet",
    }
    assert set(WORKFLOW_SEQUENCES) == expected


def test_identity_sequence_harvests_identifiers() -> None:
    adapter = FakeAdapter()
    results, claims, skips = run_workflow_sequence(
        adapter, "resolve_compound_identity", "aspirin"
    )
    assert results
    assert any(c["claim_type"].startswith("identity_") for c in claims)
    tools = [name for name, _ in adapter.calls]
    assert "PubChem_get_CID_by_compound_name" in tools
    assert "ChEMBL_search_molecules" in tools
    # Synonym step should run after CID harvest
    assert "PubChem_get_compound_synonyms_by_CID" in tools
    assert not any("missing required" in s for s in skips if "synonyms" in s.lower())


def test_adverse_event_sequence_includes_faers() -> None:
    adapter = FakeAdapter()
    results, claims, _ = run_workflow_sequence(
        adapter, "research_adverse_events", "aspirin", {"chembl_id": "CHEMBL25"}
    )
    tools = [r.tool_name for r in results]
    assert "FAERS_count_reactions_by_drug_event" in tools
    assert any("adverse" in c["claim_type"] for c in claims)


def test_mechanisms_skip_without_identifiers() -> None:
    adapter = FakeAdapter()
    results, _, skips = run_workflow_sequence(
        adapter, "research_mechanisms_and_targets", "unknown-compound"
    )
    # Without chembl/uniprot, mechanism tools should skip rather than call with nulls
    assert skips
    assert all(
        r.tool_name
        not in {
            "ChEMBL_get_molecule_targets",
            "OpenTargets_get_drug_mechanisms_of_action_by_chemblId",
            "UniProt_get_function_by_accession",
        }
        for r in results
    )


def test_regimen_sequence_uses_pubmed() -> None:
    adapter = FakeAdapter()
    results, claims, _ = run_workflow_sequence(
        adapter, "research_published_regimens", "semaglutide"
    )
    assert any(r.tool_name == "PubMed_search_articles" for r in results)
    assert any("regimen" in c["claim_type"] for c in claims)
