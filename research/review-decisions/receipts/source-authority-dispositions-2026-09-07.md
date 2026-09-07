# Source Authority Dispositions

Date: 2026-09-07. Base: `4e0ca45`.
Scope: applies the operator's ratified rulings on bucket A (over-asserted tiers) and the rejected sources
identified by the 2026-09-07 authorization audit.

**Every tier in this pass was lowered. None was raised.** No claim statement, `evidenceTier`, `confidence`,
`fieldAuthorityRequired`, `claimType`, `context`, `sourceRefs`, or conflict was changed. No packet was cleared.

## Bucket A — corrected down to the registry class tier

The packet asserted stronger authority than its own source registry class permits for that publisher.

| Compound | Source | Was | Now | Registry class |
|---|---|---|---|---|
| creatine | `nih-ods-exercise-athletic-performance-hp` | A2 | C2 | nih-ods |
| vitamin-d3 | `nih-ods-vitamind-health-professional` | A1 | C2 | nih-ods |
| mazdutide | `pubmed-mazdutide-first-approval` | A1 | C1 | pubmed |
| vk2735 | `pubmed-search-vk2735-oral-2026` | A2 | C1 | pubmed |
| elamipretide | `clinicaltrials-gov-nct03323749` | A2 | C1 | clinicaltrials |
| gsk2881078 | `clinicaltrials-nct03359473` | A2 | C1 | clinicaltrials |
| mots-c | `clinicaltrials-nct07505745` | A2 | C1 | clinicaltrials |

## Rejected sources — tier set to `X`

`X` is the schema's excluded tier, which the pipeline already treats as low authority
(`SourceAuthorityMix.IsLowAuthorityOnly`). The source entry is retained rather than deleted because
`sourceRefs` has `minItems: 1`; deleting it would make packets schema-invalid and would erase the record of
what a claim had been resting on.

| Compound | Source | Why rejected |
|---|---|---|
| afamelanotide | `fda-manookian-nooh-melanotan-ii` | Not FDA-authored despite the `fda-` prefix |
| chorionic-gonadotropin | `fda-drugs-com-hcg-diet-illegal` | Hosted on drugs.com, not FDA |
| ghk-cu | `orrick-fda-pcac-2026` | Law-firm summary restating material available from originals |
| gonadorelin | `ferring-lutrepulse-pm-canada` | Manufacturer's own hosted monograph; re-anchor to Health Canada |
| gsk2881078 | `gsk-study-register-200182` | Sponsor's own study register; re-anchor to ClinicalTrials.gov |
| thymosin-alpha-1 | `fda-ta1-bulks-183892` | Rejected as an FDA-authored authority source |
| creatine | `fda-grn-931-creatine-monohydrate` | Manufacturer GRAS notice hosted by FDA; the filing is not evidence |

Creapure was already present in the creatine packet's `compound.aliases`, so the product remains searchable
without the GRAS filing being treated as evidence. No alias was added and no corpus or route membership
changed.

## Claims Left Without Authority Support — Read This

Four claims are now carrying an open gap. Three are safety-relevant. **None of these was withdrawn here**,
because withdrawing a claim is an editorial decision beyond a tier disposition, but none of them can be
published as it stands.

| Claim | Situation |
|---|---|
| `ta1-warning-immunosuppressed-gvhd-001` | **Safety warning.** `fda-ta1-bulks-183892` was its SOLE source and is now `X`. No remaining support. |
| `ta1-regulatory-bulks-list-evaluation-001` | Regulatory claim. Same sole rejected source. No remaining support. |
| `vitamind3-dose-context-rda-ul-001` | **Dose context** (safety-critical claim type). Its sole source, NIH ODS, is now C2, so it has no A1/A2 support. |
| `mots-c-studied-use-phase2a-prediabetes-trial-001` | Sole source ClinicalTrials.gov now C1; no A1/A2 support. |

Two further claims lost their only regulatory anchor but retain other sources:
`gonadorelin-warning-tumor-vision-001` and `gonadorelin-approved-indication-human-historical-001` — both need
re-anchoring to the Health Canada record rather than the manufacturer-hosted copy.

Every affected claim carries an `authority-disposition-2026-09-07` review flag naming the source, the reason,
whether it was that claim's sole source, and the required re-anchoring target.

## Verification

- All 78 packets parse and validate against `evidence-packet.schema.json`.
- Object-level diff vs `HEAD` across all 78: 12 packets changed; the only differences are source
  `authorityTier` values, appended `authority-disposition-2026-09-07` review flags, and one `ops.reviewReasons`
  / `ops.qualityFlags` entry per packet. Every tier change was asserted to be a lowering to `C1`, `C2`, or `X`.
- Edits were claim-scoped text substitutions, not a JSON round-trip, so there is no reformatting churn.
- Re-running `tools/research/audit-source-authorization.py`: **bucket A is now empty**, and the
  self-assertion gap falls from 199 to 185 claims.

## Still Open

Buckets B, C, and D are unaffected by this pass and are handled in the registry expansion. The field-authority
gate remains packet-self-asserted; per the ratified sequencing, it becomes registry-backed only after the
registry work lands.
