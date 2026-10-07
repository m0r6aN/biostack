# Dulaglutide replacement candidate

The current mechanism claim relied on a contained DrugBank excerpt and a Wikipedia structure excerpt. This change replaces that wording with a narrower, source-cited factual paraphrase of [FDA clinical pharmacology review 5067311](https://www.fda.gov/media/164321/download), using pages 3 and 7. Every new `quote` field is null. The claim and packet remain review-required; this is not a publication decision.

The proposed text describes the receptor class, weekly subcutaneous use, approximate five-day half-life, and the review's historical adult-T2D indication background. The unverified Fc-fusion/renal-clearance rationale is omitted from both the claim and its compound/formulation metadata. Omission does not establish that those original facts are false. Dose amounts and all safety-critical statements, evidence arrays, tiers and confidence values remain unchanged.

The approved-indication review flag and packet operations note no longer treat DrugBank as current corroboration. The FDA review belongs to the same regulator/label family as the existing label; it does not close the independent-family review requirement. The existing DrugBank accession remains unchanged as identity metadata, and its source definition is marked historical provenance only.

## Rights and provenance

FDA's [website policy](https://www.fda.gov/about-fda/about-website/website-policies) allows reuse of its public-domain content unless otherwise noted. However, the proposed page-6 mechanism and page-7 elimination excerpts closely match sections 12.1 and 12.3 of [Lilly's copyrighted label, DailyMed version 60](https://dailymed.nlm.nih.gov/dailymed/lookup.cfm?setid=463050bd-2b1c-40f5-b3c3-0a04bb433309&version=60). Matching wording does not establish who originally wrote it. We therefore make no agency-only authorship claim and retain neither passage. The candidate contains factual paraphrases, exact source/page citations and no copied excerpts or figures.

The [candidate record](dulaglutide-resource-candidate-2026-09-08.json) records the downloaded PDF's SHA-256, access method, visually verified pages, changed fields, historical revision, removed-excerpt hashes and remaining gates. The original packet and issued August 29 review records remain preserved in Git at `c05625caf4d633e428bd89f0334029e3c58b9f67`; restricted excerpts are not copied into a second audit artifact.

The new item has its own source ID and remains unregistered. It does not silently reuse the `fda` lane's openFDA API authorization for a manually downloaded PDF. The registry, its bound decisions and historical review receipts are unchanged.

## Status and next review

One queue row is now `replacement-prepared-review-required`. The other 29 rows are unchanged. `claimsActuallyReSourced` remains **0** until the item/method review and evidence review are completed. Baseline withheld-excerpt counts remain historical; the live candidate no longer contains the target DrugBank or Wikipedia excerpts.

Review should assess the new item and method, ratify or revise its proposed scientific A1 tier, and accept or revise the narrowed mechanism wording and pharmacological classification. The A1 scientific tier grants no rights or operational authority. All independent-family and safety-critical gates remain open. Promotion, merge, deployment and production containment are separate outcomes. The inventory tool reports current source usage; it does not generate this queue or grant approval.
