# Governance Enforcement — Adversarial Review Findings

| Field | Value |
|---|---|
| Scope | Both contract surfaces: the .NET consumer (Keon runtime, Governed Spine, doctrine/copy guards) and the Python research sidecar (`backend/src/BioStack.Research`) that produces the governed output |
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

## F3 — HIGH — The "append-only Governed Spine" is not tamper-evident

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

**Citations**

- `RATIFICATION.md` — "Class D: remains prohibited; copy-guard tests enforce"
- `Application/Governance/DoctrineSanitizer.cs:13-31`

**Failure scenario**

The pattern set misses ordinary paraphrases: *"begin with 0.5 mg"*, *"titrate to"*, *"you ought to"*, *"work up to"*, and spelled-out amounts (`\btake\s+\d+` does not match *"take five mg"*), plus unicode lookalikes.

A blocklist is a legitimate **backstop**. The risk is documentary: if the sign-off table is read as "Class D is enforced by tests", a downstream owner may treat regex as the primary control, when the contract itself specifies reviewed templates plus human review as the actual control for Class B/C.

**Recommendation**

Reword the ratification line to "copy-guard tests provide automated backstop coverage for known Class D phrasings"; keep template/allowlist generation and human review named as the primary controls.

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

# Part 2 — Python research sidecar (`backend/src/BioStack.Research`)

The sidecar is the **producer** of the research output this contract governs; Part 1 reviewed only the .NET **consumer**. Paths below are relative to `backend/src/BioStack.Research/`.

> **Tracking defect (fixed 2026-08-02):** this directory was entirely untracked in git (`?? backend/src/BioStack.Research/` — no nested `.git`, not `.gitignore`d). The contract and the ratification record lived inside it, so the document whose own version rule requires a fresh ratification cycle for any change had no history, no diff, and no blame. The governance docs were moved to `docs/guidance/` (tracked); **the sidecar itself remains untracked and still needs a decision.**

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

**Citations:** `privacy.py:11-39` (20-name denylist), `privacy.py:42-55` (walks dict keys only), `contracts/models.py:80-81` (`subject_name` free text ≤256 chars, `known_identifiers` free-form `dict[str, str]`).

**Failure scenario:** the module docstring states "Sidecar must not accept personal health or protocol data." That boundary is enforced by comparing **dict key names** against a fixed list. Values are never examined, so `{"subject_name": "47yo M 92kg, 12mg tirzepatide, symptoms: nausea"}` passes cleanly and flows on to ToolUniverse. Absent from the denylist entirely: `dob`, `birthdate`, `height`, `bmi`, `diagnosis`, `medications`, `labs`, `bloodwork`, `blood_pressure`, `heart_rate`, `gender`, `mrn`, `address`. JSON nested inside a string value is invisible to the walk.

`data_classification` (`app.py:183-193`) is a genuine allowlist and does real work — but it is **caller-asserted**. A caller that mislabels health data as `public_scientific` is believed.

This is the same denylist-where-an-allowlist-belongs defect as F2/F5, applied to health-data egress rather than output wording — the higher-stakes instance of the pattern.

**Recommendation:** constrain `subject_name` to a compound-identifier shape (charset + length) rather than free prose, whitelist permitted `known_identifiers` keys, and treat the denylist as a backstop rather than the boundary.

## S3 — HIGH — The hosted-fallback clause has four flags and zero enforcement points

**The contract clause:** *"Hosted model fallbacks must not receive user health data merely because local GPU/Ollama failed."*

**Citations:** `config.py:47` (`hosted_fallback_enabled`), `contracts/models.py:69` (`allow_hosted_fallback`), `contracts/models.py:97` (`hosted_inference_permitted`), `config.py:16` / `models.py:39` (`ExecutionMode` includes `"hosted_fallback_allowed"`).

**Reference counts across `src/`:** `allow_hosted_fallback` → 1, `hosted_inference_permitted` → 1, `local_inference_permitted` → 1 — each occurrence being its own field definition. `hosted_fallback_enabled` → 4, all of them reporting (definition, probe read, manifest field).

**Finding:** **no execution path reads any of them.** `executor.py` performs no inference whatsoever; `inference/ollama_probe.py` is read-only inventory. The clause is therefore *vacuously* satisfied today — there is no hosted path that could violate it.

The risk is structural rather than present: there is no single choke point that fails closed, and four uncoordinated flags spread across two modules is precisely the F4 drift pattern, pre-loaded. Whoever wires inference will have to remember to consult all four.

**Recommendation:** add one `assert_hosted_inference_allowed(settings, request)` that ANDs every flag and raises by default, call it at the single inference entry point, and write the test asserting rejection-when-any-flag-is-false **now**, while it costs nothing and no implementation exists to retrofit.

## S4 — MEDIUM — Sidecar output cannot satisfy the contract's own promotion requirements

**Citations:** `executor.py:180` (`evidence_class="unknown"` hardcoded on every claim), `executor.py:181` (`source_ids=[tool_name]`), `contracts/models.py:150` (`source_locations` never populated), `contracts/models.py:169-170` (`source_manifest` / `raw_artifact_hashes` never populated).

**Failure scenario:** the contract's Class A required-fields table demands an evidence class, and its escalation rule states *"High-impact extraction without source location → Fail extraction; do not stage as valid."* Every claim the sidecar emits carries `evidence_class="unknown"` and an empty `source_locations`, and its `source_ids` holds a **tool name** rather than a source identifier. By construction, no sidecar claim can clear Class A promotion.

As a safety posture this is correct — everything is candidate, nothing is promotable, which is exactly what the ratification package asserts. The gap is documentary: the ratification record presents "sidecar output remains candidate until review" as a *policy choice*, when it is currently also a *structural impossibility*. Those should not be conflated, because the day someone implements source-location capture, the policy is the only thing left holding the line.

**Recommendation:** state explicitly in the ratification record that sidecar artifacts are structurally non-promotable today, so the policy control is not silently resting on a missing feature.

## S5 — MEDIUM — `202 Accepted` is not asynchronous; execution blocks the event loop

**Citations:** `app.py:197` (`execute_research_job(...)` called synchronously inside `async def submit_job`), `app.py:137-141` (route declares `202 ACCEPTED`), `app.py:50` (`/health`), `app.py:264` (cancel route).

**Failure scenario:** the handler runs the entire research job inline on the event loop, then returns a 202 with a job handle implying background work. A single long job freezes the whole sidecar — including `/health`, which will then report the service as down to any supervisor. The job lifecycle statuses (`QUEUED`, `GATHERING_EVIDENCE`, `PENDING_REVIEW`) are decorative, since the job is finished before the response is written, and `POST .../cancel` can never arrive while there is anything left to cancel.

**Define-only, never enforced:** `max_concurrent_research_jobs` (`config.py:41`), `job_ttl_seconds` (`config.py:42`), `maximum_execution_duration_seconds` (`models.py:71`), `maximum_execution_time_seconds` (`models.py:86`). The .NET caller passes `MaximumExecutionDuration: TimeSpan.FromMinutes(10)` (`AdminEndpoints.cs:492`); nothing on either side honours it.

**Recommendation:** either move execution to a worker and keep the 202 honest, or drop to a synchronous 200 and stop advertising a job lifecycle that does not exist. Enforce the timeout regardless — an unbounded external-tool call on the event loop is the failure mode most likely to take the service down first.

## S6 — LOW/MEDIUM — Every terminal status is `PARTIAL`

**Citations:** `executor.py:186-198` — all three branches assign `ResearchJobStatusCode.PARTIAL` with `partial=True`; `contracts/models.py:57-58` (`PENDING_REVIEW`, `COMPLETED` defined).

Full success, every-tool-errored, and no-steps-executed are indistinguishable to the caller by status alone; the distinction survives only in prose inside `progress_message`. `COMPLETED` and `PENDING_REVIEW` are unreachable. The .NET side cannot gate on outcome without string-matching a human-readable message.

## S7 — LOW — Provenance records a device it did not check

**Citations:** `executor.py:267` (`execution_device="cpu"` hardcoded), `contracts/models.py:176` (same default).

The artifact reports CPU execution unconditionally, without consulting `gpu/capability.py`. Minor today because no GPU work runs — but this is a provenance field on an artifact whose entire purpose is auditability, and a provenance field that is *always* wrong is worse than one that is absent.

---

## Remediation status

| # | Finding | Severity | Status |
|---|---|---|---|
| F1 | Safety receipts throw in default config | Critical | **Fix drafted** — graceful degradation + startup guard |
| F2 | Doctrine guard blocks permitted Class A/B evidence | High | Open — needs template/allowlist design |
| F3 | Spine not tamper-evident | High | Open — needs `PreviousEntryHash` + migration |
| F4 | Duplicate banned-phrase lists, drifted | Medium | Open — extract shared ruleset |
| F5 | Regex documented as enforcement | Medium | Open — wording change in `RATIFICATION.md` |
| F6 | Input screened with output doctrine | Low | Open — one-line scope change |
| — | Sidecar + governance docs untracked in git | Critical | **Docs fixed** — moved to `docs/guidance/`; sidecar tracking still undecided |
| S1 | Sidecar unauthenticated on all interfaces by default | High | Open — bind loopback + require token |
| S2 | Privacy boundary checks key names, not values | High | Open — constrain `subject_name` / `known_identifiers` |
| S3 | Hosted-fallback clause has no enforcement point | High | Open — add single choke point + test now |
| S4 | Sidecar output structurally non-promotable | Medium | Open — document in ratification record |
| S5 | `202 Accepted` blocks the event loop; timeouts unenforced | Medium | Open |
| S6 | All terminal statuses are `PARTIAL` | Low/Med | Open |
| S7 | `execution_device` hardcoded in provenance | Low | Open |

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
