# BIO-LOCAL-010 — Batch C seed-expansion record

## 1. Session identity

| field | value |
|---|---|
| Parcel | BIO-LOCAL-010 (seed expansion batch C — FINAL serial writer) |
| Builder | `bio_local_010_builder` |
| Branch | `proof/bio-local-010-seed-batch-c` |
| Worktree | `/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-010` |
| Starting commit (origin/main at start) | `99a0530163db2ef58440af9b8b5bbe832262f033` |
| UTC start | 2026-10-08T10:47:52Z |
| Record compile timestamp (single timestamp applied to all 15 records) | 2026-10-08T10:55:00Z |
| Seed file SHA-256 after batch C (100 records) | `a4d7d67c516100606f5db0f06d20c20500c2aa7ef0b4aafafd9255f1ebdb46e3` |
| Ending commit | HEAD of `proof/bio-local-010-seed-batch-c` (single commit: seed + this record; SHA in the PR) |
| Allowed surfaces touched | exactly `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` + this record |

Serial-chain note: batches A (BIO-LOCAL-008, PR #487) and B (BIO-LOCAL-009, PR #489) were
already MERGED into `origin/main` before this session started; this worktree branched from
the post-merge `origin/main` tip `99a0530`. All 85 pre-existing records (57 original +
14 batch A + 14 batch B) are existing, untouchable records; this parcel is a pure append
of batch C only.

## 2. Method (per Gate 2 unknown-honest doctrine + spec constraints)

- Allocation = exactly the 15 batch-C identifiers in
  `evidence/BIO-LOCAL-007-seed-gap-inventory.md` §4, in 007's allocation order. No
  substitutions, additions, or removals. Allocation match verified: 15/15, appended in
  order (asserted programmatically against 007's published list order).
- For each identifier, every 007 §2a citation was opened at its exact file:line and the
  claimed name string verified to be present **verbatim** on that line (scripted check,
  re-run at build time; any mismatch would have been a STOP-AND-REPORT — none occurred:
  30/30 citations across 15/15 identifiers verified, with cross-citation name consistency
  for each identifier).
- `Slugify` parity: the repo's `SubstanceRecordNormalizer.Slugify` (lower invariant;
  `[^a-z0-9]+` → `-`; trim leading/trailing `-`) was reimplemented in the build script and
  verified `Slugify(canonicalName) == identifier` for all 15.
- **Copied strings = the canonical name string only.** The cited lines contain only the
  `canonicalName` / `canonicalNameCandidate` string — no claim sentences exist on any cited
  line, so `evidence.claimSpecificEvidence` is empty for every record and every other field
  carries its unknown-honest default (mirrors the batch-A/batch-B / semaglutide
  draft-record shape). **Zero invented content; zero advice content; zero claims.**
- Tracking fields per doctrine (verified programmatically on all 15):
  `provenance.reviewStatus: draft`, `ops.needsReview: true` (with reasons),
  `ops.isActive: false`, `ops.completeness: partial`, `ops.lastChangeType: seed`,
  `ops.qualityFlags: [batch-c, identifier-only-citation, compiled-from-inventory-citation,
  no-claim-content-this-session, unknown-honest-defaults]`.
- Determinism: appended in 007 allocation order; existing 85 records untouched (diff is
  pure addition — see §5; the build script asserted all 85 pre-existing records
  deep-identical before writing, and the new file's prefix is byte-identical up to the
  final `}` of the last batch-B record); no reformatting (append-only string surgery).
- Posture (ModelInvoked-equivalent): compiled-from-packet-lines only — this session copied
  identifier-name strings from the exact 007-cited lines; no source acquisition, no
  browsing, no network, no model-generated claims.

## 3. Per-identifier trace table (identifier → citations → copied strings → defaulted fields)

All 30 cited source lines were opened and verified; the quoted source line for each
citation is the `canonicalName` / `canonicalNameCandidate` line shown in §3a.

| # | identifier | source citations (file:line) | copied strings (verbatim) | fields defaulted (unknown-honest) |
|---|---|---|---|---|
| 1 | ospemifene | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1432`; `research/input/evidence/ospemifene.evidence.json:12` | `Ospemifene` | everything except `identity.canonicalId`/`slug` (= `ospemifene`), `identity.canonicalName`, `identity.activeMoieties` |
| 2 | oxytocin | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1150`; `research/input/evidence/oxytocin.evidence.json:12` | `Oxytocin` | same pattern as #1 |
| 3 | peg-mgf | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1013`; `research/input/evidence/peg-mgf.evidence.json:12` | `PEG-MGF` | same pattern as #1 |
| 4 | pemvidutide | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:266`; `research/input/evidence/pemvidutide.evidence.json:12` | `Pemvidutide` | same pattern as #1 |
| 5 | pramlintide | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:185`; `research/input/evidence/pramlintide.evidence.json:12` | `Pramlintide` | same pattern as #1 |
| 6 | rad-150 | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1794`; `research/input/evidence/rad-150.evidence.json:12` | `RAD-150` | same pattern as #1 |
| 7 | s-23 | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1660`; `research/input/evidence/s-23.evidence.json:12` | `S-23` | same pattern as #1 |
| 8 | selank | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1067`; `research/input/evidence/selank.evidence.json:12` | `Selank` | same pattern as #1 |
| 9 | semax | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1040`; `research/input/evidence/semax.evidence.json:12` | `Semax` | same pattern as #1 |
| 10 | setmelanotide | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:709`; `research/input/evidence/setmelanotide.evidence.json:12` | `Setmelanotide` | same pattern as #1 |
| 11 | somatropin | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1206`; `research/input/evidence/somatropin.evidence.json:12` | `Somatropin` | same pattern as #1 |
| 12 | stenabolic | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1940`; `research/input/evidence/stenabolic.evidence.json:12` | `Stenabolic` | same pattern as #1 |
| 13 | survodutide | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:239`; `research/input/evidence/survodutide.evidence.json:12` | `Survodutide` | same pattern as #1 |
| 14 | vk2735 | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:376`; `research/input/evidence/vk2735.evidence.json:12` | `VK2735` | same pattern as #1 |
| 15 | vosilasarm | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1850`; `research/input/evidence/vosilasarm.evidence.json:12` | `Vosilasarm` | same pattern as #1 |

"Same pattern as #1" means: `aliases`/`brandNames`/`synonyms` empty; `classification: Other`
(the schema enum's catch-all — the packets' classification strings are NOT on any cited
line, so they were not copied); `compoundFamily: "unknown"`; all `externalIdentifiers`
null; `regulatory` fully unknown-honest (`requiresPrescription: false` mirroring the
semaglutide draft shape, `regulatoryStatus: "unknown"`, `jurisdiction: "unknown"`, empty
arrays, off-label note "Draft record: regulatory status has not been authoritatively
established."); `mechanism.mechanismSummary: "Mechanism has not been reviewed for this
draft record."` with empty arrays; `formulations`/`indications`/`dosingGuidance`/
`interactions` empty; `compatibility`/`safety`/`stackIntelligence`/`supportiveGuidance`
all-empty/unknown; `evidence.overallTier: "Unknown"`, `claimSpecificEvidence: []`,
one evidence-gap note; `provenance.sourceRecords` = the 007 citations as internal-curation
records; `reviewStatus: "draft"`.

### 3a. Quoted cited source lines (verbatim)

| citation | source line (verbatim) |
|---|---|
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1432` | `      "canonicalNameCandidate": "Ospemifene",` |
| `research/input/evidence/ospemifene.evidence.json:12` | `    "canonicalName": "Ospemifene",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1150` | `      "canonicalNameCandidate": "Oxytocin",` |
| `research/input/evidence/oxytocin.evidence.json:12` | `    "canonicalName": "Oxytocin",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1013` | `      "canonicalNameCandidate": "PEG-MGF",` |
| `research/input/evidence/peg-mgf.evidence.json:12` | `    "canonicalName": "PEG-MGF",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:266` | `      "canonicalNameCandidate": "Pemvidutide",` |
| `research/input/evidence/pemvidutide.evidence.json:12` | `    "canonicalName": "Pemvidutide",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:185` | `      "canonicalNameCandidate": "Pramlintide",` |
| `research/input/evidence/pramlintide.evidence.json:12` | `    "canonicalName": "Pramlintide",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1794` | `      "canonicalNameCandidate": "RAD-150",` |
| `research/input/evidence/rad-150.evidence.json:12` | `    "canonicalName": "RAD-150",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1660` | `      "canonicalNameCandidate": "S-23",` |
| `research/input/evidence/s-23.evidence.json:12` | `    "canonicalName": "S-23",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1067` | `      "canonicalNameCandidate": "Selank",` |
| `research/input/evidence/selank.evidence.json:12` | `    "canonicalName": "Selank",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1040` | `      "canonicalNameCandidate": "Semax",` |
| `research/input/evidence/semax.evidence.json:12` | `    "canonicalName": "Semax",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:709` | `      "canonicalNameCandidate": "Setmelanotide",` |
| `research/input/evidence/setmelanotide.evidence.json:12` | `    "canonicalName": "Setmelanotide",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1206` | `      "canonicalNameCandidate": "Somatropin",` |
| `research/input/evidence/somatropin.evidence.json:12` | `    "canonicalName": "Somatropin",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1940` | `      "canonicalNameCandidate": "Stenabolic",` |
| `research/input/evidence/stenabolic.evidence.json:12` | `    "canonicalName": "Stenabolic",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:239` | `      "canonicalNameCandidate": "Survodutide",` |
| `research/input/evidence/survodutide.evidence.json:12` | `    "canonicalName": "Survodutide",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:376` | `      "canonicalNameCandidate": "VK2735",` |
| `research/input/evidence/vk2735.evidence.json:12` | `    "canonicalName": "VK2735",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1850` | `      "canonicalNameCandidate": "Vosilasarm",` |
| `research/input/evidence/vosilasarm.evidence.json:12` | `    "canonicalName": "Vosilasarm",` |

## 4. Validation outputs

### 4a. Schema/validator on the FULL seed file (100 records) — repo's own validator

Command (harness in `/tmp` referencing the worktree's worker project — no repository files
added or modified; it drives the repo's `SubstanceRecordValidator.LoadFromFile` +
`Validate` over the production seed):

```
dotnet run --project /tmp/bio010/SeedValidate/SeedValidate.csproj -- \
  /home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-010
```

Output (verbatim):

```
[SeedValidate] schema: /home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-010/backend/src/BioStack.KnowledgeWorker/Schemas/substance-record.schema.json
[SeedValidate] seed:   /home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-010/backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json
[SeedValidate] record count: 100
[SeedValidate] validation failures: 0/100
[SeedValidate] canonicalId uniqueness: unique
[SeedValidate] PASS — all 100 records schema-valid (57 existing + 14 batch A + 14 batch B + 15 batch C), canonicalIds unique
```

### 4b. Count + uniqueness assertions (AC1)

`node -e "const d=require('backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json'); console.log(d.length, new Set(d.map(r=>r.identity.canonicalId)).size)"` → `100 100`
(count = 57 + 14 + 14 + 15; canonicalIds unique across the whole file; zero collisions
with the existing 85 IDs — checked programmatically in the build script before write).

### 4c. Full KnowledgeWorker test lane (run ONCE, to record — never to fix)

Command: `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj`

Output (summary, verbatim):

```
Failed!  - Failed:     4, Passed:  1002, Skipped:     0, Total:  1006, Duration: 8 s - BioStack.KnowledgeWorker.Tests.dll (net10.0)
```

## 5. Diff-addition proof (AC5)

- `git diff --numstat` → `2055  0  backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
  (**pure addition; zero deletions**).
- The build script asserted all 85 pre-existing records (57 + batch A 14 + batch B 14)
  byte-identical by deep comparison before writing; the new file's prefix is byte-identical
  up to the final `}` of the last batch-B record (append-only string surgery).
- `git diff --check` → clean (no whitespace errors).

## 6. Expected-failure list (KNOWN-PENDING-011 — never fixed in this parcel)

The same four corpus-frozen expectation tests that failed after batches A and B (see
`BIO-LOCAL-008-batch-a-record.md` §6, `BIO-LOCAL-009-batch-b-record.md` §6) fail here, with
the two count-based failures now reading 100. All four are recorded as EXPECTED FAILURES;
BIO-LOCAL-011 owns updating them. Exact messages captured via record-only filtered
re-queries of the same four tests (no fixes, no test edits).

| test | failure | cause |
|---|---|---|
| `CorpusIdentityInventoryBuilderTests.Build_CurrentRepository_RecordsIdentityCoverageWithoutPromotionAuthority` | `Assert.Equal() Failure: Expected: 57 Actual: 100` (CorpusIdentityInventoryBuilderTests.cs:16) | builder reads the production seed; frozen count 57 → now 100 |
| `StructuralEvaluationReportBuilderTests.Build_CurrentArtifacts_RecordsPartialPolicyWithoutVerdict` | `Assert.Equal() Failure: Expected: 57 Actual: 100` (StructuralEvaluationReportBuilderTests.cs:37) | report embeds `CorpusInventory.SeedRecordCount`; frozen count 57 → now 100 |
| `ResearchJobTests.ResearchJob_Applies_Review_Decision_To_Promotion_Manifest` | `Assert.Equal() Failure: Expected: "create" Actual: "update"` (ResearchJobTests.cs:245) | fixture review decision approves "Creatine"; the `creatine` seed record (batch A) exists, so the import-preview action flips `create` → `update` (unchanged by batch C; batch C adds no creatine-related identifier) |
| `ResearchJobTests.PromotionImportDryRunJob_Succeeds_For_Safe_Create_Preview` | `Assert.Equal() Failure: Expected: 1 Actual: 0` (ResearchJobTests.cs:372) | same batch-A root cause: `creatine` now exists in the seed, so `CreatedCount` 1 → 0 (unchanged by batch C) |

## 7. Identity collisions — listed for human rule, never auto-merged

Programmatic check: all 15 batch-C canonicalIds are unique and collide with neither the
original 57 nor batch A's 14 nor batch B's 14 (§4b). No new identity-token collisions were
detected within batch C, and no batch-C identifier slug-matches an existing canonicalId.

Standing items (from batch A's record, restated for human visibility; batch C adds nothing
to them and changed nothing):

1. **`creatine` (batch A) vs `creatine-monohydrate` (existing seed record)** — coexisting
   canonicalIds, never merged; root cause of the two `ResearchJobTests` expected failures
   in §6. **Human rule still required.**
2. **`chorionic-gonadotropin` (batch A) vs `human-chorionic-gonadotropin` (existing seed
   record)** — closely related identity tokens coexisting; never merged. **Human rule
   still required.**

Batch C introduces no new aliasing decisions: `aliases` were NOT copied for any batch-C
record (not on any cited line). All batch-C records carry null external identifiers, so
no externalIdentifier collisions are possible.

## 8. FINAL COUNT

```
records added by batch C (BIO-LOCAL-010) = 15
total records now present                = 100   (57 existing + 14 batch A + 14 batch B + 15 batch C)
```

Exact-count reconciliation: |A| + |B| + |C| = 14 + 14 + 15 = 43 new-traceable identifiers
(one per 007 §4 allocation), landing the corpus at 57 + 43 = 100. 100 — not 150 — is the
operative target for this parcel: 007's verdict is REACHABLE-100 (§3/§6 of the inventory:
only 78 distinct identifiers exist in the named repo inputs, 35 already seeded, so 100 is
the reachable ceiling without new source acquisition), and the coordinator's Gate 2
dispatch record (frozen at dispatch) records decision D-C: "Post-merge the corpus stands at
100 with the 50-record gap documented pending KEO-73/74 sourcing." The spec's 150 figure is
therefore satisfied-as-superseded by the recorded ruling — the 50-record gap remains open
pending KEO-73/74 source acquisition, NOT padded by this session.

## 9. Redaction attestation

No secrets, credentials, or connection strings were read, copied, printed, or committed.
The production Refresh runbook's `db-conn-string` secret was never accessed. All commands
run in this session (git, node, dotnet build/test/run, gh) are quoted above or in §4; no
command output containing sensitive values is included in this record. Validation and test
runs were offline (no database, no network source access; the dotnet restore used only the
project's pinned NuGet packages).

## 10. Session Handoff

- Starting commit: `99a0530163db2ef58440af9b8b5bbe832262f033` (origin/main, includes batch-A merge `35529d8b` / PR #487 and batch-B merge / PR #489)
- Ending commit: HEAD of `proof/bio-local-010-seed-batch-c` (single commit: seed + batch record; see PR)
- Files changed: `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` (+2055/−0); `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-010-batch-c-record.md` (new)
- Commands run: worktree add from origin/main; `/tmp/bio010/verify-citations.mjs` (30/30 citations verbatim-verified); `/tmp/bio010/build-batch-c.mjs` (citation re-verified append, 85→100, pre-existing byte-identical, doctrine assertions on all 15); `dotnet run` SeedValidate harness (PASS 0/100); `dotnet test` KnowledgeWorker lane ONCE (4 expected failures / 1002 passed); record-only filtered re-queries of the four known tests to capture exact failure messages for §6; `git diff --numstat` / `--check`
- Tests passed: full-seed schema validation 100/100; canonicalId uniqueness (100 distinct); pure-addition diff; doctrine spot-checks on all 15 records
- Tests failed: 4 (all expected, corpus-frozen, listed in §6 — BIO-LOCAL-011 owns them)
- Decisions needed: human rule on the two standing identity collisions in §7 (batch-A items); Gate 3 merge decision is the owner's; KEO-73/74 sourcing for the remaining 50-record gap
- Blockers: none
- Next safe action: independent review `bio_local_010_review_1`, then owner Gate 3 merge; **BIO-LOCAL-011 (seed-run + serving proof + count-test updates) follows this merge**
- Do not touch: tests/schemas/loaders/API/frontend; the frozen count-test expectations (011's); the ambient untracked `package.json` in the coordinator checkout root; all 85 pre-existing records (batches A and B are now existing, untouchable records)
