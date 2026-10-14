# Session handoff — resume 2

Initiative: `biostack-go-live`
Date: 2026-10-14
Agent: coordinator resume session
Decision: **NO-GO / HOLD**
This file does not authorize production release and does not change `docs/INITIATIVES/biostack-production-readiness/`.

## Starting state

- Branch: `dev`
- Worktree: `D:/Repos/BioStack` (only worktree)
- `git rev-parse HEAD`: `6c9d8eccfd69f848d69c223e547c4d40dbf6ccee`
- Matches the handoff pin and the previous resume pin.

Uncommitted work was already present. This session did not revert, stage, or edit it:

- `backend/src/BioStack.KnowledgeWorker/Pipeline/TrustGate.cs`
- `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
- `backend/tests/BioStack.KnowledgeWorker.Tests/IngestionPipelineTests.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/TrustGateTests.cs`
- `research/review-decisions/review-decision-batch-2026-10-14-go-live-library.json`

## What this session did

Shaped three review-candidate ticket specs and registered them. No builder was dispatched. No product code was implemented. No production database was written. No Stripe call. No secret was printed. No deploy.

The spec linter `docs/specs/scripts/validate-spec.ps1` returned `valid` for all three.

Read-only GETs on 2026-10-14, this session:

| Probe | Result |
|---|---|
| `GET https://api.biostack.cc/api/v1/knowledge/compounds/Glycine` | First request canceled at 20,069 ms. Retry returned 200 in 250 ms. `classification` **Unknown**. |
| `GET https://api.biostack.cc/api/v1/knowledge/compounds/Retatrutide` | 200 in 9,856 ms. `classification` **Unknown**. |
| `GET https://api.biostack.cc/openapi/v1.json` | **200** in 450 ms. 92,651 bytes. F-001 still open. |
| `GET https://api.biostack.cc/scalar/v1` | **200** in 306 ms. F-001 still open. |

The full compound list was not requested again. The previous resume already measured a warm 158-item list. This session's first Glycine timeout is another cold-start observation.

## Constraint found while shaping

`KnowledgeEntries.Classification` is an integer column. `CompoundCategory` has no Amino Acid or Research Compound member, and `SubstanceCanonicalizer.MapClassification` stores both as `Unknown` (0). A deploy of a new mapper does not rewrite rows already stored as 0. BIO-GOLIVE-001 therefore includes the enum and mapper, and it says the live Glycine and Retatrutide labels stay `Unknown` until a separate human classification repair. That repair is not this parcel and is not a Refresh rerun.

Committed seed classifies Retatrutide as Research Compound. The uncommitted working tree classifies it as Peptide. The promotion batch does not name a class. The acceptance class in BIO-GOLIVE-001 is Research Compound. The working-tree seed was not edited.

## Specs shaped

| Ticket | Status | Reviewers required | Proposed branch |
|---|---|---|---|
| BIO-GOLIVE-001 | review-candidate | 2 | `feat/bio-golive-001-classification-list` |
| BIO-GOLIVE-002 | review-candidate | 2 | `fix/bio-golive-002-exposure-and-limits` |
| BIO-GOLIVE-003 | review-candidate | 1 | `fix/bio-golive-003-checkout-plan` |

Gate 2 has not named a spec hash, builder, or worktree. Implementation remains blocked.

## Files changed

- `docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md`
- `docs/specs/active/BIO-GOLIVE-002-production-exposure-and-intake-limits.md`
- `docs/specs/active/BIO-GOLIVE-003-checkout-plan-selection.md`
- `docs/specs/INDEX.md`
- `docs/INITIATIVES/biostack-go-live/STATUS.md`
- `docs/INITIATIVES/biostack-go-live/SESSION-2026-10-14-resume-2.md`

## Verification result

Spec lint passed. Live class labels and F-001 remain failed. No product test was run because no product code changed.

## Decisions needed

None that block the next safe action. Independent spec review is the next gate. A human must later approve Gate 2 before any builder starts.

## Blockers

- No approved active spec yet. These three are review candidates.
- Public Glycine and Retatrutide `classification` are still `Unknown`.
- F-001, F-002, and F-003 are open. F-004 through F-007 stay open and blocking.
- Checkout plan selection is unfixed.
- Counsel approval, spend, drills, and billing lifecycle remain human-owned.

## Next safe action

Send BIO-GOLIVE-001 and BIO-GOLIVE-002 to two independent spec reviewers each, and BIO-GOLIVE-003 to one. Reviewers are read-only. Do not create the worktrees until Gate 2 names the spec hash.

## Do not touch

- Production Refresh, unless the seed file changes and a new dry run is read first.
- `ConnectionStrings__DefaultConnection` and Container App secrets.
- Live Stripe charges, refunds, and cancellations.
- Terms, privacy, disclaimer, and subscription-policy approval.
- The production-readiness initiative's NO-GO.
- Dose, schedule, and pairing `JsonIgnore` attributes on the public compound response.
- The uncommitted seed and trust-gate diff.
- Another 150-entry generation.
