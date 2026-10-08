# BIO-LOCAL-008 — Batch A seed-expansion record

## 1. Session identity

| field | value |
|---|---|
| Parcel | BIO-LOCAL-008 (seed expansion batch A) |
| Builder | `bio_local_008_builder` |
| Branch | `proof/bio-local-008-seed-batch-a` |
| Worktree | `/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-008` |
| Starting commit (origin/main at start) | `0af46c9fa969aca94cb2e848d842eef19d29cad3` |
| UTC start | 2026-10-08T00:26:46Z |
| Record compile timestamp (single timestamp applied to all 14 records) | 2026-10-08T00:28:12Z |
| Seed file SHA-256 after batch A (71 records) | `86f18282a76f35afbdc2cbbea024c7c5b3350528c25214b069bb24d4b90a5355` |
| Ending commit | HEAD of `proof/bio-local-008-seed-batch-a` (single commit: seed + this record; SHA in the PR) |
| Allowed surfaces touched | exactly `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` + this record |

**Prior-state note (transparency):** the worktree/branch pre-existed this dispatch with
**uncommitted** modifications to the seed file (a partial batch-A attempt from an earlier,
undelivered session; no PR ever existed; the branch tip `2687af8` was already an ancestor of
`origin/main`). Per the zero-invention doctrine this session did NOT build on unverifiable
uncommitted content: the worktree was reset to clean `origin/main` (`0af46c9`) and all 14
records were re-compiled from scratch from the cited source lines below. The leftover file
was archived outside the repository only (`/tmp/BIO-LOCAL-008-leftover-seed.json`); nothing
from it was committed.

## 2. Method (per Gate 2 unknown-honest doctrine + spec constraints)

- Allocation = exactly the 14 batch-A identifiers in
  `evidence/BIO-LOCAL-007-seed-gap-inventory.md` §4, in 007's allocation order. No
  substitutions, additions, or removals. Allocation match verified: 14/14.
- For each identifier, every 007 §2a citation was opened at its exact file:line and the
  claimed name string verified to be present **verbatim** on that line (scripted check;
  any mismatch would have been a STOP-AND-REPORT — none occurred: 14/14 verified).
- `Slugify` parity: the repo's `SubstanceRecordNormalizer.Slugify` (lower invariant;
  `[^a-z0-9]+` → `-`; trim leading/trailing `-`) was reimplemented in the build script and
  verified `Slugify(canonicalName) == identifier` for all 14.
- **Copied strings = the canonical name string only.** The cited lines contain only the
  `canonicalName` / `canonicalNameCandidate` string — no claim sentences exist on any cited
  line, so `evidence.claimSpecificEvidence` is empty for every record and every other field
  carries its unknown-honest default (mirrors the semaglutide draft-record shape).
  **Zero invented content; zero advice content; zero claims.**
- Tracking fields per doctrine (verified programmatically on all 14):
  `provenance.reviewStatus: draft`, `ops.needsReview: true` (with reasons),
  `ops.isActive: false`, `ops.completeness: partial`, `ops.lastChangeType: seed`,
  `ops.qualityFlags: [batch-a, identifier-only-citation, compiled-from-inventory-citation,
  no-claim-content-this-session, unknown-honest-defaults]`.
- Determinism: appended in 007 allocation order; existing 57 records untouched
  (diff is pure addition — see §5); no reformatting (append-only string surgery preserving
  the file's existing .NET-style serialization byte-for-byte for the pre-existing content).
- Posture (ModelInvoked-equivalent): compiled-from-packet-lines only — this session copied
  identifier-name strings from the exact 007-cited lines; no source acquisition, no
  browsing, no network, no model-generated claims.

## 3. Per-identifier trace table (identifier → citations → copied strings → defaulted fields)

All 15 cited source lines were opened and verified; the quoted source line for each
citation is the `canonicalName` / `canonicalNameCandidate` line shown in §3a.

| # | identifier | source citations (file:line) | copied strings (verbatim) | fields defaulted (unknown-honest) |
|---|---|---|---|---|
| 1 | ghk-cu | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:513`; `research/input/candidates/pilot-compound-candidates.json:81`; `research/input/evidence/ghk-cu.evidence.json:6` | `GHK-Cu` | everything except `identity.canonicalId`/`slug` (= `ghk-cu`), `identity.canonicalName`, `identity.activeMoieties` |
| 2 | acp-105 | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1716`; `research/input/evidence/acp-105.evidence.json:12` | `ACP-105` | same pattern as #1 |
| 3 | afamelanotide | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:766`; `research/input/evidence/afamelanotide.evidence.json:12` | `Afamelanotide` | same pattern as #1 |
| 4 | amycretin | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:349`; `research/input/evidence/amycretin.evidence.json:12` | `Amycretin` | same pattern as #1 |
| 5 | aod-9604 | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:541`; `research/input/evidence/aod-9604.evidence.json:12` | `AOD-9604` | same pattern as #1 |
| 6 | bazedoxifene | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1488`; `research/input/evidence/bazedoxifene.evidence.json:12` | `Bazedoxifene` | same pattern as #1 |
| 7 | cagrilintide | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:212`; `research/input/evidence/cagrilintide.evidence.json:12` | `Cagrilintide` | same pattern as #1 |
| 8 | cardarine | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1909`; `research/input/evidence/cardarine.evidence.json:12` | `Cardarine` | same pattern as #1 |
| 9 | chorionic-gonadotropin | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1177`; `research/input/evidence/chorionic-gonadotropin.evidence.json:12` | `Chorionic gonadotropin` | same pattern as #1 |
| 10 | creatine | `research/input/candidates/pilot-compound-candidates.json:16`; `research/input/evidence/creatine.evidence.json:12` | `Creatine` | same pattern as #1 |
| 11 | elamipretide | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:736`; `research/input/evidence/elamipretide.evidence.json:12` | `Elamipretide` | same pattern as #1 |
| 12 | emideltide | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1289`; `research/input/evidence/emideltide.evidence.json:12` | `Emideltide` | same pattern as #1 |
| 13 | epitalon | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1094`; `research/input/evidence/epitalon.evidence.json:12` | `Epitalon` | same pattern as #1 |
| 14 | ghrp-2 | `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:877`; `research/input/evidence/ghrp-2.evidence.json:12` | `GHRP-2` | same pattern as #1 |

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
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:513` | `      "canonicalNameCandidate": "GHK-Cu",` |
| `research/input/candidates/pilot-compound-candidates.json:81` | `      "canonicalNameCandidate": "GHK-Cu",` |
| `research/input/evidence/ghk-cu.evidence.json:6` | `    "canonicalName": "GHK-Cu",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1716` | `      "canonicalNameCandidate": "ACP-105",` |
| `research/input/evidence/acp-105.evidence.json:12` | `    "canonicalName": "ACP-105",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:766` | `      "canonicalNameCandidate": "Afamelanotide",` |
| `research/input/evidence/afamelanotide.evidence.json:12` | `    "canonicalName": "Afamelanotide",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:349` | `      "canonicalNameCandidate": "Amycretin",` |
| `research/input/evidence/amycretin.evidence.json:12` | `    "canonicalName": "Amycretin",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:541` | `      "canonicalNameCandidate": "AOD-9604",` |
| `research/input/evidence/aod-9604.evidence.json:12` | `    "canonicalName": "AOD-9604",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1488` | `      "canonicalNameCandidate": "Bazedoxifene",` |
| `research/input/evidence/bazedoxifene.evidence.json:12` | `    "canonicalName": "Bazedoxifene",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:212` | `      "canonicalNameCandidate": "Cagrilintide",` |
| `research/input/evidence/cagrilintide.evidence.json:12` | `  "canonicalName": "Cagrilintide",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1909` | `      "canonicalNameCandidate": "Cardarine",` |
| `research/input/evidence/cardarine.evidence.json:12` | `    "canonicalName": "Cardarine",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1177` | `      "canonicalNameCandidate": "Chorionic gonadotropin",` |
| `research/input/evidence/chorionic-gonadotropin.evidence.json:12` | `    "canonicalName": "Chorionic gonadotropin",` |
| `research/input/candidates/pilot-compound-candidates.json:16` | `      "canonicalNameCandidate": "Creatine",` |
| `research/input/evidence/creatine.evidence.json:12` | `    "canonicalName": "Creatine",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:736` | `      "canonicalNameCandidate": "Elamipretide",` |
| `research/input/evidence/elamipretide.evidence.json:12` | `    "canonicalName": "Elamipretide",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1289` | `      "canonicalNameCandidate": "Emideltide",` |
| `research/input/evidence/emideltide.evidence.json:12` | `    "canonicalName": "Emideltide",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1094` | `      "canonicalNameCandidate": "Epitalon",` |
| `research/input/evidence/epitalon.evidence.json:12` | `    "canonicalName": "Epitalon",` |
| `research/input/candidates/peptide-serm-sarm-market-interest.v1.json:877` | `      "canonicalNameCandidate": "GHRP-2",` |
| `research/input/evidence/ghrp-2.evidence.json:12` | `    "canonicalName": "GHRP-2",` |

## 4. Validation outputs

### 4a. Schema/validator on the FULL seed file (71 records) — repo's own validator

Command (harness in `/tmp` referencing the worktree's worker project — no repository files
added or modified; it drives the repo's `SubstanceRecordValidator.LoadFromFile` +
`Validate` over the production seed):

```
dotnet run --project /tmp/bio008/SeedValidate/SeedValidate.csproj -- \
  /home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-008
```

Output (verbatim):

```
[SeedValidate] schema: /home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-008/backend/src/BioStack.KnowledgeWorker/Schemas/substance-record.schema.json
[SeedValidate] seed:   /home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-008/backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json
[SeedValidate] record count: 71
[SeedValidate] validation failures: 0/71
[SeedValidate] canonicalId uniqueness: unique
[SeedValidate] PASS — all 71 records schema-valid (57 existing + 14 batch A), canonicalIds unique
```

### 4b. Count + uniqueness assertions (AC1)

`node -e "const d=require('backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json'); console.log(d.length, new Set(d.map(r=>r.identity.canonicalId)).size)"` → `71 71`
(count = 57 + 14; canonicalIds unique across the whole file; zero collisions with the
existing 57 IDs — checked programmatically in the build script before write).

### 4c. Full KnowledgeWorker test lane (run ONCE, to record — never to fix)

Command: `dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj`

Output (summary, verbatim):

```
Failed!  - Failed:     4, Passed:  1002, Skipped:     0, Total:  1006, Duration: 8 s - BioStack.KnowledgeWorker.Tests.dll (net10.0)
```

## 5. Diff-addition proof (AC5)

- `git diff --numstat` → `1926  0  backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
  (**pure addition; zero deletions** — git aligned the shared file tail, so not even the
  array's closing lines register as changed).
- The first 16,599 bytes of the file are byte-identical to the pre-batch file (append-only
  string surgery; build script asserted the 57 existing records unchanged by deep
  comparison before writing).
- `git diff --check` → clean (no whitespace errors).

## 6. Expected-failure list (KNOWN-PENDING-011 — never fixed in this parcel)

All four failures below are corpus-frozen expectations written against the 57-record seed.
Provenance check: with the pristine 57-record seed restored in this worktree,
`--filter "FullyQualifiedName~ResearchJobTests"` passes 9/9 — i.e. all four failures are
caused by the +14 batch-A expansion and are recorded here as expected; BIO-LOCAL-011 owns
updating them.

| test | failure | cause |
|---|---|---|
| `CorpusIdentityInventoryBuilderTests.Build_CurrentRepository_RecordsIdentityCoverageWithoutPromotionAuthority` | `Assert.Equal() Failure: Expected: 57 Actual: 71` (CorpusIdentityInventoryBuilderTests.cs:16) | builder reads the production seed; frozen count 57 → now 71 |
| `StructuralEvaluationReportBuilderTests.Build_CurrentArtifacts_RecordsPartialPolicyWithoutVerdict` | `Assert.Equal() Failure: Expected: 57 Actual: 71` (StructuralEvaluationReportBuilderTests.cs:37) | report embeds `CorpusInventory.SeedRecordCount`; frozen count 57 → now 71 |
| `ResearchJobTests.ResearchJob_Applies_Review_Decision_To_Promotion_Manifest` | `Assert.Equal() Failure: Expected: "create" Actual: "update"` (ResearchJobTests.cs:245) | fixture review decision `approve-creatine-fixture-001` approves "Creatine"; the new `creatine` seed record now exists, so the import-preview action flips `create` → `update` |
| `ResearchJobTests.PromotionImportDryRunJob_Succeeds_For_Safe_Create_Preview` | `Assert.Equal() Failure: Expected: 1 Actual: 0` (ResearchJobTests.cs:372) | same root cause: `creatine` now exists in the seed, so `CreatedCount` 1 → 0 (planned action `update`) |

## 7. Identity collisions — listed for human rule, never auto-merged

1. **`creatine` (new, batch A) vs `creatine-monohydrate` (existing seed record):** 007 §3
   recorded this identity-token collision (candidate:creatine vs seed:creatine-monohydrate).
   Both canonicalIds now coexist in the seed as separate records; nothing was merged or
   aliased (aliases were NOT copied — not on any cited line). The two `ResearchJobTests`
   expected failures in §6 are a direct consequence of `creatine` existing as a distinct
   canonicalId. **Human rule required** on whether these are one substance or two.
2. **`chorionic-gonadotropin` (new, batch A) vs `human-chorionic-gonadotropin` (existing
   seed record):** closely related identity tokens now coexisting as separate canonicalIds.
   No merge, no aliasing. **Human rule required** on identity relationship.

No externalIdentifier collisions (all new records carry null external identifiers).

## 8. Redaction attestation

No secrets, credentials, or connection strings were read, copied, printed, or committed.
The production Refresh runbook's `db-conn-string` secret was never accessed. All commands
run in this session (git, node, dotnet build/test/run, gh) are quoted above or in §4; no
command output containing sensitive values is included in this record. Validation and test
runs were offline (no database, no network source access; the dotnet restore used only the
project's pinned NuGet packages).

## 9. Session Handoff

- Starting commit: `0af46c9fa969aca94cb2e848d842eef19d29cad3` (origin/main)
- Ending commit: HEAD of `proof/bio-local-008-seed-batch-a` (single commit: seed + batch record; see PR)
- Files changed: `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` (+1926/−0); `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-008-batch-a-record.md` (new)
- Commands run: worktree reset to origin/main; `/tmp/bio008/build-batch-a.mjs` (citation-verified append); `dotnet run` SeedValidate harness (PASS 0/71); `dotnet test` KnowledgeWorker lane (4 expected failures / 1002 passed); provenance re-run of ResearchJobTests on pristine seed (9/9 pass); `git diff --numstat` / `--check`
- Tests passed: full-seed schema validation 71/71; canonicalId uniqueness; pure-addition diff; doctrine spot-checks on all 14 records
- Tests failed: 4 (all expected, corpus-frozen, listed in §6 — BIO-LOCAL-011 owns them)
- Decisions needed: human rule on the two identity collisions in §7; Gate 3 merge decision is the owner's
- Blockers: none
- Next safe action: independent review `bio_local_008_review_1`, then owner Gate 3 merge; **BIO-LOCAL-009 dispatches only after this PR is MERGED (serial chain — no parallel seed edits)**
- Do not touch: tests/schemas/loaders/API/frontend; the frozen count-test expectations (011's); the ambient untracked `package.json` in the coordinator checkout root; batches B/C identifiers
