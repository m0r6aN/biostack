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

## Publish gate (AI council, no human approval)

Owner ruling: no human approval gates. Claim sign-off comes from the two-reviewer AI council
(`RunMode=CouncilReview`, PR #556/#557). Each reviewer returns a verdict per public claim; the
decision batch records them under `council.claims` with both reviewers' provider/model ids and the
batch's `reviewedAt` (the verdict date).

- Both reviewers approve at the claim's own tier: **consensus**, the claim publishes. That is two
  concurring reviewers, which also meets the two-reviewer rule for PROVEN IN HUMANS and MYTH.
- Both request changes at the same tier: consensus to hold; the claim stays NOT YET GRADED.
- Anything else (disagreement, or agreement on a different tier): **escalated to Clint** with
  distinct options in `research/owner-escalations/council-escalation-*.{json,md}`. His pick is
  recorded as `ownerDecision` on the claim's latest version and publishes it.
- A sign-off only counts if it is dated on or after the latest version's `verifiedAt`; a new
  version makes the record due for the council again.

The verdict, note and Hype Gap band are public only with that sign-off, `verifiedAt`, at least one
resolvable source and (PROVEN IN HUMANS) the qualifier. The API returns `status`
(`published` / `pending-council` / `changes-requested` / `escalated-to-owner`), `reviewedBy`
(`ai-council` / `owner`), `reviewers` (model ids) and `publishedAt` (verdict date). Re-check due:
PROMISING 3 months, everything else 6 months after `verifiedAt`.

## Hype Gap v0 (editorial)

`HypeScore = rubric (Rare 10 · Niche 30 · Common 55 · Everywhere 75 · Viral 95) × intensity (0.8–1.3)`,
`gap = max(0, HypeScore − EvidenceScore)` with EvidenceScore PROVEN 100 · PROMISING 70 · ANIMAL ONLY 35 ·
THIN 15 · MYTH 0; bands Low <15 · Moderate 15–39 · High 40–64 · Extreme ≥65. MYTH with HypeScore ≥ 40
shows Extreme. Only the band is public. The rubric-to-score numbers are a v0 choice for review.

## To publish

Configure the council's two reviewers (`Council__Reviewers__0__*`, `Council__Reviewers__1__*`), run
`Worker__RunMode=CouncilReview`, then Seed/Refresh. Only escalations need Clint.
