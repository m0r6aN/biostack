# Narrow Gate 1 Amendment 01 — Plan-Review Corrections

**Status:** RATIFIED 2026-09-02

**Scope:** D6, D7, D8, and D9 only

**Trigger:** mandatory post-Gate-1 plan-level adversarial review of charter commit `6720edb7b37162757f14809e1eded130dc8f3cae`

All other locked decisions D1-D5 and D10-D14 remain ratified and unchanged. Existing contingent Gate 2 authority remains unchanged for orthogonal work. Gate 3 remains ungranted.

## D6 replacement — interaction safety

Replace D6 with:

> **D6 — Interaction safety.** Explicit `AvoidWith` metadata is evaluated before and outranks every graph edge and stored pair hint. Graph intelligence is eligible only when it comes from the currently active graph artifact, that artifact's `ReviewState` is exactly `reviewed`, the relationship's `ReviewState` is exactly `reviewed`, and `NeedsReview` is false; otherwise evaluation falls through to the next eligible source. Resolved entries are deduplicated by canonical identity. Confidence must be finite and clamped to `[0,1]`.

Required P03 regressions retain the four reproduced scenarios and add:

- positive stored hint cannot outrank `AvoidWith`;
- inactive/missing or non-reviewed graph artifact cannot authorize graph intelligence;
- non-reviewed edge cannot authorize graph intelligence;
- `NeedsReview` remains ineligible independently of the other checks.

## D7 replacement — evidence provenance

Replace D7 with:

> **D7 — Evidence provenance.** Only `PendingReview` and `Partial` research artifacts are candidate outputs eligible for the review-staging lane. `Queued`, `ResolvingIdentity`, `GatheringEvidence`, `Normalizing`, `Completed`, `Failed`, `Cancelled`, and `RejectedByPolicy` artifacts fail before any staging-store lookup or upsert, using the existing `ScientificResearchProviderException` with error code `artifact_not_stageable`. Internal `research_job:`, `workflow:`, `tooluniverse:`, and `tool:` labels remain provenance and never count as citations, including when their value contains locator-looking text. In addition to every existing tier, review-state, target, mechanism, fixture, and safety check, opening `EvidenceGate` requires the pipe-delimited `citations` metadata to contain at least one trimmed stable external locator: an absolute `http` or `https` URI with a nonempty host; `doi:` followed by a no-whitespace value beginning `10.` and containing `/`; or `pmid:` followed only by one or more ASCII digits. Prefix and scheme matching is case-insensitive. Missing/nonblank citations retain `missing_citations`; citations without a qualifying external locator fail with `missing_external_source_locator`. Locator validation is syntax-only and performs no I/O.

Required P04 regressions retain the two reproduced scenarios and directly prove:

- all eight ineligible states cause zero staging upserts;
- both eligible candidate states stage and preserve idempotency;
- internal-only and prefix-smuggled labels remain ineligible;
- each of the four external locator forms has a valid positive control;
- malformed and unsupported locators fail closed without network access.

## D8 replacement — sidecar lifecycle and source cap

Replace D8 with:

> **D8 — Sidecar lifecycle.** Active states are `queued`, `resolving_identity`, `gathering_evidence`, and `normalizing`. Terminal states are `pending_review`, `completed`, `failed`, `cancelled`, `partial`, and `rejected_by_policy`. The first terminal transition wins atomically at the store boundary. Once terminal, later worker updates return the existing record unchanged: they cannot change status, `finished_at_utc`, progress, partial/error fields, artifact, tools, or `updated_at_utc`; an identical replay is an idempotent no-op. A later cancellation request may not alter the terminal snapshot. `maximum_source_count` is enforced in the executor before accepted results, claims, provenance, and tool counts are materialized. P05 precedes final P06 integration verification.

Required P05/P06 verification:

- the synchronized late-worker race observes worker completion without waiting for the forbidden overwrite;
- timeout, cancellation, failure, partial, policy rejection, pending review, and completion terminal custody are covered;
- the source cap binds tools, artifact lists, provenance results, claims, and source identifiers;
- P05 final-candidate behavior is present when P06 aggregate verification runs.

## D9 replacement — outbound deny-by-default boundary

Replace D9 with:

> **D9 — Outbound deny-by-default boundary.** (a) OCR consults the current authenticated user's server-side consent gate before constructing or sending a provider request; denial or gate failure is fail-closed. (b) The frontend suggestion BFF obtains its backend origin only from server-controlled configuration or the repository's fixed local default, never from the inbound request. It reads and forwards only the named `biostack_session` cookie to `GET /api/v1/consent`, uses `cache: no-store`, forbids credential-bearing redirect following, and applies a bounded timeout. Missing session, network error, timeout, redirect, non-2xx status, malformed or oversized response, or `accepted !== true` denies before any provider request. The session cookie is never forwarded to the provider. Caller-supplied booleans and configuration flags are not authority.

Required P07 hostile regressions independently cover every denial listed above plus exactly one authenticated, currently consented, local-fake provider call. The local consent fake must assert the exact endpoint, method, cookie name, redirect policy, cache policy, and expected response shape. Aggregate verification pairs this with the real backend `ConsentGateIntegrationTests`; any contract drift remains fail-closed.

## Exact re-ratification form

> Narrow Gate 1 Amendment 01: I ratify the replacement text for D6, D7, D8, and D9 as written. All other decisions and the existing contingent Gate 2 authorization remain unchanged. Gate 3 remains ungranted.

## Human receipt

The developer supplied the exact re-ratification form above on 2026-09-02. D6-D9 are replaced as written; affected parcels P03-P07 are unblocked subject to their existing contingent Gate 2 conditions. Gate 3 remains ungranted.
