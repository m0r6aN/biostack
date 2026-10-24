# Claim verdicts and Hype Gap (v0)

Growth spec `02-verdict-system.md`, Phase 1. The **7-tier scale stays canonical**; verdicts are
plain-language labels derived from it (`BioStack.Domain/Evidence/ClaimVerdicts.cs`).

| 7-tier grade | Public verdict |
|---|---|
| Established | PROVEN IN HUMANS (only with "In [population], for [outcome], under trial conditions.") |
| Early Evidence | PROMISING |
| Plausible Theory | ANIMAL ONLY |
| Anecdotal Signal, Unsupported | THIN |
| Contradicted | MYTH |
| Hype Exceeds Evidence | Verdict of its `evidenceBasis` tier (THIN if none); Hype Gap band floored at High |
| Not graded | NOT YET GRADED |

## Data

`evidence.publicClaims[]` in the substance record: `id`, `statement` (as said online, never a dose),
`population`, `outcome`, `tier`, optional `evidenceBasis`, `qualifier`, `hype`, `note`, `sources[]`
(ids of `provenance.sourceRecords[].id`) and `versions[]` (`version`, `kind`, `tier`, `verdict`,
`sources`, `verifiedAt`, `verifiedBy`, `reviewers`, `changeReason`, `publishedAt`). Old versions are
never deleted; the latest version must match the claim's tier, derived verdict and sources.

## Publish gate

A verdict, note and Hype Gap band are public only when the latest version has `verifiedAt`,
`publishedAt`, at least one resolvable source, the qualifier (PROVEN IN HUMANS) and enough
reviewers (two for PROVEN IN HUMANS and MYTH, one otherwise). Until then the API returns
`verdict: "NOT YET GRADED"` with the statement and sources only. Re-check due: PROMISING 3 months,
everything else 6 months after `verifiedAt`.

## Hype Gap v0 (editorial)

`HypeScore = rubric (Rare 10 · Niche 30 · Common 55 · Everywhere 75 · Viral 95) × intensity (0.8–1.3)`,
`gap = max(0, HypeScore − EvidenceScore)` with EvidenceScore PROVEN 100 · PROMISING 70 · ANIMAL ONLY 35 ·
THIN 15 · MYTH 0; bands Low <15 · Moderate 15–39 · High 40–64 · Extreme ≥65. MYTH with HypeScore ≥ 40
shows Extreme. Only the band is public. The rubric-to-score numbers are a v0 choice for review.

## To publish (needs Clint)

Name reviewers, then for each claim add reviewer initials and `publishedAt` to its latest version
(an `edit` entry, no version bump) and re-run the KnowledgeWorker seed/refresh.
