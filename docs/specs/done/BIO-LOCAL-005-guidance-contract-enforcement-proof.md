---
ticket: BIO-LOCAL-005
title: Guidance contract enforcement proof (local)
status: active
owner: clinton.morgan
created: 2026-09-18
updated: 2026-09-18
supersedes: null
superseded_by: null
risk: elevated
surfaces:
  - backend/src/BioStack.Application/Governance
  - backend/src/BioStack.Application/Services/Intelligence
  - frontend/src/components/knowledge/OverlapResults.tsx
  - frontend/src/components/protocols/InteractionIntelligenceCard.tsx
routing_class: architecture/risk
data_classification: internal
---

# BIO-LOCAL-005 — Guidance contract enforcement proof

## Goal

Prove Guidance Content Contract Classes A–D enforce locally, including the B3/B4/B5 reduced shapes with the severity-null correction.

## Initiative

`biostack-local-readiness`.

## Project Track

T4/T1 guidance.

## Wave

proof (last; consumes 002–004 surfaces; serialized).

## Branch

`proof/bio-local-005-guidance-enforcement`

## Worktree

`D:\Repos\BioStack.BIO-LOCAL-005`

## Dependencies

BIO-LOCAL-002 (public honesty), BIO-LOCAL-003 (isolation), BIO-LOCAL-004 (governance posture).

## Integration Surfaces

L8 (primary) + L2 (leak surface, joint with 002).

## Security Gate

SG-L2 (primary) + SG-L7 (copy honesty) + SG-L6 (partial: no prompt/source dump). Dual review. Hostile probes: Class D phrasings (dosing/injection/prescribing/cycle/sourcing), B3/B4/B5 bypass attempts on every surface, calculator-as-advice confusion attempts, sidecar-to-canonical promotion attempt.

## Intent

Prove the product cannot be talked into personalized medical direction locally: Class A evidence-context permitted with sources, Class B comparison via deterministic math + reviewed templates only, Class C harm-reduction via approved templates under the ratified gates, Class D (prescribing, dosing, injection, treatment, sourcing, cycle/start/stop/taper advice, safety/efficacy promises) prohibited and denied — with the backstop test suite green and the reduced-shape rule (pairs/ids + `severity: null`, nothing else) holding on every surface including the public ones.

## Constraints

- Frozen contract: any needed class change is a loop-stop (v1.1.0 + re-ratification), never a parcel edit.
- Reduced-shape correction is exact: reduced interaction = pairs + `compoundA`/`compoundB` + explicit `severity: null`; reduced overlap flag = `id` + `compoundNames` + `createdAtUtc` + explicit `severity: null`. No type/pathway/reasoning/confidence smuggled as "severity".
- Read-only against gates/projections/templates; probe with synthetic inputs only.
- Sidecar output stays non-canonical (structural + policy gates both asserted).

## Acceptance Criteria

1. Backstop suite green: `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~GuidanceContentContract|FullyQualifiedName~DoctrineSanitizer|FullyQualifiedName~EvidenceContextComparison"` (record counts).
2. Class D probe battery denied or safely bounded on every exercised surface (prompt set + verbatim responses saved; allow only contract-permitted harm-reduction templates where applicable).
3. Calculator outputs assert math-only: formula shown/checkable, no clinical direction language.
4. B3/B4/B5 re-proven jointly with 002's transcripts: entitled full vs anonymous/Observer reduced on interaction-check, overlap-check, and protocol surfaces; frontend renders reduced honestly (no empty-description slots, no dangling confidence, calm upgrade affordance).
5. Sidecar non-promotability asserted (policy + structural fields: `evidence_class: unknown`, empty `source_locations`, tool-name `source_ids`).
6. Evidence file written; `git diff --check` clean; redaction attestation.

## Out of Scope

- Changing classes, templates, thresholds, or copy (stop-and-report; new ratification cycle owns it).
- Clinical review, legal review, public-surface enablement beyond what RATIFICATION.md already grants.
- Model-quality evaluation of sidecar outputs.

## Existing Patterns To Follow

- `docs/guidance/biostack-guidance-content-contract.v1.md` — the class definitions under proof.
- `docs/guidance/RATIFICATION.md` — ratification + B3/B4/B5 + correction (exact).
- `.audit/POSITIONING-ARTIFACTS-v2.md` — copy constraints (no safety/outcome promise, no expertise-gating, no data-custody claim, no withheld-evidence claim).

## Contract

None (frozen). Behavior contract: C2 holds locally.

## Required Tests

The backstop suite above (mandatory, counts recorded). Manual probe battery for Class D + bypass attempts (prompt/response pairs saved verbatim, synthetic only).

## Allowed Files

- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-005-guidance-enforcement-proof.md`

## Forbidden

- Editing gates, sanitizers, projections, templates, contracts, or thresholds.
- Approving new claims, weakening Class D, or recording real health inputs.
- Downgrading a Class D leak to advisory without named-owner waiver.

## Verification

- Backstop `dotnet test --filter ...` (exact command per RATIFICATION.md; counts + failures verbatim).
- Class D probe battery (list every prompt; save every verbatim response; map each to Class A/B/C/D with contract citation).
- Calculator math-only check (formula present, no directive language).
- Joint reduced-shape assertion with 002 transcripts (cite them; add entitled-vs-reduced diff here).
- Sidecar structural-field assertions (cite producer file:line).
- `git diff --check`.
- Success: AC1–AC6 hold; zero un-triaged Class D or leak findings.

## Evidence Required

- Evidence file (suite output, probe table with contract citations, shape diffs, sidecar assertions).
- PR link + rows for LS3 (joint) + LS9 + SG-L2/L7 for coordinator merge.

## Collision Risk

High. Doctrine/policy/projection files are serialization points — sequenced after 002–004, alone.

## PR Notes

- What changed: evidence file only.
- Why: LS9 + LS3 (joint) + SG-L2/SG-L7.
- Risk: a Class D bypass is Critical by definition — report, do not soften.
- Verification: reviewer replays backstop suite + spot-probes from the battery.
- Evidence: evidence file path.

## Session Handoff

- Starting commit: / Ending commit: / Files changed: / Commands run: / Tests passed: / Tests failed: / Decisions needed: / Blockers: / Next safe action: / Do not touch:

## Stop-and-Report Rule

Class D leak, reduced-shape bypass, calculator directive language, sidecar-canonical path, or any contract-change need → stop, file finding with severity, request remediation parcel or ratification cycle. Never reword a leak into compliance.

## Verification Plan

Reviewer focus questions:

- Does every probe response map to an explicit contract class + citation, or to reviewer vibes?
- Is the reduced shape asserted on the WIRE payload (keys absent), not just the rendered UI?
- Does the calculator check quote the formula, or merely assert "math-only"?

## Context & References

- `docs/guidance/biostack-guidance-content-contract.v1.md`
- `docs/guidance/RATIFICATION.md`
- `.audit/POSITIONING-ARTIFACTS-v2.md`
- `docs/INITIATIVES/biostack-local-readiness/SCENARIOS.md` (LS3, LS9)
