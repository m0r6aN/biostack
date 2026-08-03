# Governance Enforcement — Adversarial Review Findings

| Field | Value |
|---|---|
| Scope | Both contract surfaces: the .NET consumer (Keon runtime, Governed Spine, doctrine/copy guards) and the Python research sidecar (`backend/research-sidecar`) that produces the governed output |
| Method | Static read of enforcement source on the working tree; **not** a live/runtime test. No .NET SDK or Python runtime available in the review environment — nothing here was compiled or executed. |
| Date | 2026-08-02 |
| Companion | `RATIFICATION.md`, `biostack-guidance-content-contract.v1.md` |
| Relationship to `BIOSTACK_FRONTEND_READINESS_AUDIT.md` | That audit is explicitly product/UX and states "Not a code review". It scored *Safety / trust posture* 5/5 without reading the enforcement code. This document reviews exactly that gap. |

---

## Why this document exists

The Guidance Content Contract and the ratification package describe what BioStack **may say**. This document records whether the **runtime actually enforces it**, and where the gap between doctrine and implementation is wider than the docs admit.

Findings are ordered by blast radius. Each carries a file:line citation so the claim is checkable, and a concrete failure scenario rather than a style objection.

---

## What is genuinely sound

Recorded first so the criticism below is read as calibrated, not reflexive.

- **The Lane H choke point is real, not gate-shaped theater.** User-facing intelligence is routed through `IUserFacingIntelligenceGate.EvaluateAsync` before serialization at `BioStack.Api/Endpoints/IntelligenceEndpoints.cs:182` and `BioStack.Api/Endpoints/StackReviewEndpoints.cs:150`.
- **Spine uniqueness is a real DB constraint**, not just an app check — unique index on `ReceiptUri` at `Infrastructure/Persistence/Migrations/20260511000000_AddGovernedSpine.cs:42-46`.
- **Fail-closed intent is consistently expressed** across the stub, the policy gate, and the evidence gate.
- **`HighRiskCategoryGate` is substantive** — alias/code-variant aware (`rad-140` / `rad140` / `testolone`), with "unknown beats inference" stated and implemented (`Application/Governance/HighRiskCategoryGate.cs:38-42`).
- **Severity never downgrades** in the output gate — `Escalate` / `Rank` at `UserFacingIntelligenceGate.cs:240-251`.

---

## F1 — CRITICAL — Safety-relevant responses throw in the shipped default configuration

**Status: fix drafted in this change set (see "Remediation" below).**

**Citations**

- `BioStack.Api/appsettings.json:26-32` — `KeonRuntime.LiveMode: false`, `StubAllowAll: false`
- `BioStack.Api/appsettings.Development.json:23-26` — same
- `Infrastructure/Keon/KeonRuntimeDependencyInjection.cs:19,33` — non-live config binds `KeonRuntimeClientStub`
- `Infrastructure/Keon/KeonRuntimeClientStub.cs:31-33` — `IssueReceiptAsync` returns a **faulted task** (`KeonRuntimeUnavailableException`)
- `Infrastructure/Keon/RuntimeReceiptFactory.cs:66` — awaits `keon.IssueReceiptAsync` with no guard
- `Application/Governance/UserFacingIntelligenceGate.cs:173-174, 224-231` — awaits `receipts.IssueAndAppendAsync` with no `try`/`catch`
- `Application/Governance/UserFacingIntelligenceGate.cs:204-210` — a receipt is issued **only** when status is `Refused`, `Constrained`, or `Warning`
- `BioStack.Api/Program.cs` — no `UseExceptionHandler`, `AddProblemDetails`, or `IExceptionHandler` registered anywhere in the API project

**Failure scenario**

In any environment without a live Keon Runtime (default config, staging, a provider demo box, a misconfigured production deploy):

1. A user requests intelligence on a benign compound → gate returns `Allowed` → no receipt is issued → **request succeeds**.
2. A user requests intelligence on a SARM, BPC-157, TB-500, or a compounded GLP-1 → `HighRiskCategoryGate` matches → status escalates to `Warning` → `MaybeIssueReceiptAsync` calls the factory → stub throws → exception propagates through the endpoint unhandled → **HTTP 500**.

The same path fires for *any* fallback (non-graph) response, since fallback also escalates to `Warning` (`UserFacingIntelligenceGate.cs:164-170`).

**Why this is the top finding**

The failure is *inversely correlated with safety*: the system works on low-risk queries and breaks precisely when the safety gate does its job. It is invisible to smoke tests that check a common compound, and it is the exact surface the readiness audit graded 5/5 without reading.

**Recommendation**

Non-effecting provenance receipts must degrade, not crash. Effect-bearing receipts must continue to fail closed — that distinction is doctrinally load-bearing and is preserved by the drafted fix.

---

## F2 — HIGH — The doctrine guard blocks the Class A/B evidence language the contract exists to permit

**Status: CLOSED — see "F2 resolution" below.**

**Citations**

- `Application/Governance/DoctrineSanitizer.cs:19` — bans `\bis\s+safe\b`
- `Application/Governance/DoctrineSanitizer.cs:22` — bans `\bcures?\b`
- `Application/Governance/DoctrineSanitizer.cs:23` — bans `\bproven\s+to\b`
- `Application/Services/EvidenceGate.cs:103` — scans `mechanismSummary`, `rationaleText`, `summary`
- `Application/Services/EvidenceGate.cs:178-190` — rejects with `unsafe_recommendation_language`

**Failure scenario**

A correctly cited, reviewer-approved Class A extraction — *"the agent was well tolerated and considered safe at the studied doses (NCT…)"*, or *"the review found no evidence it cures the underlying condition"* — is rejected at promotion, or rewritten to `[review-required]` on output.

The guard cannot distinguish speaker from subject: **"BioStack says it is safe"** (prohibited Class D) and **"the cited trial reported it was safe"** (permitted Class A) are the same byte pattern.

**Why it matters now**

This directly contradicts the canon reconciliation dated 2026-08-02 and the Class A/B permissions the ratification package is being signed against. The product's differentiator — surfacing sourced evidence so users are not left to unsourced social-media protocol advice — is being suppressed by its own safety layer.

**Recommendation**

Move Class A/B toward **template/allowlist generation** (as the contract itself specifies) rather than blocklist filtering, and scope the blocklist to unattributed narrative only. Quoted, citation-attached source text should traverse a different path than model-authored prose.

---

## F2 resolution — speaker vs subject

The guard could not distinguish **who is speaking** from **what is claimed**. "BioStack says it is safe" and "the cited trial reported it is safe" are the same byte pattern, so the blocklist suppressed the sourced evidence the product exists to surface.

**Mechanism.** Doctrine is now two tiers, both living in `DoctrineRuleset`:

| Tier | Patterns | Rule |
|---|---|---|
| `PersonalizedDirection` | `you should`, `you must`, `safe for you`, `the best dose for you`, `recommended dose for your`, `ai recommends`, `take N mg`, `dose at`, `start at`, `increase to`, `stop taking` | Prohibited **unconditionally**. A citation never redeems an imperative — the contract's own Class A examples avoid them even when reporting a source. |
| `AttributionSensitiveClaim` | `is safe`, `cures`, `proven to`, `will treat` | Prohibited only when **unattributed**. As BioStack's assertion these are Class D; as a report of a cited finding they are Class A published-evidence context. |

`OutputAttribution` defaults to `Unattributed`, so a caller must **prove** attribution to get the permissive tier — fail-closed, consistent with the rest of the governance layer.

**Where it is applied.** Exactly one production call site changed: `EvidenceGate` Check 8. That check runs *after* Check 6, which already rejects any record without citations — so attribution there is **structural, not asserted**, which is what makes the relaxation safe. Every other consumer (`UserFacingIntelligenceGate`, `ProtocolIntelligenceGate`, `PolicyGate`) screens BioStack-authored narrative and keeps the strict tier.

**No contract version bump.** Class A always permitted source-backed evidence context; the *code* was over-broad relative to the contract. This makes enforcement match the ratified text rather than changing it, so the pending sign-off is unaffected — which matters while legal review is outstanding.

**Behaviour change of record.** `EvidenceGateTests` previously asserted that `summary = "This compound is safe for long-term use"` must be rejected as `unsafe_recommendation_language`. That assertion encoded the defect. It is replaced by `Evaluate_AttributedSourceClaim_IsPermitted`, and the rejection theory retains a personalized-direction case in its place, so the strict path is still covered.

**Still deliberately out of scope.** Class B/C *user-facing* templates remain gated behind the ratification sign-off table. This change unblocks Class A promotion of cited evidence; it does not enable any new public surface.

---

## F3 — HIGH — The "append-only Governed Spine" is not tamper-evident

**Status: CLOSED — see "F3 resolution" below. Hand-written migration committed.**

**Citations**

- `Domain/Governance/SpineEntry.cs:1-21` — carries `InputHash`, but **no** `PreviousEntryHash` / chain field
- `Infrastructure/Governance/SpineRepository.cs:20-31` — append-only enforced by an application-layer existence check
- `Infrastructure/Persistence/Migrations/20260511000000_AddGovernedSpine.cs:42-46` — unique index on `ReceiptUri` (prevents duplicates only)

**Failure scenario**

There is no hash chain linking entry *n* to entry *n-1*. Nothing detects an out-of-band `UPDATE` or `DELETE` issued directly against the database. In a local-first product the SQLite file sits on the user's — or a provider's — own disk and is editable with any SQLite browser.

Consequently "Receipt Supremacy" and the user-facing **Audit Receipts** surface rest on a ledger whose holder can silently rewrite it. Duplicate-insert protection is not tamper-evidence.

**Recommendation**

Add a `PreviousEntryHash` committing each entry to its predecessor, expose a chain-verification routine, and — if receipts are ever to be compliance- or dispute-load-bearing — anchor the chain head server-side on a cadence.

---

## F3 resolution — a chain, not just a unique index

**Mechanism.** `SpineEntry` gains three fields: `SequenceNumber` (genesis = 0), `PreviousEntryHash` (genesis sentinel `sha256:genesis`), and `EntryHash` — SHA-256 over the entry's governed fields *plus* its predecessor's hash. Every entry therefore commits to the one before it, so altering a field, changing a timestamp, or deleting a row invalidates every entry that follows.

Fields are **length-prefixed** before hashing, so content shifted across a field boundary cannot produce an identical digest (`"a"+"bc"` and `"ab"+"c"` hash differently). Timestamps use round-trip `"O"` format for culture- and precision-stability.

**A forked chain is unwritable, not merely discouraged.** Unique indexes on `SequenceNumber`, `PreviousEntryHash`, and `EntryHash` mean a sequence slot can be claimed once and an entry can have at most one successor. Two concurrent appends read the same head, and the loser violates the constraint — which is *correct*, so `AppendAsync` retries a bounded number of times and surfaces `SpineChainContentionException` if it genuinely cannot win.

**Verification reports the earliest break, not a boolean.** `VerifyChainAsync` walks from genesis checking sequence contiguity, linkage, and a recomputed hash, and returns the first divergent receipt with a reason. `GET /api/v1/receipts/chain/verify` exposes it (admin-only — it is ledger-wide state). Individual receipts now carry `sequenceNumber`, `previousEntryHash`, and `entryHash`, so a holder can verify a single receipt sits where it claims to.

**What this does and does not buy.** It makes the ledger tamper-**evident**. It does not make it tamper-**proof**: a holder with write access can still rewrite the entire chain consistently. Closing that is **F3+** (signed chain-head checkpoints) — see below.

**The migration is hand-written, per repository convention.** `20260803000000_AddSpineHashChain.cs` plus a partial `.Designer.cs`.

Do **not** run `dotnet ef migrations add` in this repository. `BioStack.Api/ProductionMigrationBaselineConfiguration.cs` states the convention explicitly: *"Migrations in this repository are intentionally hand-written and the central snapshot is intentionally minimal."* The committed `BioStackDbContextModelSnapshot.cs` is 126 lines describing a single entity. EF compares the full model against that deliberately incomplete snapshot, concludes most tables do not exist, and scaffolds `CreateTable` for 25 of them — including `AppUsers`, `KnowledgeEntries`, and `SpineEntries` — while rewriting the snapshot to ~1900 lines. Applying such a migration against a live database would attempt to create tables that already exist. This was tried during F3 and reverted; the guard rail is this paragraph.

**A unique index that could not survive backfill.** The first cut of F3 put unique indexes on all three chain columns. `AddColumn` gives every pre-existing row the *same* default, so a unique constraint on `PreviousEntryHash` fails on the second legacy row — the migration could not have applied to any populated database. `PreviousEntryHash` is now a plain index. Uniqueness on `SequenceNumber` is what actually enforces linearity (two concurrent appends compute the same slot; one loses at the database), and `EntryHash` uniqueness is backfillable because it is derived from the already-unique `ReceiptUri`.

**Backfill of legacy rows — a governance decision, now recorded.** Rows written before the chain existed cannot be retro-chained: their hashes were never computed, and inventing them would be precisely the forgery the chain exists to detect. The migration assigns deterministic per-row-unique placeholders (`row_number()` for the sequence, `'sha256:pre-chain:' || ReceiptUri` for the hash) purely so the constraints can be created. `VerifyChainAsync` will report the first such row as a hash mismatch. **That is correct and intended**: the migration point is the chain's effective genesis, and pre-migration history is not cryptographically verifiable. The alternative is export-and-reseed. Record whichever is chosen in the ratification package.

---

## F3+ — HIGH — Chain is tamper-evident, not tamper-proof

**Status: CLOSED (foundation).** Full external anchoring still depends on **where the signing key lives** and whether checkpoint manifests are exported off-box.

**Problem.** A holder with write access to the SQLite/Postgres file can rewrite the entire hash chain consistently. F3 detects casual edits; it does not stop a determined rewrite.

**Fix.**

| Piece | Role |
|---|---|
| `SpineChainCheckpoint` | Snapshots `(sequenceNumber, headEntryHash)` at a point in time |
| HMAC-SHA256 signature | Key from `SpineCheckpoint:SigningKey` (env / secret store — **not** the Spine DB) |
| `source` | `local-hmac` / `server-hmac` (when `SigningKeyIsServerHeld=true`) / `unsigned-local` |
| Auto every N entries | `AutoCheckpointEveryNEntries` (default 25) after Spine append |
| Cadence worker | `SpineCheckpointCadenceHostedService` every `CadenceMinutes` (default 60) if head advanced |
| Admin APIs | `POST /api/v1/receipts/chain/checkpoints`, `GET .../verify`, `GET .../latest/export` |
| Migration | `20260803120000_AddSpineChainCheckpoints` |

**Operator contract for real external anchoring**

1. Set a high-entropy `SpineCheckpoint:SigningKey` (or `SpineCheckpoint__SigningKey`) that is **not** stored on the same volume as the DB when possible.
2. Set `SigningKeyIsServerHeld=true` when the key is provisioned by platform/server, not the device.
3. Periodically `GET .../chain/checkpoints/latest/export` and store the JSON **off-box** (object storage, SIEM, Keon, paper).
4. On dispute: re-verify the live chain, then check the exported signature with the server key.

Unsigned checkpoints still record history but do **not** claim external protection.

---

## F4 — MEDIUM — Two copies of the banned-phrase doctrine, already drifted

**Citations**

- `Application/Governance/DoctrineSanitizer.cs:13-31` — **16** patterns
- `Application/Governance/PolicyGate.cs:34-45` — **9** patterns
- `Application/Governance/PolicyGate.cs:31-33` — comment: *"changes to one must be applied to the other"*
- `BioStack.Application.Tests/Governance/GuidanceContentContractCopyGuardTests.cs:14` — the contract copy-guard suite instantiates **`DoctrineSanitizer` only**

**Failure scenario**

`PolicyGate` is missing `safe for you`, `start at`, `increase to`, `the best dose for you`, `recommended dose for your`, and `ai recommends`. In live-Keon mode `PolicyGate` is the local pre-classifier, so those Class D phrases skip local blocking. The copy-guard suite stays green throughout, because it never exercises `PolicyGate`.

The hand-sync instruction has already failed once; it will fail again.

**Recommendation**

Extract a single shared ruleset consumed by both, and point the contract copy-guard tests at the shared ruleset so drift becomes impossible rather than merely discouraged.

---

## F5 — MEDIUM — Regex blocklist is documented as "enforcement"

**Status: CLOSED (documentary).**

**Failure scenario (pre-fix):** ratification read as if Class D were *enforced by* copy-guard regex tests, inviting owners to treat the blocklist as the primary control.

**Fix.** `RATIFICATION.md` engineering table now states primary control is **reviewed templates + human review**, with copy-guard tests as automated **backstop** coverage for known Class D phrasings — not the sole enforcement.

---

## F6 — LOW — Input screening will refuse legitimate safety questions

**Citations**

- `Application/Governance/UserFacingIntelligenceGate.cs:236-238` — `IsUnsafeRequest` runs `DoctrineSanitizer.ContainsBannedPhrase` over **user input**
- `Application/Governance/DoctrineSanitizer.cs:24` — `\bstop\s+taking\b`

**Failure scenario**

*"Should I stop taking this before surgery?"* matches `stop taking` → whole response becomes `RefusalText`. The output doctrine (constraints on what **BioStack** may assert) is being applied to what a **user** may ask.

This taxes the most safety-conscious users — the precise population the harm-reduction mission targets.

**Recommendation**

Screen input with the intent-based `UnsafeRequestPatterns` only (`UserFacingIntelligenceGate.cs:91-97`); reserve the output doctrine for output.

---

---

# Part 2 — Python research sidecar (`backend/research-sidecar`)

The sidecar is the **producer** of the research output this contract governs; Part 1 reviewed only the .NET **consumer**. Paths below are relative to `backend/research-sidecar/` (formerly `backend/src/BioStack.Research/`, moved out of the .NET source root).

> **Tracking defect (fixed 2026-08-02):** the sidecar directory was initially untracked in git. The contract and ratification record lived inside it. Governance docs were moved to `docs/guidance/` (tracked); the sidecar is tracked under `backend/research-sidecar/`.

## What is sound

- **No arbitrary tool execution by design** — `ALLOWED_WORKFLOWS` allowlist (`contracts/models.py:22-32`), enforced at `app.py:174-181`, and `"No ExecuteAnyTool endpoint exists by design."` (`app.py:87`).
- **ToolUniverse is pinned, optional, and double-gated** — exact version pin, install extra, plus a separate runtime flag (`config.py:50-52`, `workflows/executor.py:71-76`).
- **Kill switches, global and per-workflow** (`kill_switches.py:15-25`), checked before any execution (`executor.py:36`).
- **`data_classification` is an allowlist, not a denylist** (`app.py:183-193`) — the strongest single control in the request path.
- **A global exception handler exists** (`app.py:287-296`). Noted for contrast: this is exactly what `BioStack.Api` lacks in F1.
- **The .NET caller pins the risky knobs off** — `AllowHostedFallback: false`, `HostedInferencePermitted: false`, `DataClassification: "public_scientific"` (`BioStack.Api/Endpoints/AdminEndpoints.cs:490,497,499`), behind `RequireAuthorization("AdminOnly")` (`AdminEndpoints.cs:22`).

## S1 — HIGH — Unauthenticated on every interface in the default configuration

**Citations:** `config.py:27` (`host: str = "0.0.0.0"`), `config.py:32` (`service_token: str = ""`), `auth.py:14-17` (empty token → `return`, commented "Dev mode"), `auth.py:26` (`token != expected`).

**Failure scenario:** with stock settings the sidecar binds every interface and skips authentication entirely. All `/internal/v1/*` routes — job submission, GPU and inference capability disclosure, and job results — are reachable by anything on the network. The .NET caller connects over loopback (`appsettings.json` → `ScientificResearchSidecar.BaseUrl: http://127.0.0.1:8080`), so binding `0.0.0.0` buys nothing and costs the entire LAN surface. The GPU/inference manifests are also a free host-reconnaissance endpoint.

Separately, `auth.py:26` compares tokens with `!=` rather than `hmac.compare_digest` — a timing side channel. Low severity, one-line fix.

**Recommendation:** default `host` to `127.0.0.1`; refuse to start when `service_token` is empty **and** the bind address is non-loopback; switch to `hmac.compare_digest`.

## S2 — HIGH — The privacy boundary inspects key names and never values

**Status: CLOSED.**

**Citations (pre-fix):** key-only denylist, free-form `subject_name` / `known_identifiers`.

**Failure scenario:** the boundary compared **dict key names** against a fixed list and never examined values, so health prose in `subject_name` or free-form `known_identifiers` could reach ToolUniverse while still labeled `public_scientific`.

**Fix.** Five layers, fail closed:

1. Top-level request field allowlist (unknown keys rejected).
2. Nested key denylist for known health/identity fields (backstop).
3. Free-text value scanning for health/identity patterns.
4. **`subject_name` compound-identifier shape** — charset, max 128 chars, token-count cap (not free prose).
5. **`known_identifiers` key whitelist** — public scientific registry keys only (`cid`, `chembl_id`, `uniprot`, `pmid`, …); values must look like registry tokens.

`data_classification` remains caller-asserted and is still an allowlist for classification labels — the shape/whitelist layers are what prevent mislabeled health content from riding along.

## S3 — HIGH — The hosted-fallback clause has four flags and zero enforcement points

**Status: CLOSED.**

**The contract clause:** *"Hosted model fallbacks must not receive user health data merely because local GPU/Ollama failed."*

**Fix.** `inference_policy.py` is the single choke point: `assert_hosted_inference_allowed` ANDs every authorization flag and fails closed by default; `assert_no_silent_hosted_escalation` runs at job start in `executor.py` so partial hosted flags are rejected before any work. Tests in `test_inference_policy.py` lock the matrix. No inference path executes models yet — when one is wired, it must call these asserts (already the documented entry points).

## S4 — MEDIUM — Sidecar output cannot satisfy the contract's own promotion requirements

**Status: CLOSED (documentary).**

**Citations:** `executor.py` (`evidence_class="unknown"` hardcoded), `source_ids` = tool name, `source_locations` / `source_manifest` never populated.

**Finding:** By construction, no sidecar claim can clear Class A promotion. That is correct as safety posture, but the ratification record had presented "candidate until review" only as *policy*, when it is also a *structural impossibility*.

**Fix.** `docs/guidance/RATIFICATION.md` now has an explicit **"Structural non-promotability of sidecar output (S4)"** section: policy and structure are named separately, and the note records that when source-location capture lands, policy alone must hold the line. No contract version bump — documentation of an implementation fact, not a class change.

## S5 — MEDIUM — `202 Accepted` is not asynchronous; execution blocks the event loop

**Status: CLOSED (foundation worker).**

**Failure scenario (pre-fix):** the handler ran the research job inline, so a long job froze `/health` and made cancel/status lifecycle decorative.

**Fix.** `JobRunner` reserves a concurrency slot at submit, returns `202` with `status=queued` immediately, and executes off the event loop on a bounded thread pool. Enforced:

| Setting | Behaviour |
|---|---|
| `max_concurrent_research_jobs` | Non-blocking reserve; excess submits get `429 max_concurrent_jobs` |
| `maximum_execution_time_seconds` / `execution.maximum_execution_duration_seconds` | Tighter of the two is the worker timeout; timeout → `FAILED` / `execution_timeout` |
| `job_ttl_seconds` | Terminal jobs older than TTL are purged from the in-memory store |

`/health` reports `jobs_in_flight` and the concurrency cap. Cancel remains cooperative at job-start checkpoints (mid-tool interrupt is not yet wired).

## S6 — LOW/MEDIUM — Every terminal status is `PARTIAL`

**Status: CLOSED.**

**Fix.** Tool-sequence outcomes map as:

| Outcome | Status | `partial` |
|---|---|---|
| All allowlisted tools succeeded | `pending_review` | false |
| Mix of success and failure | `partial` | true |
| Tools invoked, all failed | `failed` | false |
| No steps executed | `failed` | false |
| ToolUniverse disabled (scaffold) | `partial` | true |
| Kill switch / hosted policy | `rejected_by_policy` | false |

Candidate claims still require human review — full tool success is `pending_review`, not silent canonical completion. `COMPLETED` remains reserved for a future fully-closed path that does not produce review-staged candidates.

## S7 — LOW — Provenance records a device it did not check

**Citations:** `executor.py:267` (`execution_device="cpu"` hardcoded), `contracts/models.py:176` (same default).

The artifact reports CPU execution unconditionally, without consulting `gpu/capability.py`. Minor today because no GPU work runs — but this is a provenance field on an artifact whose entire purpose is auditability, and a provenance field that is *always* wrong is worse than one that is absent.

---

## F7 — HIGH — Governance registrations can vanish without failing the build

**Status: CLOSED — container validation enabled in all environments + DI smoke test.**

**Citations:** PR #246 ("Restore research and evidence DI after #245 merge"), `BioStack.Api/Program.cs:408-412`, `BioStack.Application/Governance/GovernanceDependencyInjection.cs:14-20`.

**Failure scenario:** a merge silently dropped governance DI registrations and it took a dedicated follow-up PR to notice. Nothing caught it: the build succeeds, and .NET's `ValidateOnBuild` defaults to Development-only, so in Production a missing registration does not fail at startup — it surfaces as an unhandled `InvalidOperationException` (HTTP 500) on the first request that injects the service. The user discovers the safety gate is missing.

This is the same shape as F1 — a governance control failing open-ish at request time rather than loudly at boot — but arrived at through source control rather than configuration. It is a **demonstrated** regression class, not a hypothetical one.

**Fix:** `Program.cs` now sets `ValidateOnBuild` and `ValidateScopes` for every environment, so an unresolvable or captively-scoped registration fails the host build. `GovernanceDependencyInjectionSmokeTests` additionally resolves each service on the governed output path, so a dropped line fails CI before merge.

**Operational note:** enabling `ValidateOnBuild`/`ValidateScopes` globally can surface *pre-existing* latent wiring bugs elsewhere in the application as new startup failures. That is the intended behaviour — a captive dependency is a real defect — but it means the first run after this change may fail loudly on something unrelated to governance. Do not treat such a failure as a regression from this change; treat it as the check working.

---

## S8 — HIGH — The ToolUniverse allowlist existed twice, and the working directory decided which one won

**Status: CLOSED.**

**Citations:** `tooluniverse_integration/allowlist.py` (`_default_allowlist_path`, pre-fix), `config/tooluniverse_allowlist.v1.json` (removed), `src/biostack_research_sidecar/data/tooluniverse_allowlist.v1.json`.

**Failure scenario.** Two tracked copies of the allowlist, byte-identical (2497 bytes) but with nothing enforcing that. Resolution tried three candidates in order:

1. `here.parents[3]/config/...` — the repo copy. Wins in an editable/dev layout.
2. `Path.cwd()/config/...` — **whatever directory the process started in.**
3. `here.parents[1]/data/...` — the packaged copy. Wins in a wheel.

Two consequences. First, dev enforced `config/` while a deployed wheel enforced `data/`, so the two could drift into *different tool allowlists* with both environments looking healthy. Second — and worse — candidate 2 sits between them: in a container the first candidate misses, so a `config/tooluniverse_allowlist.v1.json` present in the WORKDIR (mounted, copied, or left over) silently replaced the vetted allowlist. This is the control deciding which external tools may execute, resolved by current working directory, with nothing logging which file was chosen.

**Fix.** One canonical location: the copy inside the package, which is present in the editable layout and ships in the wheel (hatchling includes the whole package directory). CWD is never consulted. Operators who need a different allowlist set `BIOSTACK_RESEARCH_TOOLUNIVERSE_ALLOWLIST_PATH` explicitly — deliberate override, no implicit discovery — and a missing packaged allowlist is a hard failure rather than a fallback to something unvetted. The resolved path now travels on the allowlist object and is reported by `/internal/v1/capabilities/tooluniverse`, because an allowlist you cannot locate is one you cannot audit. The legacy `config/` copy has been deleted; `test_legacy_config_copy_has_not_drifted` is a no-op when that path is absent.

**Verification — executed, not reasoned.** Allowlist resolution tests pass, including the security regression: a decoy `config/tooluniverse_allowlist.v1.json` planted in the working directory advertising `ExecuteAnyTool` and `shell_exec` is ignored, and the packaged allowlist (v1.0.0, pin 1.4.0) loads instead.

---

## Remediation status

| # | Finding | Severity | Status |
|---|---|---|---|
| F1 | Safety receipts throw in default config | Critical | **CLOSED** (#245) — verified on `main`: `TryIssueAndAppendAsync` + defence-in-depth catch + production startup guard |
| F2 | Doctrine guard blocks permitted Class A/B evidence | High | **CLOSED** — two-tier doctrine (`OutputAttribution`); `EvidenceGate` Check 8 now evaluates source-attributed. No contract version bump: the code was over-broad relative to Class A, not the contract |
| F3 | Spine not tamper-evident | High | **CLOSED** — hash chain + hand-written migration `20260803000000_AddSpineHashChain` + microsecond UTC stamp + non-unique `PreviousEntryHash` |
| F3+ | Spine not tamper-proof (holder rewrite) | High | **CLOSED (foundation)** — HMAC chain-head checkpoints, cadence worker, admin create/export/verify; server-held key + off-box export for full external anchor |
| F4 | Duplicate banned-phrase lists, drifted | Medium | **CLOSED** — both guards delegate to `DoctrineRuleset`; `DoctrineRulesetParityTests` fails CI on any future drift |
| F5 | Regex documented as enforcement | Medium | **CLOSED (documentary)** — primary = templates + human review; copy-guards = backstop |
| F6 | Input screened with output doctrine | Low | **CLOSED** — `IsUnsafeRequest` screens intent only; instruction-seeking patterns added as the compensating control |
| — | Sidecar + governance docs untracked in git | Critical | **CLOSED** — docs in `docs/guidance/`; sidecar tracked at `backend/research-sidecar/` |
| S1 | Sidecar unauthenticated on all interfaces by default | High | **CLOSED** (#242 + follow-up) — loopback default, `_enforce_bind_auth_policy` refuses unauthenticated non-loopback binds, `hmac.compare_digest` |
| S2 | Privacy boundary checks key names, not values | High | **CLOSED** — compound `subject_name` shape + `known_identifiers` whitelist + value scan |
| S3 | Hosted-fallback clause has no enforcement point | High | **CLOSED** — `inference_policy` choke point + job-start escalation assert + tests |
| S4 | Sidecar output structurally non-promotable | Medium | **CLOSED (documentary)** — structural vs policy called out in `RATIFICATION.md` |
| S5 | `202 Accepted` blocks the event loop; timeouts unenforced | Medium | **CLOSED** — `JobRunner` background pool, 429 at capacity, per-job timeout, TTL purge |
| S6 | All terminal statuses are `PARTIAL` | Low/Med | **CLOSED** — `pending_review` / `partial` / `failed` mapped by tool outcome |
| S8 | Duplicate allowlist; CWD decided which one loaded | High | **CLOSED** — single packaged source, CWD never consulted, resolved path reported; legacy `config/` copy removed |
| S7 | `execution_device` hardcoded in provenance | Low | **Accepted as accurate** — no GPU/inference path exists, so `"cpu"` is correct today; annotated as an audit field that must change when one lands |

### F1 remediation design (drafted)

The governing distinction is **effect-bearing vs. non-effecting**, and it is preserved:

- `IssueAndAppendAsync` is unchanged and still **throws**. Every effect-bearing receipt continues to fail closed — First Law intact.
- `TryIssueAndAppendAsync` is added for **non-effecting** provenance receipts only. It refuses any context whose `EffectStatus` is not on the known non-effect-bearing allowlist, so it cannot be used to launder an effect past the gate.
- When Keon is unavailable, a clearly-labelled **unanchored** spine row is written (`biostack://unanchored-receipt/{id}`, policy hash `unanchored-local`) so provenance is preserved rather than lost — strictly more auditability than today, where the request crashes and records nothing. An unanchored row can never be mistaken for a Keon-authoritative receipt: different URI scheme, different policy hash.
- `UserFacingIntelligenceGate` additionally wraps the call defensively, so no receipt-layer fault can ever 500 a safety response.
- A **startup guard** refuses to boot in Production with a stubbed Keon runtime unless `AllowStubInProduction` is explicitly set, so "silently running ungoverned in prod" becomes impossible rather than merely undesirable.

**Open decision for governance sign-off:** whether unanchored local rows are acceptable provenance, or whether the surface should hard-fail instead. The drafted default (`AllowUnanchoredSafetyReceipts: true`) favours preserving the audit trail and keeping the safety warning visible to the user. Flipping it to `false` degrades to "warning shown, nothing recorded".

**Follow-up not included in the fix:** surfacing provenance degradation to the UI (e.g. an `unanchored` flag on the receipts surface) is a product decision, deliberately left out of a governance-layer patch.

---

## Verification

```bash
cd backend
dotnet test tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj \
  --filter "FullyQualifiedName~GuidanceContentContract|FullyQualifiedName~DoctrineSanitizer|FullyQualifiedName~UserFacingIntelligenceGate"
```

Static claims in this document were verified by direct file read on 2026-08-02. Runtime claims (the HTTP 500 in F1) are derived from static control flow plus the absence of exception-handling middleware in `BioStack.Api`; they have **not** been reproduced against a running instance. Reproducing F1 live before ratification sign-off is recommended.
