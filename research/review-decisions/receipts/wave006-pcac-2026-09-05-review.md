# Wave 006 PCAC Independent Review

Review date: 2026-09-05.
Worktree: `D:\Repos\BioStack\.worktrees\gtm-wave006-20260905`.
Reviewer: OpenCode, independent review seat; recommendation only, not the delegated lead's approval.
Scope: **Blocker-focused, not a full per-claim review.** All three packet files were read, but not all claims received independent external verification.

## Recommendation

**HOLD whole-packet promotion for Epitalon, Semax, and KPV. Promote none in this pass.** Preserve all existing `fieldAuthorityRequired` values, `ops.needsReview=true`, partial completeness, and unresolved/needs-human-review conflicts. This Markdown receipt is not a schema-valid approval batch and must not be consumed as one.

| Packet | Recommendation | Principal blockers |
| --- | --- | --- |
| `epitalon-evidence-expansion-001` | HOLD | Independent safety corroboration incomplete; reported committee tally not officially verified here; extract/tetrapeptide and free-base/acetate boundaries require claim-level reconciliation; excerpt locations drift from the live PDF. |
| `semax-evidence-expansion-001` | HOLD | Contraindications lack claim-matched A1/A2 support; Russian indication/label provenance remains unverified; current PCAC coverage incomplete; alias/formulation mapping unresolved. |
| `kpv-evidence-expansion-001` | HOLD | Withdrawn nomination incorrectly equated with absence of active consideration; reported committee tally not officially verified here; CAS does not disambiguate free base and acetate; independent human-safety support and full animal-study checks incomplete. |

## Governing Contract

Read `D:\Repos\BioStack\AGENTS.md`, `research/README.md`, `research/directives/03-category-evidence-agent.md`, `research/directives/04-preprocess-compile-review.md`, and `research/review-decisions/OPERATOR-DELEGATION-2026-08-28.md`.

- Directive 03, lines 23 and 29-36: safety/regulatory fields retain authority requirements; resolving review issues requires a materially different source family, not re-reading the original source.
- Directive 04, lines 19-34: independent verification, fail-closed unresolved claims, A1/A2 safety backing or explicit unknown/review status, and no runtime consumption of unpublished drafts.
- Delegation, lines 11-16: transfers who approves, not how; author/reviewer separation and independent safety confirmation remain mandatory. Missing authority cannot be waived by a favorable vote or this receipt.
- The user narrowed this task to one receipt and prohibited packet edits, authority-flag changes, approval manufacture, git writes, and production effects. No model council or additional worker artifacts were created; this is one independent seat, not a multi-model consensus.

## Promotion Blockers

### B1: Semax Safety Authority Is Missing

Claims: `semax-warnings-contraindications-001`, `semax-russia-approved-indication-001`.

Packet anchors: `research/input/evidence/semax.evidence.json:200-237,269-310,403-411`.

The contraindication claim has `fieldAuthorityRequired=true` but only `semax-peptogen-label-insert`, labeled B1 and hosted on a retailer Shopify CDN. The packet itself says no A1/A2 source addressing the listed contraindications was found. FDA source S1 discusses aggregation/immunogenicity and insufficient safety information; it does not support the listed pregnancy/lactation, pediatric, seizure, anxiety, hypersensitivity, or nasal-irritation particulars. An A1 source elsewhere in a packet does not supply authority for unrelated text.

The Russian indication claim also uses that third-party PDF. The FDA citation does not authenticate Russian marketing authorization or the two concentration-specific labels. No current official Russian registry record or authenticated official label was fetched in this bounded review. This is an unresolved provenance/authority gap, not a finding that the reported foreign label is necessarily false.

Acceptance needed before clearance: independently authenticated, current, claim-matched official authority for each formulation and jurisdiction, or an authorized content decision that leaves unsupported safety particulars explicitly unknown/review-gated. Do not lower `fieldAuthorityRequired` to bypass this blocker. Escalate substantive medical/foreign-label interpretation to the delegated lead and appropriate specialist.

### B2: Withdrawn Nomination Does Not Mean No FDA Consideration

Claims: `kpv-regulatory-nomination-withdrawn-001`, `semax-us-regulatory-status-001`, `epitalon-regulatory-503a-recommendation-001`.

Packet anchors: `kpv.evidence.json:226-265,313-328`; `semax.evidence.json:240-266`; `epitalon.evidence.json:263-320,363-366` (all under `research/input/evidence/`).

S1 still places all three in the withdrawn table, but its own content date is **2026-04-22**. A successful September access does not turn this into a comprehensive September regulatory-status determination. KPV's review flag at line 263, "withdrawn-nomination status means KPV is not currently under active FDA 503A evaluation," is not supportable: S8 explicitly identifies KPV for July consideration, while S4 contains FDA's substantive evaluation. S3 expressly explains discretionary FDA evaluation after withdrawal for Epitalon. The packet's later KPV note about "possible active 503A consideration" understates the already documented evaluation.

Semax's regulatory statement and Epitalon's warning review flag also describe nomination to the "Category 2 list." S1/S2 instead distinguish nomination for the statutory bulks-list process from FDA placement in an interim safety-risk category. The nomination history, interim category, advisory consideration, and codified positive list are separate facts.

Acceptance needed: reconcile those separate statuses and their dates at claim level. Do not treat a historical withdrawn table as proof of no subsequent evaluation, approval, ban, or new enforcement discretion.

### B3: Staff Recommendation, Committee Vote, and Legal Action Must Stay Separate

Claims: `epitalon-regulatory-503a-recommendation-001`, `kpv-regulatory-nomination-withdrawn-001`, `semax-us-regulatory-status-001`, `semax-russia-approved-indication-001`.

S3/S4 confirm the FDA briefing recommendations against adding Epitalon/KPV. These are evaluation proposals, not proof of the committee's subsequent vote or final agency action. Epitalon's 7-4-1 tally rests on the packet's professional legal analysis; KPV's 8-6-1 tally rests on its C1 tracker. Those external pages were not fetched here. S8 provides the official meeting scope and non-binding nature but contains no vote tally in the accessed text. Thus these tallies remain **not independently verified in this review**, not disproved.

S5 is a direct current-list check: the displayed eCFR is up to date through **2026-09-03**, lists six other substances in section 216.23(a), and lists none of Epitalon, Semax, or KPV. This supports absence from that codified positive list through its stated currency, not a blanket legal opinion or proof that no agency action occurred on September 4-5. No exhaustive Federal Register, agency-action, or approval-database search was performed. S2 also distinguishes conditional interim non-enforcement policy from list inclusion and says nominations on or after January 7, 2025 are not intended to enter its interim categories.

The packet's "overriding" FDA staff language for KPV at line 328 should not imply displacement of agency authority: a favorable committee vote can disagree with staff but cannot itself create legal eligibility or drug approval. S5(d) explicitly rejects equating even actual list inclusion with FDA approval/endorsement.

Acceptance needed: official post-meeting minutes/transcript or equivalent vote evidence for each claimed outcome, a dated check of subsequent agency action and applicable list/policy, and precise separation from drug approval. The absence of tally verification alone prevents full approval of the compound regulatory claims here.

### B4: Identity Conflicts Are Load-Bearing, Not Cosmetic

Claims: `epitalon-identity-mechanism-001`, `epitalon-studied-use-epithalamin-melatonin-001`, `epitalon-evidence-gap-longevity-marketing-001`, `kpv-identity-structure-001`, `kpv-regulatory-nomination-withdrawn-001`, `semax-identity-structure-001`.

Packet anchors: `epitalon.evidence.json:180-225,371-410,464-472`; `kpv.evidence.json:120-145`; `semax.evidence.json:12-20,132-163`.

S3 page 8 directly supports Epitalon/Epithalamin separation: synthetic tetrapeptide versus pineal polypeptide extract. It also identifies free base and acetate as different APIs/BDSs. The packet preserves the extract distinction, correctly, but its unresolved conflict cannot be cleared without paper-level formulation/methods checks. This pass did not examine the full methods of PMID 15452611 or establish all of the broad no-human-trial/no-independent-replication assertions.

S7 directly confirms that PubChem CID 125672's synonym list contains CAS `67727-97-3`, so the packet's inability to obtain a live PubChem identifier record is no longer an access obstacle for this specific lookup. However, the same synonym payload contains both free-acid and acetate names. S4 pages 9-10 explicitly documents reused CAS numbers, distinct base/acetate formulas, and a CoA naming one BDS while giving the formula of another. The CAS match does **not** resolve salt/formulation identity or validate a marketed product's purity. Keep product/formulation claims gated.

Semax's alias array includes both `ACTH(4-7)-PGP` and `ACTH(4-10)-PGP`, whereas its family and claim describe an ACTH(4-7) fragment with the seven-residue sequence. The equivalence of those alias strings was not authenticated here. Treat the latter as an unresolved alias-mapping risk, not a confirmed chemically equivalent identifier. Do not conflate reference peptide, free base/acetate, 0.1%/1% licensed products, or gray-market variants.

### B5: Re-Fetch Is Not Independent Safety Confirmation

Claims: `epitalon-warning-immunogenicity-carcinogenicity-001`, `epitalon-regulatory-503a-recommendation-001`, `kpv-regulatory-nomination-withdrawn-001`, `kpv-evidence-gap-no-human-trials-001`, `kpv-studied-use-animal-colitis-001`.

S1/S3/S4 substantiate FDA-attributed concerns and evidence gaps. They do not demonstrate human carcinogenicity, establish safe human dosing, prove no risk, or exhaust all current human literature. Epitalon's telomere concern should remain theoretical and FDA-attributed, not a proven cancer outcome or an invented contraindication.

KPV's line 307 calls its FDA briefing "a source family distinct from the cited FDA-withdrawn-table." That is not a materially different family under the directives: both are regulator evidence. Likewise, FDA summarizing a cited primary paper is not an independent experimental replication. The second animal paper and exact 100 micromolar protocol were not independently checked in this pass; neither their attribution nor replication independence is approved here.

Epitalon excerpt locations also need refresh: the accessed PDF places no-approved-component text on page 9, the telomerase/cancer wording on page 35, the no-clinical-safety conclusion on pages 38/41, and the insomnia-effectiveness conclusion on page 40. The packet instead points to pages 7, 34, and 37 for these items. Similar wording exists, but stale locations weaken traceability and cannot be treated as exact current citations.

Acceptance needed: claim-matched distinct-family corroboration where required, accurate live source locations, and preservation of unresolved clinical uncertainty. Do not count repeat FDA pages or repeat downloads as new independent families.

## Article Flags

File: `frontend/src/app/knowledge/insights/fda-pcac-2026-peptide-vote/page.tsx`.

| Compound | Source flag | Effect of this branch |
| --- | --- | --- |
| KPV | `live: false`, line 25 | No dossier link; displays "dossier in final review". |
| Epitalon | `live: false`, line 46 | No dossier link; displays "dossier in final review". |
| Semax | `live: false`, line 53 | No dossier link; displays "dossier in final review". |

The rendering branch is at lines 177-192. These flags suppress links, **not summaries**. Semax's Russian-registration assertion at line 56 remains visible in the source-rendered article even while its supporting packet is held. This is source-code inspection, not verification of the deployed site or database publication state. Leave all three flags false.

Additional article findings, not edits:

- Lines 164-165 say "Each entry links to the full BioStack dossier," inconsistent with the three non-link branches.
- Lines 142-150 overstate "No agency reviewed ... efficacy" and that list consideration "says nothing" about effectiveness. S8 identifies uses FDA reviewed, S3/S4 assess effectiveness, and S5(c)(3) expressly includes evidence of effectiveness in the criteria. The sound distinction is **not drug approval or established efficacy**, not **no review of efficacy**. The article's broad "no such label exists" also sits awkwardly with its own Semax foreign-registration summary and the packet's foreign-label claim.
- Lines 124-126 and 202-206 distinguish recommendation from action, which is correct in principle, but current-status assertions need dated official-action verification. The source list at lines 218-229 gives no clickable official vote evidence. No article text or flags were changed.

## Access Ledger

All accesses below occurred on **2026-09-05**. Quotes are literal excerpts of the fetched text; PDF whitespace/line wraps were normalized for readability. No text is attributed to an unfetched linked document. Eight distinct official-host URLs were attempted, including one 404; seven documents were successfully retrieved. There were **11 explicit GET attempts**: eight initial web fetches plus three repeat PDF GETs for in-memory text extraction. Epitalon PDF: three GETs total; KPV PDF: two. Redirect hops are not separately counted.

Source-family accounting: conservatively **two broad evidence families accessed**, regulator/legal authority (FDA and the codified FDA rule on eCFR) and structured chemical database (PubChem). Multiple FDA documents and eCFR hosting do not automatically establish a new scientific/source family. New document, new host, independent retrieval, and materially different family are different concepts. No safety-critical claim is cleared by this ledger.

### S1: FDA Safety-Risk / Withdrawn Table

Exact accessed URL: https://www.fda.gov/drugs/human-drug-compounding/certain-bulk-drug-substances-use-compounding-may-present-significant-safety-risks

Result: readable HTML; content current as of 04/22/2026; one GET. Already cited by all three packets: **original-source re-check, not new-family corroboration**.

Excerpts:

> This list of bulk drug substances previously in category 2 of the interim policies were withdrawn by the nominators.

> Compounded drugs containing epitalon may pose risk for immunogenicity for certain routes of administration due to the potential for aggregation and peptide-related impurities.

> FDA has not identified any human exposure data on drug products containing KPV administered via any route of administration.

> Compounded drugs containing semax (heptapeptide) may pose risk for immunogenicity for certain routes of administration due to the potential for aggregation and peptide-related impurities.

Application: B1/B2/B5. All three rows occur under "Bulk drug substances nominated but withdrawn," not the active category-2 table.

### S2: FDA 503A Process and Interim Policy

Exact accessed URL: https://www.fda.gov/drugs/human-drug-compounding/bulk-drug-substances-used-compounding-under-section-503a-fdc-act

Result: readable HTML; content current as of 05/14/2026; one GET. Already an Epitalon packet source; new document relative to Semax/KPV source lists but **same regulator family**.

Excerpts:

> The agency also will continue to evaluate bulk drug substances that have been nominated with sufficient supporting information and address those substances on a rolling basis through notice-and-comment rulemaking.

> FDA does not intend to take action against a compounder for compounding drugs using bulk drug substances listed in category 1, provided that the conditions described in the guidance document are met.

> This guidance document states the agency does not intend to place bulk drug substances nominated on or after January 7, 2025, into these categories.

Application: B2/B3; process explanation, not compound-specific final action or an FDA approval search.

### S3: FDA Epitalon Briefing

Exact accessed URL: https://www.fda.gov/media/193345/download

Result: PDF retrieved; initial webfetch returned binary rather than useful text. Re-fetched twice using `py -B`, `urllib.request`, `BytesIO`, and installed `pypdf`, all in memory. First extraction failed on console encoding after printing several pages; UTF-8 retry succeeded. No PDF or extraction file was manually written. **Three GETs of one original packet source**, not three sources.

Excerpts:

> The nominations were withdrawn and FDA is evaluating the substances at its discretion.

Page 8; footnote marker omitted.

> Epitalon (free base) and epitalon acetate are different active pharmaceutical ingredients (APIs) and hence are considered different BDSs.

Page 8.

> however, FDA considers epitalon and epithalamin as different substances. Epithalamin is a polypeptide complex extracted from the pineal gland

Page 8, footnote 6.

> There is no applicable United States Pharmacopeia (USP) or National Formulary (NF) drug substance monograph for epitalon (free base) or its acetate form, and neither is a component of an FDA-approved drug.

Page 9.

> Specifically, epitalon has been shown to activate telomerase and lengthen telomeres, and longer telomeres are generally associated with increased risk for cancer.

Page 35, FDA's theoretical mechanistic concern; not an observed human cancer signal.

> Based on available information, we conclude that there are no clinical data to support the safety of epitalon (free base) or epitalon acetate when used in humans.

Pages 38 and 41.

> Accordingly, we propose not adding epitalon (free base) or epitalon acetate to the 503A Bulks List.

Page 41.

Application: B2-B5. Re-check for regulatory/warning claims. For the identity claim, regulator analysis adds a different type of assessment from its structured-database/vendor references, but this PDF already exists elsewhere in the packet and cites underlying literature. It is not new human-study replication or sufficient to clear the formulation conflict wholesale.

### S4: FDA KPV Briefing

Exact accessed URL: https://www.fda.gov/media/193346/download

Result: PDF retrieved; initial binary webfetch followed by one successful in-memory text extraction. **Two GETs of one original packet source**.

Excerpts:

> The CAS number for KPV acetate is the same as that for KPV (free base) in most public references

Page 9, Table 1 footnote. Table 1 gives free-base CAS `67727-97-3`, free-base formula/mass `C16H30N4O4/342.43`, and acetate formula/mass `C16H30N4O4.CH3COOH/402.5` (formula separator normalized to ASCII).

> Due to inconsistencies in the nomination, it is unclear which KPV-related BDS the nominator intended to nominate. For example, the certificate of analysis (CoA) submitted with the nomination refers to one BDS by name in the title and a different BDS by the molecular formula.

Page 9.

> The molecular formula of KPV (free base) is C16H30N4O4 and its molecular weight is 342.43 g/mol. Its chemical structure is shown in Figure 1. There is no CoA for KPV (free base) in the nomination.

Page 10.

> FDA is particularly concerned about the lack of any human data on drug products containing these substances administered via any route of administration, including lack of information to assess immunogenicity or aggregation of KPV-related substances. Therefore, potential safety risks associated with the use in humans are unknown.

Page 29.

> Accordingly, we propose not adding KPV (free base) or KPV acetate to the 503A Bulks List.

Page 29.

Application: B2-B5. Regulator identity assessment is different from the identity claim's PubChem-only family, but partly relies on public supplier data and does not validate physical products. For regulatory/evidence-gap claims this is already-cited regulator evidence, not a distinct family from the FDA withdrawn table. The nomination appendix is not treated as an independent FDA finding.

### S5: Codified 503A Positive List

Exact accessed URL: https://www.ecfr.gov/current/title-21/chapter-I/subchapter-C/part-216/section-216.23

Result: readable eCFR text; one GET. Page states "Displaying title 21, up to date as of 9/03/2026. Title 21 was last amended 8/31/2026." It also states the content is authoritative but unofficial. New direct codified-rule document relative to the packet source arrays; **not a new independent biomedical family**.

Excerpts:

> (a) The following bulk drug substances can be used in compounding under section 503A(b)(1)(A)(i)(III) of the Federal Food, Drug, and Cosmetic Act.

Complete paragraph-(a) inventory: Brilliant Blue G; cantharidin (topical only); diphenylcyclopropenone (topical only); N-acetyl-D-glucosamine (topical only); squaric acid dibutyl ester (topical only); thymol iodide (topical only). None of the three reviewed peptides appears.

> (3) The available evidence of the effectiveness or lack of effectiveness of a drug product compounded with the substance, if any such evidence exists; and

Paragraph (c), evaluation criteria.

> Any person who represents that a compounded drug made with a bulk drug substance that appears on this list is FDA approved, or otherwise endorsed by FDA generally or for a particular indication, will cause the drug to be misbranded under section 502(a) and/or 502(bb) of the Federal Food, Drug, and Cosmetic Act.

Paragraph (d). Application: B3 and article overstatement. No inference of legal advice, complete enforcement-policy coverage, or September 4-5 action coverage.

### S6: Failed Meeting URL

Exact attempted URL: https://www.fda.gov/advisory-committees/advisory-committee-calendar/july-23-24-2026-pharmacy-compounding-advisory-committee-meeting-announcement-07232026

Result: HTTP 404; one GET; no substantive excerpt or evidence credit. Counts toward the eight distinct attempted URLs, not toward successful documents or corroboration.

### S7: PubChem KPV Synonyms

Exact accessed URL: https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/cid/125672/synonyms/JSON

Result: readable official JSON; one GET. Same PubChem record/family as the existing CID 125672 source, accessed through a working API representation rather than a JavaScript-dependent page. **Not a distinct-family independent confirmation**.

Exact JSON values include `"CID": 125672`, `"67727-97-3"`, `"Lys-pro-val"`, `"L-Lysyl-L-prolyl-L-valine"`, `"Msh (11-13)"`, `"H-Lys-Pro-Val-OH AcOH"`, `"a-MSH (11-13) (free acid)"`, and `"H-LYS-PRO-VAL-OH ACETATE SALT"`.

Application: B4. Confirms the identifier is present in the current record; mixed salt/free-acid synonyms do not establish their interchangeability.

### S8: Official July PCAC Meeting Page

Exact accessed URL: https://www.fda.gov/advisory-committees/advisory-committee-calendar/july-23-24-2026-meeting-pharmacy-compounding-advisory-committee-07232026

Result: readable HTML; content current as of 08/06/2026; one GET. New document relative to these three packet source lists but **same regulator family**. Correct URL located in a local source reference after S6 failed.

Excerpts:

> Advisory committees make non-binding recommendations to the FDA, which generally follows the recommendations but is not legally bound to do so.

> On July 23, 2026, the Committee will discuss the following bulk drug substances being considered for inclusion on the 503A Bulks List:

The following agenda includes KPV free base/acetate, with uses evaluated "Wound healing and inflammatory conditions." The July 24 agenda includes Semax free base/acetate, "Cerebral ischemia, migraine, and trigeminal neuralgia," and Epitalon free base/acetate, "Insomnia."

> The chart below identifies the use(s) FDA reviewed for each of the bulk drug substances being discussed at this advisory committee meeting.

The accessed page links briefings, presentations, questions, agenda, and webcast information, but the returned text supplies no committee vote tallies. Its link to the Semax briefing (`/media/193348/download`) was discovered, **not fetched**, because the eight-URL budget was exhausted. Linked webcasts, minutes, other briefings, and docket records are not represented as accessed evidence.

Application: B2/B3 and article findings. Agenda scope is not proof of the final vote.

## Coverage and Effects

- Full packet text read: Epitalon 6 claims, Semax 7 claims, KPV 5 claims. No blanket approve/confirmed verdict is issued for any of these 18 claims. Coordinator validation corrected the original KPV count from six to five; this does not change the blocker-focused scope.
- Externally checked selected regulatory, safety-attribution, and identity blockers only. Broad efficacy/mechanism, no-trials assertions, trial methods/doses, foreign-label currency, and all vendor-marketing particulars were not fully verified. These omissions remain coverage blockers for whole-packet approval.
- Existing conflict IDs retained as open: `epitalon-conflict-formulation-001`, `epitalon-conflict-telomerase-safety-001`, `semax-conflict-approval-vs-evidence-rigor-001`, `kpv-conflict-pcac-vote-vs-staff-001`. This receipt neither resolves nor rewrites them.
- No source-registry authorization audit, schema/compiler run, promotion-readiness execution, or deployed-site verification was performed. Required authority and independent-family gates have not been demonstrated satisfied.
- Tools: direct reads/searches and official-source GETs; in-memory PDF extraction with the installed Python launcher. `rtk` and `python` were unavailable by those command names; `py` worked. No installation, generated research run, model delegation, service start, production call, credential access, or git command was used.
- Sole intentional workspace write: this receipt, via `apply_patch`. No packet, article, source registry, review-decision batch, flags, seeds, or configuration changed. Tool-managed output capture is not an authored research artifact.

Final recommendation: **HOLD / HOLD / HOLD**. The PubChem access improvement and verified FDA-attributed statements are useful evidence, not permission to manufacture packet approval.
