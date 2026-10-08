# BIO-PAIRWISE-003 Gate 2 Dispatch Record (relationship projection wiring)

Coordinator-owned record, frozen at dispatch (2026-10-08). Spec review: **APPROVE**, no blocking
findings. Decision D-B binds this parcel: **code-only in ALL outcomes, no migration ever**,
inert-marking is the fallback; stop-and-report if code-only proves impossible.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-PAIRWISE-003",
  "specPath": "docs/specs/active/BIO-PAIRWISE-003-relationship-projection-wiring.md",
  "builderId": "bio_pairwise_003_builder",
  "branch": "feat/bio-pairwise-003-projection-wiring",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-PAIRWISE-003",
  "permissionEnvelope": "local-only:projection-wiring-per-spec",
  "builderSurfaces": "exactly the spec's Allowed Files section",
  "reviewerIds": ["bio_pairwise_003_review_1"],
  "risk": "standard",
  "dispatchWhen": "immediate"
}
```

Dispatch notes: consumes the P0 census + BIO-PAIRWISE-002 ratification outputs (no re-census, no
re-ratification); wiring must make the authoritative relationship path project the ratified
outputs and mark the non-authoritative path inert per D-B. Required Tests = wiring assertions +
inert-path assertions (run + record). Unknown-honest discipline: no invented data. Delivery:
commit → push → PR vs `main` (Gate 3 owner's) → worktree removed.

## Amendment PW-003-A1 (coordinator, 2026-10-08, after builder stop-and-report)

The builder proved AC2/AC3 unenforceable from the listed surfaces: boundary enforcement of
`assertionClass` / `relationshipReviewStatus` / `relationshipId` / malformed-input handling lives
in `CompoundGraphBuilder.cs`, which the Allowed Files exclude; re-implementing from the read side
would build a second, parallel validation pipeline — contradicting the spec's own Verification
Plan question ("enforced once at the boundary"). Ruling:

1. `backend/src/**/CompoundGraphBuilder.cs` is ADDED to the builder surfaces (`ResearchJob.cs`
   only if the builder proves it unavoidable). The spec's ACs are unchanged — the surface list was
   under-specified, not the requirements.
2. D-B remains fully honorable: code-only, no migration, no schema change.
3. The eventual review MUST explicitly cover the added file.
4. The builder's empty retained branch may be reused.
