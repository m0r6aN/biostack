# GTM Handoff: September 5, 2026

Base: `4d8754c670a7d4553ade857be80aa170ede85653`. Scope: kickoff reconciliation and the three nominated wave006 re-reviews. Target remains 150 compounds plus supporting supplements; no new compounds or routes are added by this handoff.

## Latest Decisions

Newest-first checkpoints, not an exhaustive replacement for the directory's decision history:

| Date | Artifact | Disposition |
|---|---|---|
| 2026-09-05 | [Wave006 re-review](review-decision-batch-2026-09-05-wave006-rereview.json) and [source receipt](receipts/wave006-rereview-2026-09-05.md) | Creatine, Vitamin D3, Tamoxifen: request changes; no clearance. |
| 2026-08-29 | [Promotion wave005](review-decision-batch-2026-08-29-promotion-wave-005.json) | Historical six-compound promotion decision; production visibility not established for the promoted records. |
| 2026-08-29 | [R3](review-decision-batch-2026-08-29-wave-r3-007.json), [R2](review-decision-batch-2026-08-29-wave-r2-006.json), [R1](review-decision-batch-2026-08-29-wave-r1-005.json) | Review-batch numbers are not promotion-wave numbers. Remediation receipts do not supersede retained gates. |

The other remediated packets were not re-reviewed in this session. Epitalon/KPV remain review-required and Semax remains blocked in the historical generated manifest; this session did not independently re-review them. Their PCAC article `live:false` flags remain unchanged. Article #2 is deferred until its source assertions are corrected and reviewed, not generated from disputed packet text.

## Auth And Deployment

- [PR #264](https://github.com/m0r6aN/biostack/pull/264) merged August 30 at 13:51 UTC, SHA `339f259b1a467034db4f57cf9d774c292f11b53a`.
- Its [main deployment](https://github.com/m0r6aN/biostack/actions/runs/33315286760) succeeded. The newer [September 3 deployment](https://github.com/m0r6aN/biostack/actions/runs/33743439471) at the base SHA also succeeded. PR checks alone were not treated as deployment evidence.
- Public GET checks on September 5 around 10:36 UTC: `/auth/signin` and tokenless `/auth/verify` returned 200; tokenless verify used `Cache-Control: no-store`; `/api/v1/auth/passkeys/status` advertised enabled; `/api/v1/auth/session` reported anonymous; `https://api.biostack.cc/health` returned 200.
- Public sign-in chunk `/_next/static/chunks/3a70qmr3idx3q.js` contained the shipped "Already added a passkey" / "Or use email" copy. This proves shipped copy, not successful authenticated enrollment.
- No email was sent, token redeemed, account created, credential enrolled, or production session mutated. Real magic-link delivery, account-specific eligibility, session persistence, WebAuthn ceremony, and final authenticated redirect remain **unverified**.

Clint's authenticated acceptance walk: use a verified-email account with zero passkeys and completed onboarding in a browser that supports WebAuthn. Request and redeem the magic link, confirm the offer, exercise "Not now" and its device-local dismissal, then use a fresh browser profile for enrollment. Confirm enrollment reaches the server-returned local destination, existing credentials suppress the offer, onboarding is not interrupted, and a subsequent passkey sign-in succeeds. Do not put tokens or credentials in logs, screenshots, PRs, or this handoff.

The offer's eligibility GETs currently lack explicit timeouts (`frontend/src/app/auth/verify/page.tsx`); failure catches do not prove a hung request cannot delay redirect. Track a bounded-timeout/test fix separately rather than calling this flow unconditionally non-blocking.

## Wave005 Production

Public GET results matched on `https://biostack.cc` and `https://api.biostack.cc` for `/api/v1/knowledge/compounds/{name}`:

| Name | Status | Interpretation |
|---|---|---|
| Raloxifene | 200 | Conservative launch record; not proof of its promoted replacement. |
| Toremifene | 404 | Promoted dossier not visible through this API. |
| Lasofoxifene | 404 | Promoted dossier not visible through this API. |
| LL-37 | 404 | Promoted dossier not visible through this API. |
| AC-262536 | 404 | Promoted dossier not visible through this API. |
| LGD-3303 | 404 | Promoted dossier not visible through this API. |

Both public list endpoints returned 52 records. That is an API projection count, not the seed, packet, or canonical census. These observations cannot establish whether Clint ran Refresh or identify the database/runtime cause.

**Clint-only blocker:** `Refresh` with `DryRun=true` is not database-write-free. Before ingestion runs, `backend/src/BioStack.KnowledgeWorker/Program.cs:128-145` invokes schema/bootstrap/default-interaction seeding. `IngestionJobBase.cs` only suppresses later compound upserts. Do not run a production Refresh as a read-only diagnostic. The production launcher, approved artifact, and rollback context have not been established here; no guessed Azure/production command is provided.

Exact read-only production recheck, from PowerShell:

```powershell
foreach ($name in @('Raloxifene', 'Toremifene', 'Lasofoxifene', 'LL-37', 'AC-262536', 'LGD-3303')) {
    $url = 'https://api.biostack.cc/api/v1/knowledge/compounds/' + [uri]::EscapeDataString($name)
    $response = Invoke-WebRequest -Uri $url -Method Get -SkipHttpErrorCheck -TimeoutSec 30
    [pscustomobject]@{ Name = $name; Status = [int]$response.StatusCode }
}
```

## Offline Validation

The runner defaults to `research/input/sources/source-registry.json`, which does not exist in this checkout; it silently omits that optional argument. The present registry is `pilot-source-registry.json`. Use the explicit existing path: a successful unregistered compile must not be confused with authorized promotion readiness. Fixing the runner's missing-input behavior and completing the registry authorization pass are separate follow-ups; legal/rights changes remain Clint-only.

From the repository root, database-free research:

```powershell
pwsh -NoProfile -File tools/research/run-knowledge-research.ps1 -SourceRegistryFile research/input/sources/pilot-source-registry.json -OutputDirectory research/output/gtm-20260905
```

From `frontend`, local auth regression tests without npx:

```powershell
node_modules\.bin\vitest.cmd run src/__tests__/components/VerifyPage.test.tsx src/__tests__/components/SignInPage.test.tsx src/__tests__/lib/passkeys.test.ts src/__tests__/lib/AuthProvider.test.tsx --pool=threads
```

Validation completed so far:

- Database-free Research compiled all 78 packets with zero processing failures and explicitly logged that Postgres connectivity was skipped. The new decision batch was schema-validated by the real worker.
- With the existing registry loaded, the manifest reports **76 blocked, 1 review-required, 1 candidate for promotion**, plus one research-requested entry outside the 78-draft partition. Only Semaglutide is exported. This is not the historical unregistered 18-candidate result and not a production census.
- Executed assertions verified all 23 decision claim IDs match the current packets, each new decision is request-changes with no soft clearance, all three compounds have `HasRequestedChanges=true`, each latest decision ID is applied, and none is exported.
- **22 auth tests passed across four files** using the exact Vitest command above. This is mocked/local coverage, not the real authenticated production journey.
- Independent fresh-context artifact review found no actionable issues in schema/scope, timestamp precedence, fail-closed behavior, and production-claim boundaries (host task `ses_f8ece5ec0ffevqho75m6cBOrrp`). It did not re-fetch the entire science corpus or verify subsequently added test results.

Research artifacts: `research/output/gtm-20260905/` (ignored, separate from historical `latest`). Local execution logs: `C:\Users\clint\AppData\Local\Temp\opencode\biostack-gtm-20260905-*.log`. Existing nullable-reference and Vite configuration warnings were observed; no tracked application code was changed to suppress them.

No corpus or public-route membership changes are made, so the four census fixtures are not edited in this PR; all four remain mandatory for the eventual seed/route change.

## Boundaries And Next Work

Branches and PRs only. Clint retains merges, deployment, production DB writes including Refresh startup, secrets/infra, Stripe production, legal, and money. No such actions were taken. Pre-existing `.audit/foreman-line-goals/` and `.codex-temp/` work is untouched.

Immediate order: correct the demonstrated wave006 packet errors; obtain independent re-review and exact authority support; resolve explicit-registry promotion gates; only then stage a genuine promotion wave. Do not fill the boat with false claims.
