# Parcel Spec: P07 Frontend Authenticated-Consented Relay

Status: **active — implementation functionally green; clean-base lint evidence accepted and verification contract re-linted; awaiting fresh builder restatement**

Coordinator lint: **PASSED 2026-09-02.** AF-P07, pinned route/test/contract/diagnostic blobs, Q04 transcript custody, trusted-origin and named-cookie boundary, exact consent request/response controls, eight-denial plus one-positive call matrix, baseline and candidate count deltas, offline install/cleanup rules, paired backend consent verification, and dual-adversarial plus separate security-review depth were checked against the pinned repository and ratified D9(b).

Lint-evidence re-lint: **PASSED 2026-09-02 after the first builder stopped on the aggregate lint tripwire.** A fresh clean-base evidence parcel proved that `npm run lint` already exits `1` with exactly `98` ambient findings (`48` errors and `50` warnings) across `49` paths outside AF-P07, while direct local ESLint on the two base AF-P07 files exits `0` with zero findings. Candidate verification therefore retains the full lint command as an exact ambient-delta receipt and adds focused local ESLint over all three candidate AF-P07 files. This changes no D9(b) decision, invariant, Allowed File, security gate, test count, or Gate authority.

## Identity

- Goal: `biostack-algorithm-remediation`
- Initiative: BioStack algorithm remediation
- Project track: `BioStack` / frontend research-suggestion BFF, paired with the read-only backend consent contract
- Parcel: `P07-frontend-authenticated-consented-relay`
- Wave: Wave 2 outbound boundary
- Routing: fresh frontier implementation
- Risk: critical authentication, consent, session-cookie, and outbound-data boundary
- Branch: `codex/biostack-remediation-p07`
- Isolated worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-p07\BioStack`
- Required starting commit: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Required starting tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`

## Goal

Retain R-OUT-02's diagnostic scenario as a regression and add the smallest fail-closed check to the frontend research-suggestion BFF: before any provider request, obtain the caller's current authenticated consent from BioStack's server-controlled backend origin by forwarding only the named `biostack_session` cookie to `GET /api/v1/consent`. Every missing, unavailable, redirected, invalid, oversized, or unaccepted consent result denies provider relay; exactly one canonical authenticated-and-currently-consented local-fake case may reach the provider.

This parcel does not grant Gate 3. It creates a local candidate only.

## Authority and Dependencies

- Controlling authority: the ratified goal charter, `plan-review-findings.md`, `loop-directive.md`, and ratified Narrow Gate 1 Amendment 01.
- Active decision: D9(b), exactly as ratified on 2026-09-02.
- Prerequisite Q04 is accepted `READY`: `npm ci --offline` exited `0`; the exact direct-node Vitest command ran one file and passed `2/2`; cleanup/Git equality passed; fresh review accepted the evidence.
- Q04 transcript: `C:\Users\clint\.codex\evidence\biostack-algorithm-remediation\q04-final-attempt-2026-09-02.transcript.txt`, `3529` bytes, SHA-256 `F185ADCD538B813814C73ADBA67FC43B9F0D0A9FB2A7ACB35BF7DFFC53383633`.
- Diagnostic source: exact outbound reproduction commit `c3e30a93e64be5a2662bb9040a52105d3dc909c9`; import only `frontend/src/__tests__/app/api/research/suggest.outbound-boundary.reproduction.test.ts`, blob `fcd44293b2869eff593c5cb9c8052daf3b937987`. Do not cherry-pick or merge that commit.
- The diagnostic coordination and parcel branches remain preserved, intentionally failing, and unmerged.
- No implementation parcel is a parent or dependency. P07 branches directly from the pinned base.

## Integration Surface

Surface: frontend BFF `POST /api/research/suggest` -> authenticated backend `GET /api/v1/consent` -> provider only after current consent.

- Producer/authority: backend `ConsentEndpoints.GetConsent` and existing scoped `IConsentGate`.
- Consumer: frontend `frontend/src/app/api/research/suggest/route.ts`.
- Authentication carrier: only inbound cookie named `biostack_session`.
- Backend request: exact method `GET`, exact path `/api/v1/consent`, no body, `cache: 'no-store'`, `redirect: 'manual'`, bounded abort signal.
- Positive response contract: JSON object with exactly the seven camel-case fields emitted by `ConsentStatusResponse`: `accepted` boolean, nullable-string `consentAcceptedAtUtc`, nullable-string `consentVersion`, `declined` boolean, nullable-string `consentDeclinedAtUtc`, nullable-string `consentDeclinedVersion`, and nonempty-string `currentVersion`. Provider relay requires `accepted === true`; all contract drift fails closed.
- Data classification: the BioStack session cookie is credential material. It may go only to the configured/local BioStack consent endpoint and must never appear in the provider request, response, logs, or evidence.
- Environment: local isolated worktree only, using synthetic fixtures and non-delegating Vitest fetch fakes. No staging, production, provider, registry, DNS, socket, or cloud access.

The backend contract remains read-only. Aggregate verification runs the real `ConsentGateIntegrationTests` but P07 may not edit backend code or tests.

## Security Gates

- **SG-OUTBOUND:** no provider request before a valid session and an exact, timely, current `accepted === true` server response; denial cases observe zero provider calls; the sole positive case observes exactly one provider call. The session cookie is absent from that provider call.
- **SG-SCOPE:** only AF-P07 changes. Fixtures, API key, cookie, origins, consent response, and provider response are synthetic. Fetch fakes are non-delegating and throw on every unrecognized URL. No external access, secret, protected/customer data, payload dump, package/config mutation, or production operation.
- P07 requires two fresh independent adversarial reviews and a separate fresh defensive SG-OUTBOUND/SG-SCOPE security review of the exact candidate commit. Reviewers are read-only.

Security-gate failure blocks Gate 3. No waiver is implied.

## Exact Allowed Files — AF-P07

Only these three paths may change:

- `frontend/src/app/api/research/suggest/route.ts`
- `frontend/src/__tests__/app/api/research/suggest.route.test.ts`
- `frontend/src/__tests__/app/api/research/suggest.outbound-boundary.reproduction.test.ts`

The first two exist on the pinned base. The reproduction path is absent on the pinned base and is copied by content from exact diagnostic commit `c3e30a93e64be5a2662bb9040a52105d3dc909c9`.

If any requirement needs another file, stop for a charter/spec amendment. Do not edit a package, lockfile, config, middleware, backend contract, endpoint, test project, documentation, environment file, or nearby route.

## Forbidden and Out of Scope

- Every path outside AF-P07, including `frontend/package.json`, `frontend/package-lock.json`, `frontend/vitest.config.ts`, `frontend/src/middleware.ts`, auth/session routes, backend source/tests, deployment/IaC, workflows, and goal documents.
- No new dependency, package install mode other than the exact offline install, lockfile change, route registration, proxy rewrite, middleware change, schema change, or config enablement.
- No use of the inbound request URL, host, origin, forwarding headers, query, body, `Authorization` header, or caller-supplied authentication/consent boolean to select the backend origin or authorize provider relay.
- No forwarding of the complete inbound `Cookie` header. Decoy cookies, duplicate/malformed session cookies, and any cookie other than the single nonblank `biostack_session` value are not authority.
- No forwarding of `biostack_session` or any other caller credential to OpenAI or another provider.
- No weakening of the existing feature flag, API-key requirement, request validation, prompt/schema, suggestion parsing/normalization, provider response handling, model selection, or hard-blocker safeguards except the minimum test adaptation needed to supply an affirmative local consent response.
- No personalized medical advice, live biomedical request, provider benchmarking, model routing, endpoint-wide gate claim, consent-version/copy change, session issuance change, or backend contract edit.
- No real network, provider, registry, cloud, production database, protected/customer data, credentials, secrets, or payload dumps.
- No push, PR, merge, deployment, provider enablement, publication, release, or diagnostic-branch mutation. Gate 3 remains ungranted.

## Pinned-Base Facts and Existing Patterns

Inspected at base `339f259b1a467034db4f57cf9d774c292f11b53a`:

- `route.ts`, blob `ec34ec2fccd3a737edf84efb38392a138bbd2d5d`, checks production feature enablement, API-key presence, and body shape, then directly calls `https://api.openai.com/v1/responses`; it does not inspect a session cookie or query current backend consent.
- `suggest.route.test.ts`, blob `b1619779e26cdf34d7d47d3e51858c54b43a31fc`, has exactly two tests. Its provider case uses a Vitest global fetch stub and synthetic key; its missing-key case proves the existing `503` behavior.
- The retained diagnostic method is exactly `does not relay a provider request without an authenticated or consented caller`. It supplies no cookie or authorization and currently observes the configured provider fake being called.
- `frontend/src/middleware.ts`, blob `d931087f6f454afec75a389ec8928e868fda7cdb`, demonstrates the repository origin order `NEXT_PUBLIC_API_URL || API_URL || 'http://localhost:5050'`, trailing-slash removal, exact named-session-cookie forwarding, and `cache: 'no-store'`. Follow that server-side origin pattern without editing middleware.
- Backend `ConsentEndpoints.cs`, blob `95b7ce062aaa00a655b9a1420313a5803e9040a2`, exposes authenticated `GET /api/v1/consent`; `ConsentStatusResponse.cs`, blob `d0e5bbf28f1ba2b98e3bb88947db16672d43fc76`, defines the seven-field response contract.
- `ConsentGateIntegrationTests.cs`, blob `338a415bc5ce52c8aecef8bf75cac50b2f881dd5`, has seventeen passing cases on the pinned base and proves anonymous denial plus current server-consent behavior.
- `frontend/package.json`, blob `c961913b9c7fd598f4a6c5a6be811edbe35bfe07`, maps `test` to `vitest run`; lock blob `6772a07e5d8cfb575788f78afda23ec388bc5720` resolves Vitest `4.1.10`.
- Fresh shaping receipts on the pinned base: the research API directory passed `6` files / `18` tests, and the deterministic full frontend suite passed `135` files / `981` tests with zero failures/skips under two workers. Q04 separately proved the unchanged suggestion-route file at `1` file / `2` tests using an offline dependency install.
- Fresh clean-base lint receipt: full `npm run lint` exits `1` with exactly `98` ambient findings (`48` errors, `50` warnings) across `49` paths outside AF-P07; direct local ESLint on `route.ts` and `suggest.route.test.ts` exits `0` with zero findings. Transcript `C:\Users\clint\.codex\evidence\biostack-algorithm-remediation\p07-clean-base-lint-20260902-152511-1425010.transcript.txt` is `97,999` bytes with SHA-256 `EDBCCF6EDA244649D6EB99B08E589F8E4887E0118B14BBDE818BA19A2380E181`; raw lint output `C:\Users\clint\.codex\evidence\biostack-algorithm-remediation\p07-clean-base-lint-20260902-152511-1425010.full-lint.raw.txt` is `67,767` bytes with SHA-256 `21F740C507C0E255CBBCAE38726828C668CC5D638E32F35DAA915ADB2D2D3EA3`. The evidence worktree returned exactly to the pinned base/tree with all generated roots absent.
- At shaping time, `main` and `origin/main` both resolve to the pinned base/tree, and neither the P07 branch nor worktree exists.

`rtk` was not resolvable during shaping. Commands below use the repository's raw debugging fallback. If `rtk` is available during dispatch, the builder may use semantically identical wrapped commands but must preserve every argument and record which form ran.

## Required D9(b) Invariant

Before constructing or issuing the provider fetch:

1. Resolve the backend origin only from `NEXT_PUBLIC_API_URL`, then `API_URL`, then fixed local default `http://localhost:5050`; trim trailing slashes. Never consult the inbound request URL or headers.
2. Parse the inbound `Cookie` header for exactly one nonblank, case-sensitive cookie named `biostack_session`. Missing, blank, malformed, or duplicate named cookies deny without any fetch.
3. Fetch exactly `<trusted-origin>/api/v1/consent` with method `GET`, no body, only `cookie: biostack_session=<value>`, `cache: 'no-store'`, `redirect: 'manual'`, and an abort signal with an exact `2,000 ms` timeout.
4. Reject a fetch exception, abort/timeout, `response.redirected === true`, every `3xx`, every non-`2xx`, non-JSON content, invalid JSON, a declared or streamed body larger than `16,384` bytes, any missing/extra/wrong-typed response field, or any response whose `accepted` value is not the boolean `true`.
5. Read at most `16,384` response bytes before JSON parsing. Do not call an unbounded `text()`, `json()`, or `arrayBuffer()` on the consent response. Cancel/release the reader on overflow when supported.
6. Only a canonical response with `accepted === true` may continue through the existing provider logic. Preserve existing feature/API-key/body validation and provider mapping/normalization behavior.
7. Construct the provider request with only its existing provider Authorization and content headers. Never attach the session cookie, inbound cookies, inbound Authorization, consent response, or caller authentication/consent claims.
8. All authorization inputs are server state plus the named cookie. `RESEARCH_AI_SUGGEST_ENABLED`, `OPENAI_API_KEY`, model settings, body fields, headers, or query values cannot substitute for the backend consent result.

The timeout and size constants are internal route safeguards, not new environment options. If the exact consent contract cannot be enforced inside `route.ts`, stop; do not add an abstraction or dependency outside AF-P07.

## Smallest Production Fix

Inside `route.ts` only:

- add small internal helpers/constants for trusted-origin resolution, unique named-cookie extraction, bounded consent-body reading, strict response validation, and fail-closed current-consent evaluation;
- preserve the current feature-flag, API-key, JSON-parse, and request-shape checks in their current order; invoke the consent evaluator immediately after a valid `SuggestionRequest` and before model/provider request materialization, then return a generic non-success denial response when it is not affirmative;
- preserve the existing provider URL, prompt/schema, compacted payload, response extraction, normalization, and error handling; and
- avoid logging the cookie, consent body, request body, API key, or provider payload.

The exact denial status/message is not a new public product contract in P07. Tests require a non-`2xx` response and zero provider calls, and must not distinguish internal gate-failure reasons in a way that leaks credential or consent state.

## Regression Custody and Exact Test Matrix

Copy the diagnostic reproduction file content from exact commit `c3e30a93e64be5a2662bb9040a52105d3dc909c9`. Preserve its class/describe title, synthetic body, and exact retained method name. Adapt only environment setup and the fetch fake needed to test the ratified boundary; do not weaken its zero-provider assertion.

The final reproduction file contains exactly eight independently named denial cases, all synthetic and locally faked:

1. **Retained missing-session regression:** preserve the original no-cookie request first, then exercise blank, malformed, and duplicate named-cookie variants in the same test; every request observes zero consent calls and zero provider calls and returns non-`2xx`.
2. **Consent network error:** named session exists; consent fetch rejects; zero provider calls; non-`2xx` response.
3. **Consent timeout:** named session exists; fake remains pending until the exact `2,000 ms` abort under fake timers; zero provider calls; no wall-clock wait.
4. **Consent redirect:** local fake returns `3xx`/redirect evidence; request init is `redirect: 'manual'`; zero provider calls.
5. **Consent non-2xx:** local fake returns a non-redirect error status; zero provider calls.
6. **Malformed consent response:** local fake returns JSON that violates the exact seven-field contract; zero provider calls.
7. **Oversized consent response:** local fake streams more than `16,384` bytes (and may also declare the oversized length); zero provider calls and bounded-read rejection.
8. **Not currently accepted:** canonical seven-field response has `accepted: false`; body/header/config spoofed authentication/consent booleans and a configured API key/feature flag do not authorize; zero provider calls.

Use one non-delegating fetch fake per test that throws on any unrecognized URL. In every denial test, record provider count separately from consent count. No reproduction test may return an affirmative consent response or observe a provider call.

Update the existing two-case `suggest.route.test.ts` only as follows:

- preserve the missing-API-key case and its existing `503` assertion;
- make the existing provider/normalization case the **sole positive case in all of P07**: use a hostile-looking inbound URL/host, a synthetic trusted `API_URL`, decoy cookies plus exactly one `biostack_session`, and exactly one consent GET followed by exactly one provider POST. The consent fake asserts the exact trusted URL/path, method, no body, only the named cookie, `cache: 'no-store'`, `redirect: 'manual'`, and a live abort signal, then returns the canonical seven-field response with `accepted: true`. The provider fake asserts its exact existing URL/method, synthetic response mapping, and absence of `cookie`, `biostack_session`, inbound Authorization, and decoy cookies. Preserve every current suggestion-normalization assertion; and
- add no new test to this file. Its final count remains exactly two.

## Step 0 — Restate and Stop

Before importing/editing/installing/building/testing, a fresh builder sends one restatement and stops for coordinator confirmation. It must include:

1. P07 goal/risk, Amendment 01 ratification, Q04 accepted `READY`, exact branch/worktree/base/tree, and no dependency on another implementation candidate;
2. all three AF-P07 paths and the rule that every other repository path is forbidden;
3. diagnostic commit/blob, exact retained method, copy-by-content/no-cherry-pick custody, and preservation of all diagnostic branches;
4. the trusted-origin order/default, unique named cookie, exact consent endpoint/method/request policy, `2,000 ms` timeout, `16,384`-byte streamed bound, strict seven-field response, and `accepted === true` rule;
5. all eight independent denial cases in the reproduction file plus exactly one positive local-fake case in the existing route-test file; denial provider count zero and sole positive provider count one; cookie never reaches provider;
6. existing two-test adaptation, research-directory and full-suite expected counts/deltas, backend `ConsentGateIntegrationTests` `17/0/0`, the accepted full-lint `98 = 48 errors + 50 warnings` outside-AF baseline plus zero-finding three-file focused lint, build, scope, ancestry, cleanup, and no-network receipts;
7. offline-only dependency install, permitted pre-absent ignored roots, no registry fallback, and exact cleanup limits;
8. two fresh adversarial reviews plus separate SG-OUTBOUND/SG-SCOPE security review; and
9. Gate 3 ungranted and no push/PR/merge/deploy/enable/publish/release/diagnostic mutation.

Silence is not confirmation. Any discrepancy blocks edits and execution.

## Deterministic Verification

Run only in `C:\Users\clint\.codex\worktrees\biostack-remediation-p07\BioStack`. Record each exact command, exit code, file/test totals, warnings, and Git state. Set synthetic environment values only inside the test process; clear `OPENAI_API_KEY`, `OPENAI_REVIEW_MODEL`, `RESEARCH_AI_SUGGEST_ENABLED`, `API_URL`, and `NEXT_PUBLIC_API_URL` before shell-level commands.

No command may contact a registry or provider. If `frontend/node_modules` is absent, the only install command is `npm ci --offline`. A cache miss is `ENVIRONMENT_BLOCKED`; do not retry online, change registry, run `npm install`, or edit package/lock/config files. `frontend/node_modules`, `frontend/.next`, and `frontend/coverage` are the only permitted ignored generated roots, only if each is absent at initial receipt. After verification, remove only those exact pre-absent resolved directories after verifying each is a real directory (not a link) inside the exact P07 frontend root. Any cleanup uncertainty stops for coordinator recovery.

For the paired backend test, use coordinator-provisioned, per-file SHA-verified local restore metadata from an identical base/tree and `--no-restore`. If assets are unavailable, report `ENVIRONMENT_BLOCKED`; do not restore or access NuGet.

### Base and dependency receipt before edits

```powershell
git rev-parse HEAD
git show -s --format=%T HEAD
git status --porcelain=v1 --untracked-files=all
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git ls-tree -r --name-only 339f259b1a467034db4f57cf9d774c292f11b53a -- frontend/src/app/api/research/suggest/route.ts frontend/src/__tests__/app/api/research/suggest.route.test.ts frontend/src/__tests__/app/api/research/suggest.outbound-boundary.reproduction.test.ts
Push-Location frontend
npm ci --offline
node .\node_modules\vitest\vitest.mjs run src/__tests__/app/api/research/suggest.route.test.ts --pool=threads --maxWorkers=2 --no-file-parallelism
node .\node_modules\vitest\vitest.mjs run src/__tests__/app/api/research --pool=threads --maxWorkers=2 --no-file-parallelism
node .\node_modules\vitest\vitest.mjs run --pool=threads --maxWorkers=2 --no-file-parallelism
Pop-Location
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --filter 'FullyQualifiedName~BioStack.Api.Tests.Integration.ConsentGateIntegrationTests' --disable-build-servers --logger 'console;verbosity=minimal'
```

Expected before edits: exact base/tree, clean status, ancestry exit `0`; two listed base files and no reproduction path; offline install exit `0`; suggestion route `1` file / `2` tests; research API `6` files / `18` tests; full frontend `135` files / `981` tests; backend consent `17` passed, zero failed/skipped. Any changed discovery/count or base is a coordinator stop, not authority to rewrite this spec silently.

### Targeted and adjacent candidate tests

```powershell
Push-Location frontend
node .\node_modules\vitest\vitest.mjs run src/__tests__/app/api/research/suggest.route.test.ts src/__tests__/app/api/research/suggest.outbound-boundary.reproduction.test.ts --pool=threads --maxWorkers=2 --no-file-parallelism
node .\node_modules\vitest\vitest.mjs run src/__tests__/app/api/research --pool=threads --maxWorkers=2 --no-file-parallelism
Pop-Location
```

Expected: targeted `2` files / `10` passed (`2` existing + `8` reproduction), zero failed/skipped; research API `7` files / `26` passed, zero failed/skipped. The reproduction delta is exactly `0 -> 8 (+8)` and the existing route remains `2 -> 2 (+0)`.

The detailed targeted receipt must enumerate consent/provider calls per case: reproduction missing-session variants `0/0`; each other reproduction denial `1/0`; existing-route sole positive `1/1`. No fetch fake may delegate.

### Aggregate frontend and paired backend verification

```powershell
Push-Location frontend
npm run lint
node .\node_modules\eslint\bin\eslint.js src/app/api/research/suggest/route.ts src/__tests__/app/api/research/suggest.route.test.ts src/__tests__/app/api/research/suggest.outbound-boundary.reproduction.test.ts
node .\node_modules\vitest\vitest.mjs run --pool=threads --maxWorkers=2 --no-file-parallelism
$env:NEXT_TELEMETRY_DISABLED = '1'
npm run build
Pop-Location
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --filter 'FullyQualifiedName~BioStack.Api.Tests.Integration.ConsentGateIntegrationTests' --disable-build-servers --logger 'console;verbosity=minimal'
```

Expected: full lint exit `1` with exactly the accepted ambient baseline of `98` findings (`48` errors, `50` warnings) across the same `49` outside-AF paths and no AF-P07 finding; focused local ESLint exit `0` with zero findings across all three candidate AF-P07 files. Any new, changed, or AF-P07 lint finding is a failure even though the aggregate command already exits nonzero on the pinned base. Full frontend must report `136` files / `989` passed, zero failed/skipped, exact `+1` file / `+8` tests from base; production build exits `0`; real backend consent integration reports `17` passed, zero failed/skipped. The backend result proves the server contract independently; it does not replace the frontend local contract tests.

### Scope, cleanup, ancestry, and custody

After exact generated-root cleanup and one local candidate commit:

```powershell
git diff --check 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --name-only 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git show -s --format='%H%n%P%n%T' HEAD
git status --porcelain=v1 --untracked-files=all
```

Expected: changed paths are exactly AF-P07; the candidate has the pinned base as sole parent; diff check and ancestry pass; generated roots are absent; final status is empty. Confirm the diagnostic commit/branches were not merged, rebased, deleted, or modified.

## Acceptance Criteria

- Amendment 01 and Q04 prerequisite custody are intact; exact Step 0 is confirmed before work.
- Only AF-P07 differs from the pinned base, with the diagnostic file imported by content and retained method recognizable.
- All eight hostile denials independently return non-`2xx` and observe zero provider calls; exactly one canonical positive case observes one provider call.
- Trusted backend origin, exact endpoint/method, unique named cookie, no-store, manual redirect, bounded timeout, streamed size limit, strict response contract, and `accepted === true` are directly asserted.
- The session cookie and all inbound credentials are absent from the provider request; caller body/header/config claims cannot bypass consent.
- Existing provider request/response normalization behavior remains green, including the two original suggestion-route tests.
- Exact targeted/adjacent/full frontend counts, the accepted full-lint ambient delta plus zero-finding focused lint, production build, and all seventeen real backend consent integration tests satisfy their contracts.
- All fakes are local and non-delegating; no network/provider/registry/cloud/production/protected-data/secret access occurs.
- Exact Allowed Files, ancestry, diff check, generated-root cleanup, and clean final status pass.
- Two fresh adversarial reviewers and one separate defensive security reviewer accept the exact candidate, or all findings are fixed and the entire verification/review chain reruns against the new hash.
- Candidate remains local and unmerged. Gate 3 remains ungranted.

## Evidence Required

The builder handoff records:

1. exact branch/worktree/base/tree, initial status, candidate commit/tree/sole parent, ancestry, and final clean status;
2. AF-P07 changed paths and hashes, `git diff --check`, and proof no package/lock/config/backend/diagnostic branch changed;
3. diagnostic commit/blob, exact retained method, and copy-by-content/no-merge custody;
4. Q04 accepted transcript path/size/hash and the fresh offline-install receipt;
5. static order proof that current consent completes before provider request construction/fetch;
6. all eight denial case names plus the existing-route sole positive case, consent/provider call counts, response classifications, exact trusted URL/request policy, timeout/size/strict-contract receipts, and provider-header cookie absence;
7. exact baseline and candidate targeted/adjacent/full frontend, full/focused lint, build, and backend consent commands with exit codes/counts/deltas, including equality to the accepted 98-finding ambient lint set;
8. non-delegating fake identities, cleared shell environment, and explicit no-network/no-provider/no-registry/no-protected-data/no-secret receipt;
9. generated-root pre-absence, resolved cleanup targets, removal results, and post-absence;
10. residual risk, rollback, decisions needed/blockers, and next safe action; and
11. explicit authority close: no push, PR, merge, deploy, enable, publish, release, or diagnostic mutation; Gate 3 ungranted.

## Reviews

After deterministic verification, dispatch two fresh independent read-only adversarial reviews of the exact candidate:

- Reviewer A: regression custody, exact denial coverage, trusted-origin/cookie/consent contract, timeout/size implementation, strict parsing, call counts, and preservation of existing suggestion behavior.
- Reviewer B: hostile bypass attempts through inbound origin/headers/body/query, duplicate/decoy cookies, feature/API-key/model config, redirect behavior, fetch ordering, abort races, size-declaration/body mismatch, response-shape drift, and provider-header leakage.

Then dispatch a separate fresh defensive security review applying SG-OUTBOUND and SG-SCOPE. It must reject the candidate if any denial can fail open, any credential can follow a redirect or reach the provider, an unbounded consent read exists, any fake can delegate, evidence includes sensitive payloads, or any out-of-scope path/change/access occurred.

Reviewers never edit, stage, fix, or commit. Any production or test change invalidates every prior review and requires the full chain again.

## Collision Risk, Rollback, and Custody

Collision risk: **medium-high**. `route.ts` is a production outbound serialization point and both tests share global environment/fetch state. P07 has no Allowed File collision with P01-P06 or P08 on the pinned base, but any movement of `main`, route/test blobs, backend consent contract, package lock, or test counts stops dispatch for reconciliation.

Logical rollback after a future authorized merge is the single P07 candidate commit. Reverting it restores the prior route and removes the retained regression; no provider/config/deployment rollback is authorized here. Until Gate 3, the candidate, diagnostic commit, and all diagnostic branches remain local/preserved/unmerged.

## PR Notes

- What changed: fail-closed authenticated/current-consent relay guard plus retained hostile frontend regression.
- Why: R-OUT-02 and ratified D9(b).
- Risk: fail-open provider transmission, credential leakage, and availability-only denial on consent contract drift.
- Verification: exact local-fake call matrix, frontend lint/test/build, real backend consent integration, scope/ancestry/cleanup, and three fresh reviews.
- Evidence: builder handoff and exact candidate hash only.

Do not open or update a PR. These notes are a future Gate-3 handoff template, not release authority.

## Session Handoff

- Starting commit/tree:
- Ending commit/tree/sole parent:
- Branch/worktree:
- Files changed:
- Diagnostic source commit/blob and custody:
- Commands run and exit codes:
- Baseline/candidate counts and deltas:
- Consent/provider call matrix:
- Timeout/size/response-contract evidence:
- Backend integration receipt:
- No-network and cleanup receipts:
- Tests passed/failed/skipped:
- Reviews/security disposition:
- Decisions needed:
- Blockers/residual risk:
- Rollback:
- Next safe action:
- Do not touch:

## Stop-and-Report Rules

Stop immediately if Step 0 is not confirmed; origin/base/tree/status/blob/counts differ materially; Q04 or Amendment 01 custody is absent; AF-P07 is insufficient; another file/dependency/config is needed; the retained method/scenario would be deleted, renamed, or weakened; any D9(b) denial cannot be independently tested; consent cannot precede provider relay; the cookie can reach a provider/redirect; the response must be read unbounded; a fake could delegate; offline dependencies or verified backend assets are unavailable; full lint diverges from the accepted 98-finding outside-AF baseline, focused lint reports any AF-P07 finding, or any test/build/backend integration, count, scope, ancestry, cleanup, or security review fails; the same tripwire recurs twice; or any network/provider/registry/cloud/production/protected-data/secret/push/PR/merge/deploy/enable/publish/release/diagnostic mutation is proposed.

If the backend contract changed, retain fail-closed behavior and stop for a narrow contract/spec amendment. Do not broaden AF-P07 or silently relax strict parsing.

Gate 3 remains human-only and ungranted. A green builder result or reviewer acceptance is not merge or release approval.

## Shaping Boundary

BioStack does not contain the Foreman emitter/linter required for a `ShapingResult`. This docs-only spec creates no `ShapingResult`. Coordinator lint passed; dispatch remains contingent on the clean isolated worktree and confirmed Step 0.
