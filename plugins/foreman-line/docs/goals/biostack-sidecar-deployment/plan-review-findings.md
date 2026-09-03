# Plan-Level Adversarial Review — Findings and Triage

Goal: `biostack-sidecar-deployment`. Charter status at review time: ratified at Gate 1 (2026-09-03), contingent Gate 2 not yet exercised — zero parcels dispatched.

Two fresh, independent, zero-context frontier reviews were dispatched per `COORDINATOR-PATTERN.md` ("Plan-level adversarial review — always"): Reviewer A (decomposition/boundary mandate), Reviewer B (security mandate, D17-aligned given this goal's architecture/risk routing). Neither reviewer read the other's output. The coordinator then reproduced every load-bearing claim on disk before triaging (loop-directive.md step 6 / lesson: reproduction is the tie-breaker and the closure proof).

**Every claim the coordinator attempted to verify reproduced exactly as reported.** No finding below is marked UNVERIFIED-and-discarded; the two the reviewers themselves flagged UNVERIFIED are carried as such.

## How to read the triage column

- **FIX** — actionable now, within the coordinator's standing authority (Allowed Files correction, spec-content instruction, scenario-matrix wording). Does not touch a locked decision. Proceeds under Gate 2 once the affected parcel is shaped.
- **AMEND (D-n)** — the finding falsifies or destabilizes a specific ratified decision's text. Requires a scoped Gate 1 re-open naming exactly that decision. **Blocks dispatch of any parcel the decision governs until re-ratified.**
- **NEW DECISION (D-n)** — the finding surfaces a real design question D1-D18 never answered. Drafted as a new locked decision, proposed with a recommendation, for ratification alongside the AMEND items.
- **ACCEPT-AS-DOCUMENTED** — true, does not require code/plan change, but must be recorded as a named residual so Stage-F closure doesn't silently omit it.
- **INFORMATIONAL** — correct observation, no action required.

---

## Master structural finding (both reviewers converge on this independently)

**No parcel's Allowed Files includes any sidecar source file, the Dockerfile, or the C# API/client code.** P01's Allowed Files are `.github/workflows/research-sidecar-ci.yml`, `scripts/verify-research-sidecar-container.mjs(+test)`, `backend/research-sidecar/docs/PARCELS.md` — verification and CI plumbing only. P02-P04 are infra/workflow/docs only. Every defect below that lives in `Dockerfile`, `app.py`, `ScientificResearchSidecarClient.cs`, or `AdminEndpoints.cs` is therefore **provable but not fixable** by the parcel that would find it — a guaranteed collision with charter stop condition #2 ("any implementation requires a file outside an exact parcel's Allowed Files").

This is not itself a single finding; it is the mechanism connecting findings A-2, A-3(partial), B-2, B-3, B-8, B-9, B-10(partial) below. Triage resolves it once, by AMENDing P01's Allowed Files rather than by patching each symptom separately.

---

## Findings

### 1. [BLOCKER] SC-07/D12 is structurally unachievable while API routing stays disabled — AMEND (D12)
**Source:** A-1. **Reproduced:** `ScientificResearchDependencyInjection.cs:20-39` — the named `HttpClient` and the live provider are registered only `if (options.Enabled && !string.IsNullOrWhiteSpace(options.BaseUrl))`. With `Enabled=false` (D8/SC-06's dark requirement), there is no code path by which the API can originate the "correct token succeeds from the API network boundary" probe D12/SC-07 require.
**Triage:** AMEND D12. Either drop the live API-boundary auth probe from the dark (Gate 3A) acceptance set and fold it into Gate 3B's SC-08 (where routing is enabled), or add an explicit throwaway probe mechanism as a named P02/P03 deliverable. **Recommendation: fold into SC-08.** The dark phase's job is to prove the sidecar is healthy and unreachable *except* by the token-holder; proving the API can actually use that token requires routing to be live, which is definitionally Gate 3B's job, not Gate 3A's.

### 2. [BLOCKER] P01 Allowed Files omit every file that defines the container it must prove — AMEND (P01 Allowed Files, not a D-number)
**Source:** A-2, corroborated by B-3. **Reproduced:** `.dockerignore` does not exclude `.env` (config.py:25 `env_file=".env"`) or `tests/`; `Dockerfile:12` `ARG TOOLUNIVERSE_EXTRA=tooluniverse` makes the **default production build install the full ToolUniverse dependency tree**; no `HEALTHCHECK`.
**Triage:** FIX at the Allowed Files level (see Master structural finding). Add to P01: `backend/research-sidecar/Dockerfile`, `backend/research-sidecar/.dockerignore`, `backend/research-sidecar/pyproject.toml`, `backend/research-sidecar/uv.lock`. This is a scope correction, not a reversal of any D-decision — D1 already forbids ToolUniverse *enablement*; building the image without the extra makes the artifact match the decision more closely, not less.

### 3. [BLOCKER] Dark image ships provider SDKs, `pip`, `setuptools` into a writable venv — attack surface, not just bloat — NEW DECISION (D19)
**Source:** B-2, B-13. **Reproduced:** `uv.lock` — `tooluniverse==1.4.0` pulls `openai`, `google-genai`, `huggingface-hub`, `ddgs`, `playwright`, `mcp[cli]`, `pip`, `setuptools`; `Dockerfile:32-34` `chown -R biostack:biostack /app` makes the runtime user's own venv writable.
**Triage:** NEW DECISION D19 — *the deployed image is built without the ToolUniverse extra* (`--build-arg TOOLUNIVERSE_EXTRA=""` or an equivalent no-extra stage), and P01's container contract asserts `openai`, `google-genai`, `huggingface-hub`, `pip`, and `setuptools` are absent from `/app/.venv`. **Recommendation: adopt.** This directly shrinks SG-OUTBOUND's real attack surface (an RCE in a no-provider-SDK, no-pip image cannot trivially exfiltrate via a pre-installed OpenAI client) at zero cost to this goal's scope, since D1 already forbids using those SDKs.

### 4. [BLOCKER] Gate 3A merge itself triggers an unratified production mutation via existing `deploy.yml` — AMEND (D5)
**Source:** A-3, corroborated by B-12 (same root cause, independently found). **Reproduced:** `.github/workflows/deploy.yml:3-8` triggers on `push: branches: [main]`, no `paths:` filter, no `environment:` protection; lines 128/161/185 run `az containerapp update`/`hostname bind` against the **production API and frontend** unconditionally.
**Triage:** AMEND D5. Current text ("A push to `main` may not automatically mutate the sidecar or API configuration") is true only for the *sidecar-specific* workflow D5 itself creates — it reads as, and was evidently intended as, a property of the repo, and it is not one. **Recommendation:** reword D5 to state explicitly that the Gate 3A merge triggers the pre-existing `deploy.yml` and therefore redeploys the production API and frontend at the candidate SHA as a disclosed, accepted side effect; require the Gate 3A request package to name the resulting API/frontend revision. This does not require editing `deploy.yml` itself (no scope expansion) — it requires the charter to stop asserting something the repo contradicts.

### 5. [BLOCKER] SG-EVIDENCE and SG-IAM's least-privilege half have no enforcing artifact in any parcel — FIX
**Source:** B-1. **Reproduced:** union of all four Allowed Files lists contains nothing touching `ScientificResearchCandidateStagingService.cs` or EvidenceGate; D16 only fingerprints identities, never narrows or enumerates the OIDC principal's actual RBAC.
**Triage:** FIX. Add to P04: a `verify-research-sidecar-live-boundary.test.mjs` assertion against a recorded staging response that `review_state` is non-canonical and `tools_invoked` is empty (making SG-EVIDENCE a real check, not a UAT observation). Add to P03's preflight: a non-mutating `az role assignment list --assignee <oidc-principal>` fingerprint compared against a ratified expected scope, failing on anything wider. No D-number touched — this fills a gap the charter left open, it doesn't contradict one.

### 6. [MAJOR] SG-IMAGE is a verify-but-cannot-fix trap beyond finding 2 — same remedy as finding 2
**Source:** B-3. Duplicate root cause of finding 2 (floating base image tag `ghcr.io/astral-sh/uv:python3.12-bookworm-slim`, `.env` leakage into build context). **Triage:** FIX, folded into finding 2's Allowed Files amendment. Additionally require P01's spec to pin the base image by digest, not just tag, as part of "immutable identity" (D6).

### 7. [MAJOR] Six of ten scenarios plus every container-based scenario are un-runnable under local-only Gate 2 — FIX (scenario matrix wording)
**Source:** A-4. **Reproduced:** SC-05 through SC-10 require Azure access withheld until Gate 3A/3B; SC-01's container checks and SC-02/03/04 require `docker build` + `uv sync` resolving 207 packages, which the loop directive already authorizes locally (Docker daemon confirmed up) — so these four are NOT actually blocked, contra part of A-4's framing, but they DO require an explicit "provable now vs. provable only at Gate 3A/3B" split so a parcel doesn't get accepted on a fixture standing in for the real thing.
**Triage:** FIX. Add an `Owner` and `Provable-at` column to the SC-xx matrix: local-container-provable now (SC-01..SC-04) vs. Gate-3A/3B-only (SC-05..SC-10, plus SC-07 per finding 1's fold-in). Label every Gate-3A/3B-only row **DEFERRED-TO-GATE-3** in every parcel's completion claim — a fixture imitating a production result is a self-graded claim (lesson #33), already written into loop-directive.md step 7.

### 8. [MAJOR] D6/SG-IAM: ACR admin credentials are already enabled and in use, contradicting the "no introduction" framing — ACCEPT-AS-DOCUMENTED
**Source:** A-5, corroborated by B-16 (independently found). **Reproduced:** `source-acquisition-acr-transition.bicep:39` `adminUserEnabled: true`; `deploy-container-apps.ps1:122,225` use `--registry-username/--registry-password` for the existing API and web apps.
**Triage:** ACCEPT-AS-DOCUMENTED. D6 is literally true (this goal introduces no *new* admin credential) but the pre-existing admin user is a real residual D16's preflight should surface, not silently omit. FIX: add `az acr show --query adminUserEnabled` to P03's preflight fingerprint list and record it in the Gate 3A packet as a pre-existing condition, not a new one.

### 9. [MAJOR] Scenario matrix has no owner column; SC-09's custody chain spans P01→P03 with no defined handoff artifact — FIX
**Source:** A-6. **Triage:** FIX, folded into finding 7's matrix-wording fix. Name the concrete SC-09 mechanism: P03's release job re-runs the exact P05/P06 test IDs in the same job step that computes `docker inspect --format='{{index .RepoDigests 0}}'`, and records both in the same artifact.

### 10. [MAJOR] P03 must consume P02's bicep parameter surface but cannot edit it; sidecar `BaseUrl` derivation is unowned — FIX
**Source:** A-7. **Triage:** FIX. Require P02's spec to freeze the exact bicep parameter contract (names + types) as part of its Acceptance Criteria, so P03 codes against a ratified interface rather than discovering it. Add "API `BaseUrl` derivation and injection, sourced from `az containerapp show --query properties.configuration.ingress.fqdn`" as an explicit P04 deliverable.

### 11. [MAJOR] Three overlapping verifiers proposed; the natural fork target can't work against internal ingress — FIX
**Source:** A-8. **Reproduced:** `scripts/verify-containerapp-deployment.mjs:157-158` does `fetch(new URL(healthPath, https://${fqdn}))` — for `external:false` ingress this FQDN does not resolve from a GitHub-hosted runner.
**Triage:** FIX. Add `scripts/verify-containerapp-deployment.mjs` (+ its test) to P03's Allowed Files so the existing verifier is extended with an `--internal` mode (ARM `healthState` instead of HTTP fetch) rather than triforked. Reword D12's "dark health status" clause to mean ARM-reported `healthState: Healthy`, not an HTTP body assertion — folded into finding 1's D12 amendment.

### 12. [MAJOR] D12 is the single most load-bearing, least-scrutinized decision — folded into finding 1
**Source:** A-9. Not a separate action; captured by the D12 AMEND in finding 1, expanded to decompose D12 into numbered sub-clauses (D12.1-D12.n) each naming its owning parcel and observation command, per A-9's remedy.

### 13. [MAJOR] Decomposition: P04 bundles three unrelated jobs (docs, verifier code, live API config transition) — FIX (parcel-plan restructure, not a D-amendment)
**Source:** A-10. **Triage:** FIX. Split P04 into **P04a** (runbook + live-boundary verifier + tests — buildable and reviewable now, under Gate 2) and **P04b** (API configuration transition + disable-first rollback — a genuine production mutation, dispatched only after Gate 3B is granted, outside this goal's current authorization). This is a parcel-boundary correction within the ratified four-track plan, not a reversal of D13's substance — P04a and P04b together still satisfy D13. Recorded here rather than as an AMEND because it doesn't change what Gate 3B must prove, only when its two halves are built.

### 14. [MAJOR] Service token traverses the internal network in cleartext; no transport-encryption requirement anywhere in the charter — NEW DECISION (D20)
**Source:** B-4. **Reproduced:** `ScientificResearchSidecarOptions.cs:11` defaults `BaseUrl` to `http://127.0.0.1:8080`; D2 fixes internal ingress on port 8080 with no `allowInsecure`/`transport` clause.
**Triage:** NEW DECISION D20 — P02's bicep sets `ingress.allowInsecure: false`, and the API's `ScientificResearchSidecar__BaseUrl` uses `https://`; P02's verifier asserts both. **Recommendation: adopt.** The service token is the sole barrier to job execution (finding 16); sending it in cleartext over even an internal network is a real, cheaply-closed gap.

### 15. [MAJOR] Secret scanner cannot detect the service token: working-tree-only scan, `generic-api-key` rule overridden to empty — AMEND (D7, scope) / FIX
**Source:** B-5, corroborated by A-12 (same file, independently found). **Reproduced:** `.github/workflows/secret-scan.yml:24` runs `gitleaks dir` (working tree only, no history); `.gitleaks.toml:22-23` redefines `generic-api-key` with **no regex**, and under `useDefault = true` a same-id user rule overrides the default. (Reviewer B labeled the override-precedence half UNVERIFIED pending an actual gitleaks run — the config text is confirmed, the merge semantics are not independently re-verified by the coordinator either; carried forward as UNVERIFIED, not asserted.)
**Triage:** FIX, with a narrow AMEND to D7's Allowed Files reach: add `.github/workflows/secret-scan.yml` and `.gitleaks.toml` to P03's Allowed Files, switch to `gitleaks detect --log-opts=<base>..HEAD` so history is scanned, and restore a real high-entropy rule (or confirm — by an actual `gitleaks detect` run in P03 — that `useDefault` in fact still applies the default regex despite the empty override, before assuming the gap is real). This doesn't reverse D7's substance (token storage discipline); it closes a detection gap D7 assumed was covered by an existing control that turns out not to fully cover it.

### 16. [MAJOR] "Configuration-set-to-disabled" is a soft control: the kill switch is post-admission, not admission control — ACCEPT-AS-DOCUMENTED + FIX (scenario wording)
**Source:** B-6. **Reproduced:** `app.py:152-239` `submit_job` returns `202`/`queued` regardless of `global_kill_switch`; `kill_switches.assert_research_allowed` is reached only inside `workflows/executor.py:40`, i.e. after admission. The repo's own test (`test_health_and_jobs.py:236-249`) confirms this is intended behavior, not a bug: `202` then later `rejected_by_policy`.
**Triage:** ACCEPT-AS-DOCUMENTED as the sidecar's real (and intentional) dark contract — this is existing, tested behavior outside any parcel's Allowed Files, not a defect this goal introduces. FIX: reword SC-02 to state the actual contract (`202` followed by terminal `rejected_by_policy`/`global_kill_switch`), and require P02's bicep to set every D8 flag in the **initial revision template** (never a post-create `az containerapp update`), so there is no window where a Ready revision is live with defaults. Also FIX: D14's rollback order — require P03/P04's rollback test to prove the *restored* revision's env still carries `GLOBAL_KILL_SWITCH=true`, closing B-6(b)'s rollback-reintroduces-defaults path.

### 17. [MAJOR] The service token is the only barrier to job execution across the whole managed environment (the public web app shares the network) — ACCEPT-AS-DOCUMENTED
**Source:** B-7. **Reproduced:** `AdminEndpoints.cs:19-21` `RequireAuthorization("AdminOnly")` protects only the API's own route, not the sidecar; `deploy-container-apps.ps1` places the public web app in the same managed environment D2 places the sidecar in — internal ingress means "not internet-routable," not "only the API can reach it."
**Triage:** ACCEPT-AS-DOCUMENTED as a named residual at Gate 3A: "any workload in `biostackmissionctrl-env` can reach the sidecar; the service token is the sole control." FIX (optional hardening, not required for this goal): P02's verifier may assert `ipSecurityRestrictions` scoping if Container Apps' internal-ingress model supports it — flag as an open question for P02's shaping session rather than a locked requirement, since it may not be achievable on the Consumption profile.

### 18. [MAJOR] SG-DATA "payload-safe logs" contradicted: the C# client logs sidecar error bodies that echo request content — FIX (blocked by Allowed Files, folds into master finding)
**Source:** B-8. **Reproduced:** `ScientificResearchSidecarClient.cs:46-53` logs the raw error body on failure; the sidecar's 422 bodies (`app.py:172-188`) can include caller-supplied field names and Pydantic's `input_value` echo.
**Triage:** FIX, but the actual code fix (`ScientificResearchSidecarClient.cs`) is outside every current Allowed Files list. **Recommendation:** add `backend/src/BioStack.Application/ScientificResearch/ScientificResearchSidecarClient.cs` to P04's Allowed Files (narrow, single-file addition) so P04 can change the log statement to log `code` only, never `Body`. Until that lands, P04's live-boundary verifier must include a negative test asserting rejected-submission values don't appear in API logs, and the gap is recorded as an open residual if the Allowed Files expansion isn't ratified.

### 19. [MAJOR] `/docs`, `/redoc`, `/openapi.json` are unauthenticated and untested — FIX (blocked by Allowed Files, folds into master finding)
**Source:** B-9. **Reproduced:** `app.py:41-48`'s `FastAPI(...)` constructor sets no `docs_url=None`/`redoc_url=None`/`openapi_url=None`; zero test references to `openapi`/`docs`/`redoc` in `tests/`.
**Triage:** FIX. Add `backend/research-sidecar/src/biostack_research_sidecar/app.py` to P01's Allowed Files (narrow: disabling the three FastAPI doc routes only) and require P01's container contract to assert all three return 404 in the production container. This is a one-line-per-route change with no behavioral risk to anything else in the file.

### 20. [MAJOR] SC-04's privacy gate is a denylist over free text below the top level, not true protected-data detection — ACCEPT-AS-DOCUMENTED + FIX (scenario wording)
**Source:** B-10. **Reproduced:** `privacy.py`'s allowlist is top-level-keys-only; everything else is ~55 hardcoded field names plus 8 regexes. A constructed example (locale-shaped name + age + dose in free-text `purpose`) matches none of the eight patterns and would be accepted; `AdminEndpoints.cs:481-484` passes `Purpose`/`SubjectName` through with no additional validation, and hardcodes `AllowGpu: true`, `LocalInferencePermitted: true` on every request (confirmed at lines 476/487/496).
**Triage:** ACCEPT-AS-DOCUMENTED — the gate's real scope (structural/known-format rejection, not arbitrary-prose PII detection) must be stated honestly in P04's runbook and SC-04's wording, not oversold. FIX: P04's negative-test set includes a prose-in-`purpose` case demonstrating the accept, recorded as a known residual rather than silently passing as "privacy verified." A real fix to `AdminEndpoints.cs` (bounding `Purpose` to an enum) is out of scope for this goal absent a further Allowed Files expansion — not requested now given the existing D10 data-boundary control (synthetic/operator-entered public identifiers only) partially mitigates real-world exposure.

### 21. [MINOR] `/health` is an unauthenticated configuration oracle — ACCEPT-AS-DOCUMENTED
**Source:** B-11. **Triage:** ACCEPT-AS-DOCUMENTED. `/health` must stay unauthenticated for Container Apps probes (D2); its disclosure of `global_kill_switch`/`tooluniverse_enabled`/concurrency is by design and low-sensitivity. Record as a residual, no action required.

### 22. [MINOR] Non-root confirmed, but runtime user's own venv (with `pip`/`setuptools`) is writable — folds into finding 3 (D19)
**Source:** B-13. **Triage:** Resolved by D19's no-provider-SDK build (removes `pip`/`setuptools` from the image entirely per finding 3). No separate action.

### 23. [MINOR] Single-replica design self-DoS's under one long-running job; no server-side timeout ceiling — FIX (scenario/runbook wording)
**Source:** B-14. **Reproduced:** `BoundedSemaphore(1)` in `jobs/runner.py`; `_resolve_timeout_seconds` has no server-side ceiling (`del settings  # reserved for a future global ceiling`); `AdminEndpoints.cs` sets a 10-minute client-side timeout. **Triage:** FIX. P04's runbook documents the 429-under-load and state-loss-on-restart semantics as operator-visible facts, not defects to remediate in this goal.

### 24. [MINOR] Release runner unnamed; two contradictory ACR-reachability precedents exist in-repo — FIX
**Source:** A-13. **Triage:** FIX. P03's spec names the runner (`ubuntu-latest` per `deploy.yml`'s precedent, matching current `publicNetworkAccess: Enabled` on the ACR) and states that assumption explicitly, so a future private-link tightening is a deliberate decision rather than a silent break.

### 25. [MINOR] `.gitleaks.toml` unowned; P02/P04 artifacts are likely to trip its `generic-api-key`-adjacent surface — folds into finding 15
**Source:** A-12 = B-5's `.gitleaks.toml` half. Already resolved by finding 15's Allowed Files amendment.

### 26. [INFORMATIONAL] Secret-handling path enumeration (B-15) — no separate action
Table of token-leak paths and their defense status. Two concrete FIXes already captured above (finding 15's history-scan gap; argv-exposure in `az containerapp secret set` — add "pipe the token via `--secrets @file` or stdin, never argv, and never run `az --debug` in the release job" to P03's spec as a named constraint). Rest is confirmed-defended or accept-as-documented, no further action.

### 27. [INFORMATIONAL → NEW DECISION] No token rotation or compromise-response plan; API compromise = sidecar compromise — NEW DECISION (D21)
**Source:** B-17. **Reproduced:** `auth.py:19-45` — `hmac.compare_digest` against a static string with no issuance time, audience, or lifetime field anywhere in `config.py` or `auth.py`. D14's rollback order (disable routing → assert kill switch → restore revisions) never rotates the credential.
**Triage:** NEW DECISION D21 — the service token has a named rotation cadence, and D14's rollback order gains an explicit step: **emergency token rotation precedes revision restore** whenever rollback is triggered by suspected compromise (not required on a routine rollback). P04's runbook names the rotation owner and procedure. **Recommendation: adopt**, scoped narrowly — this is a runbook/process addition, not new code, and directly closes the scenario where a rollback leaves a compromised credential live.

---

## Amendment summary — what requires developer ratification

| # | Decision | Change | Blocks |
|---|---|---|---|
| D12 | AMEND | Decompose into D12.1-D12.n; drop the live API-boundary token probe from dark/Gate-3A acceptance, fold into Gate-3B's SC-08; "dark health status" means ARM `healthState`, not an HTTP body fetch | P02 (probe/verifier design), P03 (release verifier), P04 (SC-08 wording) |
| D5 | AMEND | Reword to disclose that the Gate 3A merge triggers the existing `deploy.yml` and redeploys production API+frontend at the candidate SHA; Gate 3A packet must name the resulting revision | P03 (release workflow spec framing), Gate 3A packet content — does not block P01/P02 shaping |
| D19 | NEW | Deployed image excludes the ToolUniverse ARG/extra and provider SDKs (`openai`, `google-genai`, `huggingface-hub`, `pip`, `setuptools`); P01 asserts their absence | P01 (Allowed Files + Acceptance Criteria) |
| D20 | NEW | Sidecar ingress sets `allowInsecure: false`; API `BaseUrl` uses `https://`; both asserted by P02's verifier | P02 (bicep + verifier) |
| D21 | NEW | Named service-token rotation cadence; emergency rotation precedes D14's revision-restore step on compromise-triggered rollback | P04 (runbook) — does not block P01/P02/P03 |

**Allowed Files corrections requested (coordinator authority, reported for visibility, not requiring ratification):**
- P01 gains: `backend/research-sidecar/Dockerfile`, `.dockerignore`, `pyproject.toml`, `uv.lock`, and a narrow addition of `src/biostack_research_sidecar/app.py` (disabling `/docs`,`/redoc`,`/openapi.json` only).
- P03 gains: `scripts/verify-containerapp-deployment.mjs` (+test), `.github/workflows/secret-scan.yml`, `.gitleaks.toml`.
- P04 gains: `backend/src/BioStack.Application/ScientificResearch/ScientificResearchSidecarClient.cs` (narrow: log-statement change only).
- P04 splits into **P04a** (runbook + live-boundary verifier, buildable now) and **P04b** (API configuration transition + disable-first rollback execution, dispatched only after Gate 3B).

## What proceeds now vs. what's blocked (scoped re-open, not blanket)

Per `COORDINATOR-PATTERN.md` and this goal's loop-directive.md (stop condition #15): the re-open is scoped to D5, D12, and the three new decisions. **Nothing is provably orthogonal to all five** — D12 and D5 both bear on the Gate 3A packet every parcel eventually feeds, and D19/D20 both land inside P01/P02's Allowed Files before those parcels can be shaped without rework. The practical effect: **shaping for P01 and P02 should wait for this ratification**, since shaping today would need to be redone once Allowed Files and D19/D20 are settled. This is a short, bounded hold, not an open-ended stop — the amendments are narrow, drafted, and ready for a single ratification pass.

No parcel has been dispatched. No code has been written. This finding set is entirely pre-Gate-2.
