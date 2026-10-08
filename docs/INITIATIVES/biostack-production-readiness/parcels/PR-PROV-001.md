# Parcel: PR-PROV-001

## Goal
Harden provider operations (rate limiting, intake data-minimization/retention, SLA/owner
accountability, audit trail) and deliver the authorized deployment-config review that documents
and closes the `KeonRuntime__*` production-compose pass-through gap, without touching Stripe,
SMTP/Azure email, or any live/production environment.

## Initiative
`biostack-production-readiness`

## Project Track
T4 — Provider operations (`TRACKS.md`)

## Wave
Hardening (`PARCELS.md` row `PR-PROV-001`)

## Owner Authorization
Owner ruling D-F(2) (`COORDINATOR-DECISIONS-2026-10-07.md`): "PR-PROV-001 (provider operations)
+ deployment-config review START NOW in parallel; Stripe remains TEST-MODE until the KEO-68
runbook validates; owner Clint Morgan is the named release owner for PR-REL-001. The production
initiative's NO-GO/HOLD verdict is unchanged until its own gates clear with evidence." This spec
is the authorized shape for that start; it does not itself authorize dispatch, merge, or release.

## Branch
`TBD` — assigned by the coordinator at dispatch, per `PARCELS.md`'s `PR-PROV-001` row
(`isolated required`). This shaping parcel does not dispatch implementation work.

## Worktree
`isolated required` — a dedicated worktree is named at dispatch; no ambient worktree is used.

## Dependencies
- `PR-DOC-001` (merged foundation; this parcel reads its reconciled initiative state).
- `SEC-PROVIDER-001` (`integrated-local`): non-enumerating provider intake, opaque public
  acknowledgement, admin-only queue mutation. This parcel builds on that boundary and must not
  weaken it.
- Current release baseline referenced by `RELEASE-GATES.md`/`EVIDENCE.md` (`E12`,
  hosted `c96bc3b`). This parcel does not change baseline status.

## Integration Surfaces
- Public provider-access intake: `POST /api/v1/provider-access/requests` (anonymous, rate-limited).
- Administrative provider queue: `GET /api/v1/admin/provider-access/requests`,
  `PATCH /api/v1/admin/provider-access/requests/{requestId}` (`AdminOnly`).
- `backend/src/BioStack.Api/Program.cs` rate-limiter policy registration (`provider-access`
  policy only).
- Production deployment configuration: `docker-compose.yml`, `.env.example`, and the environment
  matrix they describe (review/documentation surface, not a deployment surface).

## Security Gate
Addresses `SECURITY-GATES.md` `SG4` (Provider PII abuse/retention: rate-limit, minimization,
access, retention and deletion review) and `RISKS.md` `R6` (provider leads lack
notification/SLA/retention ownership). This parcel produces local/test-mode evidence toward SG4
and R6; it cannot itself close SG4 (SG4 additionally requires the dedicated defensive security
review named in `SECURITY-GATES.md`, which this parcel does not perform or claim).

## Deployment-Config Review (D-F(2) authorized deliverable)

Continuation of the gap recorded in
`docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-001-config-note.md`
("Production Compose Gap — Known Limitation"): `docker-compose.yml` does not pass any
`KeonRuntime__*` variable to the `biostack-api` service, and `.env.example` does not list the
corresponding `KEON_RUNTIME_*` host variables. Left uncorrected, a production deployment using
this compose file either fails closed with no operator guidance (opaque `InvalidOperationException`
at boot) or an operator sets `KeonRuntime__AllowStubInProduction=true` without a documented,
reviewed decision to run ungoverned.

Required deliverable (config review, not a live deployment):

1. Add `KeonRuntime__LiveMode`, `KeonRuntime__BaseUrl`, `KeonRuntime__BearerToken`, and
   `KeonRuntime__TimeoutMs` to the `biostack-api` service's `environment` block in
   `docker-compose.yml`, sourced from new `.env` keys with safe defaults (`LiveMode` defaults to
   `false`; `BaseUrl`/`BearerToken` default empty; `TimeoutMs` defaults to the existing option
   default). No value changes behavior unless an operator supplies it — the compose file passes
   configuration through; it does not set production live-mode itself.
2. Document the new `KEON_RUNTIME_BASE_URL`, `KEON_RUNTIME_BEARER_TOKEN`, and
   `KEON_RUNTIME_TIMEOUT_MS` keys in `.env.example` as blank/placeholder values with a comment
   explaining the fail-closed boot check they satisfy.
3. Produce a named config review record,
   `docs/INITIATIVES/biostack-production-readiness/evidence/PR-PROV-001-deployment-config-review.md`,
   that: (a) confirms the full required-variable matrix across `docker-compose.yml` vs
   `.env.example` for every `*__*`-style settings-section variable the application reads at
   startup (not Keon-only — a complete pass), (b) states which variables are present, which are
   newly added by this parcel, and which remain intentionally operator-supplied secrets, and
   (c) states explicitly that no production deployment, secret value, or live environment
   variable was touched to produce the review.
4. Confirm (by local/test-mode run only) that the existing fail-closed startup check in
   `KeonRuntimeDependencyInjection.cs` is unchanged and still throws under a simulated
   `Production` environment with the new variables absent/default, and starts in stub mode under
   `Development`/`Testing`.

This review does not enable live Keon Runtime, does not supply a live `KeonRuntime__BaseUrl` or
bearer token, and does not deploy. Enabling live mode in a real environment is a GATED item (see
Environment Posture).

## Hardening Scope (provider operations)

1. **Rate limiting.** Verify the existing `provider-access` fixed-window policy
   (`PermitLimit = 5`, `Window = 1 hour`, IP-partitioned, `QueueLimit = 0`) against the documented
   threat model in `SEC-PROVIDER-001.md` and `SECURITY-GATES.md` SG4. Add a structured,
   non-PII-bearing rejection signal (log event or counter) when the limiter rejects a request, so
   abuse volume is observable without adding new stored PII. No change to the limiter's numeric
   thresholds is required unless evidence from the Required Tests shows the current window is
   ineffective against the specific submission/update endpoints; if changed, the new thresholds
   must be stated in Acceptance Criteria at merge, not left open.
2. **Intake data-minimization and retention.** Add a deterministic, configurable retention rule:
   `ProviderAccessRequest` rows in `closed` status older than a configured retention window
   (default `365` days, via `ProviderAccess__RetentionDays` configuration) become eligible for
   anonymization (email/name/organization/role replaced with a fixed redacted marker; `Status`,
   `Owner`, timestamps, and `ConsentVersion` retained for audit). Implement this as a pure,
   testable domain method plus a manually-invoked admin endpoint
   (`POST /api/v1/admin/provider-access/requests/retention-sweep`) that executes it — not a live
   background job or cron schedule. Scheduling the sweep in a real environment is a GATED item.
3. **SLA/owner accountability.** Expose read-only, deterministically computed SLA staleness on
   the existing admin list response: `DaysOpen` (derived from `CreatedAtUtc` and current time) and
   `IsOverdue` (true when `Status == "pending"` and `DaysOpen` exceeds a configured
   `ProviderAccess__SlaDays`, default `5`). No new persisted field; no behavior change to
   `UpdateRequest`'s existing `Owner` assignment.
4. **Audit trail.** Record an append-only audit entry (new `ProviderAccessAuditEntry` table:
   `Id`, `RequestId`, `ActorId` from the authenticated admin principal, `FromStatus`, `ToStatus`,
   `FromOwner`, `ToOwner`, `OccurredAtUtc`) on every `PATCH` to the admin queue. No PII beyond what
   `ProviderAccessRequest` already stores; no new public surface.

## Hard Constraints

- Stripe, SMTP, and Azure Communication Email live-touching behavior is **out of scope**. Only
  `TEST-MODE`/local configuration is exercised, per D-F(2) ("Stripe remains TEST-MODE until the
  KEO-68 runbook validates"). This parcel makes no Stripe, SMTP, or Azure code or config change.
- **No production deployment.** All verification is local/test-mode (in-process test host,
  SQLite/in-memory or local Postgres fixture, simulated environment variables).
- **No change to the initiative's verdict.** `RELEASE-GATES.md`'s NO-GO/HOLD stands untouched;
  this spec and its eventual build record cannot mark any `RG*` or `SG*` row passing.
- **Claims bounded to evidence.** Any acceptance or evidence statement names the exact
  environment, commit, and test run that produced it, per the initiative's honest-language
  discipline (`PR-DOC-001.md` Contract: "Status is valid only for a named commit, environment,
  configuration, procedure, time and artifact. Unknown or missing evidence cannot become
  passing.").
- **No weakening of `SEC-PROVIDER-001`'s boundary.** Non-enumeration, opaque public
  acknowledgement, and admin-only mutation remain exactly as merged.

## Environment Posture

Local/test-mode verification (xUnit integration tests against the existing in-memory/SQLite test
host, `dotnet test`, and manual `docker compose config` rendering) is the proof medium for every
acceptance criterion in this spec. No claim in this parcel's evidence may assert deployed,
hosted, or live-traffic behavior.

The following items require live-environment evidence and are **GATED** — not assumed, not
claimed by this parcel, and named to the release owner:

- **GATED-1:** Enabling `KeonRuntime__LiveMode=true` against a real Keon Runtime base URL in a
  deployed environment. Owner: **Clint Morgan** (named release owner, D-F(2)).
  Evidence required before claim: deployed revision, `KeonRuntime__BaseUrl` reachability, and a
  successful `/health/keon` live check, captured per `KEO-69`/`RELEASE-GATES.md` evidence
  discipline.
- **GATED-2:** Scheduling the retention-sweep endpoint as a recurring job in a real environment
  (frequency, alerting, failure handling). Owner: **Clint Morgan**.
- **GATED-3:** Validating the `provider-access` rate-limit thresholds against real traffic/abuse
  patterns. Owner: **Clint Morgan**.
- **GATED-4:** Treating the SLA staleness computation as an operational commitment (actual
  response-time SLA, escalation path, named on-call). Owner: **Clint Morgan**, consistent with
  `RISKS.md` `R6` ("Provider leads lack notification/SLA/retention ownership ... operational
  contract").

## Class Controls — `provider-pilot`

This parcel touches the provider-access intake and admin-operations surface, so the
governed-delivery `provider-pilot` delivery-class controls
(`biostack-governed-delivery/CHARTER.md`) are applied as additional due-diligence sections, even
though this parcel is dispatched under `biostack-production-readiness`, not under the
governed-delivery P9 capstone. Applying these controls does not place this parcel inside
governed-delivery's standing authorization or its P1–P7 scope; it borrows the class's required
sections because the surface and risk match.

- **Pilot population.** Organizations/individuals who submit the public
  `provider-access/requests` form, expressing interest in a future provider pilot. No clinical
  workflow, patient data, or prescribing surface exists yet; this parcel does not create one.
- **Role/consent.** `Role` (free-text, 2–120 chars) is self-declared at submission; `Consent` is
  required to submit and is recorded as `ConsentVersion = "provider-access-v1"` plus
  `ConsentRecordedAtUtc`. This parcel does not change consent text or version; any text change is
  out of scope and would require the `legal-policy` controls this parcel does not carry.
  Role is operator-facing triage metadata only — never used to grant elevated access or bypass
  `AdminOnly` authorization.
  - **Dual review, see the governed overlay's required-reviewer count: this parcel requires two
    independent reviews before merge** because it is explicitly carrying `provider-pilot`
    controls; a single reviewer is not sufficient even though `PR-DOC-001`-shaped parcels
    otherwise default to one.
- **Permitted workflow.** Intake capture, admin triage (status transitions among `pending`,
  `contacted`, `qualified`, `pilot`, `closed`), owner assignment, SLA-staleness visibility, and
  time-bounded retention/anonymization. Nothing beyond this.
- **Retention.** The default-365-day `closed`-status anonymization rule above, configurable via
  `ProviderAccess__RetentionDays`, executed only by an explicit admin-invoked sweep in this
  parcel's scope (no autonomous schedule).
- **SLA/owner.** `Owner` (existing field) plus the new read-only `DaysOpen`/`IsOverdue`
  computation. The *operational* SLA commitment itself (response-time promise, escalation,
  on-call) is GATED-4, named to Clint Morgan — this parcel delivers only the deterministic
  visibility mechanism, not the operational promise.
- **Prohibited clinical behavior.** This parcel adds no diagnosis, prescribing, patient-record,
  or clinical-decision surface. The provider-access queue remains a sales/pilot-intake workflow
  only. Any future clinical-workflow expansion is explicitly out of scope and would require a
  new, separately reviewed parcel (governed-delivery `provider-pilot` class stop condition:
  "clinical-workflow expansion").
- **Merge gate.** Per the `provider-pilot` row's authorization-eligibility rule, merge of the
  eventual implementation requires an **explicit human owner acknowledgment** in addition to dual
  review. That owner is **Clint Morgan** (named release owner, D-F(2)); acknowledgment is
  recorded in the parcel's evidence before merge, not inferred.

## Allowed Files
- `docs/INITIATIVES/biostack-production-readiness/PARCELS.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/PR-PROV-001.md`
- `docs/INITIATIVES/biostack-production-readiness/evidence/PR-PROV-001-deployment-config-review.md`
- `backend/src/BioStack.Api/Program.cs` (rate-limiter policy section only)
- `backend/src/BioStack.Api/Endpoints/ProviderAccessEndpoints.cs`
- `backend/src/BioStack.Contracts/Responses/ProviderAccessResponse.cs` (additive fields only:
  `DaysOpen`, `IsOverdue`)
- `backend/src/BioStack.Domain/Entities/ProviderAccessRequest.cs`
- `backend/src/BioStack.Domain/Entities/ProviderAccessAuditEntry.cs` (new file)
- `backend/src/BioStack.Infrastructure/Persistence/BioStackDbContext.cs`
- `backend/src/BioStack.Infrastructure/Persistence/Migrations/**` (one additive migration: audit
  table; no destructive change)
- `backend/tests/BioStack.Api.Tests/Integration/ProviderAccessEndpointsIntegrationTests.cs`
- `docker-compose.yml` (`biostack-api` `environment` block only, `KeonRuntime__*` pass-through)
- `.env.example` (`KEON_RUNTIME_*` documentation only)

## Forbidden
- Stripe, SMTP, Azure Communication Email, or any other billing/email code, configuration, or
  secret.
- Any production deployment, Azure resource, CI/CD workflow trigger, or live credential.
- Consent text/version changes (`legal-policy` territory, not this parcel).
- Weakening `SEC-PROVIDER-001`'s non-enumeration or admin-only boundary.
- Scheduling or triggering the retention sweep automatically (manual admin-invoked endpoint only).
- Setting `KeonRuntime__LiveMode=true` or supplying a real `KeonRuntime__BaseUrl`/bearer token
  anywhere in this parcel's artifacts.
- Changing `RELEASE-GATES.md`, `SECURITY-GATES.md`, or `RISKS.md` row status to passing/mitigated.

## Out of Scope
- PR-BILL-001 (billing), PR-DATA-001 (data/platform), PR-REL-001 (release), and
  SEC-RECEIPT-001 (receipt authorization) **remain blocked/gated exactly as recorded in
  `PARCELS.md`**. This spec does not unblock, advance, or alter any of them, their dependencies,
  or their evidence.
- Clinical-workflow features of any kind for providers.
- Live Keon Runtime enablement, live retention scheduling, live rate-limit tuning, and live SLA
  operational commitment (all GATED-1..4 above).
- The dedicated defensive security review required to close SG4 in full.

## Contract
Every acceptance claim names its exact commit, environment (local/test-mode only), and the test
run that produced it. No `RG*`/`SG*` gate status changes. No live-environment claim is made
without the named GATED owner's recorded evidence. Unknown or missing evidence cannot become
passing, per the initiative's Contract discipline.

## Required Tests
- Existing `ProviderAccessEndpointsIntegrationTests` continue to pass unmodified in behavior
  (non-enumeration, opaque acknowledgement, admin-only mutation from `SEC-PROVIDER-001`).
- Rate limit: a 6th request from the same simulated IP within the 1-hour window to
  `POST /api/v1/provider-access/requests` returns `429`; the rejection emits the new structured,
  non-PII rejection signal exactly once.
- Retention sweep: a `closed` request with `UpdatedAtUtc`/retention-eligible timestamp older than
  `ProviderAccess__RetentionDays` is anonymized (email/name/organization/role redacted; `Status`,
  `Owner`, `ConsentVersion`, timestamps retained) when the sweep endpoint is invoked by an admin
  principal; a `closed` request inside the window is untouched; a non-`closed` request is
  untouched regardless of age; the sweep endpoint is `AdminOnly`.
- SLA staleness: `DaysOpen`/`IsOverdue` compute correctly for a fixed clock against `pending`
  requests at, under, and over the configured `ProviderAccess__SlaDays` threshold; non-`pending`
  requests report `IsOverdue = false`.
- Audit trail: a `PATCH` status/owner change creates exactly one `ProviderAccessAuditEntry` row
  with the correct `ActorId`, from/to status, and from/to owner; no audit row is created on a
  no-op `PATCH` that changes nothing; the audit table is not exposed on any public endpoint.
- Deployment-config review: a deterministic check (test or script) confirms `docker-compose.yml`
  declares all four `KeonRuntime__*` pass-through keys for `biostack-api`, that they default to
  values preserving current stub-mode behavior, and that `.env.example` documents the
  corresponding `KEON_RUNTIME_*` keys as blank; confirms (via a local `ASPNETCORE_ENVIRONMENT`
  simulation, not a deployment) that the existing fail-closed `Production` boot check is
  unchanged.
- Focused backend integration test run, serial solution build (0 errors/0 warnings beyond the
  pre-existing documented baseline), and `git diff --check` all pass.

## Acceptance Criteria
- Only Allowed Files change; `git diff --name-only` matches the Allowed Files list exactly.
- `SEC-PROVIDER-001`'s non-enumeration and admin-only boundary is unchanged and its existing
  tests still pass.
- Rate-limit rejection is observable (structured signal) without adding new stored PII; numeric
  thresholds are stated explicitly if changed from the current `5/hour` policy (none are changed
  unless Required Tests prove it necessary).
- Retention anonymization is deterministic, configurable, admin-invoked only (no autonomous
  schedule shipped), and reversible-by-design only in the sense that it is additive/audit-
  preserving, not destructive of the audit trail.
- SLA staleness fields are read-only, computed, and additive to the existing admin response
  contract; no existing field changes meaning.
- Every admin status/owner mutation produces exactly one audit row; the audit surface is never
  public.
- The deployment-config review record exists at the named path, covers the full `*__*` settings
  matrix (not Keon-only), and states plainly that no live value or deployment was touched.
- `docker-compose.yml`'s new `KeonRuntime__*` lines default to current (stub, `LiveMode=false`)
  behavior when the new `.env` keys are unset.
- No Stripe/SMTP/Azure file, config key, or secret is touched.
- No row in `RELEASE-GATES.md`, `SECURITY-GATES.md`, or `RISKS.md` is marked passing/closed/
  mitigated by this parcel; at most, this parcel's evidence is referenced as partial progress
  toward SG4/R6 in those documents' existing format, added by a human reconciliation step, not
  this parcel's own claim of gate closure.
- `provider-pilot` class controls section is satisfied: dual review recorded, and merge carries
  Clint Morgan's explicit human owner acknowledgment.

## Rollback
- The audit-entry migration is strictly additive (new table only); rollback is `dotnet ef
  database update <previous-migration>` against the same connection used for forward migration,
  verified in the test environment before any claim of rollback safety.
- The retention sweep is invoked manually and only acts on data the parcel itself can regenerate
  in test fixtures; no irreversible production data action occurs because no production
  environment is touched by this parcel.
- The `docker-compose.yml`/`.env.example` change is reverted by `git revert` of this parcel's
  commit; because the new variables default to current stub behavior, no running system's
  behavior changes until an operator deliberately supplies live values (GATED-1).
- If any Required Test fails after merge-track verification, the fix remains inside this parcel's
  Allowed Files; no separate emergency parcel is implied by this spec.

## Evidence Required
- Full Required Tests output (pass/fail per case), serial build result, `git diff --name-only`
  and `git diff --check`, and the deployment-config review record content.
- Explicit statement of environment (local/test-mode), commit SHA, and date for every claim.
- Dual review records and Clint Morgan's merge acknowledgment (`provider-pilot` class).
- No secrets, tokens, real email addresses/PII, or Keon bearer tokens in any evidence artifact.

## Collision Risk
Medium: shared `Program.cs` rate-limiter registration block and `BioStackDbContext` are touched
by other security/hardening parcels (`SEC-*`). Serialize against any in-flight parcel touching the
same files; confirm `PARCELS.md` before dispatch.

## Stop-and-Report Rule
Stop and report to the coordinator on any of:
- A required file falls outside Allowed Files.
- Any Stripe, SMTP, Azure, or live-credential touch is requested or appears necessary.
- A finding that the `provider-access` rate-limit, retention, or SLA mechanism cannot be made
  deterministic/testable in local/test-mode (would require live traffic/environment evidence this
  parcel cannot produce).
- Any request to mark an `RG*`/`SG*` gate or `RISKS.md` row as passing/closed/mitigated.
- Any request to expand provider-access into a clinical workflow.
- A second reviewer disagrees with the first on a `provider-pilot`-class finding and the
  disagreement cannot be reproduced and resolved without the release owner.
- Any request to enable `KeonRuntime__LiveMode`, schedule the retention sweep, or treat the SLA
  computation as an operational commitment without Clint Morgan's recorded acknowledgment
  (GATED-1..4).
