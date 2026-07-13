# Parcel: PR-DOC-001

## Goal
Establish durable, evidence-reconciled BioStack production-readiness initiative state.

## Initiative
`biostack-production-readiness`

## Project Track
Evidence/release documentation

## Wave
Foundation

## Branch
`codex/production-readiness-phase2`

## Worktree
`D:/Repos/BioStack-release-governance`

## Dependencies
- None

## Integration Surfaces
- S1 through S6, documentation only

## Security Gate
Security review required before release; no security analysis is claimed by this parcel.

## Allowed Files
- `docs/INITIATIVES/biostack-production-readiness/**`
- `docs/launch-readiness-ledger.md`

## Forbidden
- Code, configuration, workflows, dependencies, deployments, secrets, live data, commits, pushes, PRs and production mutation.

## Out of Scope
Closing implementation, security, human-approval or live-environment gates.

## Existing Patterns To Follow
- `docs/launch-readiness-ledger.md` for evidence vocabulary and NO-GO discipline.
- Initiative Coordination and Parcel-Driven Development artifact contracts.

## Contract
Status is valid only for a named commit, environment, configuration, procedure, time and artifact. Unknown or missing evidence cannot become passing.

## Required Tests
No automated product tests required. Documentation scope, links and diff hygiene require manual verification.

## Acceptance Criteria
- Required initiative artifacts exist and agree on baseline `a37726a` and NO-GO/HOLD.
- Latest workflow evidence is accurate.
- Monthly-only implementation is separated from human/live configuration approval.
- Only allowed files change and `git diff --check` passes.

## Verification
- `rtk git status --short`
- `rtk git diff --check`
- `rtk git diff --name-only`
- PowerShell Markdown link/required-file checks through `rtk proxy`

## Evidence Required
- Diff, required-file list, link check, workflow run IDs, session handoff.

## Collision Risk
Low inside the initiative directory; medium for the shared launch ledger. Serialize ledger edits.

## PR Notes
- What changed: durable initiative control plane and reconciled evidence.
- Why: multi-session release work requires persistent, truthful state.
- Risk: documentation may drift; future sessions must reconcile before updating status.
- Verification: scope, links and diff hygiene.
- Evidence: `EVIDENCE.md` and `SESSION-HANDOFF.md`.

## Stop-and-Report Rule
Stop on files outside Allowed Files, contract changes, unclear security boundaries, or any request to upgrade a gate without required evidence.
