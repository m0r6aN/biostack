# BIO-LOCAL-014 — Seed Identity Consolidation (owner rules D-D)

Builder: `bio_local_014_builder`. Parcel: BIO-LOCAL-014. Branch:
`fix/bio-local-014-identity-consolidation`. Worktree:
`/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-014`.

Bounded seed-consolidation parcel executing coordinator decision **D-D** (owner's human
identity rules for the two open collisions), per Gate 2 dispatch
(`docs/INITIATIVES/biostack-local-readiness/dispatch/GATE2-BIO-LOCAL-014.md`).
This parcel merges/cross-references EXISTING record content only; no record content is
authored, interpreted, or invented.

## Exact revision and environment

- Worktree base commit (origin/main at worktree creation, `git rev-parse HEAD`):
  `b5f516db486ad368b64bc84089b5df48a0248cbf`
- UTC start (worktree creation): `2026-10-08T11:36:37Z`
- Tool versions: `dotnet 10.0.401`, `node v26.8.2`, Linux (devbox).
- Allowed surfaces (exactly four, all touched or created as listed in §7):
  `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`,
  `backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs`,
  `backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs`,
  and this evidence record.

## Owner rulings (quoted verbatim)

From `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`, section "D-D — Seed
identity-collision human rules (owner, 2026-10-08)":

> 1. **`creatine` vs `creatine-monohydrate`: DISTINCT and cross-referenced.** Both records remain.
>    "Creatine comes in many forms" — the records are related, not identical. Cross-references added
>    in whatever fields the frozen schema supports; where it has none, the mapping is documented in
>    the consolidation evidence record (unknown-honest: no invented schema fields).
> 2. **`chorionic-gonadotropin` vs `human-chorionic-gonadotropin`: SAME identity.** "Exactly the
>    same hormone; the word 'human' is just left out in some medical labels and shorthand."
>    Consolidate to ONE record; canonical = `human-chorionic-gonadotropin` (fuller standard term),
>    `chorionic-gonadotropin` recorded as a known shorthand/alias per schema capability (else
>    documented in the evidence record). Corpus total becomes 99; count-asserting tests updated
>    EXPLICITLY in the consolidation parcel.

## 1. Frozen-schema capability inspection (precondition for both rules)

`backend/src/BioStack.KnowledgeWorker/Schemas/substance-record.schema.json` (frozen schema)
was inspected before any edit:

- Root object: `additionalProperties: false`; properties are exactly `schemaVersion`,
  `recordType`, `identity`, `regulatory`, `mechanism`, `formulations`, `indications`,
  `dosingGuidance`, `compatibility`, `safety`, `interactions`, `stackIntelligence`,
  `supportiveGuidance`, `evidence`, `provenance`, `ops`.
- `identity` (`#/$defs/identity`): `additionalProperties: false`; required properties are
  exactly `canonicalId`, `canonicalName`, `slug`, `aliases`, `brandNames`, `synonyms`,
  `classification`, `compoundFamily`, `isCombinationProduct`, `activeMoieties`,
  `externalIdentifiers`.
- `stackIntelligence`, `ops`, `provenance`, `evidence` defs likewise `additionalProperties: false`.

**Finding: the frozen schema has NO cross-record reference field.** The only string-array
name fields in `identity` are `aliases`, `brandNames`, and `synonyms` — naming fields, not
record-to-record references; there is no `relatedSubstances`/`seeAlso`-style property
anywhere, and `additionalProperties: false` at every level forbids adding one.
Consequences, per the owner's own fallbacks:

- Rule 1's cross-reference mapping is **documented in this evidence record** (§2) — no
  invented schema fields, and no misuse of naming fields to imply identity the owner
  explicitly ruled out ("related, not identical").
- Rule 2's shorthand IS recordable in a schema-supported field: `identity.aliases`
  (string array). The shorthand is added there (§3); nothing else is invented.

## 2. D-D rule 1 — `creatine` vs `creatine-monohydrate`: DISTINCT, cross-referenced here

**Both records remain in the seed, byte-for-byte unchanged.** The corpus identity
inventory still reports both seed records and both of their identity-token collisions
(see §5) — the pair was not merged, renamed, or re-pointed.

Because the frozen schema has no cross-reference field (§1), the mapping is documented
here, per the owner's ruling:

| Record (canonicalId) | Related record | Relationship per owner ruling |
| --- | --- | --- |
| `creatine` (canonicalName "Creatine", batch-A draft, inactive/needsReview) | `creatine-monohydrate` | Related, NOT identical — "Creatine comes in many forms" |
| `creatine-monohydrate` (canonicalName "Creatine monohydrate", launch record, reviewed) | `creatine` | Related, NOT identical — same ruling, symmetric |

Pre-existing naming overlap (unchanged by this parcel, recorded for the reviewer): the
launch `creatine-monohydrate` record already carried `"creatine"` in its `identity.aliases`
before this parcel; that pre-existing alias was left exactly as-is. This parcel adds
nothing to either creatine record — adding each record's name to the other's
`aliases`/`synonyms` would misrepresent the owner's "related, not identical" ruling as
sameness, so the documentation path was taken instead.

## 3. D-D rule 2 — CG/HCG consolidation into one record

### Before (two records, excerpts)

`chorionic-gonadotropin` (batch-A draft record, removed by this parcel) — identity,
evidence, provenance, and ops blocks verbatim:

```json
"identity": {
  "canonicalId": "chorionic-gonadotropin",
  "canonicalName": "Chorionic gonadotropin",
  "slug": "chorionic-gonadotropin",
  "aliases": [],
  "classification": "Other",
  "compoundFamily": "unknown"
}
"evidence": {
  "overallTier": "Unknown",
  "claimSpecificEvidence": [],
  "evidenceGaps": [
    "No claim-level evidence was sourced this session; only the candidate/evidence-packet identifier-name citation(s) recorded in BIO-LOCAL-007-seed-gap-inventory.md were used to compile this record."
  ]
}
"provenance": { "sourceRecords": [ <2 internal-curation BIO-LOCAL-007 inventory citations — see merged verbatim in After> ],
  "reviewStatus": "draft" }
"ops": { "isActive": false, "needsReview": true, "completeness": "partial",
  "qualityFlags": ["batch-a","identifier-only-citation","compiled-from-inventory-citation","no-claim-content-this-session","unknown-honest-defaults"] }
```

`human-chorionic-gonadotropin` (launch record, the survivor) — identity and provenance
blocks verbatim:

```json
"identity": {
  "canonicalId": "human-chorionic-gonadotropin",
  "canonicalName": "Human chorionic gonadotropin",
  "slug": "human-chorionic-gonadotropin",
  "aliases": [
    "hCG",
    "chorionic gonadotropin",
    "Pregnyl",
    "Novarel"
  ]
}
"provenance": { "sourceRecords": [
  { "sourceType": "label", "title": "Prescribing information for Human chorionic gonadotropin", "publisher": "DailyMed", "lastCheckedAt": "2026-04-20T00:00:00Z" } ],
  "reviewStatus": "approved" }
"evidence": { "overallTier": "Strong",
  "claimSpecificEvidence": [ { "claim": "Human chorionic gonadotropin has evidence relevant to fertility and endocrine indications per label.", "tier": "Strong", "confidence": "high" } ] }
```

### After (one record)

`human-chorionic-gonadotropin` (canonical per the owner's ruling — canonicalName
"Human chorionic gonadotropin", unchanged) with exactly three edits:

1. **Shorthand recorded as alias** (schema-supported `identity.aliases`):

```json
"aliases": [
  "hCG",
  "chorionic gonadotropin",
  "chorionic-gonadotropin",
  "Pregnyl",
  "Novarel"
]
```

   The added string is the superseded record's own `canonicalId`/`slug` identifier
   `chorionic-gonadotropin`, moved verbatim into the alias field — existing content
   cross-referenced, nothing authored. (The space-separated display form "chorionic
   gonadotropin" was already a pre-existing alias of the survivor; both forms now appear.)

2. **Provenance sourceRecords merged verbatim** — the superseded record's two
   `internal-curation` BIO-LOCAL-007 inventory citations appended unchanged after the
   survivor's DailyMed label entry (deduplicated: both were unique):

```json
{ "sourceType": "internal-curation",
  "title": "BIO-LOCAL-007 inventory citation: research/input/candidates/peptide-serm-sarm-market-interest.v1.json:1177",
  "url": "research/input/candidates/peptide-serm-sarm-market-interest.v1.json#L1177",
  "lastCheckedAt": "2026-10-08T00:28:12Z" },
{ "sourceType": "internal-curation",
  "title": "BIO-LOCAL-007 inventory citation: research/input/evidence/chorionic-gonadotropin.evidence.json:12",
  "url": "research/input/evidence/chorionic-gonadotropin.evidence.json#L12",
  "lastCheckedAt": "2026-10-08T00:28:12Z" }
```

3. **The superseded `chorionic-gonadotropin` record removed cleanly** — the full record
   object was deleted from the seed array (full before-text preserved in this record's
   working notes and reproducible via `git diff` on the parcel commit).

**Claim-string arithmetic (nothing lost):** the superseded record's
`evidence.claimSpecificEvidence` was `[]` — zero claim strings. The survivor's one claim
("Human chorionic gonadotropin has evidence relevant to fertility and endocrine
indications per label.") is unchanged. Union of both records' claim strings = 1;
deduplication removed nothing (no identical strings existed across the pair). No claim
string was lost or altered.

**Strings of the superseded record that had no schema-supported home in an approved
record** (its `curationNotes`, `ops.reviewReasons`, `ops.qualityFlags`, and its
`evidenceGaps` note — all batch-A draft-process statements about the superseded record
itself, quoted verbatim in the Before excerpt above) are preserved here in this evidence
record per the documentation fallback in the owner's ruling, rather than being merged
into the approved survivor where they would misdescribe it.

**Tracking fields:** the survivor's unknown-honest tracking fields (`ops.*`,
`provenance.reviewStatus`, `lastReviewedAt`) are unchanged; the parcel does not promote,
review, or re-status anything.

## 4. Count arithmetic (AC3) and explicit test updates

- Corpus total: 100 records before − 1 consolidated (the `chorionic-gonadotropin` record)
  = **99 after** (verified: §5 file count and builder output).
- Seed-only canonical IDs: 84 before − 1 (both CG and HCG were seed-only; the seed-only
  `chorionic-gonadotropin` ID is gone, the survivor remains) = **83 after**.
  `SeedCandidateOverlapCount` unchanged at 16 (the only overlapping name in this pair's
  orbit is `creatine`, which is untouched; neither gonadotropin ID is a candidate ID).
- Identity-token collisions: 3 before − 1 resolved (the `chorionic-gonadotropin` token now
  has exactly one owning canonical ID — the surviving record, via its aliases) = **2
  after**. The two remaining collisions (`creatine`, `creatine-monohydrate`) are unchanged
  because the creatine pair stays DISTINCT per rule 1.

Explicit assertion changes in the two count-asserting test files (the only test files
that read the real seed corpus — all other KnowledgeWorker tests use the 1-record
`Fixtures/substances-seed.json`):

`backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs`:

- `Assert.Equal(100, snapshot.SeedRecordCount)` → `Assert.Equal(99, snapshot.SeedRecordCount)`
- `Assert.Equal(84, snapshot.SeedOnlyCanonicalIds.Count)` → `Assert.Equal(83, ...)`
- `Assert.Equal(3, snapshot.IdentityTokenCollisions.Count)` → `Assert.Equal(2, ...)`
- Collision key list `["chorionic-gonadotropin", "creatine", "creatine-monohydrate"]` →
  `["creatine", "creatine-monohydrate"]`
- The `chorionic-gonadotropin` owners assertion
  (`["seed:chorionic-gonadotropin", "seed:human-chorionic-gonadotropin"]`) removed;
  the two creatine owners assertions re-indexed `[1]→[0]`, `[2]→[1]` (owner lists
  character-for-character unchanged).
- Comments updated to record the BIO-LOCAL-014 arithmetic alongside the retained
  BIO-LOCAL-011 history notes.

`backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs`:

- `Assert.Equal(100, report.Payload.CorpusInventory.SeedRecordCount)` → `99`
- `Assert.Equal(84, report.Payload.CorpusInventory.SeedOnlyCanonicalIds.Count)` → `83`
- `Assert.Equal(3, report.Payload.CorpusInventory.IdentityTokenCollisions.Count)` → `2`
- Comments updated with the same arithmetic.

## 5. Validation outputs

File count (AC3):

```
$ python3 -c "import json; print(len(json.load(open('backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json'))))"
99
```

Full-file schema/validator pass — every record in the seed validated against the frozen
`substance-record.schema.json` (standalone scratch console project outside the repository
tree, `/tmp/bio014-scratch`, referencing the KnowledgeWorker project, rooted at this
worktree; same method as BIO-LOCAL-011; no repository file modified to produce it):

```
Validator=substance-record.schema.json
RecordsLoaded=99
RecordsValid=99
RecordsInvalid=0
```

Observed builder output (`CorpusIdentityInventoryBuilder` / `StructuralEvaluationReportBuilder`,
same scratch harness):

```
SeedRecordCount=99
CandidateRecordCount=16
EvidencePacketCount=78
SourceRegistryRecordCount=30
SeedCandidateOverlapCount=16
SeedOnlyCanonicalIds.Count=83
CandidateOnlyCanonicalIds.Count=0
CandidatesMissingEvidenceCanonicalIds.Count=0
EvidenceWithoutCandidateCanonicalIds.Count=62
ApprovedRightsSourceCount=7
ActiveOperationsSourceCount=7
AcquisitionEnabledSourceCount=7
RegistryAuthorizedEvidencePacketCount=2
IdentityTokenCollisions.Count=2
  collision key=creatine owners=[candidate:creatine,seed:creatine,seed:creatine-monohydrate]
  collision key=creatine-monohydrate owners=[candidate:creatine,seed:creatine-monohydrate]
ExternalIdentifierCollisions.Count=0
ModelInvoked=False
NetworkAccessed=False
Report.SeedRecordCount=99
Report.SeedOnlyCanonicalIds.Count=83
Report.IdentityTokenCollisions.Count=2
```

Count/corpus test run (the two updated test files plus the seed-loading validator,
ingestion-pipeline, and refresh-job tests):

```
$ dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj \
    --filter "FullyQualifiedName~CorpusIdentityInventoryBuilderTests|FullyQualifiedName~StructuralEvaluationReportBuilderTests|FullyQualifiedName~SubstanceRecordValidatorTests|FullyQualifiedName~IngestionPipelineTests|FullyQualifiedName~RefreshJobIntegrationTests"
Passed!  - Failed: 0, Passed: 18, Skipped: 0, Total: 18, Duration: 4 s
```

`git diff --check`: clean (no output — no whitespace errors).

## 6. Knock-on recorded-failure expectations (recorded, not fixed)

The KnowledgeWorker lane's previously recorded failures around create/update behavior on
the name "Creatine" were re-run and re-recorded. Full-suite run:

```
$ dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj
Failed!  - Failed: 2, Passed: 1004, Skipped: 0, Total: 1006, Duration: 7 s
```

| Test | Failure (verbatim) | Status vs this parcel |
| --- | --- | --- |
| `ResearchJobTests.ResearchJob_Applies_Review_Decision_To_Promotion_Manifest` | `Assert.Equal() Failure: Strings differ` Expected: `"create"` Actual: `"update"` (ResearchJobTests.cs:245) | **Unchanged.** Pre-existing recorded failure (first recorded in BIO-LOCAL-008, re-recorded in 009/010/011); root cause is the batch-A `creatine` seed record, which rule 1 keeps in the corpus. Verified failing identically at the base commit with this parcel's changes stashed. |
| `ResearchJobTests.PromotionImportDryRunJob_Succeeds_For_Safe_Create_Preview` | `Assert.Equal() Failure: Values differ` Expected: `1` Actual: `0` (ResearchJobTests.cs:372) | **Unchanged.** Same pre-existing batch-A root cause (`CreatedCount` 1 → 0 because `creatine` exists in the seed); verified failing identically at the base commit with changes stashed. |

**No knock-on shift was introduced by this parcel**: both tests fail with the same
messages and at the same lines before and after the consolidation (the creatine records
are untouched, and the fixture-driven promotion flow does not reference either
gonadotropin record). Per the contract these are recorded only — remediation (fixture or
promotion-preview expectations, or the creatine corpus question under rule 1) belongs to
a future, separately-ratified parcel. All other 1004 KnowledgeWorker tests pass.

## 7. Scope proof

```
$ git status --porcelain
 M backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json
 M backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs
 M backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs
?? docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-014-identity-consolidation.md
```

Exactly the four allowed surfaces. No schema, loader, builder, fixture, API, frontend,
contract, or research-input file was touched. No merges to `main` (delivery is PR-only;
Gate 3 merge is the owner's decision). The coordinator's checkout
(`/home/cmorgan76/Repos/biostack`) was used read-only (fetch/worktree-add only); all
changes were made in this parcel's worktree. The ambient untracked `package.json` in the
repository root was not touched.

## 8. Redaction attestation

- No production, staging, or shared connection string, credential, or secret appears
  anywhere in this record, in the worktree diff, or in any committed file. No credentials
  were used or generated by this parcel (local file + test runs only).
- No model was invoked and no network access occurred during validation; the builder
  snapshot confirms `ModelInvoked=False`, `NetworkAccessed=False` directly from observed
  output (§5), not by assertion. The only network operations in the parcel were git
  fetch/push and `gh pr create` against `origin` for delivery.
- Excerpts above quote only seed-record and coordinator-record content already committed
  in this repository; no customer data, raw source payloads, or restricted excerpts are
  included.

## Reviewer replay notes

- §1's schema capability finding is the load-bearing judgment call for rule 1: confirm
  `substance-record.schema.json` indeed has no cross-record reference field and that
  leaving both creatine records byte-identical (mapping documented in §2) is the
  owner-sanctioned fallback, not an omission.
- §3's alias addition is the only new string placed into the seed: confirm it is the
  superseded record's own `canonicalId` moved verbatim (not authored prose), and that the
  sourceRecords merge is verbatim with no duplicates.
- §4 asks the reviewer to check the two test files' new literals against §5's observed
  builder output character-for-character (99 / 83 / 2; two creatine collision keys with
  unchanged owner lists).
- §6's "no shift" claim is replayable: stash this parcel's changes in the worktree,
  re-run the two ResearchJobTests, and observe the identical failures at the base commit.

## Session Handoff

- Starting commit: `b5f516db486ad368b64bc84089b5df48a0248cbf` (origin/main at worktree
  creation, 2026-10-08T11:36:37Z).
- Ending commit: this parcel's commit on `fix/bio-local-014-identity-consolidation`
  (see PR). Reviewer: `bio_local_014_review_1` — review pending; Gate 3 merge is the
  owner's decision.
