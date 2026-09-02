# Narrow Gate 1 Amendment 04 — P03 Reviewed-Graph Fixture

**Status:** RATIFIED 2026-09-02 — coordinator lint passed

**Scope:** replacement AF-P03 only; no production invariant change

**Trigger:** the scoped P03 candidate passed its 10-case target, 20-case adjacent set, and Application aggregate, but the full solution exposed one stale API fixture. `CompoundGraphIntelligenceTests.InteractionIntelligence_PrefersGraph_OverStringInference` expects graph-backed intelligence while its shared `PublishGraphAsync` helper creates the artifact with `ReviewState = "provisional"`. Ratified D6 correctly rejects that artifact and falls back. The relationship fixture is already exactly `reviewed`.

Amendments 01-03 remain ratified. Every other decision and authorization remains unchanged. Gate 3 remains ungranted.

## Fixture decision

The API fixture's graph-positive scenarios must publish an artifact whose `ReviewState` is exactly `reviewed`, matching their existing graph-backed assertions and ratified D6. P03 may change only the shared test helper's stale artifact review state from `provisional` to `reviewed` and make no other API-test semantic change.

This does not weaken D6, make provisional artifacts eligible, add a production exception, change graph persistence, or alter the public response contract. If another API-test change is required, stop for another narrow amendment.

## Replacement Exact Allowed Files — AF-P03

Replace AF-P03 with exactly these three paths:

1. `backend/src/BioStack.Application/Services/InteractionIntelligenceService.cs`
2. `backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs`
3. `backend/tests/BioStack.Api.Tests/CompoundGraphIntelligenceTests.cs`

Every other path remains forbidden. The third path may change only the stale graph-positive fixture state described above.

## Verification and review

- Preserve the 4 retained plus 6 hostile P03 cases: target `10/10`.
- Preserve the independent interaction/public/swap adjacent set: `20/20`.
- Application remains `619 passed / 5 skipped` with zero failures.
- `CompoundGraphIntelligenceTests` and full API suite must be green; API discovery remains exactly `377` with no new test.
- Full solution must be green with only Application gaining the ten P03 cases relative to base.
- Exact AF-P03 diff, base ancestry, diagnostic non-ancestry, no merge, `git diff --check`, offline/no-network receipt, and clean final status remain mandatory.
- One fresh independent adversarial review remains required on the exact final three-file candidate. Any rework invalidates the review.

## Gate boundary

Ratification reactivates only P03's existing contingent Gate 2 authority after the coordinator updates/re-lints the spec and the builder restates this amendment before touching the third file. It does not authorize any push, PR, merge, deployment, publication, release, external access, or diagnostic-branch mutation. Gate 3 remains human-only and ungranted.

## Exact ratification form

> Narrow Gate 1 Amendment 04: I replace AF-P03 with the three files listed and authorize only the stale API graph-positive fixture change from `provisional` to exactly `reviewed`. Ratified D6 and all other decisions and authorizations remain unchanged. Gate 3 remains ungranted.

## Human receipt

The developer supplied the exact ratification form above on 2026-09-02. Replacement AF-P03 is active and permits only the named stale API graph-positive fixture correction from `provisional` to exactly `reviewed`; ratified D6 is unchanged. Contingent Gate 2 authority for P03 is reactivated subject to its amended spec and Step 0. Gate 3 remains ungranted.
