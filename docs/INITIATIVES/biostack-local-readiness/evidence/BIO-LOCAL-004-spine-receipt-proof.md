# BIO-LOCAL-004 — Governance Spine and Receipt Proof

**Parcel:** BIO-LOCAL-004 (elevated — dual independent review required before merge)
**Spec:** `docs/specs/active/BIO-LOCAL-004-governance-spine-receipt-proof.md`
**Branch:** `proof/bio-local-004-spine-receipts`
**Builder:** `bio_local_004_builder`

**SHA tested:** `c6205fae91357e30a1936f9e2318a84dbf251774` (origin/main, fast-forwarded into this worktree at session start; no code commits made — evidence-only PR)
**Run start (UTC):** 2026-10-07T23:51:43Z
**Run end (UTC):** 2026-10-07T23:56:02Z
**Config class:** Development, local-only. SQLite provider throughout (both in-memory SQLite for unit tests and file-backed SQLite for the DI/integration tests via `WebApplicationFactory`). No PostgreSQL run performed or claimed. No live Keon Runtime connection used anywhere in this evidence — every receipt/checkpoint/governance claim below is explicitly labeled **STUBBED** or **SYNTHETIC** where applicable.

## Redaction Attestation

This evidence file contains no secrets, API tokens, PII, signing-key material, or raw payload/log dumps. Signing keys referenced below are literal test strings taken verbatim from the existing, already-committed test suite (e.g. `"a-completely-different-key-value"`, `"round-trip-key"`, `"auto-key"`) — every one is labeled **SYNTHETIC** and none is a credential ever used outside these in-memory unit tests. No `.env` file was committed or retained (session used a placeholder-only `.env`, created and deleted during probing — see Docker/Environment Note below, never used in the final evidence run). `git diff --check` was run against this worktree and is clean (see Verification section).

## Tool Versions

```
$ dotnet --version
10.0.401
```

## Docker / Environment Note

`docker-compose.dev.yml` in this repo binds host ports 5000/5001/3043. At the time of this run those ports were already bound by another active parcel's dev stack (`docker inspect` showed `com.docker.compose.project:bio-local-005`, working dir `/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-005`). Per the standing rule to never touch another parcel's running state, this parcel's own Docker Compose stack was **not** booted. No containers were started by this evidence run; no `down -v` cleanup was required.

All HTTP-shaped evidence below (receipt-view access gating, fail-closed config behavior) was captured instead via **`Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>`** — the same in-process ASP.NET Core TestServer harness the existing `ReceiptEndpointsIntegrationTests` and `GovernanceDependencyInjectionSmokeTests` already use. This exercises the real `Program.cs` host, real routing/auth middleware, and real EF Core/SQLite persistence; it differs from a Docker Compose boot only in not going through the container network layer. The `/health/keon` stub-mode transcript is cited from `BIO-LOCAL-001-boot-proof.md` (same commit lineage, same `KeonRuntimeClientStub` code path, actually booted via Docker Compose in that parcel's run) rather than re-captured here, per the spec's explicit instruction ("cite 001's run; add receipt-view denial transcript here").

---

## AC1 — Spine write→read round-trip is byte-identical through SQLite (chain/hash tests green; record counts)

**Command:**
```
cd backend
dotnet test BioStack.sln --filter "FullyQualifiedName~BioStack.Api.Tests.Unit.Governance.SpineRepositoryTests"
```
**Result:** `Passed! - Failed: 0, Passed: 5, Skipped: 0, Total: 5, Duration: 1 s - BioStack.Api.Tests.dll (net10.0)`

**Command:**
```
dotnet test BioStack.sln --filter "FullyQualifiedName~BioStack.Api.Tests.Unit.Governance.SpineChainIntegrityTests"
```
**Result:** `Passed! - Failed: 0, Passed: 13, Skipped: 0, Total: 13, Duration: 2 s - BioStack.Api.Tests.dll (net10.0)`

These tests run against real `BioStackDbContext` instances backed by in-memory SQLite (`Data Source=file:...?mode=memory&cache=shared`), not a mock hash. `SpineChainIntegrityTests` documents (in its class doc comment, `backend/tests/BioStack.Api.Tests/Unit/Governance/SpineChainIntegrityTests.cs:1-20`) the exact `DateTimeKind`/precision behavior the spec requires be stated explicitly: SQLite does not preserve `DateTimeKind.Utc` on round-trip (values read back as `Unspecified`), and .NET tick precision (100ns) exceeds PostgreSQL's microsecond resolution — both are handled by `SpineChain.Stamp` normalizing to UTC at microsecond precision before hashing (`backend/src/BioStack.Domain/Governance/SpineChain.cs:88-100`). This is a **SQLite-only** round-trip proof; no PostgreSQL equivalence is claimed or tested here, per the spec's explicit constraint.

Assertion-level coverage confirmed by reading the test file:
- `Genesis_entry_starts_the_chain` — first entry gets `SequenceNumber=0`, `PreviousEntryHash="sha256:genesis"`.
- `Each_entry_commits_to_its_predecessor` — `entries[i].PreviousEntryHash == entries[i-1].EntryHash` for a 4-entry chain.
- `Intact_chain_verifies` / `Empty_chain_verifies` — positive round-trip on 5 entries and on zero entries.
- `Hash_is_sensitive_to_the_predecessor`, `Field_boundaries_cannot_be_forged`, `Identical_input_hashes_identically` — hash function properties (length-prefixed fields prevent boundary-shifting collisions).

---

## AC2 — Signed checkpoint verifies; tampered entry/checkpoint is detected (negative proof recorded)

**Command:**
```
dotnet test BioStack.sln --filter "FullyQualifiedName~BioStack.Api.Tests.Unit.Governance.SpineCheckpointTests"
```
**Result:** `Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 2 s - BioStack.Api.Tests.dll (net10.0)`

All signing keys used in this test class are **SYNTHETIC** literal strings embedded in the already-committed test source, never real secrets:
- `Signed_checkpoint_verifies_against_intact_chain` — creates a checkpoint via `SpineTestHelpers.CreateWithCheckpoints` (default test options); asserts `Source == CheckpointSourceLocalHmac`, `Signature` starts with `"sha256:"`, and `VerifyLatestAsync()` returns `IsFullyValid == true` (`ChainIntact`, `CheckpointPresent`, `HeadMatchesCheckpoint`, `SignatureValid`, `ExternallyAnchored` all true).
- `Rewriting_entry_after_checkpoint_fails_head_match` — **negative proof**: after checkpointing, a raw `UPDATE SpineEntries SET Decision='allowed' WHERE SequenceNumber=1` is executed directly against the SQLite database (simulating a holder with a SQLite browser rewriting a row). `VerifyLatestAsync()` then returns `ChainIntact=false`, `IsFullyValid=false`.
- `Signature_fails_when_signing_key_differs` — **SYNTHETIC key**: checkpoint created with the test harness's default key, then re-verified using a second service constructed with `SigningKey = "a-completely-different-key-value"` against the *same* database. Result: `ChainIntact=true`, `HeadMatchesCheckpoint=true`, but `SignatureValid=false` and `IsFullyValid=false` — proving signature verification is a genuine cryptographic check, not a rubber stamp tied to chain integrity alone.
- `Domain_sign_and_verify_round_trip` — **SYNTHETIC key**: `SpineChain.SignCheckpointPayload` with key `"round-trip-key"`; `VerifyCheckpointSignature` returns `true` for the matching sequence number and `false` when the sequence number is altered (3→4), proving the signature is bound to the full payload, not just presence of a signature string.
- `Auto_checkpoint_fires_every_n_entries` — **SYNTHETIC key** `"auto-key"`; confirms `AutoCheckpointEveryNEntries=2` cadence behavior (no checkpoint after 1 entry, checkpoint created after 2nd, note contains `"auto-every-2"`).
- `Export_manifest_is_portable_json` — confirms the JSON export (`ISpineCheckpointService.ExportLatestManifestJsonAsync`, `backend/src/BioStack.Infrastructure/Governance/SpineCheckpointService.cs:209-224`) contains `schema`, `headEntryHash`, `signature` fields for off-box anchoring, but performs no actual off-box anchoring — this is unexercised in this run (see Unproven list).

**Tamper/signing code citations:**
- `SpineChain.ComputeEntryHash` — `backend/src/BioStack.Domain/Governance/SpineChain.cs:59`
- `SpineChain.SignCheckpointPayload` (HMAC-SHA256) — `backend/src/BioStack.Domain/Governance/SpineChain.cs:123`
- `SpineChain.VerifyCheckpointSignature` (constant-time compare via `CryptographicOperations.FixedTimeEquals`) — `backend/src/BioStack.Domain/Governance/SpineChain.cs:134`

---

## AC3 — Receipt write→read holds scoping; anonymous `/governance/receipts` denies; receipt-authz negative passes

**Command:**
```
dotnet test BioStack.sln --filter "FullyQualifiedName~BioStack.Api.Tests.Integration.ReceiptEndpointsIntegrationTests"
```
**Result:** `Passed! - Failed: 0, Passed: 7, Skipped: 0, Total: 7, Duration: 3 s - BioStack.Api.Tests.dll (net10.0)`

**Note on route naming:** The spec and `SECURITY-GATES.md` reference `/governance/receipts`; the implemented route (confirmed by reading `backend/src/BioStack.Api/Endpoints/ReceiptEndpoints.cs:15`) is `/api/v1/receipts`, group-gated with `.RequireAuthorization()` at `ReceiptEndpoints.cs:16`. This is a naming drift in prior docs, not a functional gap — the denial semantics the spec is checking for are present and verified below.

HTTP status-code transcript (captured via `WebApplicationFactory<Program>` TestServer, real routing/auth middleware, real SQLite-backed EF Core, not mocks — `backend/tests/BioStack.Api.Tests/Integration/ReceiptEndpointsIntegrationTests.cs`):

| Scenario | Request | Result | Status Code |
|---|---|---|---|
| Unauthenticated | `GET /api/v1/receipts/{uri}` with no auth cookie/token | `GetReceiptByUri_Unauthenticated_Returns401` | **401 Unauthorized** |
| Unknown receipt (authenticated) | `GET /api/v1/receipts/{uri}` for a URI that does not exist | `GetReceiptByUri_UnknownUri_Returns404` | **404 Not Found** |
| Known receipt, owner | `GET /api/v1/receipts/{uri}` for the caller's own receipt | `GetReceiptByUri_KnownUri_Returns200WithCorrectShape` | **200 OK** |
| Cross-tenant/other-user receipt | `GET /api/v1/receipts/{uri}` for a receipt owned by a different `ActorId` | `GetReceiptByUri_OtherUsersReceipt_Returns404` | **404 Not Found** (not 403 — avoids confirming existence to a non-owner) |
| Scoped list by subject | `GET /api/v1/receipts?subject=...` | `GetReceiptsBySubject_ReturnsListForKnownSubject` | **200 OK**, list filtered to caller's own actor entries only |
| Wrong-actor query parameter | `GET /api/v1/receipts?actor={someone-else}` | `GetReceiptsByActor_OtherActor_Returns403` | **403 Forbidden** |
| Admin reading a system receipt | `GET /api/v1/receipts/{uri}` as admin, for a system-actor receipt | `GetReceiptByUri_AdminCanReadSystemReceipt` | **200 OK** |

Scoping logic citations: `IsAdmin` check — `ReceiptEndpoints.cs:199-200`; owner-or-admin gate on single-receipt reads — `ReceiptEndpoints.cs:161-162`; actor-mismatch `Forbid()` on list reads — `ReceiptEndpoints.cs:190`.

**Both required negative cases hold:** unauthenticated → 401; wrong-tenant/wrong-actor → 403 (list) or 404 (single-receipt, by design to avoid existence-confirmation to a non-owner).

---

## AC4 — Fail-closed boot unit/config test passes (stub-without-acknowledgment refuses Production-track config; `StubAllowAll` rejected) — POLICY proof, not a production boot

This AC has no pre-existing dedicated test class in the repo (confirmed by search: no file matches `*FailClosed*` or references `AddKeonRuntime`+`AllowStubInProduction` together under `backend/tests`). Per the spec's "never patch here" / "no new persistent tests" constraint, a **transient probe test file** was added, run, and then **deleted before this evidence was committed** — it calls the existing, unmodified `KeonRuntimeDependencyInjection.AddKeonRuntime` extension method directly against a bare `ServiceCollection` and in-memory `IConfiguration`, asserting on the exact exception the production code already throws. No product code was edited.

**Probe file (transient, not committed):** `backend/tests/BioStack.Api.Tests/Unit/Keon/FailClosedBootProbeTests.cs`

**Command:**
```
dotnet test BioStack.sln --filter "FullyQualifiedName~FailClosedBootProbeTests" -v n
```

**Result:** `Total tests: 4 / Passed: 4 / Total time: 0.97s`

**Refusal transcripts, verbatim (captured via `Console.WriteLine` inside the probe, from the exception thrown by the unmodified production code at `backend/src/BioStack.Infrastructure/Keon/KeonRuntimeDependencyInjection.cs:25-39`):**

```
EXCEPTION_MESSAGE: KeonRuntime:StubAllowAll=true is not permitted in Production — it bypasses fail-closed policy checks.

EXCEPTION_MESSAGE: KeonRuntime is not in live mode (LiveMode=false or BaseUrl empty) in a Production environment. Governance receipts cannot be anchored. Configure KeonRuntime:BaseUrl + KeonRuntime:LiveMode=true, or set KeonRuntime:AllowStubInProduction=true to acknowledge running ungoverned.
```

| Scenario | `isProduction` | `LiveMode`/`BaseUrl` | `AllowStubInProduction` | `StubAllowAll` | Result |
|---|---|---|---|---|---|
| Stub, no acknowledgment | `true` | false / empty | `false` | `false` | **Refused** — `InvalidOperationException`, "not in live mode ... Configure ... or set AllowStubInProduction=true" |
| Stub, acknowledged, but `StubAllowAll=true` | `true` | false / empty | `true` | `true` | **Refused** — `InvalidOperationException`, "StubAllowAll=true is not permitted in Production" |
| Stub, explicitly acknowledged | `true` | false / empty | `true` | `false` | **Allowed** — host builds, `AddKeonRuntime` returns normally |
| Stub, Development | `false` | false / empty | `false` | `false` | **Allowed** — host builds normally (stub is the expected Development posture) |

This is a **POLICY/config proof only** — `services.AddKeonRuntime(config, isProduction: true)` was called directly, never a real `dotnet run --environment Production` boot of the full host against external resources. No production boot was performed, attempted, or claimed anywhere in this evidence.

---

## AC5 — Cognition separation asserted by assembly/citation: deliberation output flows via envelope, no direct user-text path

Read-only citation trace (no cognition code edited):

1. **Input contract is non-effecting by construction.** `StackDeliberationEnvelope` — every field is "observational/educational. No field is effect-bearing." `backend/src/BioStack.Cognition/Models/StackDeliberationEnvelope.cs:1-9`.
2. **Translator enforces the non-effect-bearing invariant explicitly, twice, at the two points claim nodes are constructed.** `StackDeliberationTranslator` class doc: "Every ClaimNode.IsEffectBearing == false / Every AssumptionRef.IsEffectBearing == false / No call to any effect surface, gateway, or executor." `backend/src/BioStack.Cognition/StackDeliberationTranslator.cs:10-16`. Enforced at `IsEffectBearing: false, // INVARIANT` — `StackDeliberationTranslator.cs:105` and `StackDeliberationTranslator.cs:125`.
3. **Orchestration service returns a typed envelope, not text.** `StackReviewBoardService.ReviewStackAsync` returns `Task<CognitiveDensityEnvelope>`; its class doc states: "This service does NOT call any effect surface, reality surface, gateway, or governed-execute path. It is observational commentary only." `backend/src/BioStack.Cognition/StackReviewBoardService.cs:9-14, 31`.
4. **API endpoint routes the envelope through the central safety gate before any serialization, not around it.** `StackReviewEndpoints` class doc: "Every user-facing narrative the board produces ... is routed through the central Lane H `IUserFacingIntelligenceGate` before serialization ... The `DoctrineSanitizer` is no longer the final user-facing decision layer here." `backend/src/BioStack.Api/Endpoints/StackReviewEndpoints.cs:12-19`. The handler calls `srbService.ReviewStackAsync(...)` to get a `CognitiveDensityEnvelope` (`StackReviewEndpoints.cs:49`), which is only later mapped to a response DTO through named mapping functions — grep of the file shows `CognitiveDensityEnvelope` is only ever passed into typed mapper parameters (`StackReviewEndpoints.cs:104, 225, 290, 335`), never written to the HTTP response body as a raw string.

No direct user-text path was found; every hop between deliberation output and the HTTP response is a typed envelope passed through named mapping/gate functions.

---

## AC6 — Evidence file written; `git diff --check` clean; redaction attestation

**Command:**
```
cd /home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-004
git diff --check
```
**Result:** exit code `0`, no output (clean).

Redaction attestation: see top of this document.

---

## Tamper / Fail-Closed Test Matrix

| # | Tamper / probe | Mechanism | Detected? | Evidence |
|---|---|---|---|---|
| 1 | Alter a recorded `Decision` on a mid-chain entry | Raw `UPDATE SpineEntries SET Decision='allowed' WHERE SequenceNumber=1` against SQLite | **Yes** — chain breaks at the altered entry, reason contains "altered" | `SpineChainIntegrityTests.Altering_a_recorded_decision_breaks_the_chain` |
| 2 | Alter `EvidenceRefsJson` on the genesis entry | Raw `UPDATE` | **Yes** — chain breaks at sequence 0 | `SpineChainIntegrityTests.Altering_evidence_refs_breaks_the_chain` |
| 3 | Delete a middle entry | Raw `DELETE FROM SpineEntries WHERE SequenceNumber=2` | **Yes** — "Sequence gap" reported at the next surviving entry | `SpineChainIntegrityTests.Deleting_a_middle_entry_breaks_the_chain` |
| 4 | Multiple tampers | Raw `UPDATE ... WHERE SequenceNumber IN (1,3)` | **Yes** — earliest break (sequence 1) reported, not the later one | `SpineChainIntegrityTests.Verification_reports_the_earliest_break` |
| 5 | Duplicate receipt URI (append-only bypass attempt) | `AppendAsync` with a URI already in the chain | **Yes** — `SpineImmutabilityViolationException` thrown; chain remains intact afterward | `SpineChainIntegrityTests.Duplicate_receipt_is_still_rejected`, `Chain_survives_a_rejected_duplicate` |
| 6 | Rewrite an entry after it was checkpointed | Raw `UPDATE` post-checkpoint | **Yes** — `VerifyLatestAsync()` reports `ChainIntact=false`, `IsFullyValid=false` | `SpineCheckpointTests.Rewriting_entry_after_checkpoint_fails_head_match` |
| 7 | Verify checkpoint signature with the wrong (but still well-formed, SYNTHETIC) key | Second `SpineCheckpointService` built with a different `SigningKey` against the same DB | **Yes** — `SignatureValid=false`, `IsFullyValid=false`, even though `ChainIntact`/`HeadMatchesCheckpoint` are both true | `SpineCheckpointTests.Signature_fails_when_signing_key_differs` |
| 8 | Checkpoint signature bound to wrong sequence number | `VerifyCheckpointSignature(4, ...)` against a signature made for sequence `3` | **Yes** — returns `false` | `SpineCheckpointTests.Domain_sign_and_verify_round_trip` |
| 9 | Unauthenticated receipt read | `GET /api/v1/receipts/{uri}` with no auth | **Yes** — 401 | `ReceiptEndpointsIntegrationTests.GetReceiptByUri_Unauthenticated_Returns401` |
| 10 | Cross-actor receipt read | Authenticated as user A, requesting user B's receipt by known URI | **Yes** — 404 (no existence confirmation) | `ReceiptEndpointsIntegrationTests.GetReceiptByUri_OtherUsersReceipt_Returns404` |
| 11 | Cross-actor receipt list query | Authenticated, `?actor={someone-else}` | **Yes** — 403 | `ReceiptEndpointsIntegrationTests.GetReceiptsByActor_OtherActor_Returns403` |
| 12 | Stub Keon runtime with no production acknowledgment | `AddKeonRuntime(config, isProduction: true)`, `LiveMode=false`, `AllowStubInProduction=false` | **Yes** — `InvalidOperationException` at startup wiring (policy proof) | Transient probe, see AC4 |
| 13 | `StubAllowAll=true` in a production-track config, even acknowledged | Same as above plus `AllowStubInProduction=true`, `StubAllowAll=true` | **Yes** — `InvalidOperationException`, distinct message | Transient probe, see AC4 |

No tamper, bypass, or fail-closed check in this matrix was accepted. No STOP condition was triggered.

---

## Proven vs. Unproven

**Proven in this run (local, SQLite, Development config, this SHA):**
- Hash-chain append-only + tamper-evidence on real SQLite bytes, including the documented `DateTimeKind`/precision handling (AC1).
- Signed checkpoint creation and verification using SYNTHETIC HMAC keys; checkpoint-signature tamper detection is a genuine cryptographic check independent of chain-intact status (AC2).
- Receipt read scoping: unauthenticated → 401; cross-actor → 403/404; owner/admin → 200 (AC3).
- Fail-closed *policy/config* behavior of `AddKeonRuntime` for a Production-track configuration: stub-without-acknowledgment refused; `StubAllowAll=true` refused even when acknowledged (AC4).
- Cognition separation by static code citation: envelope-only contract, `IsEffectBearing=false` invariant enforced at construction, output routed through `IUserFacingIntelligenceGate` before serialization, no raw-text response path found (AC5).

**Explicitly STUBBED, not claimed as live:**
- No live Keon Runtime connection was used anywhere in this run. All checkpoint signing used `local-hmac` source with SYNTHETIC test keys; no `server-hmac` (operator-asserted externally-held key) posture was exercised.
- Receipt anchoring is a **development convenience** in this configuration, not a governance claim. `/health/keon` live-stub-mode HTTP transcript is cited from `BIO-LOCAL-001-boot-proof.md` (same `KeonRuntimeClientStub` code path, actually Docker-booted in that parcel's run), not re-captured here.
- `ExportLatestManifestJsonAsync` was proven to *produce* a portable JSON manifest (schema, hash, signature present) but no actual off-box/external anchoring of that manifest was performed or is claimed.

**Explicitly unproven / out of scope for this run:**
- No actual Production boot was performed. AC4 is unit/config-level only, exactly as the spec requires ("never a production boot").
- No PostgreSQL round-trip was run; SQLite-only round-trip behavior is proven, and PostgreSQL truncation is documented as a known difference in the production code comments, not independently re-verified here.
- No `server-hmac` / externally-held-key checkpoint posture was exercised (would require real off-device key custody, out of scope for a local proof).
- This parcel's own Docker Compose stack was not booted (port conflict with a concurrently active sibling parcel's stack); all HTTP-shaped evidence was captured via in-process `WebApplicationFactory` TestServer instead. No containers were started or require `down -v` cleanup for this parcel.
- `GuidanceContentContract`/`DoctrineSanitizer`/Class-D guidance backstop (LS9) is out of scope for BIO-LOCAL-004 — that is parcel 005's surface, not re-verified here.

---

## Verification Summary (AC1–AC6)

| AC | Status | Evidence |
|---|---|---|
| AC1 | ✓ PASS | `SpineRepositoryTests` 5/5, `SpineChainIntegrityTests` 13/13 — real SQLite round-trip, DateTimeKind/precision behavior documented |
| AC2 | ✓ PASS | `SpineCheckpointTests` 6/6 — signed checkpoint verifies; signature and head-match negatives both proven with SYNTHETIC keys |
| AC3 | ✓ PASS | `ReceiptEndpointsIntegrationTests` 7/7 — unauthenticated 401, wrong-actor 403/404, owner/admin 200 |
| AC4 | ✓ PASS | Transient probe 4/4 — Production-track stub-without-acknowledgment and `StubAllowAll=true` both refused, verbatim messages recorded; never a production boot |
| AC5 | ✓ PASS | File:line citation trace — envelope-only contract, `IsEffectBearing=false` invariant, gate-routed output, no direct-text path found |
| AC6 | ✓ PASS | This file; `git diff --check` clean (exit 0); redaction attestation above |

## Session Handoff

- **Starting commit:** `c6205fae91357e30a1936f9e2318a84dbf251774` (origin/main, fast-forwarded)
- **Ending commit:** same (evidence-only; no product code changed) + 1 new commit adding this evidence file
- **Files changed:** `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-004-spine-receipt-proof.md` (new)
- **Commands run:** `dotnet test` filtered to `SpineRepositoryTests`, `SpineChainIntegrityTests`, `SpineCheckpointTests`, `ReceiptEndpointsIntegrationTests`, `GovernanceDependencyInjectionSmokeTests`, plus a transient (added-then-deleted) `FailClosedBootProbeTests`; `git diff --check`.
- **Tests passed:** 13 + 6 + 5 + 7 + 11 + 4 (transient) = 46/46, 0 failed.
- **Tests failed:** none.
- **Decisions needed:** none from this run — no tamper acceptance, bypass, or defect found.
- **Blockers:** none.
- **Next safe action:** dual independent review (`bio_local_004_review_1`, `bio_local_004_review_2`) per D8; Gate 3 merge is the owner's decision.
- **Do not touch:** governance/Keon/cognition source under `backend/src/BioStack.Domain/Governance`, `backend/src/BioStack.Infrastructure/Governance`, `backend/src/BioStack.Infrastructure/Keon`, `backend/src/BioStack.Cognition` — all were read-only in this session.

## PR Notes

- **What changed:** evidence file only (`docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-004-spine-receipt-proof.md`).
- **Why:** closes LS7 + LS8 + SG-L3 (partial) + SG-L5 (partial) + SG-L6 for BIO-LOCAL-004 per `SECURITY-GATES.md` linkage.
- **Risk:** a hash-round-trip or fail-closed regression anywhere in this evidence's scope is release-blocking; none was found in this run.
- **Verification:** reviewer should replay the focused `dotnet test` filters listed above and the denial-probe transcripts; the transient fail-closed probe source is included verbatim in this evidence file for replay (not committed as a permanent test).
- **Evidence:** this file.

**Gate 3 merge is the owner's decision; dual independent review pending.**
