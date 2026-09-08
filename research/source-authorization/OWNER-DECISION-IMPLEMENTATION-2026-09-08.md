# Implementation of Clint's source-permission decisions

Clint explicitly approved A1, B1, C2 and C3 and selected C1's **contain and re-source** option.
[The new decision receipt](owner-source-decisions-2026-09-08.v1.json) records those choices against the
reviewed proposal at `4e62d8d`. These decisions are no longer waiting for confirmation. The original
unsigned proposal and earlier decision records remain historical inputs.

| Decision | Recorded effect and implementation |
|---|---|
| A1 | Clint Morgan succeeds Johnathan Harper as `legal-rights-approver` for future owner review of permissions. The receipt contains the effective owners array; only that role's name changes. Its shape validates against the existing owner schema. The separate NCCIH reviewer/action requirement remains. |
| B1 | The reviewed public GSRS displayed-name, UNII and version scope is approved. The seven exact public record URLs and allowed fields are in the receipt; the method is documented below. No clinical fields or provenance migration are included. |
| C1 | DrugBank acquisition stays disabled. Worker export, historical artifact reads and the AI provider request now apply the same containment policy. Independent frontend tests and corpus comparison verify all 29 retained quote fields (3,501 characters). The [queue](drugbank-resource-queue-2026-09-08.json) covers 30 claims; none is yet counted as re-sourced. Production containment is not inferred from a local patch. |
| C2 | Two exact ChEMBL excerpts (57 and 68 characters) are conditionally approved, bound by text hash. [Attribution notices](CHEMBL-EXCERPT-ATTRIBUTION-2026-09-08.md) are recorded; any display/export must carry the notices and applicable redistribution terms. This does not activate a general ChEMBL source lane. |
| C3 | The two generic authorization classes are retired. Both already have rejected/disabled enforcement, which was independently verified. A [per-item source map](retired-placeholder-source-map-2026-09-08.json) preserves ten underlying source items and their citation/locator history for re-anchoring. |

The C3 map corrects a denominator in the proposal: **21 claim/source pairs belong to 17 distinct
packet/claim pairs**. The retirement decision still applies to the same two named classes; no scope
was added or removed by correcting that count. Retirement does not itself clear the underlying papers'
reuse rights or evidence quality.

## B1 acquisition method and boundaries

The documented method for the approved identity scope is manual review of the rendered public NCATS
GSRS substance page over HTTPS, at each exact URL listed in the receipt. The reviewer reads only its
displayed substance name, UNII and record version, recording the public source URL and observation
date as provenance. This method was used for the [seven-record verification](GSRS-PUBLIC-IDENTITY-VERIFICATION-2026-09-08.md).
It uses no account, paid API, bulk dataset download or linked commercial database. The applicable
data licence is recorded at [GSRS licensing](https://gsrs.ncats.nih.gov/licensing).

The observations establish public identity, not regulatory approval, efficacy or safety. Preserve
original precisionFDA citations and their retrieval dates; do not retroactively assign the newly
observed GSRS versions to them. Additional fields or a proposed automated access method need technical
verification of their actual values, notices and method before use. Only a material scope or permission
change needs a new owner decision; do not ask to reconfirm B1 merely to do that verification.

## Validation and remaining execution

Independent validation confirmed the exact user choices, the reviewed proposal/input hashes, the
single-role change, the effective owners array against the existing JSON schema, and both retained
ChEMBL text hashes and lengths. It also verified the placeholder map's ten items, 21 source references
and 17 distinct claims. Evidence is in `research/output/owner-decisions-20260908/decision-validation.json`.

The issued seven-source registry binding remains unchanged. These decisions approve the selected
scopes and actions; they do not ratify all 26 registry approval flags or the other 23 dispositions,
automatically activate sources, promote evidence, or approve a merge/deployment. A future executable
authorization batch must carry the actual decisions and retain all other applicable stage controls.

The decisions remove the earlier owner-choice dependency. Continue C1 containment and verified
replacement work, plus specific source-item review for the retired placeholders. Preserve unresolved
items as such rather than presenting a mapping, attribution file or passing test as full launch clearance.

The shared containment manifest is packaged into each application using
`node scripts/sync-source-rights.mjs`. Both relevant CI workflows run `--check` before building or
publishing to reject missing or stale copies. This follows the repository's existing product-contract
packaging pattern and keeps Docker contexts self-contained. The worker embeds its copy; the frontend
imports its copy within the configured Next.js root.

Historical input and output files remain preserved. The artifact reader filters returned packets, so
rewriting every historical run is not required to contain that read path. Any direct export, alternate
consumer, deployed copy or cache still needs its own verification before release. See the
[containment review](C1-CONTAINMENT-REVIEW-2026-09-08.md) for the bounded verification evidence.
