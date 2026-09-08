# Follow-up to the source approval sheet

Prepared by Codex on 2026-09-08. Recommendations and factual corrections only; no source, role, acquisition route, content deletion or claim promotion is approved by this document.

## Current findings

The inventory now preserves query parameters and URL path case. The earlier assessment's description of the normalizer is historical and is not a defect in the inspected current version. The current snapshot reports 621 source IDs and 581 normalized URL groups. Calling those groups legally distinct works requires further evidence; URLs can represent different versions over time or different formats of one work.

Evidence snapshot:

- Inventory generation: `2026-09-08T15:13:27Z`.
- Inventory SHA-256: `6e5f6537c7fddaf797723f56c613bd5022762c78b74a3f81513853162f672452`.
- Script SHA-256: `91cfdd02086213837a2a27be2e239f695404f2d6a65b590cde2917c9b9087d40`.
- Registry SHA-256: `79aa44c5e8139de87a220da86b65e95d135a6598e4f0b41b3e066eb064e7c0e2`.

These hashes identify reviewed local bytes, not authorization. No deployed worker or production execution was inspected. The worker test results quoted by Claude were not independently rerun for these documentation changes.

**Superseding snapshot appended by Claude Opus 5, 2026-09-08.** The snapshot above is retained as
the record of the bytes Codex actually reviewed at `15:13:27Z` and is not edited. Three subsequent
naming-only corrections to the script (no change to collection, grouping or counting logic) produced
new bytes:

- Inventory SHA-256: `515ffd0d33fddcced818043ad970eee54df64ca819f1e8f46072b243f8871c94`.
- Script SHA-256: `085c7c3cf923947f9ecdb42d0dfcbadcb5dc5b760ee24312e235f811a65b4536`.
- Registry SHA-256: `79aa44c5e8139de87a220da86b65e95d135a6598e4f0b41b3e066eb064e7c0e2` (unchanged from the snapshot above).

The corrections were: `recordedRightsholder` renamed to `declaredPublisherUnverified`; the
`AGGREGATOR_HOSTS` comment, which had asserted Oxford University Press as the Basaria rightsholder,
rewritten to record the three distinct parties and to direct the reader to the work's own notice; and
the stdout label `distinct works` changed to `normalized URL groups`, since it described the grouping
as the unit rights review operates on. All three were raised in the addendum above. Counts are
unchanged: 621 source IDs, 581 normalized URL groups, 50 aliases in 10 shared-URL groups.

## S1 — precisionFDA records under the openFDA approval

**Recommendation: document and review a separate GSRS data/acquisition scope. Do not extend the existing openFDA approval by inference.**

Seven aliases in `fda` point to precisionFDA UNII web records: `unii-srs-afamelanotide`, `fda-gsrs-unii-6y24o4f92s`, `fda-gsrs-unii-chorionic-gonadotropin`, `fda-gsrs-unii-creatine`, `fda-unii-srs-m9l22y19h9`, `fda-unii-somatropin`, and `fda-gsrs-unii-yk11-z9748j6b0r`.

The recorded lane permission identifies `https://open.fda.gov/terms/`, API acquisition, and the reviewed openFDA method. That does not by itself establish a reviewed precisionFDA web acquisition scope. The configured enabled flag is not evidence of actual acquisition, and the current registry still differs from v1's approval binding.

The GSRS public-data permission is now **verified in a rendered browser**, resolving the earlier text-fetch uncertainty. On 2026-09-08, the official licensing page visibly stated that GSRS data is public domain under CC0 unless otherwise noted and permits commercial reuse. Its code/documentation use separate Apache 2.0 terms. This finding agrees with the earlier indexed text; it is not inferred from an empty HTTP response. [NCATS GSRS licensing](https://gsrs.ncats.nih.gov/licensing).

FDA identifies both the precisionFDA UNII search and NCATS's public GSRS as public UNII sources. Match each needed record and field to the licensed NCATS public distribution, check any record-specific exceptions and permitted access method, and record the resulting scope. That offers a concrete free commercial-data route; it does not clear all precisionFDA user uploads or establish terms for every platform feature. [FDA GSRS description](https://www.fda.gov/industry/fda-data-standards-advisory-board/fdas-global-substance-registration-system).

## S2 — ChEMBL is already a retained source

**Recommendation: add ChEMBL to the current rights-review inventory; assess a scoped commercial-use approval under its published licence. Do not classify it as absent or automatically treat it like unlicensed DrugBank use.**

Four unregistered source records are present: `chembl-2105395`, `chembl-chembl5095142`, `chembl-liraglutide-CHEMBL4084119`, and `chembl-molecule-chembl5314776`. Two claims have two stored quote fields; the inventory reports 125 characters. Direct inspection shows these fields contain pemvidutide/survodutide names and ChEMBL identifiers. They are not copied article paragraphs. That matters to the actual rights analysis, although it does not supply the missing governance record or validate their medical use.

ChEMBL's official documentation specifies CC BY-SA 3.0. It permits reuse with attribution and applicable share-alike obligations, and separately flags restrictions concerning certain calculations derived from commercial software. Commercial use is allowed under CC BY-SA; the licence is not a noncommercial restriction. Determine the obligations for the exact data and any redistributed adaptation before authorizing a broader derived dataset. Do not infer that every part of BioStack's unrelated software must be relicensed. [ChEMBL licensing FAQ](https://chembl.gitbook.io/chembl-interface-documentation/frequently-asked-questions/general-questions), [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/).

This corrects section D of the original approval sheet. UMLS/SNOMED were not assessed for licensing here; an absence of named source records is not a complete dependency/licence audit.

## Basaria article — publisher and copyright owner differ

The DrugBank-routed `basaria-2013-jgerontol-rct` record corresponds to DOI `10.1093/gerona/gls078`, PMID `22459616`, PMCID `PMC4111291`. Its accessible article notice identifies the author as the 2012 copyright holder and Oxford University Press as publisher on behalf of the Gerontological Society of America, with rights reserved. It is not established as commercially reusable merely because the OUP lane has a conditional pathway. [Article copyright notice](https://pmc.ncbi.nlm.nih.gov/articles/PMC4111291/).

Record the acquisition host, declared publisher, actual copyright notice and reuse permission as distinct evidence. The current script assigns `recordedRightsholder` from the corpus `publisher` string; that field should be understood as an unverified publisher declaration, not a verified ownership finding. The 41 aggregator-flagged records are a useful review queue, not an exhaustive census of content hosted by someone other than its author. No source rights change is made here.

## Duplicate consolidation proposal

**Recommend inventory-level grouping, with all aliases and evidence locators preserved. Do not authorize a bulk deletion or citation rewrite from these counts.**

The supplied current inventory contains **50 source IDs in 10 shared-URL groups**, leaving 40 excess identifiers relative to one identifier per group. The 50 figure counts members, not groups. Below is the complete group-level proposal; none of these groups is newly licensed by consolidation.

| Group | IDs | Proposed handling |
|---|---:|---|
| Drugs.com WADA S1 page | 2 | Group the route/document reference. Preserve the fact that this is a Drugs.com page, not an official WADA publication. |
| FDA consumer SARMs warning | 5 | Group the page identity; preserve title variants, retrieval dates and specific quoted passages. |
| FDA GLP-1 compounding supply update | 2 | Group the page identity; retain dated snapshots/status evidence because the page changes. |
| FDA bodybuilding-products warning | 5 | Group the page identity; preserve the original excerpts, dates and aliases. |
| FDA section 503A bulk-substances page | 2 | Group the page identity; retain rule/process version and date context. |
| FDA significant-safety-risks/withdrawn-nominations page | 24 | Group the parent page only. Keep compound/table-row/section and retrieval date on each citation; these aliases address different entries. |
| Frontiers post-cycle-therapy paper, DOI `10.3389/fchem.2025.1536858` | 2 | Group the matching DOI and URL identity; confirm version and any corrections before rewriting references. |
| LiverTox SARMs, NBK619971 | 3 | Group the Bookshelf document; preserve aliases and edition/update dates. |
| Ochsner RAD-140 liver-injury article | 2 | Group the matching article URL; retain early-online versus final-version information where present. |
| WADA 2026 Prohibited List PDF | 3 | Group the specific PDF/version; preserve S1.2 versus S2.3 section locators and rights-review status. |

The acceptance criteria for later implementation are that every original source ID still resolves, no claim/excerpt/section/retrieval history is lost, distinct application/label/DOI query values stay separate, changed versions remain distinguishable, and no approval, authority tier or independent-evidence count is increased by grouping. Work at the document-identity layer first; any packet or registry rewrite should have a reviewed migration map and appropriate existing validation.

---

**Third snapshot appended by Claude Opus 5, 2026-09-08.** Both snapshots above are retained unedited.
This one follows the inventory occurrence fix requested in Codex's PR #288 review — every packet
occurrence retained, `variantFields` surfaced, citation locators kept without quote text, JSON group
keys renamed, `schemaVersion` 2.0.0, and 11 behavioural tests added.

- Inventory SHA-256: `da013cef8a7adf83230b2f087446608c8c5fdd5a6592e62b5dc3efcb9a04ce36`.
- Script SHA-256: `ab86a43ce71165629e34e38420afbb52b91146c623e1fd53650c118800be94ae`.
- Tests SHA-256: `b1a3674ea39c20e9222309b50cf7150d4af9fdf452c115adf7319b6c77fd5659`.
- Registry SHA-256: `79aa44c5e8139de87a220da86b65e95d135a6598e4f0b41b3e066eb064e7c0e2` (unchanged across all three snapshots).

Counts changed by the fix: source occurrences now reported at 634 against 621 distinct ids, 10 ids
carry differing per-packet metadata, and normalized URL groups moved 581 → 582 because
`pubchem-cid-44200882` declares two URLs and now contributes a group for each. Claim citations (1014)
and stored quotes (1329 / 160,067 chars) are unchanged, asserted by test. Worker suite re-run after
the change: 8 failed / 875 passed, unchanged.

---

**Fourth snapshot appended by Claude Opus 5, 2026-09-08.** All snapshots above are retained unedited.
`origin` advanced during this work: main was merged into the branch, bringing PRs #283-#287 including
the registry-backed field-authority gate and one added `dailymed` alias
(`dailymed-soltamox-oral-solution-label`). The registry hash therefore moved again, through no edit by
either agent in this parcel.

- Inventory SHA-256: `78defbaea53ee37394d268f4abf8e5756d97e58b077b1a7a272a16108780f539`.
- Script SHA-256: `ab86a43ce71165629e34e38420afbb52b91146c623e1fd53650c118800be94ae` (unchanged from the third snapshot).
- Tests SHA-256: `b1a3674ea39c20e9222309b50cf7150d4af9fdf452c115adf7319b6c77fd5659` (unchanged from the third snapshot).
- Registry SHA-256: `248d8f02d9c812f524e7fa48eba3971de8de17b936537d74df15a82898445d8f` (**changed by the merge**, not by this parcel).

Post-merge corpus counts: 636 source occurrences, 623 distinct ids, 584 normalized URL groups, 1018
claim citations, 1343 stored quotes / 160,804 characters. Worker suite post-merge: **8 failed / 881
passed / 889 total** -- the same 8 failures, with 6 additional passing tests from
`EvidencePacketPreprocessorRegistryGateTests`. The earlier 875/883 figures in this file describe the
pre-merge tree and are left as recorded.
