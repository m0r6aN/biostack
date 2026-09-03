# Parcel Receipts — BioStack Algorithm Remediation

Status: **live coordinator record; exact Gate 3 merge/release authority active**

## P01 — Parser semantics

- Base: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Candidate: `751090eabf3ee6f066c17ee83861ae04711fd4a0`
- Changed paths: exactly AF-P01 (`ProtocolParser.cs` and retained `ProtocolParserReproductionTests.cs`)
- Diagnostic test blob: `ab4bd922f2f17c05b37e05115245e52ef265ee98`, byte-identical
- Targeted: 6 passed
- Adjacent parser/ingestion: 28 passed
- Application: 615 passed, 5 skipped, 620 total
- Full solution: Domain 7; Application 615 passed/5 skipped; verifier 158; API 377; KnowledgeWorker 866; zero failures
- Independent adversarial review: ACCEPT, no P0-P3 findings
- Custody: clean local branch `codex/biostack-remediation-p01`; not pushed, merged, deployed, or released

## P02 — Protocol ingestion boundaries

- Base: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Candidate: `1b5742051ee2b54a80dac9bbdf68c68e5a6b5992`
- Changed paths: exactly AF-P02 (`ProtocolIngestionService.cs` plus retained PDF and OCR reproduction tests)
- Targeted: 4 passed; denial/gate-failure/authorized fake sends `0/0/1`
- Adjacent ingestion/security: 19 passed
- Real current-user/consent integration: 17 passed
- Full solution: 2,021 passed, 5 pre-existing live-test skips, zero failures
- Adversarial review A: ACCEPT, no findings
- Adversarial review B: ACCEPT, no Critical/High/Medium/Low findings
- Defensive security review: ACCEPT; SG-SCOPE PASS; SG-OUTBOUND PASS; no security findings
- Custody: clean local branch `codex/biostack-remediation-p02`; not pushed, merged, deployed, or released

## P04 — Evidence provenance

- Base: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Candidate: `38ebffb10411c763ed4f570fdcfece51ab177aa3`; direct child of the pinned base
- Changed paths: exactly AF-P04 (`ScientificResearchCandidateStagingService.cs`, `EvidenceGate.cs`, and retained `EvidenceProvenanceReproductionTests.cs`)
- Target: base `0`; candidate `23/23` passed
- Adjacent evidence/staging: base and candidate `36/36` passed
- Five-project matrix: Domain `7`, verifier `158`, API `377`, and KnowledgeWorker `866` unchanged; Application `609 passed / 5 skipped` to `632 passed / 5 skipped`, exactly `+23`; zero failures
- Solution orchestration: exact builder command recorded exit `0`; durable counters come from the unique per-project TRXs, and the overwritten solution TRX remains explicitly excluded
- Adversarial review A: ACCEPT, no findings
- Adversarial review B: ACCEPT, no findings; standalone solution-log absence is informational only under the shaped evidence contract
- Defensive security review: ACCEPT; SG-EVIDENCE PASS; SG-SCOPE PASS; no Critical/High/Medium/Low findings
- Residual informational coverage: no direct rows for every mixed-case/duplicate/oversized citation variant; source review found the ratified grammar deterministic and fail-closed
- Custody: clean local branch `codex/biostack-remediation-p04`; not pushed, merged, deployed, or released

## P08 — Collective outbound authorization

- Base: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Candidate: `47c2be0358e7ca024b524c7693af8b06846671d6`; tree `24f59b756f09b5895c7bd70319f5eaad8764e38a`; sole parent is the pinned base
- Changed paths: exactly all eight AF-P08 files; `git diff --check`, ancestry, and clean status passed
- Boundary: authorization completes before submit request, named client, API client, headers/body, or send construction; denied/error/authorized local-handler counts are exactly `0/0/1`
- Real composition: current-user plus existing `ConsentGate` integration passed `1/1`, with zero sends before consent and one after current consent
- Verification: target `3/3`; adjacent `6/6` and `26/26`; Application `612 passed / 5 skipped`; API `378/378`; full solution `2,021 passed / 5 expected skips / 0 failed`
- Rework custody: reviewer A's false-green finding was fixed with explicit degraded-envelope and no-client-borrow assertions; the byte-identical verified tree was normalized into the one-commit candidate, while prior history remains at `codex/biostack-remediation-p08-review-history-20260902`
- Adversarial reviewer A: ACCEPT, no blocking findings
- Adversarial reviewer B: ACCEPT, no blocking findings; independent full solution green
- Defensive security review: ACCEPT; SG-OUTBOUND PASS; SG-SCOPE PASS; no Critical/High/Medium/Low findings
- Ratified residuals: one authorization decision per `RunAsync`; no authenticated-user-to-intent tenant/actor binding; no mid-poll consent re-check; no endpoint-wide coverage conclusion
- Custody: clean local branch `codex/biostack-remediation-p08`; not pushed, merged, deployed, enabled, or released

## Q04 — Frontend dependency readiness

- Base/head: `339f259b1a467034db4f57cf9d774c292f11b53a`; no repository changes
- First run: `npm ci --offline` succeeded and the main-based route test passed 2/2, but npm 11.6.2 consumed the required worker flags
- Fresh review: REJECT; the first `READY` token is void and does not unblock P07
- Procedural repair: goal commit `f5af63ea313d4c5dcb4cebd221d9df1677f29d28` uses the installed local Vitest entrypoint through `node`, emits exact argv, rejects unknown-option/config warnings, and requires a preserved external transcript
- Second run: direct-node argv was exact and the live terminal displayed 1/1 file and 2/2 tests passing, but `Start-Transcript` omitted native npm/Vitest output; outcome `BASELINE_TEST_FAILED`; P07 remained blocked
- Final attempt: `READY`; `npm ci --offline` exit 0; exact direct-node worker argv; one file and 2/2 tests passed; no unknown config/option warnings; exact cleanup and Git equality green
- Final transcript: `3529` bytes; SHA-256 `F185ADCD538B813814C73ADBA67FC43B9F0D0A9FB2A7ACB35BF7DFFC53383633`
- Fresh review: ACCEPT, no blocking findings
- Current status: Q04 accepted; P07 candidate `c35df75be835d26b4c37618874cf502335b9a24c` is review-complete and held unmerged

## Q01 — Collective outbound behavior

- Evidence commit: `83d1235b3a964ddf7130f67900f4dcd3b38dcf42`, sole parent at the pinned base, exactly AF-Q01
- Outcome: `REPRODUCED_CONFIG_ONLY_TRANSMISSION`
- Local fake observed one `POST /api/collective/live-runs`; synthetic objective and identity sentinels crossed; no caller-specific authorization/consent input reached the adapter
- Verification: targeted 1/1 intended failure; adjacent 6/6 passed; application aggregate gained exactly the intended failing test and no unrelated failure
- Transcript SHA-256: `40AFC7C374BD4A393532823266CD8A54A4009AE69786C1A2F399CE5046A4AC66`
- Fresh review: ACCEPT; endpoint authentication and processor status remain explicit non-conclusions
- Current status: evidence branch clean and unmerged; P08 candidate `47c2be0358e7ca024b524c7693af8b06846671d6` is review-complete and held unmerged

## P07 — Frontend authenticated-consented relay

- Base: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Candidate: `c35df75be835d26b4c37618874cf502335b9a24c`; tree `88241d74bb42dadfcfb9fab3c7d01a377ff82eac`; sole parent is the pinned base
- Changed paths: exactly all three AF-P07 files; diff check, ancestry, generated-root cleanup, and clean status passed
- Boundary: exact trusted backend consent GET with only one named session cookie, manual redirects, no-store, 2-second abort, 16,384-byte streamed bound, strict seven-field response, and `accepted === true` before provider construction
- Call matrix: eight denial tests retain zero provider calls; the sole positive performs exactly one consent call and one provider call; session, decoy cookies, and inbound authorization never reach the provider
- Verification: target `2 files / 10 tests`; research directory `7/26`; full frontend `136/989`; exact ambient lint equality `98 = 48 errors + 50 warnings`; focused AF lint zero; build exit `0`; backend consent `17/17`
- Rework custody: first review found whitespace around the named cookie's `=` was normalized into authority; the parser and retained regression now reject both variants. Rejected commit `842fb33749290dcd96a12350bc530312f080c652` remains at `codex/biostack-remediation-p07-review-history-20260902`
- Adversarial reviewer A: ACCEPT, no blocking findings
- Adversarial reviewer B: ACCEPT, no findings
- Defensive security review: ACCEPT; SG-OUTBOUND PASS; SG-SCOPE PASS; no Critical/High/Medium/Low findings
- Traceability: exact candidate runtime results are in the signed builder handoff rather than a standalone transcript; reviewers accepted this under the shaped contract
- Residuals: configured backend origin is an operator-controlled credential-receiving trust root; after-headers stream abort and several malformed response modes are statically fail-closed but not separate runtime rows
- Custody: clean local branch `codex/biostack-remediation-p07`; not pushed, merged, deployed, enabled, or released

## P07 — Clean-base lint evidence

- Evidence worktree: `codex/biostack-remediation-p07-lint-baseline` at exact base/tree; no source change or commit
- Offline install: `npm ci --offline` exit `0`; no registry fallback
- Full base lint: exit `1`; exactly `98` ambient findings (`48` errors, `50` warnings) across `49` paths outside AF-P07
- Focused base lint: direct local ESLint on `route.ts` and `suggest.route.test.ts` exit `0`, zero findings
- Transcript: `97,999` bytes; SHA-256 `EDBCCF6EDA244649D6EB99B08E589F8E4887E0118B14BBDE818BA19A2380E181`
- Raw lint: `67,767` bytes; SHA-256 `21F740C507C0E255CBBCAE38726828C668CC5D638E32F35DAA915ADB2D2D3EA3`
- Cleanup/custody: generated roots final-absent; package/lock blobs unchanged; exact base/tree and clean status restored
- Disposition: P07 full lint becomes an ambient-delta receipt, while all three candidate AF-P07 files must pass focused lint with zero findings; no Gate 1 product decision or authority changed

## Q02 — Endpoint gate inventory

- Baseline: adjacent 21 passed; API project 377 passed
- Bounded runtime inventory: all 11 exact rows observed; expected `INCONCLUSIVE_NO_COMPLETE_RECOMMENDATION_SURFACE_MARKER` emitted
- Rejected v1 evidence: commit `dea721288521a5789350e2398d0d7292b01ad10f` and transcript SHA-256 `767AA4D74D3F7E71CB8CED33BF411B91B4E193EC6BE0DE4B90D487B8C1EC9375` are preserved but not accepted; review found pre-teardown outcome emission and incomplete native transcript evidence
- V2 disposition: `TERMINALLY BLOCKED`; after fresh Step 0 and clean base-rooted worktree creation, the first Baseline procedure invocation failed before transcript creation because `New-Item` in the installed PowerShell does not expose `-LiteralPath`
- V2 custody: no test ran, AF-Q02 remains absent, no transcript or commit exists, Git status is clean at base `339f259b1a467034db4f57cf9d774c292f11b53a` / tree `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`, temporary-database count is zero, and v1 custody is unchanged
- Final Q02 conclusion: endpoint-wide gate coverage remains inconclusive; no v3 or retry is authorized under this goal

## Q03 — Regex exploitability

- Outcome: `INCONCLUSIVE_NO_DETERMINISTIC_SEAM`
- Inventory: five synchronous regex operations, zero cancellation observations, zero finite-timeout/non-backtracking configuration matches
- Target: AF-Q03 absent `0 -> 0 (+0)`; no payload, timing oracle, fuzz/stress, repository change, or commit
- Transcript: `7193` bytes; SHA-256 `5212639568C112446E3258A865A2894D797C3481903A8425730B74F4EC8EB45C`
- Fresh review: ACCEPT; practical exploitability remains unproved and unexcluded

## P03 — Interaction safety

- Rejected candidate: `7e8087a00722e38c2a090d49826fa6289c880356`; direct child of the pinned base; exact three-file AF-P03; clean local worktree
- Accepted candidate: `a69d945a3cd8a3655911707031386441fc55079f`; tree `7205d5ea11148eac865534cf5ae545634eb84be5`; direct child of the rejected candidate; exact three-file aggregate AF-P03; clean local worktree
- Amendment-05 rework: one production condition requires `edge.GraphArtifactId == artifact.Id`; one exact synthetic regression proves cross-artifact graph/hash denial and eligible stored-hint fallthrough
- Verification: target `11/11`; adjacent `20/20`; Application `620 passed / 5 skipped`; focused API `8/8`; full solution Domain 7, Application 620+5, verifier 158, API 377, KnowledgeWorker 866; zero failures; independent rerun matched all counts
- Amendment 04 fixture custody: API diff is exactly one `provisional` to `reviewed` value
- Fresh adversarial review of exact accepted candidate/tree: ACCEPT; no Critical, High, Medium, or Low actionable findings
- Residual: deterministic mock models active-artifact rotation rather than a live concurrent database publication; direct store inspection confirms independently resolved active-artifact lookup
- Disposition: review-complete local candidate, unmerged/unpushed/undeployed/unreleased; rejected parent and diagnostic history preserved; Gate 3 ungranted

## P05 — Sidecar terminal-state custody

- Accepted candidate: `2f4a092fb7f0ec534996e6ad1116e8b50d612b77`; tree `1c0f8406cde32eefeba4411ecde6df3bb680fcf4`; base ancestor pinned; clean local worktree
- Rejected parent: `923900c66d7046c282d397ba60d85b9eebcf1a58`; preserved in candidate history and never accepted for merge custody
- Changed paths: exactly the three AF-P05 files; timeout is one atomic `failed`/`execution_timeout` snapshot, and store updates prevalidate all keys before mutation
- Verification: target `10/10`; adjacent `38 passed / 1 expected skip`; aggregate `48 passed / 1 expected skip`; exact eight pinned lint findings unchanged
- Adversarial reviewer A: ACCEPT; independent offline target/adjacent/aggregate rerun green; no findings or coverage gaps
- Adversarial reviewer B: ACCEPT; independent offline rerun plus 200-iteration three-way race produced only complete winner snapshots; no actionable findings
- Defensive security review: ACCEPT; SG-SIDECAR PASS; SG-SCOPE PASS; no blocking findings
- Non-blocking residuals: trusted in-process callers still receive mutable `JobRecord` aliases; timeout/cancellation does not forcibly stop already-running provider work; both are outside ratified P05 scope
- Custody: accepted P05 hash may now be pinned into P06 shaping and final combined verification; local only, unmerged, unpushed, undeployed, unreleased

## P06 — Sidecar accepted-source cap

- Candidate: `c435caf3cf0cf95fb1bfbc51d2924ea960183957`; tree `942d51ae9b0605af66f8d1d4ec2d7d1745dd1cf3`; direct child of the pinned base; clean local worktree
- Changed paths: exactly AF-P06 (`workflows/executor.py` and retained `test_request_constraint_reproduction.py`)
- Boundary: non-negative ordered result-occurrence prefix is applied immediately after sequence return and before tool lists, provenance results, warnings, claims/source IDs, and status calculation; provider invocation itself remains explicitly outside this seam
- Standalone verification: target `5/5`; adjacent `38 passed / 1 expected skip`; aggregate `43 passed / 1 expected skip` from 44; exactly five pinned executor lint findings
- Combined custody: detached HEAD is exact accepted P05 `2f4a092fb7f0ec534996e6ad1116e8b50d612b77`; staged diff is exactly AF-P06 and byte-identical to P06 patch SHA-256 `B2882552D186668F800F1E943EEF831C73DF939955208C8625ADD122C34E57CC`
- Combined verification: targets `15/15`; adjacent `38 passed / 1 expected skip`; aggregate `53 passed / 1 expected skip` from 54; exactly thirteen pinned combined lint findings
- Adversarial reviewer A: ACCEPT; no Critical/High/Medium/Low findings; independent standalone and combined reruns green
- Adversarial reviewer B: ACCEPT; no actionable findings; additional blank/unmatched/excess/failed/discarded hostile probes passed
- Defensive security review: ACCEPT; SG-SIDECAR PASS; SG-SCOPE PASS; no Critical/High/Medium/Low findings
- Informational residual: accepted result arguments are stored directly and future nested mutable structures may merit separate copy/redaction hardening; no discarded data leaked in current scope
- Custody: local only, unmerged, unpushed, undeployed, unreleased; combined verification worktree preserved for evidence; Gate 3 ungranted

## Gate custody

P01-P08 are review-complete source candidates whose exact aggregate blobs form staged integration tree `17b3ff60c74098f67edec222f1854f6486df7fe5` in the preserved detached integration worktree; the full backend, sidecar, frontend, build, lint, scope, and byte-parity chain is green. Fresh independent integration review ACCEPTED the exact tree with no blocking findings and mandated aggregate-patch normalization for P03/P05 rejected-parent custody. Q01, Q03, and Q04 have accepted inquiry reviews; Q02 is terminally stopped and remains inconclusive. Amendments 01-05 were explicitly ratified on 2026-09-02. On 2026-09-03 the developer ratified `gate-3-request.md` at goal commit `ebb3318e3fb5f1b5186d23736719150d28a5fede` as written. The exact normalized-branch push, PR, merge-commit, API/frontend deployment, monitoring, guarded rollback, and Stage-F authority is active; all request stop conditions and exclusions remain controlling, including no research-sidecar deployment or diagnostic cleanup.
