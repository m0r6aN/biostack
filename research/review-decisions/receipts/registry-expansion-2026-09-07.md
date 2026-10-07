# Source Registry Expansion

Date: 2026-09-07. Base: `7314f1d`.
Implements the operator's ratified rulings on buckets B, C and D from the 2026-09-07 authorization audit.

The registry goes from **13 classes to 30**, with **307 per-item aliases registered by host verification**.
Nothing was promoted. Rights were granted only where explicitly ratified.

## Result

| Measure | Before | After |
|---|---|---|
| Claims requiring A1/A2 support | 224 | 224 |
| Pass under a registry-backed, rights-approved gate | 13 | **166** |
| Pass only by packet self-assertion | 199 | **32** |
| Registry-backed claims whose source is unauthorized for the field | 0 (nothing was backed) | **2** |
| Claims that would still fail field-use after class registration | 43 | **11** |

Database-free Research run: `Scanned=78 Created=78 Failed=0`. Manifest moves from 76 blocked / 1
review-required to **72 blocked / 5 review-required**, with **CandidatesForPromotion unchanged at 1
(Semaglutide)**. Melatonin, Pramlintide, Setmelanotide and Testosterone cypionate moved from Blocked to
ReviewRequired because their source references now resolve. **No compound became a promotion candidate.**

## Host Verification, Not Prefix Matching

Every alias was assigned by parsing the source's own URL and matching its **host** to a class. A `fda-` prefix
is not evidence of FDA authorship, and prefix-based aliasing would have converted a naming convention into an
authority grant. This is what kept the following out of the `fda` class:

- `fda-drugs-com-hcg-diet-illegal` (drugs.com) and `fda-manookian-nooh-melanotan-ii` — already tier `X`.
- `fda-ph-advisory-2019-182` — host `fda.gov.ph`, the **Philippine** FDA, now its own class.
- `fda-grn-931-creatine-monohydrate` — explicitly on a never-register list.
- 10 `fda-pcac-*` items — routed to the new lower `fda-advisory-committee` class, not to `fda`.

307 entries were registered. **277 entries across 152 hosts were deliberately left unregistered** because
their publisher is not in the ratified set — Wikipedia, vendor catalogues (caymanchem, medchemexpress), press
releases (prnewswire), investor relations, forums, and law-firm blogs among them. Leaving them unregistered is
the correct state: they cannot support an authority-gated claim.

## New Classes

| Class | Tier | Notes |
|---|---|---|
| `accessdata-fda` | A1 | Approved labeling and approval packages |
| `ema` | A1 | EPARs; does not establish US status |
| `federal-register` | A1 | A proposed rule is not a final rule |
| `health-canada` | A1 | Canadian status does not establish US status |
| `hsa-singapore` | A1 | Jurisdiction-limited |
| `fda-philippines` | A1 | Distinct regulator from the US FDA |
| `uspstf` | A2 | A draft recommendation is not the official position |
| `nci` | A2 | PDQ syntheses, not primary trial reports |
| `cdc` | A2 | Population-level guidance |
| `usada` | A2 | Prohibited status is not efficacy or safety |
| `ncbi-bookshelf` | B1 | StatPearls, LiverTox; chapters can be stale |
| `vada` | B1 | Private testing programme, not a standards body |
| `pmc` | B2 | A PMC copy is the same paper, not an independent family |
| `oxford-academic` | B2 | Publisher platform; assess each article |
| `mdpi` | B2 | Editorial rigour varies; corroborate elsewhere |
| `semantic-scholar` | C1 | Bibliographic metadata aggregator |
| `fda-advisory-committee` | C1 | **The ratified lower class.** Briefing packages are advisory-process material compiled partly from nominator submissions, not FDA determinations |

Tier assignments within the ratified publisher set are the lead's judgment and are recorded here so they can
be overridden cheaply. Every class carries explicit `limitations`.

## Amendments To Existing Classes

- **`dailymed` and `fda` gain `efficacy-claims`.** This was the fix for the 21-claim "efficacy" gap. A drug
  label's Clinical Studies section *is* the authoritative record of the pivotal trials, so the omission was in
  the class definition, not the packets. Scoped by a limitation: labeling supports efficacy **only for the
  approved indication as studied in the pivotal trials the label itself reports**, never off-label,
  general-population, or comparative efficacy, and a label predating a trial cannot speak to that trial.
  `dailymed` additionally records that an SPL is authored by the **labeler**, not by FDA or NLM, and that one
  labeler's SPL is not "the" label for a molecule.
- **`issn-position-stands`** — rights ratified to approved.
- **`drugbank`** — rights ratified to approved, but `acquisition.enabled` set to **false**: the API and MCP
  endpoints appear temporarily disabled while the publisher changes data delivery. Existing cited records stay
  usable; no new acquisition until access returns.
- **`peer-reviewed-paper` / `peer-reviewed-review`** — set to **rejected** and retired. These are category
  placeholders, not sources. Peer-reviewed literature must attach to a concrete publisher or host class
  (`pmc`, `pubmed`, `oxford-academic`, `mdpi`) so provenance and rights attach to a real publisher.
- **`community-monitoring`** — left `pending-human-legal`. No source in the corpus is tiered D or typed as
  community monitoring, so approving rights would grant nothing while carrying redistribution risk. Documented
  in its limitations; revisit when a concrete source is proposed.

## Deliberately Not Granted

**WADA rights remain `pending-human-legal`.** The 2026-09-07 ratification pass did not rule on WADA, and
redistribution rights for WADA content are a legal determination that was not made. Six claims still rest on
it and remain unsupported. This is the largest single remaining item in the self-assertion gap and needs an
explicit ruling.

## The Two Real Field-Use Gaps Now Visible

Registration made these detectable for the first time — both are correct findings:

- `afamelanotide-controversy-melanotan-conflation-001` is a `controversy` claim needing
  `misinformation-monitoring`, but is backed by `dailymed-scenesse-label`. A drug label is not a
  misinformation-monitoring source.
- `igf1lr3-identity-structure-001` is a `mechanism` claim backed by an `fda` class source, and `fda` is not
  authorized for `mechanism`.

## Remaining Self-Assertion Gap: 32 Claims

- **6** WADA, pending the rights ruling above.
- **11** sources whose host falls outside the ratified map (Semantic Scholar and NCBI items with unmatched
  URL hosts, bare `nct…` ids without a resolvable host, a VADA list, an MDPI review).
- **9** claims backed only by `fda-pcac-*`, which now correctly resolve to the C1 advisory class and therefore
  no longer carry A1/A2 support. This is the ratified outcome, not a regression; those claims are being
  relabelled separately.

Note one tool inaccuracy: the audit's bucket labelling still infers `fda-pcac-*` into the `fda` class by
prefix, so it reports those 9 under "registration missing" when they are in fact registered to
`fda-advisory-committee`. The gap count is right; the bucket label is stale.
