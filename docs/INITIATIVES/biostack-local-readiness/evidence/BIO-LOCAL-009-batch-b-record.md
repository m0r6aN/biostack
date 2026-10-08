# BIO-LOCAL-009 — Batch B seed-expansion record

## 1. Session identity

| field | value |
|---|---|
| Parcel | BIO-LOCAL-009 (seed expansion batch B) |
| Builder | `bio_local_009_builder` |
| Branch | `proof/bio-local-009-seed-batch-b` |
| Worktree | `/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-009` |
| Starting commit (origin/main at start) | `5894578ff397cc5e2d54377daed883ddd0f6c312` (contains batch A's merge `35529d8b`, PR #487) |
| UTC start | 2026-10-08T01:24:03Z |
| Record compile timestamp (single timestamp applied to all 14 records) | 2026-10-08T01:24:44Z |
| Seed file SHA-256 after batch B (85 records) | `00898b258be26d900a2204e121936958ef0d2468faa30fda288b0e5a102f745a` |
| Ending commit | HEAD of `proof/bio-local-009-seed-batch-b` (single commit: seed + this record; SHA in the PR) |
| Allowed surfaces touched | exactly `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` + this record |

Serial-chain note: batch A (BIO-LOCAL-008) was already MERGED into `origin/main` (PR #487,
`35529d8b`) before this session started; this worktree branched from the post-merge
`origin/main` tip `5894578` (a later docs-only merge on top). Batch A's 14 records are
existing, untouchable records; this parcel is a pure append of batch B only.

## 2. Method (per Gate 2 unknown-honest doctrine + spec constraints)

- Allocation = exactly the 14 batch-B identifiers in
  `evidence/BIO-LOCAL-007-seed-gap-inventory.md` §4, in 007's allocation order. No
  substitutions, additions, or removals. Allocation match verified: 14/14.
- For each identifier, every 007 §2a citation was opened at its exact file:line and the
  claimed name string verified to be present **verbatim** on that line (scripted check;
  any mismatch would have been a STOP-AND-REPORT — none occurred: 28/28 citations across
  14/14 identifiers verified, with cross-citation name consistency for each identifier).
- `Slugify` parity: the repo's `SubstanceRecordNormalizer.Slugify` (lower invariant;
  `[^a-z0-9]+` → `-`; trim leading/trailing `-`) was reimplemented in the build script and
  verified `Slugify(canonicalName) == identifier` for all 14.
- **Copied strings = the canonical name string only.** The cited lines contain only the
  `canonicalName` / `canonicalNameCandidate` string — no claim sentences exist on any cited
  line, so `evidence.claimSpecificEvidence` is empty for every record and every other field
  carries its unknown-honest default (mirrors the batch-A / semaglutide draft-record shape).
  **Zero invented content; zero advice content; zero claims.**
- Tracking fields per doctrine (verified programmatically on all 14):
  `provenance.reviewStatus: draft`, `ops.needsReview: true` (with reasons),
  `ops.isActive: false`, `ops.completeness: partial`, `ops.lastChangeType: seed`,
  `ops.qualityFlags: [batch-b, identifier-only-citation, compiled-from-inventory-citation,
  no-claim-content-this-session, unknown-honest-defaults]`.
- Determinism: appended in 007 allocation order; existing 57 + 14 (batch A) records
  untouched (diff is pure addition — see §5; the build script asserted all 71 pre-existing
  records byte-identical by deep comparison before writing); no reformatting (append-only
  string surgery).
- Posture (ModelInvoked-equivalent): compiled-from-packet-lines only — this session copied
  identifier-name strings from the exact 007-cited lines; no source acquisition, no
  browsing, no network, no model-generated claims.

## 3. Per-identifier trace table (identifier → citations → copied strings → defaulted fields)

All 28 cited source lines were opened and verified; the quoted source line for each
citation is the `canonicalName` / `canonicalNameCandidate` line shown in §3a.

| # | identifier | source citations (file:line) | copied strings (verbatim) | fields defaulted (unknown-honest) |
|---|---|---|---|---|
| 1 | ghrp-6 | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:904`; `research/input/evidence/ghrp-6.evidence.json:12` | `GHRP-6` | everything except `identity.canonicalId`/`slug` (= `ghrp-6`), `identity.canonicalName`, `identity.activeMoieties` |
| 2 | glutathione | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1262`; `research/input/evidence/glutathione.evidence.json:12` | `Glutathione` | same pattern as #1 |
| 3 | gonadorelin | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1234`; `research/input/evidence/gonadorelin.evidence.json:12` | `Gonadorelin` | same pattern as #1 |
| 4 | gsk2881078 | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1824`; `research/input/evidence/gsk2881078.evidence.json:12` | `GSK2881078` | same pattern as #1 |
| 5 | hexarelin | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:931`; `research/input/evidence/hexarelin.evidence.json:12` | `Hexarelin` | same pattern as #1 |
| 6 | ibutamoren | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1878`; `research/input/evidence/ibutamoren.evidence.json:12` | `Ibutamoren` | same pattern as #1 |
| 7 | igf-1-lr3 | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:958`; `research/input/evidence/igf-1-lr3.evidence.json:12` | `IGF-1 LR3` | same pattern as #1 |
| 8 | kisspeptin-10 | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1122`; `research/input/evidence/kisspeptin-10.evidence.json:12` | `Kisspeptin-10` | same pattern as #1 |
| 9 | kpv | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:822`; `research/input/evidence/kpv.evidence.json:12` | `KPV` | same pattern as #1 |
| 10 | magnesium | `research/input/candidates/pilot-compound-candidates.json:29`; `research/input/evidence/magnesium.evidence.json:6` | `Magnesium` | same pattern as #1 |
| 11 | maridebart-cafraglutide | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:293`; `research/input/evidence/maridebart-cafraglutide.evidence.json:12` | `Maridebart cafraglutide` | same pattern as #1 |
| 12 | mazdutide | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:321`; `research/input/evidence/mazdutide.evidence.json:12` | `Mazdutide` | same pattern as #1 |
| 13 | mecasermin | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:985`; `research/input/evidence/mecasermin.evidence.json:12` | `Mecasermin` | same pattern as #1 |
| 14 | melatonin | `research/input/candidates/pilot-compound-candidates.json:185`; `research/input/evidence/melatonin.evidence.json:6` | `Melatonin` | same pattern as #1 |

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
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:904` | `      "canonicalNameCandidate": "GHRP-6",` |
| `research/input/evidence/ghrp-6.evidence.json:12` | `    "canonicalName": "GHRP-6",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1262` | `      "canonicalNameCandidate": "Glutathione",` |
| `research/input/evidence/glutathione.evidence.json:12` | `    "canonicalName": "Glutathione",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1234` | `      "canonicalNameCandidate": "Gonadorelin",` |
| `research/input/evidence/gonadorelin.evidence.json:12` | `    "canonicalName": "Gonadorelin",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1824` | `      "canonicalNameCandidate": "GSK2881078",` |
| `research/input/evidence/gsk2881078.evidence.json:12` | `    "canonicalName": "GSK2881078",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:931` | `      "canonicalNameCandidate": "Hexarelin",` |
| `research/input/evidence/hexarelin.evidence.json:12` | `    "canonicalName": "Hexarelin",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1878` | `      "canonicalNameCandidate": "Ibutamoren",` |
| `research/input/evidence/ibutamoren.evidence.json:12` | `    "canonicalName": "Ibutamoren",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:958` | `      "canonicalNameCandidate": "IGF-1 LR3",` |
| `research/input/evidence/igf-1-lr3.evidence.json:12` | `    "canonicalName": "IGF-1 LR3",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1122` | `      "canonicalNameCandidate": "Kisspeptin-10",` |
| `research/input/evidence/kisspeptin-10.evidence.json:12` | `    "canonicalName": "Kisspeptin-10",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:822` | `      "canonicalNameCandidate": "KPV",` |
| `research/input/evidence/kpv.evidence.json:12` | `    "canonicalName": "KPV",` |
| `research/input/candidates/pilot-compound-candidates.json:29` | `      "canonicalNameCandidate": "Magnesium",` |
| `research/input/evidence/magnesium.evidence.json:6` | `    "canonicalName": "Magnesium",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:293` | `      "canonicalNameCandidate": "Maridebart cafraglutide",` |
| `research/input/evidence/maridebart-cafraglutide.evidence.json:12` | `    "canonicalName": "Maridebart cafraglutide",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:321` | `      "canonicalNameCandidate": "Mazdutide",` |
| `research/input/evidence/mazdutide.evidence.json:12` | `    "canonicalName": "Mazdutide",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:985` | `      "canonicalNameCandidate": "Mecasermin",` |
| `research/input/evidence/mecasermin.evidence.json:12` | `    "canonicalName": "Mecasermin",` |
| `research/input/candidates/pilot-compound-candidates.json:185` | `      "canonicalNameCandidate": "Melatonin",` |
| `research/input/evidence/melatonin.evidence.json:6` | `    "canonicalName": "Melatonin",` |

## 4. Validation outputs

### 4a. Schema/validator on the FULL seed file (85 records) — repo's own validator

Command (harness in `/tmp` referencing the worktree's worker project — no repository files
added or modified; it drives the repo's `SubstanceRecordValidator.LoadFromFile` +
`Validate` over the production seed):

```
dotnet run --project /tmp/bio009/SeedValidate/SeedValidate.csproj -- \
  /home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-009
```

Output (verbatim):

```
[SeedValidate] schema: /home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-009/backend/src/BioStack.KnowledgeWorker/Schemas/substance-record.schema.json
[SeedValidate] seed:   /home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-009/backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json
[SeedValidate] record count: 85
[SeedValidate] validation failures: 0/85
[SeedValidate] canonicalId uniqueness: unique
[SeedValidate] PASS — all 85 records schema-valid (57 existing + 14 batch A + 14 batch B), canonicalIds unique
```

### 4b. Count + uniqueness assertions (AC1)

`node -e "const d=require('backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json'); console.log(d.length, new Set(d.map(r=>r.identity.canonicalId)).size)"` → `85 85`
(count = 57 + 14 + 14; canonicalIds unique across the whole file; zero collisions with
the existing 57 + batch-A 14 IDs — checked programmatically in the build script before
write).

### 4c. Full KnowledgeWorker test lane (run ONCE, to record — never to fix)

Command: `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj`

Output (summary, verbatim):

```
Failed!  - Failed:     4, Passed:  1002, Skipped:     0, Total:  1006, Duration: 24 s - BioStack.KnowledgeWorker.Tests.dll (net10.0)
```

## 5. Diff-addition proof (AC5)

- `git diff --numstat` → `1918  0  backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
  (**pure addition; zero deletions**).
- The build script asserted all 71 pre-existing records (57 + batch A) byte-identical by
  deep comparison before writing; the new file's prefix is byte-identical up to the final
  `}` of the last batch-A record (append-only string surgery).
- `git diff --check` → clean (no whitespace errors).

## 6. Expected-failure list (KNOWN-PENDING-011 — never fixed in this parcel)

The same four corpus-frozen expectation tests that failed after batch A (see
`BIO-LOCAL-008-batch-a-record.md` §6) fail identically here, with the two count-based
failures now reading 85 instead of 71. All four are recorded as EXPECTED FAILURES;
BIO-LOCAL-011 owns updating them.

| test | failure | cause |
|---|---|---|
| `CorpusIdentityInventoryBuilderTests.Build_CurrentRepository_RecordsIdentityCoverageWithoutPromotionAuthority` | `Assert.Equal() Failure: Expected: 57 Actual: 85` (CorpusIdentityInventoryBuilderTests.cs:16) | builder reads the production seed; frozen count 57 → now 85 |
| `StructuralEvaluationReportBuilderTests.Build_CurrentArtifacts_RecordsPartialPolicyWithoutVerdict` | `Assert.Equal() Failure: Expected: 57 Actual: 85` (StructuralEvaluationReportBuilderTests.cs:37) | report embeds `CorpusInventory.SeedRecordCount`; frozen count 57 → now 85 |
| `ResearchJobTests.ResearchJob_Applies_Review_Decision_To_Promotion_Manifest` | `Assert.Equal() Failure: Expected: "create" Actual: "update"` (ResearchJobTests.cs:245) | fixture review decision approves "Creatine"; the `creatine` seed record (batch A) exists, so the import-preview action flips `create` → `update` (unchanged by batch B; batch B adds no creatine-related identifier) |
| `ResearchJobTests.PromotionImportDryRunJob_Succeeds_For_Safe_Create_Preview` | `Assert.Equal() Failure: Expected: 1 Actual: 0` (ResearchJobTests.cs:372) | same batch-A root cause: `creatine` now exists in the seed, so `CreatedCount` 1 → 0 (unchanged by batch B) |

## 7. Identity collisions — listed for human rule, never auto-merged

Programmatic check: all 14 batch-B canonicalIds are unique and collide with neither the
original 57 nor batch A's 14 (§4b). No new identity-token collisions were detected within
batch B (007 §3 recorded token collisions only for `creatine`, a batch-A identifier).

Standing items (from batch A's record, restated for human visibility; batch B adds nothing
to them and changed nothing):

1. **`creatine` (batch A) vs `creatine-monohydrate` (existing seed record)** — coexisting
   canonicalIds, never merged; root cause of the two `ResearchJobTests` expected failures
   in §6. **Human rule still required.**
2. **`chorionic-gonadotropin` (batch A) vs `human-chorionic-gonadotropin` (existing seed
   record)** — closely related identity tokens coexisting; never merged. **Human rule
   still required.**

Batch B introduces no new aliasing decisions: `aliases` were NOT copied for any batch-B
record (not on any cited line). All batch-B records carry null external identifiers, so
no externalIdentifier collisions are possible.

## 8. Redaction attestation

No secrets, credentials, or connection strings were read, copied, printed, or committed.
The production Refresh runbook's `db-conn-string` secret was never accessed. All commands
run in this session (git, node, dotnet build/test/run, gh) are quoted above or in §4; no
command output containing sensitive values is included in this record. Validation and test
runs were offline (no database, no network source access; the dotnet restore used only the
project's pinned NuGet packages).

## 9. Session Handoff

- Starting commit: `5894578ff397cc5e2d54377daed883ddd0f6c312` (origin/main, includes batch-A merge `35529d8b`)
- Ending commit: HEAD of `proof/bio-local-009-seed-batch-b` (single commit: seed + batch record; see PR)
- Files changed: `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` (+1918/−0); `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-009-batch-b-record.md` (new)
- Commands run: worktree add from origin/main; `/tmp/bio009/verify-citations.mjs` (28/28 citations verbatim-verified); `/tmp/bio009/build-batch-b.mjs` (citation-verified append, 71→85, pre-existing byte-identical); `dotnet run` SeedValidate harness (PASS 0/85); `dotnet test` KnowledgeWorker lane ONCE (4 expected failures / 1002 passed); two single-test filtered re-queries to capture exact failure messages for §6 (record-only; no fixes); `git diff --numstat` / `--check`
- Tests passed: full-seed schema validation 85/85; canonicalId uniqueness (85 distinct); pure-addition diff; doctrine spot-checks on all 14 records
- Tests failed: 4 (all expected, corpus-frozen, listed in §6 — BIO-LOCAL-011 owns them)
- Decisions needed: human rule on the two standing identity collisions in §7 (batch-A items); Gate 3 merge decision is the owner's
- Blockers: none
- Next safe action: independent review `bio_local_009_review_1`, then owner Gate 3 merge; **BIO-LOCAL-010 dispatches only after this PR is MERGED (serial chain — no parallel seed edits)**
- Do not touch: tests/schemas/loaders/API/frontend; the frozen count-test expectations (011's); the ambient untracked `package.json` in the coordinator checkout root; batches A/C records (A's 14 records are now existing, untouchable records)
