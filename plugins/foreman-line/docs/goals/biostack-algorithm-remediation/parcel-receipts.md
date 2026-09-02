# Parcel Receipts — BioStack Algorithm Remediation

Status: **live coordinator record; no merge or release authority**

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

## Q04 — Frontend dependency readiness

- Base/head: `339f259b1a467034db4f57cf9d774c292f11b53a`; no repository changes
- First run: `npm ci --offline` succeeded and the main-based route test passed 2/2, but npm 11.6.2 consumed the required worker flags
- Fresh review: REJECT; the first `READY` token is void and does not unblock P07
- Procedural repair: goal commit `f5af63ea313d4c5dcb4cebd221d9df1677f29d28` uses the installed local Vitest entrypoint through `node`, emits exact argv, rejects unknown-option/config warnings, and requires a preserved external transcript
- Second run: direct-node argv was exact and the live terminal displayed 1/1 file and 2/2 tests passing, but `Start-Transcript` omitted native npm/Vitest output; outcome `BASELINE_TEST_FAILED`; P07 remained blocked
- Final attempt: `READY`; `npm ci --offline` exit 0; exact direct-node worker argv; one file and 2/2 tests passed; no unknown config/option warnings; exact cleanup and Git equality green
- Final transcript: `3529` bytes; SHA-256 `F185ADCD538B813814C73ADBA67FC43B9F0D0A9FB2A7ACB35BF7DFFC53383633`
- Fresh review: ACCEPT, no blocking findings
- Current status: Q04 accepted; P07 shaping may proceed only after Amendment 01, and P07 remains subject to its own Step 0, verification, two adversarial reviews, security review, and gates

## Q01 — Collective outbound behavior

- Evidence commit: `83d1235b3a964ddf7130f67900f4dcd3b38dcf42`, sole parent at the pinned base, exactly AF-Q01
- Outcome: `REPRODUCED_CONFIG_ONLY_TRANSMISSION`
- Local fake observed one `POST /api/collective/live-runs`; synthetic objective and identity sentinels crossed; no caller-specific authorization/consent input reached the adapter
- Verification: targeted 1/1 intended failure; adjacent 6/6 passed; application aggregate gained exactly the intended failing test and no unrelated failure
- Transcript SHA-256: `40AFC7C374BD4A393532823266CD8A54A4009AE69786C1A2F399CE5046A4AC66`
- Fresh review: ACCEPT; endpoint authentication and processor status remain explicit non-conclusions
- Current status: evidence branch clean and unmerged; any production fix requires a new P08 production owner/invariant/Allowed Files, SG-OUTBOUND, narrow Gate 1 ratification, and contingent Gate 2 authority

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

## Gate custody

P01 and P02 are review-complete candidate commits, not approved merge commits. Q01, Q03, and Q04 have accepted inquiry reviews; Q02 is terminally blocked and remains inconclusive. Amendments 01 and 02 remain pending. Gate 3 remains ungranted, so no push, PR, merge, deployment, publication, or release is authorized.
