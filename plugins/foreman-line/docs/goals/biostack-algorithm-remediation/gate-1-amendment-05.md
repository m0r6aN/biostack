# Narrow Gate 1 Amendment 05 — P03 Graph Artifact Identity

**Status:** RATIFIED 2026-09-02 — coordinator lint passed

**Scope:** D6 artifact-identity/provenance supplement and P03 regression/count replacement only

**Trigger:** fresh adversarial review rejected P03 candidate `7e8087a00722e38c2a090d49826fa6289c880356`. The candidate validates and caches active reviewed artifact A, but `CompoundGraphStore.FindRelationshipAsync` independently resolves the current active artifact. If publication rotates to artifact B between those reads, the service can accept edge B and label it with artifact A's reviewed hash because it never compares `CompoundGraphRelationship.GraphArtifactId` with the validated artifact ID. B may be provisional or otherwise ineligible. Current target `10/10` has no artifact-identity mismatch case, and its exact census cannot be changed without a narrow amendment.

Amendments 01-04 remain ratified. Every other decision and authorization remains unchanged. Gate 3 remains ungranted.

## D6 artifact-identity and provenance supplement

Add this controlling requirement to ratified D6:

> A graph relationship is eligible only when its `GraphArtifactId` equals the `Id` of the same present, active, exactly `reviewed` artifact whose `ArtifactHash` will be cited. If artifact state rotates or the relationship belongs to any different artifact, graph evaluation fails closed and follows the existing next-source semantics. A relationship may never be attributed to another artifact's hash.

The smallest production repair remains inside `InteractionIntelligenceService.cs`: compare the returned relationship's artifact ID with the validated reviewed artifact ID before emitting graph intelligence. No interface, graph-store, entity, persistence, publication, or response-contract change is authorized.

## Replacement P03 regression census and counts

Retain the existing four diagnostic cases and six Amendment-01 hostile cases, and add exactly one synthetic local fact:

- `EvaluateAsync_GraphEdgeFromDifferentArtifact_IsNotAttributedToReviewedArtifact` — configure a present active exactly reviewed artifact A and an otherwise reviewed, `NeedsReview == false` relationship whose `GraphArtifactId` belongs to distinct artifact B; assert no graph-backed result and no artifact hash, with existing next-source fallthrough preserved.

The replacement target census is exactly 11 discovered/passed cases: the existing 10 plus this one fact. The independent adjacent set remains `20/20`. Application becomes exactly `620 passed / 5 skipped`, total 625: exactly +11 passed/discovered relative to the pinned base. Focused `CompoundGraphIntelligenceTests` remains `8/8`; API remains exactly 377. In the full solution only Application gains the 11 P03 cases; Domain remains 7, verifier 158, API 377, and KnowledgeWorker 866.

Any additional test, theory row, count change, or production file requires another narrow decision.

## Allowed Files and Amendment 04 custody

AF-P03 remains exactly:

1. `backend/src/BioStack.Application/Services/InteractionIntelligenceService.cs`
2. `backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs`
3. `backend/tests/BioStack.Api.Tests/CompoundGraphIntelligenceTests.cs`

Amendment 04's third-file restriction remains unchanged: the API test file may contain only its already-ratified one-value fixture correction from `provisional` to exactly `reviewed`. The new mismatch regression belongs only in the Application reproduction test file. Every other path remains forbidden.

## Verification and review

- Rerun target 11, adjacent 20, Application 620+5, focused API 8, and the full solution with API 377 and the other pinned counts.
- Re-establish exact AF-P03 scope, base ancestry, diagnostic non-ancestry, no merge, `git diff --check`, offline/no-network receipt, and clean final status.
- The rejected candidate remains preserved in Git history but is not review-complete and cannot enter the merge set.
- Any rework invalidates prior receipts and review. One fresh independent read-only adversarial review is required on the new exact candidate.

## Gate boundary

Ratification reactivates only P03's existing contingent Gate 2 authority after the coordinator incorporates this supplement into the shaped spec and accepts a fresh Step 0. It does not authorize external access, push, PR, merge, deployment, publication, release, or diagnostic-branch mutation. Gate 3 remains human-only and ungranted.

## Exact ratification form

> Narrow Gate 1 Amendment 05: I ratify the D6 artifact-identity/provenance supplement and replacement P03 regression census and verification counts as written. AF-P03 remains the same three files, and the Amendment 04 API fixture restriction remains unchanged. P03 retains contingent Gate 2 authorization subject to its amended shaped spec and fresh Step 0. All other decisions and authorizations remain unchanged. Gate 3 remains ungranted.

## Human ratification receipt

The developer supplied the exact ratification form above on 2026-09-02. The D6 artifact-identity/provenance supplement and replacement 11-case P03 census are active. AF-P03 remains exactly three files, the Amendment 04 API fixture restriction is unchanged, and P03 is reactivated only subject to its amended shaped spec and a fresh coordinator-confirmed Step 0. All other decisions and authorizations remain unchanged. Gate 3 remains ungranted.
