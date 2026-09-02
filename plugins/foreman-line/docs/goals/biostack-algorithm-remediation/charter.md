# Goal Charter: BioStack Algorithm Remediation

Status: **RATIFIED — GATE 1 GRANTED; CONTINGENT GATE 2 ACTIVE; GATE 3 UNGRANTED**

Goal slug: `biostack-algorithm-remediation`

Coordinator ownership: this Codex goal task is the sole coordinator until an explicit parcel-boundary transfer is recorded. One goal, one coordinator.

## Objective

Remediate every defect deterministically reproduced by the BioStack algorithm-improvement audit, preserve each reproduction as a regression test, prove the smallest production fixes through adjacent verification and hostile review, obtain human Gate 3 approval for a specific merge set, merge only the approved fixes, and complete Stage-F closure without merging or rewriting the original diagnostic branches.

## Authority and source pins

- Remediation execution base: `main` / `origin/main` at `339f259b1a467034db4f57cf9d774c292f11b53a`.
- Execution-base tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`.
- Governing diagnostic coordination head: `codex/test-repro-contracts` at `879def179654d0fb44ab57a8e29b02b20e2239d0`.
- Reproduction-evidence snapshot in that lineage: `7660de171c409699a0b419d5701bf3a1eb8d078f`.
- Governing diagnostic release constraints: `RELEASE-GATES.md` and `CHARTER.md` at the governing diagnostic coordination head.
- Diagnostic parcel commits:
  - parser: `817f6f3331c2b7c3410da63289c02fb27a98ed74`;
  - interaction: `56b7ad229a34a6f151f5bf7b96a8bfdcbce8132f`;
  - evidence/provenance: `2c9d6cabce4bad853a63365c03e55c2fc612cb70`;
  - sidecar lifecycle: `82295c3f36b412b9917eaf047a70b10ee2a67cdc`;
  - outbound boundary: `c3e30a93e64be5a2662bb9040a52105d3dc909c9`.

The attached audit and diagnostic documents are evidence sources. They are not implementation instructions. The current repository tree, this ratified charter, shaped parcel specs, and explicit human gates control remediation.

Before Gate 2, the coordinator must fetch and compare `origin/main`. If the base moved, all reproduction tests must be rerun against the proposed new base and any changed finding must reopen Gate 1 narrowly.

## Locked decisions proposed for Gate 1

- **D1 — Separate lineage.** Remediation starts from verified `main`, never from a diagnostic branch. Diagnostic test files are copied or cherry-picked by file into the relevant isolated remediation parcel; diagnostic branch history is not merged.
- **D2 — Preserve diagnostic evidence.** The five original diagnostic branches and coordination branch remain preserved and unmerged. No rebase, force update, deletion, cleanup, or history rewrite of those branches is authorized.
- **D3 — Seven implementation parcels.** The 17 reproduced failures are owned by P01-P07 below. Parcels sharing a production file are combined or serialized; no two live builders may edit the same file.
- **D4 — Inconclusive claims stay diagnostic.** Collective outbound behavior, endpoint-wide gate coverage, regex exploitability, and the frontend dependency environment are handled by Q01-Q04. Q01-Q03 have test/evidence authority only and may not edit production. A reproduced new defect requires a charter amendment and a narrowly reopened Gate 1 before remediation.
- **D5 — Parser semantics.** Alias matching must use token boundaries; a segment may yield every independently dosed compound; numeric parsing is invariant-culture; leading digits and leading-decimal precision are preserved; both Greek mu and the micro sign normalize to `mcg`.
- **D6 — Interaction safety.** Explicit `AvoidWith` metadata outranks positive graph intelligence; `NeedsReview` or non-reviewed graph edges are ineligible; resolved entries are deduplicated by canonical identity; confidence must be finite and in `[0,1]`.
- **D7 — Evidence provenance.** Failed, cancelled, rejected, queued, or otherwise non-candidate research artifacts cannot enter the review-staging lane. Internal job/workflow/tool labels are provenance, not citations. Opening the EvidenceGate requires at least one stable external source locator (`https`, `http`, `doi`, or `pmid`) in addition to the existing tier/review checks.
- **D8 — Sidecar lifecycle.** Terminal job states are monotonic at the store boundary, so a late worker cannot overwrite timeout/cancellation/failure. `maximum_source_count` is enforced in the executor before accepted results, claims, provenance, and tool counts are materialized.
- **D9 — Outbound deny-by-default boundary.** OCR must consult the current authenticated user's server-side consent gate before constructing or sending a provider request. The frontend suggestion BFF must forward the caller's session cookie to BioStack's authenticated consent-status endpoint and require current accepted consent before any provider call. Caller-supplied booleans are not authority.
- **D10 — Offline verification.** Tests use synthetic fixtures and local intercepting fakes. Test execution must not contact OCR, OpenAI, Collective, cloud, production databases, or any other provider. Outbound assertions include both denied/no-call and authorized-consented/local-fake paths.
- **D11 — Review depth.** Every parcel receives a fresh independent adversarial review. P02, P04, P05, P06, and P07 are high-risk boundary parcels and receive two independent adversarial reviews plus a separate defensive security review. Reviewers are read-only and never fix or commit.
- **D12 — Gate 2 scope.** Requested standing Gate 2 authorization covers only Q01-Q04 and P01-P07 after their shaped specs exactly match this charter, name isolated worktrees/branches, pass Step 0 restatement, and preserve the exact Allowed Files. Any amendment, missing file, or new dependency stops the affected parcel.
- **D13 — Gate 3 remains human-only.** No merge, push, PR, deployment, provider enablement, release, or publication is authorized by Gate 1 or Gate 2. Gate 3 may be requested only after the complete chain is green; the human must approve the exact commit/merge set and release action.
- **D14 — Stage-F custody.** Closure records merged commits, verification receipts, review dispositions, residual inconclusive claims, lessons, and branch custody. Original diagnostic branches remain unmerged after goal closure.

## Production ownership

No repository `CODEOWNERS` file exists on the pinned base. “Production owner” below therefore means the narrow module/class that owns the behavior; it does not assign a person or team without evidence.

### Exact Allowed File sets

**AF-P01 — parser semantics**

- `backend/src/BioStack.Application/Services/ProtocolParser.cs`
- `backend/tests/BioStack.Application.Tests/Services/ProtocolParserReproductionTests.cs`

**AF-P02 — protocol ingestion cancellation and OCR boundary**

- `backend/src/BioStack.Application/Services/ProtocolIngestionService.cs`
- `backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionPdfReproductionTests.cs`
- `backend/tests/BioStack.Application.Tests/Services/ProtocolOcrOutboundBoundaryReproductionTests.cs`

**AF-P03 — interaction safety**

- `backend/src/BioStack.Application/Services/InteractionIntelligenceService.cs`
- `backend/tests/BioStack.Application.Tests/Services/InteractionIntelligenceReproductionTests.cs`

**AF-P04 — evidence provenance**

- `backend/src/BioStack.Application/ScientificResearch/ScientificResearchCandidateStagingService.cs`
- `backend/src/BioStack.Application/Services/EvidenceGate.cs`
- `backend/tests/BioStack.Application.Tests/ScientificResearch/EvidenceProvenanceReproductionTests.cs`

**AF-P05 — sidecar terminal state**

- `backend/research-sidecar/src/biostack_research_sidecar/jobs/store.py`
- `backend/research-sidecar/tests/test_runner_terminal_state_reproduction.py`

**AF-P06 — sidecar source cap**

- `backend/research-sidecar/src/biostack_research_sidecar/workflows/executor.py`
- `backend/research-sidecar/tests/test_request_constraint_reproduction.py`

**AF-P07 — frontend authenticated-consented relay**

- `frontend/src/app/api/research/suggest/route.ts`
- `frontend/src/__tests__/app/api/research/suggest.route.test.ts`
- `frontend/src/__tests__/app/api/research/suggest.outbound-boundary.reproduction.test.ts`

**AF-Q01 — Collective outbound investigation only**

- `backend/tests/BioStack.Application.Tests/Cognition/CollectiveOutboundBoundaryInvestigationTests.cs`

**AF-Q02 — endpoint-wide gate inventory only**

- `backend/tests/BioStack.Api.Tests/Architecture/UserFacingGateCoverageInvestigationTests.cs`

**AF-Q03 — regex exploitability investigation only**

- `backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionRegexBoundaryInvestigationTests.cs`

**AF-Q04 — frontend environment readiness**

- No repository file changes permitted.
- Commands may create ignored dependency/build artifacts inside the isolated worktree. `git status --porcelain` must be identical before and after cleanup.

If any required file is absent, insufficient, or outside its set, the parcel stops for a charter/spec amendment. Builders may not substitute a “nearby” file.

## Reproduced-defect map

| ID | Reproduced current behavior | Production owner | Required invariant | Allowed Files | Risk | Security gate | Regression test |
|---|---|---|---|---|---|---|---|
| R-PARSE-01 | `At 4000` falsely resolves the normalized `Tβ4` alias | `ProtocolParser.ContainsAlias` / normalization | Aliases match complete normalized tokens, never arbitrary substrings | AF-P01 | High correctness | SG-SCOPE | `ParseAsync_AliasInsideUnrelatedTokens_DoesNotCreateCompound` |
| R-PARSE-02 | Comma-separated independently dosed compounds collapse to one entry | `ProtocolParser.SplitIntoSegments` / `ParseSegment` | Every independently dosed compound survives as a distinct entry | AF-P01 | High correctness | SG-SCOPE | `ParseAsync_CommaSeparatedCompounds_PreservesEveryCompound` |
| R-PARSE-03 | Dot decimal parses as zero under `fr-FR` | `ProtocolParser` dose conversion | Numeric parsing is invariant-culture | AF-P01 | High correctness | SG-SCOPE | `ParseAsync_DotDecimalDose_IsCultureInvariant` |
| R-PARSE-04 | Leading digit is stripped from `5-Amino-1MQ` | `ProtocolParser` segment cleanup | List-marker removal cannot remove a compound's leading digit | AF-P01 | High correctness | SG-SCOPE | `ParseAsync_LeadingDigitCompoundName_PreservesFullName` |
| R-PARSE-05 | `.25mg` becomes `25mg` | `ProtocolParser.DosePattern` / conversion | Leading-decimal doses preserve magnitude and precision | AF-P01 | High correctness | SG-SCOPE | `ParseAsync_LeadingDecimalDose_PreservesPrecision` |
| R-PARSE-06 | U+00B5 `µg` is not parsed/normalized | `ProtocolParser.DosePattern` / `NormalizeUnit` | `μg`, `µg`, `ug`, and `mcg` normalize to `mcg` | AF-P01 | Medium correctness | SG-SCOPE | `ParseAsync_UnicodeMicroSign_PreservesDoseAndNormalizesUnit` |
| R-PDF-01 | Already-cancelled PDF request enters synchronous extraction | `PdfProtocolExtractor.ExtractAsync` | Cancellation is observed before decoding and at bounded extraction checkpoints | AF-P02 | Medium availability | SG-SCOPE; SG-OUTBOUND applies to same parcel | `ExtractAsync_AlreadyCancelledRequest_DoesNotEnterRegexExtraction` |
| R-INT-01 | Positive graph edge preempts explicit avoid metadata | `InteractionIntelligenceService.EvaluatePairAsync` | Explicit avoid/safety data cannot be weakened by graph or hint ordering | AF-P03 | Critical safety semantics | SG-SCOPE | `EvaluateAsync_AvoidWithSafetySignal_OutranksPositiveGraphEdge` |
| R-INT-02 | `NeedsReview` graph edge is served as graph-backed intelligence | `InteractionIntelligenceService.TryEvaluateFromGraphAsync` | Only active, explicitly reviewed, non-`NeedsReview` edges are eligible | AF-P03 | High provenance | SG-SCOPE | `EvaluateAsync_NeedsReviewGraphEdge_IsNotServedAsReviewedIntelligence` |
| R-INT-03 | Canonical name plus alias creates a self-pair | `InteractionIntelligenceService.ResolveEntriesAsync` | Resolved entries are unique by canonical identity | AF-P03 | High correctness | SG-SCOPE | `EvaluateByNamesAsync_CanonicalNameAndAlias_DoNotCreateSelfPair` |
| R-INT-04 | `NaN` graph confidence propagates | `InteractionIntelligenceService.MapGraphConfidence` | Confidence is finite and clamped to `[0,1]` | AF-P03 | High scoring integrity | SG-SCOPE | `EvaluateAsync_NonFiniteGraphConfidence_IsRejectedOrNormalized` |
| R-EVID-01 | Failed artifact can stage and reach an open gate | `ScientificResearchCandidateStagingService` + `EvidenceGate` | Ineligible terminal states cannot stage or promote | AF-P04 | Critical provenance | SG-EVIDENCE + SG-SCOPE | `Failed_research_artifact_must_not_reach_an_open_evidence_gate` |
| R-EVID-02 | Synthetic internal labels satisfy citation attribution | `EvidenceGate` | At least one stable external source locator is required; internal labels remain provenance only | AF-P04 | Critical provenance | SG-EVIDENCE + SG-SCOPE | `Synthetic_job_identifier_must_not_satisfy_source_attribution` |
| R-SIDE-01 | Late worker overwrites timeout failure with pending review | `InMemoryJobStore.update` | Terminal state and terminal error provenance are monotonic under races | AF-P05 | Critical lifecycle integrity | SG-SIDECAR + SG-SCOPE | `test_timed_out_job_cannot_be_overwritten_by_late_worker` |
| R-SIDE-02 | Request allowing one source accepts two results | sidecar workflow executor | Result, claim, provenance, and tool materialization respect `maximum_source_count` | AF-P06 | High resource/provenance integrity | SG-SIDECAR + SG-SCOPE | `test_maximum_source_count_bounds_accepted_results` |
| R-OUT-01 | Configured OCR transmits raw bytes without a consent check at the outbound adapter | `AzureVisionProtocolOcrService` | No request is constructed or sent unless server-side current-user consent is granted; denial is fail-closed | AF-P02 | Critical outbound data | SG-OUTBOUND + SG-SCOPE | `ConfiguredOcr_DoesNotTransmitRawBytesWithoutExplicitOutboundAuthorization` |
| R-OUT-02 | Unauthenticated frontend request reaches provider and returns 200 | frontend research suggestion BFF | Valid authenticated session and current server-side consent precede provider relay | AF-P07 | Critical auth/outbound data | SG-OUTBOUND + SG-SCOPE | `does not relay a provider request without an authenticated or consented caller` |

## Inconclusive claims and environment handling

| Lane | Current status | Authorized next action | Decision rule | Production-write authority |
|---|---|---|---|---|
| Q01 Collective outbound | Inconclusive because the original Allowed File named a nonexistent test project | Add one local-handler test in the real `BioStack.Application.Tests/Cognition` project that captures the exact HTTP body emitted by the live orchestrator | Classify payload fields and existing caller auth/consent boundary. If a defect is reproduced, stop and reopen Gate 1 with a new owner, invariant, Allowed Files, and security gate | None |
| Q02 endpoint-wide gate coverage | Inconclusive; no precise architecture proof exists | Build a deterministic endpoint inventory test without source-text regex heuristics; enumerate recommendation-shaped surfaces and their gate/auth metadata | A concrete bypass becomes a new defect only with a reachable endpoint and observable missing gate. Otherwise retain an explicit coverage limitation | None |
| Q03 regex exploitability | Cancellation omission reproduced; denial-of-service behavior not established | Add a bounded synthetic test using deterministic size/time/cancellation limits; no live target or external input corpus | Reproduce only if the production regex path exceeds the ratified bound or ignores cancellation. Timing-only flaky evidence is rejected | None |
| Q04 frontend dependencies | Coordinator rerun lacked dependencies; a prior dependency-complete run existed | In an isolated worktree, attempt `npm ci --offline`, run the single Vitest file, and prove the tree remains clean | Cache miss is an environment blocker, not a product defect. Any registry access requires a separately recorded registry-only authorization; tests remain network-faked | None |

## Security gates

- **SG-SCOPE — universal:** exact Allowed Files; synthetic fixtures; no secrets, protected data, payload dumps, production/cloud access, external providers, or unapproved network; clean worktree and diff checks.
- **SG-EVIDENCE — P04:** reject ineligible statuses before staging; distinguish provenance labels from source locators; prove valid external locators still pass; run a separate defensive security review for promotion bypasses and provenance confusion.
- **SG-SIDECAR — P05/P06:** prove terminal-state atomicity under the synchronized race; prove the cap applies before downstream materialization; run a separate defensive security review for race, cancellation, resource-cap, and provenance bypasses.
- **SG-OUTBOUND — P02/P07 and any future Q01 remediation:** deny before constructing an outbound request; authority comes from authenticated server state and current consent, not caller claims or config alone; local interceptors must observe zero calls on denial and one synthetic call on authorization; run a separate defensive security review.
- Security review failure blocks Gate 3. Waivers require explicit human approval naming the finding and residual risk; no implicit severity downgrade is allowed.

## Parcel decomposition and dependency order

### Wave Q — adjudicate inconclusive items

- **Q01 Collective outbound investigation** — test-only, high boundary risk, frontier investigator; independent review required.
- **Q02 endpoint gate inventory** — test-only architecture inventory, high risk, frontier investigator; two independent reviews if it claims completeness.
- **Q03 regex exploitability investigation** — test-only robustness probe, medium risk, frontier investigator; no flaky timing claim accepted.
- **Q04 frontend dependency readiness** — command-only environment proof, medium risk; prerequisite for P07 dispatch.

Q01-Q03 do not block unrelated reproduced-defect remediation. A reproduced new defect blocks only its newly proposed remediation until a charter amendment is ratified.

### Wave 1 — independent fixes

- **P01 parser semantics** — AF-P01, high correctness risk, standard implementation routing.
- **P03 interaction safety** — AF-P03, critical semantics risk, frontier implementation routing.
- **P04 evidence provenance** — AF-P04, critical boundary risk, frontier implementation routing plus SG-EVIDENCE.
- **P05 sidecar terminal state** — AF-P05, critical concurrency risk, frontier implementation routing plus SG-SIDECAR.
- **P06 sidecar source cap** — AF-P06, high boundary risk, frontier implementation routing plus SG-SIDECAR.

### Wave 2 — outbound and shared-file boundaries

- **P02 protocol ingestion cancellation/OCR** — AF-P02, critical outbound risk, frontier implementation routing plus SG-OUTBOUND. It may run with Wave 1 because no other parcel touches `ProtocolIngestionService.cs`.
- **P07 frontend authenticated-consented relay** — AF-P07, critical auth/outbound risk, frontier implementation routing plus SG-OUTBOUND; depends on Q04 environment readiness.

All implementation parcels branch independently from the Gate-2-verified base. No implementation parcel branches from or merges a diagnostic branch.

## Per-parcel execution contract

Every shaped parcel must:

1. name its branch and isolated worktree;
2. restate the goal, invariant, exact Allowed Files, forbidden files, base commit, and tests at Step 0, then stop for coordinator confirmation;
3. import only its diagnostic test file contents and preserve the original failing scenario as a regression;
4. add the smallest production change that makes the regression green;
5. avoid refactors, package changes, schema changes, config enablement, unrelated cleanup, or public behavior expansion;
6. run its targeted regression, named adjacent suites, `git diff --check`, Allowed Files diff, and clean-tree checks;
7. record exact commands, final exit codes, test counts, changed files, residual risk, and a session handoff;
8. stop if the invariant or contract cannot be met inside Allowed Files.

## Deterministic verification chain

The coordinator consumes, but does not author, parcel verification evidence. Before any Gate 3 request:

- P01: targeted parser reproduction class is green; existing parser and ingestion suites pass.
- P02: PDF cancellation and OCR boundary regressions are green; existing ingestion suites pass; denial records zero fake sends and authorized-consented path records exactly one fake send.
- P03: all four interaction regressions are green; existing interaction/counterfactual suites pass.
- P04: both provenance regressions are green; existing EvidenceGate and scientific-research staging suites pass; valid external locator cases remain green.
- P05/P06: both sidecar regressions are green; existing executor, health/job, inference-policy, allowlist, and workflow-sequence suites pass.
- P07: the outbound regression and existing suggestion-route suite are green in a dependency-complete isolated worktree; unauthenticated, unauthorised, or unconsented paths record zero provider calls; the authorized-consented path records exactly one local-fake provider call.
- All branches: only Allowed Files changed; no untracked residue; base ancestry verified; no test contacted an external service.
- Q01-Q04: each has an evidence-backed classification and cannot be silently promoted to “fixed.”
- Fresh adversarial reviews are based on the exact candidate commits. Any rework invalidates earlier review acceptance and requires fresh review of the new commit.
- SG-EVIDENCE, SG-SIDECAR, and SG-OUTBOUND are passed by separate security reviewers on the final candidate commits.

Passing builder tests alone does not satisfy this chain.

## Human gates and requested standing authorization

- **Gate 1 — granted 2026-09-02:** the developer explicitly ratified D1-D14 as written. The ratified decision-set commit is `0c4c0204d7ca512ad3de903e3caf257dc5b28b40`, SHA-256 `991151BF01718B3A8D283666B5A110F7D60CF360E290D84A440553F4B8D318B8`.
- **Plan adversarial review — mandatory after Gate 1:** a fresh frontier reviewer receives only this charter and repository canon. Findings are triaged as fix, accept-as-documented, or informational. Any finding that changes D1-D14 reopens Gate 1 narrowly.
- **Gate 2 — contingent authorization granted 2026-09-02:** standing dispatch authorization covers Q01-Q04 and P01-P07 only after shaped specs match the ratified charter exactly, name isolated worktrees/branches, pass Step 0, and preserve exact Allowed Files. It does not authorize merges, pushes, PRs, external access, or release.
- **Narrow Gate 1 Amendment 01 — ratified 2026-09-02:** D6-D9 are replaced exactly as recorded in `gate-1-amendment-01.md`; the affected P03-P07 lanes are unblocked subject to their existing contingent Gate 2 conditions.
- **Narrow Gate 1 Amendment 02 — ratified 2026-09-02:** D3, D4, D11, and D12 are replaced; D15, AF-P08, and P08 are approved; contingent Gate 2 dispatch authority includes P08 subject to its shaped spec, isolated worktree, exact Allowed Files, and confirmed Step 0.
- **Narrow Gate 1 Amendment 03 — ratified 2026-09-02:** the D8 timeout-terminalization supplement and replacement three-file AF-P05 in `gate-1-amendment-03.md` are active. P05 retains contingent Gate 2 authority subject to its amended shaped spec and fresh Step 0; P06 remains downstream of the verified P05 candidate.
- **Narrow Gate 1 Amendment 04 — ratified 2026-09-02:** replacement three-file AF-P03 in `gate-1-amendment-04.md` is active. Only the stale API graph-positive fixture state may change from `provisional` to exactly `reviewed`; ratified D6 and all other P03 constraints remain unchanged.
- **Narrow Gate 1 Amendment 05 — awaiting human ratification:** `gate-1-amendment-05.md` proposes the D6 artifact-identity/provenance supplement and an exact 11-case P03 census. It grants no authority unless ratified; P03 is held.
- **Gate 3 — human-only and not requested now:** after the complete green chain, present exact candidate commits, review/security dispositions, merge order, rollback notes, and remaining uncertainty. The human approves or rejects the specific merge/release action.

## Merge and Stage-F closure

After explicit Gate 3:

1. rebase each approved parcel onto then-current `origin/main` in dependency/collision order;
2. rerun the full parcel and aggregate verification chain on the rebased commits;
3. merge only the approved commits in the approved order;
4. verify merged `main` and required CI checks;
5. move completed parcel specs to the goal's `done/` area, update evidence and decision records, append narrowly distilled lessons, record residual inconclusive claims, and write the final handoff;
6. clean only remediation worktrees/branches authorized for cleanup;
7. verify the original diagnostic branches still exist and remain unmerged.

No deployment or public release is implied by a code merge unless the Gate 3 approval explicitly includes it.

## Exit criterion

The goal is complete only when:

- every reproduced defect is fixed by an approved merged parcel and its retained regression is green;
- adjacent suites and aggregate verification are green on merged `main`;
- all adversarial and required security findings are closed or explicitly human-waived;
- Q01-Q04 have durable evidence-backed dispositions;
- Gate 3 identifies and approves the exact merge/release action;
- Stage-F records commits, tests, reviews, decisions, residual risk, lessons, and custody;
- original diagnostic branches are preserved and unmerged.

## Stop conditions

Stop and report if:

- Gate 1 is not explicit;
- current `origin/main` differs materially from the verified base;
- a shaped spec needs a file outside its Allowed Files;
- a production decision or public contract is missing;
- a reproduction cannot be made green without weakening or deleting its invariant;
- a test needs a real external provider, protected data, secret, cloud resource, or production access;
- a security finding cannot close inside the parcel;
- a tripwire or equivalent rework failure repeats twice;
- any merge, push, PR, deployment, publication, or release is proposed before explicit Gate 3;
- any action would merge, rewrite, delete, or clean the original diagnostic branches.
