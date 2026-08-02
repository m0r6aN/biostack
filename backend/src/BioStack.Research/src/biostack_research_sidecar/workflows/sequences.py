"""Allowlisted ToolUniverse tool sequences per BioStack research workflow.

Only tools present on the allowlist may appear here. Sequences are ordered
candidate probes; they never write canonical knowledge.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Callable

from biostack_research_sidecar.tooluniverse_integration.adapter import (
    ToolInvocationResult,
    ToolUniverseAdapter,
)


@dataclass(frozen=True)
class SequenceStep:
    tool_name: str
    build_arguments: Callable[[dict[str, Any]], dict[str, Any]]
    claim_type: str
    claim_template: str


def _context(subject_name: str, known_identifiers: dict[str, str] | None = None) -> dict[str, Any]:
    ids = {k.lower(): v for k, v in (known_identifiers or {}).items()}
    return {
        "subject_name": subject_name,
        "chembl_id": ids.get("chembl")
        or ids.get("chembl_id")
        or ids.get("molecule_chembl_id"),
        "pubchem_cid": ids.get("cid") or ids.get("pubchem_cid") or ids.get("pubchem"),
        "uniprot": ids.get("uniprot") or ids.get("accession"),
        "pmid": ids.get("pmid"),
    }


def _identity_steps() -> list[SequenceStep]:
    return [
        SequenceStep(
            tool_name="PubChem_get_CID_by_compound_name",
            build_arguments=lambda ctx: {"name": ctx["subject_name"]},
            claim_type="identity_pubchem_cid",
            claim_template="PubChem CID candidate lookup for '{subject_name}'.",
        ),
        SequenceStep(
            tool_name="PubChem_get_compound_synonyms_by_CID",
            build_arguments=lambda ctx: {"cid": ctx["pubchem_cid"]}
            if ctx.get("pubchem_cid")
            else {"cid": None},
            claim_type="identity_pubchem_synonyms",
            claim_template="PubChem synonym candidate lookup for '{subject_name}'.",
        ),
        SequenceStep(
            tool_name="ChEMBL_search_molecules",
            build_arguments=lambda ctx: {
                "query": ctx["subject_name"],
                "pref_name__contains": ctx["subject_name"],
                "limit": 5,
            },
            claim_type="identity_chembl_search",
            claim_template="ChEMBL molecule candidate search for '{subject_name}'.",
        ),
    ]


def _literature_steps(query_suffix: str, claim_type: str) -> list[SequenceStep]:
    return [
        SequenceStep(
            tool_name="PubMed_search_articles",
            build_arguments=lambda ctx, suffix=query_suffix: {
                "query": f"{ctx['subject_name']} {suffix}".strip(),
                "limit": 10,
                "include_abstract": True,
            },
            claim_type=claim_type,
            claim_template=f"PubMed literature candidates ({query_suffix}) for '{{subject_name}}'.",
        ),
    ]


def _adverse_event_steps() -> list[SequenceStep]:
    return [
        SequenceStep(
            tool_name="FAERS_count_reactions_by_drug_event",
            build_arguments=lambda ctx: {"medicinalproduct": ctx["subject_name"]},
            claim_type="adverse_event_faers_signal",
            claim_template=(
                "FAERS reaction-count candidates for '{subject_name}'. "
                "Spontaneous reports are not incidence or proven causation."
            ),
        ),
        SequenceStep(
            tool_name="OpenTargets_get_drug_warnings_by_chemblId",
            build_arguments=lambda ctx: {"chemblId": ctx["chembl_id"]}
            if ctx.get("chembl_id")
            else {"chemblId": None},
            claim_type="adverse_event_opentargets_warnings",
            claim_template="Open Targets drug-warning candidates for '{subject_name}'.",
        ),
        *_literature_steps("adverse events safety discontinuation", "adverse_event_literature"),
    ]


def _mechanism_steps() -> list[SequenceStep]:
    return [
        SequenceStep(
            tool_name="ChEMBL_get_molecule_targets",
            build_arguments=lambda ctx: {
                "molecule_chembl_id": ctx["chembl_id"],
                "limit": 25,
            }
            if ctx.get("chembl_id")
            else {"molecule_chembl_id": None},
            claim_type="mechanism_chembl_targets",
            claim_template="ChEMBL target candidates for '{subject_name}'.",
        ),
        SequenceStep(
            tool_name="OpenTargets_get_drug_mechanisms_of_action_by_chemblId",
            build_arguments=lambda ctx: {"chemblId": ctx["chembl_id"]}
            if ctx.get("chembl_id")
            else {"chemblId": None},
            claim_type="mechanism_opentargets",
            claim_template="Open Targets mechanism-of-action candidates for '{subject_name}'.",
        ),
        SequenceStep(
            tool_name="UniProt_get_function_by_accession",
            build_arguments=lambda ctx: {"accession": ctx["uniprot"]}
            if ctx.get("uniprot")
            else {"accession": None},
            claim_type="mechanism_uniprot_function",
            claim_template="UniProt function candidates for '{subject_name}'.",
        ),
    ]


def _pathway_steps() -> list[SequenceStep]:
    # Pathway intelligence reuses target/mechanism surfaces until Reactome tools
    # are explicitly allowlisted and pinned.
    return [
        *_mechanism_steps(),
        *_literature_steps("pathway mechanism systems biology", "pathway_literature"),
    ]


def _regimen_steps() -> list[SequenceStep]:
    return [
        *_literature_steps(
            "dose titration escalation schedule initiation maintenance route",
            "published_regimen_literature",
        ),
        *_literature_steps(
            "clinical trial dosing regimen weekly mg",
            "published_regimen_trial_literature",
        ),
    ]


def _evidence_packet_steps() -> list[SequenceStep]:
    return [
        *_identity_steps(),
        *_literature_steps("clinical trial efficacy safety", "compound_evidence_literature"),
        *_adverse_event_steps()[:1],  # FAERS once; avoid triple literature fan-out
        *_mechanism_steps()[:2],
    ]


WORKFLOW_SEQUENCES: dict[str, Callable[[], list[SequenceStep]]] = {
    "resolve_compound_identity": _identity_steps,
    "research_compound_evidence": _evidence_packet_steps,
    "research_published_regimens": _regimen_steps,
    "research_adverse_events": _adverse_event_steps,
    "research_mechanisms_and_targets": _mechanism_steps,
    "research_pathways": _pathway_steps,
    "refresh_evidence_packet": _evidence_packet_steps,
}


def _enrich_context_from_result(
    ctx: dict[str, Any],
    tool_name: str,
    result: ToolInvocationResult,
) -> None:
    if not result.success or result.result is None:
        return
    payload = result.result
    # Best-effort identifier harvesting; shapes vary by tool.
    text = str(payload)
    if tool_name.startswith("PubChem_get_CID") and not ctx.get("pubchem_cid"):
        # Common shapes: list of ints, {"IdentifierList": {"CID": [...]}}
        if isinstance(payload, list) and payload:
            ctx["pubchem_cid"] = str(payload[0])
        elif isinstance(payload, dict):
            ids = payload.get("IdentifierList") or payload.get("identifier_list") or {}
            cids = ids.get("CID") or ids.get("cid") or []
            if cids:
                ctx["pubchem_cid"] = str(cids[0])
            elif "cid" in payload:
                ctx["pubchem_cid"] = str(payload["cid"])
    if "chembl" in tool_name.lower() and not ctx.get("chembl_id"):
        if isinstance(payload, dict):
            mols = payload.get("molecules") or payload.get("molecule") or payload
            if isinstance(mols, list) and mols:
                first = mols[0]
                if isinstance(first, dict):
                    chembl = first.get("molecule_chembl_id") or first.get("chembl_id")
                    if chembl:
                        ctx["chembl_id"] = str(chembl)
            chembl = payload.get("molecule_chembl_id") or payload.get("chemblId")
            if chembl:
                ctx["chembl_id"] = str(chembl)
        # Fallback scan
        import re

        match = re.search(r"CHEMBL\d+", text, re.IGNORECASE)
        if match and not ctx.get("chembl_id"):
            ctx["chembl_id"] = match.group(0).upper()


def run_workflow_sequence(
    adapter: ToolUniverseAdapter,
    workflow: str,
    subject_name: str,
    known_identifiers: dict[str, str] | None = None,
) -> tuple[list[ToolInvocationResult], list[dict[str, Any]], list[str]]:
    """Execute the allowlisted sequence for a workflow.

    Returns (results, claim_dicts, skip_warnings).
    """
    builder = WORKFLOW_SEQUENCES.get(workflow)
    if builder is None:
        return [], [], [f"No tool sequence registered for workflow '{workflow}'."]

    ctx = _context(subject_name, known_identifiers)
    results: list[ToolInvocationResult] = []
    claims: list[dict[str, Any]] = []
    skips: list[str] = []

    for step in builder():
        args = step.build_arguments(ctx)
        # Skip steps that require identifiers we do not yet have.
        if any(value is None for value in args.values()):
            missing = [k for k, v in args.items() if v is None]
            skips.append(
                f"Skipped {step.tool_name}: missing required argument(s) {missing}."
            )
            continue
        result = adapter.run_tool(step.tool_name, args)
        results.append(result)
        _enrich_context_from_result(ctx, step.tool_name, result)
        if result.success:
            claims.append(
                {
                    "claim_type": step.claim_type,
                    "text": step.claim_template.format(subject_name=subject_name),
                    "tool_name": step.tool_name,
                }
            )

    return results, claims, skips
