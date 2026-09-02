---
parcel: P04
title: Evidence provenance
status: active-awaiting-evidence-matrix-restatement
goal: biostack-algorithm-remediation
initiative: BioStack Algorithm Remediation
project: BioStack backend
wave: Wave 1 - independent fixes
risk: critical boundary
routing: frontier implementation plus SG-EVIDENCE
branch: codex/biostack-remediation-p04
worktree: 'C:\Users\clint\.codex\worktrees\biostack-remediation-p04\BioStack'
base: 339f259b1a467034db4f57cf9d774c292f11b53a
diagnostic_commit: 2c9d6cabce4bad853a63365c03e55c2fc612cb70
---

# P04 - Evidence provenance

Evidence-matrix re-lint: **PASSED 2026-09-02 after a pre-edit builder stop.** A fixed `LogFileName` on `dotnet test backend/BioStack.sln` was overwritten once per test project and could not prove aggregate deltas. P04 therefore uses five uniquely named per-project TRX receipts for base/candidate counters plus a separate solution orchestration command. This changes no product decision, invariant, Allowed File, count expectation, security gate, or Gate authority. The initial target `0`, adjacent `36/36`, and solution exit `0` remain valid preliminary receipts; the builder must add the complete five-project base vector before any edit.

## Goal and outcome

Remediate R-EVID-01 and R-EVID-02 from the pinned `main` base. An artifact may enter the scientific-research review-staging lane only when its status represents candidate output, and an approved review may pass `EvidenceGate` only when its citation metadata contains at least one stable external source locator. Preserve the two diagnostic scenarios as regressions, add the plan-review harness repairs, make the smallest production changes in AF-P04, and return a committed local candidate plus deterministic evidence for independent review.

This parcel does not authorize a push, pull request, merge, deployment, provider enablement, publication, release, or diagnostic-branch mutation. Gate 3 is human-only and remains ungranted.

## Authority, dependencies, and gates

- Controlling goal: `biostack-algorithm-remediation`.
- Controlling decisions: ratified D1-D14 in `plugins/foreman-line/docs/goals/biostack-algorithm-remediation/charter.md`; P04 depends specifically on D1, D2, D7, D10-D14.
- Remediation base: `339f259b1a467034db4f57cf9d774c292f11b53a` from `main` / `origin/main`.
- Diagnostic evidence source: `2c9d6cabce4bad853a63365c03e55c2fc612cb70`. Import the test-file contents into the remediation branch; do not merge, rebase, cherry-pick, rewrite, or delete the diagnostic branch or its history.
- Plan-review dependency: F5 in `plan-review-findings.md` must be satisfied by direct no-upsert assertions for every ineligible status, an independent internal-only attribution denial, and positive controls for candidate staging and external locators.
- Original Gate 1 and Narrow Gate 1 Amendment 01 are ratified. Amendment 01 supplies the exact candidate-status allowlist, locator grammar, and rejection codes below.
- Gate 2 is contingent: coordinator lint must pass against the amended charter, `main` and the locally verified `origin/main` ref must remain at the pinned base, the named isolated branch/worktree must start from that base, and the builder's Step 0 restatement must be accepted before edits.
- SG-SCOPE and SG-EVIDENCE are mandatory. Two fresh adversarial reviews and a separate defensive security review are required on the exact final candidate commit. A review failure blocks Gate 3.
- Gate 3 remains ungranted. The parcel candidate stays local and unmerged until the full goal chain is green and the human approves the exact commit/merge set and release action.

No Q-lane or other implementation parcel is a code dependency. P04 owns a disjoint production-file set, but aggregate verification and final integration remain coordinator-owned.

## Exact Allowed Files - AF-P04

The builder may edit exactly these files:

1. `backend/src/BioStack.Application/ScientificResearch/ScientificResearchCandidateStagingService.cs`
2. `backend/src/BioStack.Application/Services/EvidenceGate.cs`
3. `backend/tests/BioStack.Application.Tests/ScientificResearch/EvidenceProvenanceReproductionTests.cs`

The third path is absent on the pinned base and must be reconstructed from the diagnostic commit, then repaired in place. No substitute or nearby file is authorized.

## Forbidden and out of scope

- Every repository path outside AF-P04 is forbidden, including `AdminEndpoints.cs`, scientific-research contract/client/DI files, project/solution files, adjacent existing tests, sidecar files, frontend files, schemas, migrations, configuration, packages, lockfiles, workflows, and goal documents.
- No public API redesign, endpoint/status-code expansion, persistence/schema change, canonical-knowledge write, new promotion lifecycle, broad refactor, dependency upgrade, unrelated cleanup, or config/provider enablement.
- Do not weaken, delete, rename away, or skip the two original diagnostic scenarios. Do not replace direct store assertions with a later gate-only assertion.
- No secrets, protected or production data, payload dumps, cloud resources, production databases, external providers, registries, or unapproved network access.
- Tests use only synthetic artifacts, in-memory stores, and deterministic local values. No provider process, sidecar process, Docker container, or HTTP listener is needed.
- Do not stage, commit, push, merge, open a pull request, deploy, publish, release, or clean any diagnostic branch until the relevant authority is explicit. The builder may create the one local candidate commit requested by the coordinator only after verification; reviewers remain read-only.

If any required change falls outside AF-P04, stop for a charter/spec amendment. Do not substitute a nearby file.

## Verified existing patterns on the pinned base

- `ScientificResearchCandidateStagingService.StageFromJobAsync` fetches a `ScientificResearchArtifact`, checks only for an existing record, and then calls `ITranscriptCandidateReviewStore.UpsertAsync`; it has no artifact-status eligibility guard.
- The staging service currently builds `citations` solely from `research_job:`, `workflow:`, optional `tooluniverse:`, and `tool:` labels. Those are useful provenance fields but are not external source locators.
- `EvidenceGate` already fails closed on deterministic fixtures, review state, promotion target, evidence tier, mechanism requirements, and unsafe recommendation language. Its citation check currently accepts any nonblank string.
- `ResearchJobStatusCode` has ten values. The live sidecar executor identifies `PendingReview` as the successful candidate-output path and `Partial` as partial candidate output; it maps all-tools-failed and no-tools-executed paths to `Failed`. Therefore P04 uses an explicit allowlist of `PendingReview` and `Partial`; `Queued`, `ResolvingIdentity`, `GatheringEvidence`, `Normalizing`, `Completed`, `Failed`, `Cancelled`, and `RejectedByPolicy` are otherwise ineligible to enter staging.
- `ScientificResearchProviderException` is an existing `InvalidOperationException` with an `ErrorCode`, and the existing admin stage endpoint already handles that exception family. The staging service can fail closed with the existing exception type without an API-file change.
- `ScientificResearchCandidateStagingServiceTests.StageFromJobAsync_creates_pending_non_canonical_review_record` uses a synthetic `Partial` artifact and asserts idempotent one-upsert staging.
- `EvidenceGateTests` already uses stable `http`/`https` values in its valid requests and exercises the existing tier, review, target, mechanism, fixture, and safety-language behavior.
- The diagnostic commit adds only `EvidenceProvenanceReproductionTests.cs`; it does not contain a production fix. Its failed-artifact test currently allows the upsert and observes only the later open gate, which is the F5 harness defect this parcel must repair.

## Exact D7 contract

The ratified decision is controlling:

> Failed, cancelled, rejected, queued, or otherwise non-candidate research artifacts cannot enter the review-staging lane. Internal job/workflow/tool labels are provenance, not citations. Opening the EvidenceGate requires at least one stable external source locator (`https`, `http`, `doi`, or `pmid`) in addition to the existing tier/review checks.

P04 binds that decision to the current contracts as follows:

1. `PendingReview` and `Partial` are the only stageable `ResearchJobStatusCode` values. Every other current enum value is denied before `GetByArtifactIdAsync` or `UpsertAsync` can create or return a staged record.
2. An ineligible artifact causes `StageFromJobAsync` to throw the existing `ScientificResearchProviderException` with the exact error code `artifact_not_stageable`. The exception must not include payloads, protected data, failure details, tool outputs, or other sensitive metadata.
3. Denial is direct and observable at the store boundary: `UpsertAsync` call count remains zero for each of `Failed`, `Cancelled`, `RejectedByPolicy`, `Queued`, `ResolvingIdentity`, `GatheringEvidence`, `Normalizing`, and `Completed`.
4. Existing idempotency remains intact for eligible candidates: a valid candidate stages once and a repeated request returns the existing record without a second upsert.
5. `research_job:`, `workflow:`, `tooluniverse:`, and `tool:` entries remain provenance labels. Alone, together, or repeated, they never satisfy source attribution and never open the gate.
6. The existing `citations` metadata key remains required and nonblank. In addition, at least one trimmed pipe-delimited citation entry must be a stable external locator:
   - an absolute `http` or `https` URI with that exact scheme and a nonempty host;
   - `doi:` followed by a DOI-shaped value beginning `10.`, containing a `/`, and containing no whitespace; or
   - `pmid:` followed by one or more ASCII digits.
7. Scheme/prefix matching is case-insensitive, but internal-label prefixes are not recursively reinterpreted. For example, `tool:https://example.test/source` remains an internal tool label and does not qualify.
8. A valid external locator may coexist with internal provenance labels. One qualifying locator is sufficient only after every existing fixture, review-state, target, tier, mechanism, and safety check also passes.
9. Locator recognition is syntax-only and deterministic. It must never resolve a URL, DOI, or PMID or perform network I/O.

If the builder concludes that a status outside `PendingReview`/`Partial` must stage, or that another locator scheme or delimiter is required, stop for a narrow decision rather than widening D7.

## Required regression harness

Preserve the original class `EvidenceProvenanceReproductionTests` and, where viable, these exact original test names:

- `Failed_research_artifact_must_not_reach_an_open_evidence_gate`
- `Synthetic_job_identifier_must_not_satisfy_source_attribution`

Repair and extend the harness with these exact scenario groups:

1. The preserved failed-artifact scenario must now assert that `StageFromJobAsync` denies the artifact and that the in-memory store's `UpsertCount` is exactly zero. It must not first stage the artifact and rely only on `EvidenceGate` denial.
2. A theory covers `Cancelled`, `RejectedByPolicy`, `Queued`, `ResolvingIdentity`, `GatheringEvidence`, `Normalizing`, and `Completed`; every row independently asserts the same direct denial and zero upserts. Together with the preserved failed scenario, all eight ineligible statuses are bound.
3. The preserved synthetic-job-identifier scenario evaluates `EvidenceGate` independently of staging and asserts that internal-only job/workflow/tool labels close the gate with the new external-locator rejection code.
4. A valid-candidate control covers both `PendingReview` and `Partial`, proving each stages, produces a pending non-canonical record, and upserts exactly once. Repeated staging for one eligible row must preserve the existing idempotency behavior.
5. A valid-locator theory covers `https://example.test/study-1`, `http://example.test/study-1`, `DOI:10.1000/182`, and a whitespace-padded `PMID:12345678`. Each request also satisfies the existing review/tier/target checks and must open the gate. At least one row coexists with internal provenance labels to prove labels are ignored rather than globally forbidden.
6. An invalid-locator theory covers exactly eight independently denied rows: unsupported `ftp://example.test/study-1`; prefix-smuggled `tool:https://example.test/study-1`; hostless `https:///study-1`; empty `doi:`; DOI `doi:10.1000` without `/`; DOI `doi:10.1000/has space` with whitespace; empty `pmid:`; and nonnumeric `pmid:12x`. Each row returns `missing_external_source_locator` and performs no I/O.

The final target census is 23 discovered cases: 1 preserved failed case, 7 other ineligible-status rows, 1 preserved internal-only denial, 2 valid-candidate rows, 4 valid-locator rows, and 8 invalid-locator rows. This is +23 cases relative to the pinned base, where the target file is absent, and +21 cases relative to the two-case diagnostic snapshot.

## Smallest production change

- In `ScientificResearchCandidateStagingService`, add one explicit fail-closed status allowlist/check immediately after the artifact is fetched and before any review-store lookup or upsert. Reuse the existing provider exception family; do not change endpoint code or the shared enum.
- In `EvidenceGate`, retain the current ordered checks and strengthen the existing citation check with one small deterministic helper that recognizes only the D7 locator forms above. Return the exact rejection `missing_external_source_locator` when citations are present but contain no qualifying external locator; preserve `missing_citations` for absent/blank citation metadata.
- Do not change staging metadata to fabricate a locator. Internal labels remain provenance, and only real locator syntax supplied in citation metadata may satisfy the gate.
- Avoid moving unrelated logic, renaming public types, changing supported tiers, altering the doctrine sanitizer, or reformatting untouched regions.

## Security gates

### SG-SCOPE

- Exact AF-P04 only; synthetic fixtures only.
- No secrets, protected data, payload dumps, production/cloud/database access, providers, registry access, or external network.
- Verify base ancestry, diff scope, `git diff --check`, and final clean status.
- A test process attempting an external connection is a failure even if the request is intercepted or later succeeds.

### SG-EVIDENCE

- Ineligible statuses are rejected before any staging-store read/upsert can admit a candidate.
- Internal provenance labels never count as citations, including label values that embed locator-looking text.
- Every supported external locator form has a positive control; malformed/empty prefixes and unsupported schemes remain denied.
- Existing tier, review-state, target, mechanism, fixture, and safety checks remain mandatory and ordered fail-closed.
- Review for bypasses involving mixed case, whitespace, multiple pipe-delimited entries, prefix smuggling, embedded `http` text, `tool:https://...`, empty DOI/PMID values, malformed hosts, and duplicate labels.
- No locator is resolved and no network path is introduced.

## Deterministic verification and count receipts

Run commands from the P04 worktree root in PowerShell. `rtk` is preferred when available; if it is unavailable, record that fact once and run the exact underlying command. Do not restore packages or contact a registry. If the pinned assets are unavailable for `--no-restore`, stop as `ENVIRONMENT_BLOCKED`; do not remove `--no-restore`.

Use an external results directory so receipts do not alter repository scope:

```powershell
$p04ReceiptDir = Join-Path ([System.IO.Path]::GetTempPath()) 'biostack-p04-receipts'
New-Item -ItemType Directory -Force -Path $p04ReceiptDir | Out-Null
```

Before importing the diagnostic test, run and retain the base receipts:

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.ScientificResearch.EvidenceProvenanceReproductionTests" --logger "trx;LogFileName=p04-base-target.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Services.EvidenceGateTests|FullyQualifiedName~BioStack.Application.Tests.ScientificResearch.ScientificResearchCandidateStagingServiceTests" --logger "trx;LogFileName=p04-base-adjacent.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/tests/BioStack.Domain.Tests/BioStack.Domain.Tests.csproj --no-restore --logger "trx;LogFileName=p04-base-domain.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --logger "trx;LogFileName=p04-base-application.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj --no-restore --logger "trx;LogFileName=p04-base-verifier.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --logger "trx;LogFileName=p04-base-api.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --logger "trx;LogFileName=p04-base-knowledge-worker.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/BioStack.sln --no-restore --verbosity minimal
```

Expected pinned-base counts are target `0`, adjacent `36` passed cases, and the five-project vector Domain `7`, Application `609 passed / 5 skipped`, verifier `158`, API `377`, and KnowledgeWorker `866`, with zero failures. The builder must record every actual TRX counter and stop before edits if any discovery count differs. A test-filter no-match result for the absent base target class is recorded as zero, not represented as a product pass. The already captured overwritten solution TRX is explicitly non-evidence and must not be cited.

After implementation, run the exact targeted and adjacent commands:

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.ScientificResearch.EvidenceProvenanceReproductionTests" --logger "trx;LogFileName=p04-new-target.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Services.EvidenceGateTests|FullyQualifiedName~BioStack.Application.Tests.ScientificResearch.ScientificResearchCandidateStagingServiceTests" --logger "trx;LogFileName=p04-new-adjacent.trx" --results-directory $p04ReceiptDir --verbosity minimal
```

Expected final counts are target `23/23` passed and adjacent `36/36` passed, with zero failed, skipped, or not-executed cases. Then run the candidate five-project receipt vector and solution orchestration command:

```powershell
dotnet test backend/tests/BioStack.Domain.Tests/BioStack.Domain.Tests.csproj --no-restore --logger "trx;LogFileName=p04-new-domain.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --logger "trx;LogFileName=p04-new-application.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj --no-restore --logger "trx;LogFileName=p04-new-verifier.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --logger "trx;LogFileName=p04-new-api.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --logger "trx;LogFileName=p04-new-knowledge-worker.trx" --results-directory $p04ReceiptDir --verbosity minimal
dotnet test backend/BioStack.sln --no-restore --verbosity minimal
```

The final five-project vector must differ from base only in Application, which has exactly 23 more discovered/executed/passed cases; every other project's counters remain identical, and no project increases failed, skipped, not-executed, aborted, or error counters. The separate solution command must exit `0`. Read each uniquely named receipt from its `TestRun/ResultSummary/Counters` element and record the exact path, command, exit code, total, executed, passed, failed, skipped/not-executed, and timestamp. Do not sum an overwritten solution TRX, source counts, or intermediate console lines as the final receipt.

Run the scope and ancestry checks:

```powershell
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git diff --check 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --name-only 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git status --short --branch
```

The name-only output must equal AF-P04 exactly, with no extra tracked or untracked residue. Record that all tests used synthetic local data and that no external connection, provider, sidecar, cloud resource, or production data source was configured or contacted.

## Evidence package

The builder's completion evidence must include:

- exact candidate commit and verified base ancestry;
- `git diff --stat`, `git diff --check`, and the exact AF-P04 name-only list;
- the diagnostic source commit and confirmation that its branch remains preserved and unmerged;
- base/new target, adjacent, and solution TRX paths and counters, plus exact commands and exit codes;
- explicit no-network/no-provider/no-production-data receipt;
- direct zero-upsert results for every ineligible status;
- valid `PendingReview`/`Partial` staging results and all four locator controls;
- changed-file summary, implementation rationale, residual risk, and any environment limitation;
- final `git status --short --branch` and a session handoff.

Builder tests are necessary but not sufficient. The coordinator must inspect the exact candidate diff and receipts before review dispatch.

## Review plan

After a green candidate commit, dispatch three fresh, read-only reviewers against that exact commit:

1. Adversarial reviewer A: D7 semantic custody, all status rows, original-scenario preservation, smallest-change discipline, and adjacent regression risk.
2. Adversarial reviewer B: test-harness independence, direct no-upsert proof, positive controls, count receipts, false-green opportunities, and AF-P04 scope.
3. Separate defensive security reviewer: SG-EVIDENCE and SG-SCOPE, promotion bypasses, provenance/citation confusion, locator-parser smuggling and malformed-input behavior, information leakage, and any network/provider path.

Reviewers do not edit, fix, stage, or commit. Every finding is dispositioned as fix, accept-as-documented, or informational. Any rework produces a new candidate commit and invalidates all earlier acceptances; rerun verification and obtain three fresh reviews. Security-review failure blocks Gate 3 unless the human explicitly names and accepts the residual risk.

## Collision risk and sequencing

- At shaping time, `codex/biostack-remediation-p04` and `C:\Users\clint\.codex\worktrees\biostack-remediation-p04\BioStack` do not exist.
- AF-P04 is disjoint from P01-P03 and P05-P07. The known code-collision risk is low while each parcel preserves exact ownership.
- Semantic collision risk is moderate because `EvidenceGate` is consumed by promotion and preview services. The named adjacent suite and aggregate solution run are mandatory even though those consumers are forbidden edits.
- If `origin/main` moves, if another branch changes an AF-P04 file, or if the named branch/worktree appears under another owner before dispatch, stop for coordinator reconciliation. Do not rebase or combine work silently.
- P04 may complete independently, but its commit remains unmerged until the full goal chain and exact human Gate 3 are complete.

## Step 0 - restate and stop

Before any builder edit, the builder must send the coordinator one restatement containing all of the following, then stop for explicit confirmation:

1. goal, initiative, P04 identifier, Wave 1, and critical-boundary risk;
2. branch `codex/biostack-remediation-p04`, worktree `C:\Users\clint\.codex\worktrees\biostack-remediation-p04\BioStack`, and base `339f259b1a467034db4f57cf9d774c292f11b53a`;
3. the three exact AF-P04 paths and the rule that every other file is forbidden;
4. the exact D7 status allowlist/deny set, internal-label rule, four locator forms, and preserved existing EvidenceGate checks;
5. the two original test names, repaired direct no-upsert assertions, positive/negative locator controls, expected `23` target and `36` adjacent counts, and exact commands;
6. SG-SCOPE, SG-EVIDENCE, synthetic/offline/no-provider/no-network limits, review depth, Gate 3 prohibition, and all stop rules.

Silence is not confirmation. Any discrepancy in the restatement blocks edits.

## Handoff contract

The builder hands the coordinator one concise record containing:

- `P04`, branch, worktree, base, candidate commit, and parent commit;
- exact changed files and confirmation they equal AF-P04;
- concise production/test changes and preserved original test names;
- exact command/exit-code/count receipt table and TRX paths;
- scope/ancestry/status outputs and the no-network receipt;
- open findings, residual risks, or a precise `none` statement;
- explicit statements: local candidate only, not pushed, no PR, not merged, not deployed/released, Gate 3 ungranted, diagnostic branches untouched;
- reviewer-ready diff command and any evidence paths needed by the two adversarial reviewers and separate security reviewer.

The coordinator verifies the handoff against Git and disk. A handoff claim is not acceptance by itself.

## Stop rules

Stop the parcel and report without widening scope if any of the following occurs:

- `origin/main` differs materially from the pinned base or base ancestry fails;
- a named AF-P04 file is absent, insufficient, or a required change touches any other file;
- Step 0 is not explicitly confirmed;
- an external locator, candidate status, delimiter, rejection behavior, or public contract is needed outside the exact D7 interpretation above;
- the invariant can pass only by weakening/deleting a regression, fabricating a locator, treating an internal label as a citation, or permitting an ineligible status;
- dependencies are unavailable under `--no-restore`, a test needs a provider/network/cloud/production/protected-data path, or an external connection is attempted;
- target/adjacent/base-new counts differ from the expected contract without a reconciled source explanation;
- a security finding cannot close inside AF-P04;
- the same tripwire or false-closure condition fires twice;
- any push, PR, merge, deployment, publication, release, or diagnostic-branch mutation is proposed before exact human Gate 3;
- the builder or a reviewer is asked to edit outside its authorized role.

BioStack does not contain the Foreman emitter/linter required for a `ShapingResult`. This spec intentionally creates no `ShapingResult` JSON. Coordinator lint passed after Amendment 01 ratification; dispatch remains contingent on the clean isolated worktree and confirmed Step 0.
