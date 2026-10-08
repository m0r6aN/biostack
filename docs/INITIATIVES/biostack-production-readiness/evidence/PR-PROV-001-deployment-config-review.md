# PR-PROV-001 — Deployment Configuration Review

**Parcel:** PR-PROV-001 (provider operations hardening + deployment-config review)
**Owner authorization:** D-F(2) (`COORDINATOR-DECISIONS-2026-10-07.md`)
**Branch:** `feat/pr-prov-001-provider-ops`
**Reviewer/author:** `pr_prov_001_builder`
**Review date:** 2026-10-08 (UTC)
**Environment:** local/test-mode only — no production deployment, no live credential, no Azure
resource, no Stripe live call. This document is produced entirely from static file review,
`dotnet build`/`dotnet test` output, `dotnet ef migrations script` output (schema-only, no live
database), and `docker compose config` rendering against a throwaway, non-secret `.env` file
created solely for this review and deleted immediately after use.

**This review does not enable live Keon Runtime, does not supply a live `KeonRuntime__BaseUrl` or
bearer token, and does not deploy.** Enabling live mode in a real environment is **GATED-1**,
named to the release owner (Clint Morgan) in `parcels/PR-PROV-001.md`.

---

## 1. Continuation context

`docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-001-config-note.md` ("Production
Compose Gap — Known Limitation") recorded that `docker-compose.yml` passed no `KeonRuntime__*`
variable to `biostack-api`, and `.env.example` documented none of the corresponding
`KEON_RUNTIME_*` host variables. That parcel's scope (governed-delivery, read-only against
product code) deliberately did not fix the gap. This review closes that specific gap and extends
the same pass to every other `*__*`-style settings-section the application reads at startup, per
this parcel's spec (`parcels/PR-PROV-001.md`, "Deployment-Config Review" section).

## 2. What changed in this parcel

`docker-compose.yml` (`biostack-api` `environment:` block) now declares:

```yaml
- KeonRuntime__LiveMode=false
- KeonRuntime__BaseUrl=${KEON_RUNTIME_BASE_URL:-}
- KeonRuntime__BearerToken=${KEON_RUNTIME_BEARER_TOKEN:-}
- KeonRuntime__TimeoutMs=${KEON_RUNTIME_TIMEOUT_MS:-5000}
```

`KeonRuntime__LiveMode` is a **fixed literal `false`**, not sourced from an operator-settable
`.env` key. This is a deliberate design choice beyond the spec's minimum: the spec's Hard
Constraints and Forbidden sections prohibit this parcel from setting
`KeonRuntime__LiveMode=true` or supplying a real `BaseUrl`/bearer token "anywhere in this
parcel's artifacts." Making `LiveMode` a plain `.env`-overridable variable (even defaulted to
`false`) would create exactly the path by which a future `.env` edit alone — with no corresponding
code or compose change — could flip production into live mode. Fixing it at `false` in the
compose file itself means enabling live mode (GATED-1) requires a deliberate, reviewable edit to
`docker-compose.yml`, not a `.env` value.

`.env.example` documents the three operator-facing keys as blank placeholders:

```bash
KEON_RUNTIME_BASE_URL=
KEON_RUNTIME_BEARER_TOKEN=
KEON_RUNTIME_TIMEOUT_MS=5000
```

with a comment explaining the fail-closed boot check these satisfy, and explicitly stating that
`KeonRuntime__LiveMode` is not operator-settable through this file.

**Result:** a production deployment using the now-updated `docker-compose.yml`, with these new
`.env` keys left unset (the default, unchanged-from-today state), continues to run in
`KeonRuntimeClientStub` mode exactly as before this parcel — it still fails closed at boot with
the existing, unmodified `InvalidOperationException` from `KeonRuntimeDependencyInjection.cs`
(see §5) unless an operator explicitly sets `KeonRuntime:AllowStubInProduction=true` elsewhere
(a setting this parcel does not add to `docker-compose.yml`) or deliberately edits
`docker-compose.yml`/`.env` to supply live values (GATED-1). The only behavior change from this
parcel is that an operator now *can* pass `KEON_RUNTIME_BASE_URL`/`KEON_RUNTIME_BEARER_TOKEN`/
`KEON_RUNTIME_TIMEOUT_MS` through `.env` instead of hand-editing `docker-compose.yml` to do so —
the fail-closed posture and its error message are unchanged.

## 3. Full `*__*` settings-section matrix (not Keon-only)

The table below lists every settings section the application reads via `IConfiguration` /
`IOptions<T>` at startup (enumerated from `backend/src/BioStack.Api/Program.cs`,
`backend/src/BioStack.Api/appsettings.json`, and each options class's `SectionName` constant),
whether it is present in `docker-compose.yml`, whether it is documented in `.env.example`, and
whether a missing/default value causes a Production boot failure today.

| Section | In `docker-compose.yml`? | In `.env.example`? | Fails closed in Production if unset? | Notes |
|---|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | Yes | Yes (documented, commented) | **Yes** | Required Postgres connection; SQLite rejected in Production. |
| `Database:Provider` | Yes (`postgres`) | — (implied by connection string) | N/A | Hardcoded `postgres` in compose. |
| `Jwt:Secret` | Yes (`:?` required) | Yes | **Yes** | Fails at config-read time (`?? throw`). |
| `Jwt:Issuer` / `Jwt:Audience` / `Jwt:ExpiryMinutes` | No | Yes (with safe non-secret defaults) | No | Code falls back to `"biostack"`/`"biostack-ui"`/`60` if unset; `.env.example` values match the code defaults, so omission from compose is safe. |
| `Auth:CallbackSecret` | Yes (`:?` required) | Yes | **Yes** | |
| `Auth:Passkeys:*` | No | Yes (feature documented, `Enabled=false` default) | No | Fail-closed by default (`Enabled=false`); enabling requires explicit operator configuration per `.env.example`'s own warning. Not added to `docker-compose.yml` by this parcel — out of this parcel's Allowed Files (`docker-compose.yml` is restricted to the `KeonRuntime__*` pass-through only; see §6). |
| `FrontendUrl` | Yes (`:?` required) | Yes | **Yes** | |
| `PublicApiUrl` | Yes (`:-` default) | Yes | No | |
| `Cors:AllowedOrigins:0` | Yes (`:?` required) | Yes | **Yes** | |
| `Stripe:*` (7 keys) | Yes (all `:?` required) | Yes | **Yes** | Out of this parcel's scope — already present pre-parcel; unmodified. |
| `Smtp:*` (8 keys) | Yes (`:-` defaults, blank) | Yes | Conditional | `ProductionAuthConfiguration` requires exactly one of `Smtp:Host`/`AzureCommunicationEmail:ConnectionString` to be set in Production — both blank fails closed; this is a deliberate "operator must choose one" gate, not a gap. |
| `AzureCommunicationEmail:*` (2 keys) | Yes (`:-` defaults, blank) | Yes (commented, Option B) | Conditional | See `Smtp:*` row — same either/or gate. |
| `KeonRuntime:*` (4 keys relevant to compose: `LiveMode`, `BaseUrl`, `BearerToken`, `TimeoutMs`; `StubAllowAll` is a dev-only escape hatch not intended for compose) | **Yes — added by this parcel** | **Yes — added by this parcel** (`BaseUrl`/`BearerToken`/`TimeoutMs` only; `LiveMode` intentionally fixed, not a `.env` key) | **Yes (pre-existing, unchanged)** | This parcel's primary deliverable. See §5 for the unchanged fail-closed proof. |
| `KeonCollective:*` (6 keys) | **No** | **No** | No | `AddCollectiveIntegration` selects stub vs. live purely from `KeonCollective:LiveMode`/`ControlBaseUrl` with no Production fail-closed check (unlike `KeonRuntime`). Defaults (`LiveMode=false`, blank `ControlBaseUrl`) are safe; omission does not block boot. **Not fixed by this parcel** — outside the named `KeonRuntime__*` gap this parcel is authorized to close (`docker-compose.yml` is restricted to that one pass-through; see §6), and introducing a new compose/`.env` surface for it would expand this parcel's Allowed Files. Flagged here as a candidate for a future, separately authorized config-review parcel. |
| `DataProtection:*` (4 keys: `ApplicationName`, `BlobUri`, `KeyVaultKeyIdentifier`, `ManagedIdentityClientId`) | **No** | **No** | **Yes** | **Finding, not fixed by this parcel.** `ProductionDataProtectionConfiguration.ReadAndValidate` requires `DataProtection:BlobUri` and `DataProtection:KeyVaultKeyIdentifier` to be non-blank, valid HTTPS URIs in a specific shape, when `ASPNETCORE_ENVIRONMENT=Production`. Neither is declared in `docker-compose.yml` or documented in `.env.example` today. `DataProtection:ApplicationName`'s `appsettings.json` default already equals the required stable value, so that one key is not at risk — but `BlobUri`/`KeyVaultKeyIdentifier` being absent means a production deployment from the current `docker-compose.yml`, exactly like the original Keon gap, fails closed at boot with no operator guidance in the compose file itself. This is **structurally the same class of gap this parcel's `KeonRuntime__*` fix addresses**, but `DataProtection:*` is Azure Blob/Key Vault configuration, not `KeonRuntime__*`, and is outside this parcel's Allowed Files (`docker-compose.yml`'s environment block is scoped to the `KeonRuntime__*` pass-through only per `parcels/PR-PROV-001.md`). **Recommended follow-on:** a dedicated config-review parcel (or coordinator-authorized extension) to add `DataProtection__BlobUri`, `DataProtection__KeyVaultKeyIdentifier`, and `DataProtection__ManagedIdentityClientId` pass-through to `docker-compose.yml` and `.env.example`, mirroring this parcel's `KeonRuntime__*` pattern. |
| `ScientificResearchSidecar:*` (4 keys) | No | No | No | `Enabled=false` default; fail-closed-safe (disabled stub). No Production-only validator exists for this section. |
| `TranscriptProviders:YouTube:Enabled` | No | No | No | `Enabled=false` default; safe. |
| `Redis:Configuration` / `Redis:InstanceName` | No | Commented example only | No | Explicitly optional; falls back to in-memory cache per code comment and `.env.example`'s own heading. |
| `Analyzer:Ocr:*` | No | No | No | No Production-only validator found; `ProtocolOcrOptions` binds with its own class defaults. |
| `Kompress:StorePath` / `Kompress:TenantId` | No | No | No | Inline `Configuration[...]` reads with code-level fallbacks (`"./data/kompress"` equivalent / `"biostack"`); no Production-only throw. |
| `SpineCheckpoint:*` (6 keys) | No | No | No | All fields have safe class-level defaults (`AutoCheckpointEveryNEntries=25`, `CadenceMinutes=60`, truncation watermark **on** by default); `SigningKey` empty is a disclosed reduced-posture default (`unsigned-local` checkpoints), not a boot failure. |
| `Worker:*` (3 keys) | N/A (separate one-shot worker process, not `biostack-api`) | Yes | N/A | Belongs to `BioStack.KnowledgeWorker`, not the API service this review's scope covers. |
| `ProviderAccess:RetentionDays` / `ProviderAccess:SlaDays` | No | No | No | New in this parcel (see hardening scope below). Both have safe, documented-in-code defaults (`365`/`5`) read directly via `IConfiguration` in `ProviderAccessEndpoints.cs`; no `appsettings.json` entry or compose/`.env.example` entry was added, consistent with this parcel's Allowed Files (no `appsettings.json` edit authorized) and because neither is required for safe default operation. |

## 4. What is present, newly added, or intentionally operator-supplied

- **Present before this parcel, unchanged:** `ConnectionStrings:DefaultConnection`, `Jwt:*`,
  `Auth:CallbackSecret`, `Auth:Passkeys:*`, `FrontendUrl`, `PublicApiUrl`, `Cors:AllowedOrigins:0`,
  `Stripe:*`, `Smtp:*`, `AzureCommunicationEmail:*`.
- **Newly added by this parcel:** `KeonRuntime__LiveMode` (fixed `false` in compose, not a
  `.env` key), `KeonRuntime__BaseUrl`, `KeonRuntime__BearerToken`, `KeonRuntime__TimeoutMs` in
  `docker-compose.yml`; `KEON_RUNTIME_BASE_URL`, `KEON_RUNTIME_BEARER_TOKEN`,
  `KEON_RUNTIME_TIMEOUT_MS` in `.env.example`.
- **Intentionally operator-supplied secrets (blank in `.env.example`, never committed with real
  values):** `Jwt__Secret`, `Auth__CallbackSecret`, `AUTH_SECRET`, `AUTH_CALLBACK_SECRET`,
  `Stripe__SecretKey`, `Stripe__WebhookSecret`, `Stripe__OperatorPriceId`,
  `Stripe__CommanderPriceId`, `Smtp__Username`/`Smtp__Password` (when SMTP is chosen),
  `AzureCommunicationEmail__ConnectionString` (when Azure email is chosen), and now
  `KEON_RUNTIME_BASE_URL`/`KEON_RUNTIME_BEARER_TOKEN` (when an operator deliberately enables live
  Keon Runtime — GATED-1).
- **Identified but not added by this parcel (see §3 findings):** `DataProtection:BlobUri` /
  `DataProtection:KeyVaultKeyIdentifier` / `DataProtection:ManagedIdentityClientId`;
  `KeonCollective:*`.

## 5. Fail-closed boot check confirmation (local/test-mode only)

**Claim scope:** this confirmation was produced on branch `feat/pr-prov-001-provider-ops`
(based on `517ebc10315f712f14b12aaff296df2b334fa56d`; see the PR's head commit for the exact SHA
this evidence ships with, since this file is itself part of that commit) on 2026-10-08 (UTC),
using `dotnet test` against the in-process xUnit test host. No deployed, hosted, or live-traffic
environment was used.

`backend/src/BioStack.Infrastructure/Keon/KeonRuntimeDependencyInjection.cs` is **unchanged** by
this parcel (it is not in the Allowed Files list). Two new tests in
`backend/tests/BioStack.Api.Tests/Integration/ProviderAccessEndpointsIntegrationTests.cs` call the
exact same public method Program.cs calls (`AddKeonRuntime(configuration, isProduction)`)
directly against a minimal in-memory configuration:

- `DeploymentConfigReview_ProductionBootStillFailsClosedWithoutLiveKeonConfiguration`: calling
  `AddKeonRuntime` with `isProduction: true` and no Keon configuration throws
  `InvalidOperationException` containing `"KeonRuntime is not in live mode"` — the existing,
  unmodified fail-closed message. **PASS.**
- `DeploymentConfigReview_NonProductionBootStartsInStubModeWithoutLiveKeonConfiguration`: calling
  `AddKeonRuntime` with `isProduction: false` and no Keon configuration does not throw and
  resolves `IKeonRuntimeClient` as `KeonRuntimeClientStub`. **PASS.**

A third new test, `DeploymentConfigReview_ComposeDeclaresKeonRuntimePassThroughWithStubPreservingDefaults`,
statically asserts `docker-compose.yml` contains the four `KeonRuntime__*` lines with their
stub-preserving default forms and that `.env.example` documents the three new `KEON_RUNTIME_*`
keys as blank/`5000`. **PASS.**

All three are part of the Required Tests run recorded in the parcel's PR description (full
`dotnet test` output, 466/466 passing on the focused `BioStack.Api.Tests` project).

### `docker compose config` rendering (manual verification, not committed)

`docker compose config` against the full `docker-compose.yml` cannot currently complete end-to-end
because of a **pre-existing, unrelated** parse error in the `biostack-ui` service
(`services.biostack-ui.environment.[2]: unexpected type map[string]interface {}`), reproduced
identically against the unmodified file on `main` at this review's base commit — **not introduced
by this parcel**. To verify the `biostack-api` service's `KeonRuntime__*` interpolation in
isolation, the `biostack-api` service block was copied into a throwaway scratch compose file
(`/tmp`, never committed) alongside a throwaway, non-secret `.env` file, and rendered with
`docker compose config`:

- With the new `KEON_RUNTIME_*` keys **unset**: rendered
  `KeonRuntime__LiveMode: "false"`, `KeonRuntime__BaseUrl: ""`, `KeonRuntime__BearerToken: ""`,
  `KeonRuntime__TimeoutMs: "5000"` — exactly the current stub-preserving behavior.
- With `KEON_RUNTIME_BASE_URL`/`KEON_RUNTIME_BEARER_TOKEN`/`KEON_RUNTIME_TIMEOUT_MS` set to
  fake, non-production placeholder values: those three fields rendered the supplied values while
  `KeonRuntime__LiveMode` remained `"false"` (confirming it is not operator-overridable through
  this file, by design — see §2).

No real Keon Runtime base URL, bearer token, or other live credential was used in this
verification; the scratch files were deleted immediately after the check.

## 6. Scope boundary

This review confirms and closes the `KeonRuntime__*` pass-through gap and performs a complete
pass over every other `*__*` settings section the API reads at startup, per the spec. It does
**not**:

- Enable `KeonRuntime__LiveMode=true` anywhere (GATED-1, owner: Clint Morgan).
- Supply a real `KeonRuntime__BaseUrl` or bearer token anywhere.
- Touch any Stripe, SMTP, or Azure Communication Email file, configuration key, or secret.
- Fix the `DataProtection:*` or `KeonCollective:*` gaps identified in §3 — those are named as
  findings for a future, separately authorized parcel, not remediated here, because
  `docker-compose.yml`'s allowed edit scope for this parcel is the `KeonRuntime__*` pass-through
  only.
- Deploy to any environment, touch any Azure resource, or mark any `RELEASE-GATES.md`,
  `SECURITY-GATES.md`, or `RISKS.md` row as passing/closed/mitigated.

**No production deployment, secret value, or live environment variable was touched to produce
this review.**
