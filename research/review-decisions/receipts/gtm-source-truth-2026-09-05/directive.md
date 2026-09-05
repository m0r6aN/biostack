# Directive: GTM Source Truth and Production Handoff

Repo: `D:\Repos\BioStack`, GitHub `m0r6aN/biostack`.
Base: `4d8754c670a7d4553ade857be80aa170ede85653`.
Date: 2026-09-05. Branch: `lead/gtm-source-truth-20260905`.
Evidence: [brief](brief.md), [lead verdict](lead-verdict.md),
[Claude verdict](claude-verdict.txt), [Gemini verdict](gemini-verdict.txt).

## Verdict

Hold Creatine, Vitamin D3, and Tamoxifen out of wave 006. This PR records three
request-changes decisions, not three approvals or new dossiers. Correct the
verified factual defects first, then independently re-review the corrected packets.
Do not carry the old in-packet `RESOLVED` assertions forward as review authority.

## Findings and Council Reconciliation

F1 [P1, lead + both CLI seats]: Creatine conflates study-level and participant-level
adverse-event percentages. The lead verified the primary abstract. Either measure
can be described if labeled correctly; merely replacing one percentage with another
does not independently clear broad safety language or the earlier tier upgrade.

F2 [P1, lead + Gemini]: ATLAS all-cause death is mislabeled breast-cancer mortality;
crude event proportions and cumulative risks are mixed. The primary paper and NCI
confirm corrected endpoints. Preserve cohort/window definitions and paired harms.

F3 [P1, primary-source reviewers + lead]: D3's assertion that kidney-stone harm was
absent from the 2018 statement is directly contradicted by that statement. Both CLI
seats instead emphasized intervention scope. That is useful caution, but not proof
that the existing packet explicitly asserts a D3-alone causal effect.

Council outputs are not votes that override sources. Rejected/narrowed proposals:

- Claude's "non-overlapping cohorts" is incorrect: ATLAS efficacy is an ER-positive
  subset of the broader safety population, not a disjoint cohort. The populations
  still must be distinguished, including hysterectomy exclusions.
- Claude's proposal to drop VITAL as efficacy evidence is rejected. A negative
  primary-endpoint trial is evidence about efficacy, not evidence of a benefit and
  not proof of exactly zero effect. The packet's negative framing belongs in the dossier.
- Claude's denial of a second source family is too broad: NCI is a separate editorial
  synthesis under this review contract, but not independent trial replication.
- Gemini's "exact 12,894-woman" endometrial denominator omits the endpoint-specific
  hysterectomy exclusion. Do not use the full allocation as a crude incidence denominator.
- "Reject" is not the appropriate machine decision here: the index treats rejection
  as archival. Use `request-changes`. Do not turn absence of renal evidence into a
  resolved safety conflict or apply the historical 65+ falls scope to the newer draft.

The quick council comprised Claude CLI (`sonnet` alias requested), Gemini CLI
(configured default, exact model not attested), and the lead. Codex/Grok seats
were not invoked. Initial launches failed prompt transport/argument parsing;
one bounded retry per seat returned the captured verdicts. Only the public-source
brief was requested as input, with read-only tools and MCP servers disabled.
No separate model-identity guarantee is claimed beyond the observed CLI routes.

## Execution Order

1. Merge the new review decisions only after Clint reviews this PR. They change
   offline review inputs; they do not edit existing packets or live content.
2. Remediate the exact claim statements, extracted evidence, repeated flags, and
   conflict text specified in the batch. Preserve historical decision receipts.
   One author owns each packet; a separate reviewer checks its changed claims
   against materially different source families. Do not dilute safety authority.
3. For future approval, resolve the named reviewer-authority provenance: the older
   delegation names Claude, whereas this session's lead is OpenCode. This batch
   issues only change requests and does not impersonate that named grantee.
4. Recompile with explicit intended inputs, including an actual source-registry
   path. The helper's default `research/input/sources/source-registry.json` does
   not exist at this base; `pilot-source-registry.json` does. Schema success and
   missing-registry compilation do not establish registry authorization.
5. Stage a promotion batch only for fully eligible packets. Reconcile manifest,
   inactive export, preview, and import dry-run before editing seeds. Clint merges
   and performs production steps. A green app deploy is not a knowledge Refresh.

## Production Evidence

Read-only checks on 2026-09-05:

- [PR #264](https://github.com/m0r6aN/biostack/pull/264) merged at
  `2026-08-30T13:51:08Z`, SHA `339f259b1a467034db4f57cf9d774c292f11b53a`.
- [Merge deployment 33315286760](https://github.com/m0r6aN/biostack/actions/runs/33315286760)
  succeeded for that exact SHA. Latest observed main deployment
  [33743439471](https://github.com/m0r6aN/biostack/actions/runs/33743439471) succeeded
  for this PR's base. No live build-SHA endpoint was verified.
- Public passkey status returned `enabled=true`. Anonymous `credentialCount=0`
  does not establish enrollment eligibility for a particular account.
- Public compound listing returned 52 entries. This is not the count of promoted
  dossiers, evidence packets, or seed identities.

| Wave-005 compound | Public dossier GET | Content observation |
| --- | ---: | --- |
| Raloxifene | 200 | Older conservative launch record; one DailyMed search reference |
| Toremifene | 404 | Missing |
| Lasofoxifene | 404 | Missing |
| LL-37 | 404 | Missing |
| AC-262536 | 404 | Missing |
| LGD-3303 | 404 | Missing |

The lead checked the same-origin `/api/v1/knowledge/compounds/{canonicalName}`
GETs. A separate read-only investigator also checked direct API dossiers.
Conclusion: wave 005 was not fully visible at check time. This does not prove
whether a worker was ever executed, which image it used, or which DB it targeted.

## Clint-Only Refresh

Do not run either command below from an unverified local checkout or against a
guessed Azure job. The production resource group/job/image digest were not
established by this session. Clint must confirm the existing worker runtime,
approved seed-containing image, existing secret bindings, same PostgreSQL target
as the API, and `DOTNET_ENVIRONMENT=Production` first. No new credentials or
infrastructure changes are requested by this PR.

**Configuration precedence must be checked before execution.** `Program.cs:21-25`
adds environment configuration after the default command-line provider. An inherited
`Worker__DryRun=false` can override `--Worker:DryRun=true` and cause full dossier
upserts, not just startup writes. The same applies to run mode and seed path.
Clint must ensure every environment entry resolving to `Worker:RunMode`,
`Worker:DryRun`, or `Worker:SeedFilePath` (including double-underscore/colon and
case variants) is absent or matches the intended invocation. Stop on any conflict;
do not discover it by reading the post-run summary. Do not dump the full environment
or connection string to establish these three non-secret settings.

Inside that verified runtime, with effective values `RunMode=Refresh`, `DryRun=true`,
and `SeedFilePath=/app/Seeds/substances-seed.json`, dry-run first:

```text
dotnet BioStack.KnowledgeWorker.dll --Worker:RunMode=Refresh --Worker:DryRun=true --Worker:SeedFilePath=/app/Seeds/substances-seed.json
```

**DryRun is not globally read-only.** `Program.cs:128-145` can ensure schema and
seed default interaction hints before job-level dry-run handling. Both invocations
therefore remain Clint-only. Review exit status, accepted/rejected records, target,
and summary before deciding whether to execute the write run. Before that separate
invocation, Clint must again check environment precedence with effective values
`RunMode=Refresh`, `DryRun=false`, and the same approved `SeedFilePath`:

```text
dotnet BioStack.KnowledgeWorker.dll --Worker:RunMode=Refresh --Worker:DryRun=false --Worker:SeedFilePath=/app/Seeds/substances-seed.json
```

Refresh operates on the selected seed, not exclusively six named compounds. Verify
the full intended change set. Preserve sanitized run time, image/seed revision,
exit status, and summary. Then use public GETs to verify all six updated dossiers:

```powershell
$names = 'Raloxifene', 'Toremifene', 'Lasofoxifene', 'LL-37', 'AC-262536', 'LGD-3303'
foreach ($name in $names) {
    $url = 'https://biostack.cc/api/v1/knowledge/compounds/' + [Uri]::EscapeDataString($name)
    curl.exe --fail --silent --show-error --max-time 30 $url
    if ($LASTEXITCODE -ne 0) { throw "Dossier GET failed: $name" }
}
```

An HTTP 200 is necessary but insufficient. Compare mechanisms and source references
with the approved seed; Raloxifene's old placeholder already returns 200.

## Auth Walkthrough Still Pending

No magic-link email, token exchange, passkey ceremony, consent action, or account
mutation was performed. No authenticated browser/device interaction was available
in this session. Local mocks and deployment receipts do not replace that check.

1. Clint uses `/auth/signin` with a controlled test identity, completed onboarding,
   current consent, no enrolled passkeys, and a fresh WebAuthn-capable browser profile.
2. Open the email privately. Expect token removal from the address bar, successful
   POST exchange, then the enrollment offer. Never record the URL/token/cookie.
3. Choose Add a passkey and personally complete the device prompt. Confirm normal
   redirect and passkey availability for a later sign-in.
4. With a separate eligible test context, verify Not now redirects without blocking
   access and suppresses the offer on that device. Also verify cancellation allows
   retry or skip. Do not delete credentials just to manufacture this state.
5. Record only timestamp, browser/device type, final route, and sanitized outcome.
   New users should finish onboarding rather than receive this nudge first.

## PCAC and Article Two

Epitalon, Semax, and KPV each returned 404. Keep their existing `live:false` flags
in `frontend/src/app/knowledge/insights/fda-pcac-2026-peptide-vote/page.tsx`.
Prior decision/input inspection identifies standing formulation/telomerase gaps
for Epitalon, an authoritative contraindication-source blocker for Semax, and
identity/protocol verification gaps for KPV. Those are carry-forward blockers,
not three new independent biomedical reviews in this pass. Recheck time-sensitive
FDA status before promotion; advisory votes are not binding listing or approval.

[Article-two brief](article-two-brief.md) prepares the narrow creatine/hair-loss
piece without introducing a route or public claim. Publication waits for its own
source review and Clint's merge/deploy. Dossier promotion is a separate gate;
never link a missing dossier as live.

The 150-compound-plus-supporting-supplements target remains the expansion objective.
Track seed identities, draft packets, reviewed/promoted dossiers, and actual live
content separately. Do not subtract a public API count from 150 and call it a
review backlog. Remaining remediated packets, candidate sync, registry authorization,
Stripe lifecycle, and Azure SKU checks remain queued; Stripe/infra actions are Clint-only.

## Validation and Boundaries

114 focused worker tests passed, including three added real-batch cases validating
schema, claim references, newest-first ordering, and promotion refusal for an
otherwise clean draft after older claim review. No production DB is used by these tests.

```powershell
dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter 'FullyQualifiedName~ResearchWorkflowRegressionTests|FullyQualifiedName~ResearchArtifactValidatorTests|FullyQualifiedName~ResearchEvidenceProcessingTests|FullyQualifiedName~ResearchJobTests'
```

Eight auth verification tests passed against the unchanged main checkout at the
same base using the existing frontend dependencies:

```powershell
.\node_modules\.bin\vitest.cmd run src/__tests__/components/VerifyPage.test.tsx --pool=threads --maxWorkers=2
```

The first unconstrained worker-count invocation exceeded the 60-second tool limit;
the bounded retry ran in 15.41 seconds. Installed Vitest reported 4.1.10. Existing
Vite config-loader and two C# nullable warnings were observed; no full frontend
build or browser E2E pass is claimed.

Local logs: `C:\Users\clint\AppData\Local\Temp\opencode\biostack-gtm-tests-out.txt`,
`biostack-gtm-test-results\gtm-source-truth.trx`, and
`biostack-gtm-auth-tests-out.txt`. Test results validate mechanics, not biomedical truth.

Final read-only artifact review identified the environment-over-CLI precedence
hazard above; the handoff was corrected before commit. This PR does not change the
worker's configuration order or make production dry-run globally read-only.

No corpus identity, seed, evidence-packet content, route, or live-link change is made,
so the four census tripwires are unchanged: `CorpusIdentityInventoryBuilderTests.cs`,
`StructuralEvaluationReportBuilderTests.cs`, `structural-evaluation-report.yml`,
and `SeoMetadata.test.ts`. Update all four on the next actual corpus/route change.
Historical `research/output/latest` is deliberately not regenerated or represented
as current. No merge, deploy, Refresh, source-acquisition run, production write,
secret/infra change, Stripe action, legal approval, or financial transaction occurred.
