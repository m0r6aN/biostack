# BIO-PAIRWISE-001 — P0 Label Interaction Census
Tested SHA: `c6205fae91357e30a1936f9e2318a84dbf251774`  
Census generated (UTC): `2026-10-07T23:58:17Z`

Scope: `research/input/evidence/*.evidence.json` (78 files). String-level census only. No interpretation of what any token means; counts and `file:line` citations only. Absence of a cited FDA/DailyMed source or a classified product-label source is not evidence that no interaction exists for that compound.

## Method tokens (reproducible, literal-string classification)

Each `sources[]` entry in an evidence packet is tested against these literal-string rules, applied in order, against the concatenation of its `sourceType`, `publisher`, `title`, `url`, `sourceId` fields (lowercased):

1. `sourceType` in `{label, product-label, regulator-label, regulator-label-database, regulator-label-repository, regulator-label-search}` → token `product-label`
2. blob contains `dailymed` AND one of `{labeling, spl, prescribing, package insert, label}` → token `product-label`
3. blob contains `warning letter` → token `warning-letter`
4. blob contains one of `{approval package, drugs@fda, nda , bla , accessdata.fda.gov/drugsatfda}` → token `approval-package`
5. `sourceType` in `{government-fact-sheet, government-science-communication, professional-guidance, professional-guidance-draft, regulatory-analysis, regulatory-history, government-surveillance-report}` OR blob contains one of `{safety communication, advisory, alert, consumer update}` → token `advisory`
6. else, if the source is FDA/DailyMed-lane (blob contains `dailymed`, `fda`, or `food and drug administration`) and `sourceType` in `{regulator, regulator-database, regulator-structured-database, structured-database, database}` → token `undetermined`
7. else, if FDA/DailyMed-lane → token `other`
8. else the source is not counted as an FDA/DailyMed lane for this packet at all.

'Label with interactions section present': a source classified `product-label` is marked `interactions-section-cited` only if some `claims[].extractedEvidence[]` entry whose `sourceRef` equals that source's `sourceId` has `pageOrSection` or `quote` text matching the literal substring `interaction` (case-insensitive). Repository-wide check: zero matches of `drug interaction`, `interactions? section`, `7 interactions`, or `section 7` (case-insensitive) across all 78 files (`grep -rniE` over `research/input/evidence/*.evidence.json`, 0 hits). 27 literal case-insensitive hits for the bare substring `interaction` exist in the corpus; none of them are `extractedEvidence` entries keyed to a `product-label`-classified `sourceId` (verified against the full per-packet table below). Per the spec's instruction to report `undetermined` rather than infer, the interactions-section column below is `undetermined` for every packet that has a product-label source, and `not-applicable (no product-label source)` otherwise.

## Per-packet census (all 78 evidence packets)

| Compound (canonicalName) | file | total sources | FDA/DailyMed lane cited | classifications present (token:count) | product-label sourceIds | interactions-section present |
|---|---|---|---|---|---|---|
| AC-262536 | `research/input/evidence/ac-262536.evidence.json` | 7 | yes | undetermined:1 | — | not-applicable (no product-label source) |
| ACP-105 | `research/input/evidence/acp-105.evidence.json` | 9 | yes | advisory:1, undetermined:1 | — | not-applicable (no product-label source) |
| Afamelanotide | `research/input/evidence/afamelanotide.evidence.json` | 8 | yes | approval-package:2, product-label:1, undetermined:2 | `dailymed-scenesse-label`(research/input/evidence/afamelanotide.evidence.json:34) | undetermined |
| Amycretin | `research/input/evidence/amycretin.evidence.json` | 5 | no | — | — | not-applicable (no product-label source) |
| Andarine | `research/input/evidence/andarine.evidence.json` | 11 | yes | undetermined:1, warning-letter:1 | — | not-applicable (no product-label source) |
| AOD-9604 | `research/input/evidence/aod-9604.evidence.json` | 7 | yes | advisory:1, undetermined:1 | — | not-applicable (no product-label source) |
| Bazedoxifene | `research/input/evidence/bazedoxifene.evidence.json` | 11 | yes | approval-package:1, product-label:1 | `dailymed-duavee-label`(research/input/evidence/bazedoxifene.evidence.json:35) | undetermined |
| BPC-157 | `research/input/evidence/bpc-157.evidence.json` | 7 | yes | advisory:2, undetermined:2 | — | not-applicable (no product-label source) |
| Bremelanotide | `research/input/evidence/bremelanotide.evidence.json` | 10 | yes | approval-package:1, product-label:1, undetermined:1 | `dailymed-vyleesi-label`(research/input/evidence/bremelanotide.evidence.json:33) | undetermined |
| Caffeine | `research/input/evidence/caffeine.evidence.json` | 2 | no | — | — | not-applicable (no product-label source) |
| Cagrilintide | `research/input/evidence/cagrilintide.evidence.json` | 20 | yes | other:2, product-label:1, undetermined:4 | `dailymed-cagrilintide-search-2026`(research/input/evidence/cagrilintide.evidence.json:141) | undetermined |
| Cardarine | `research/input/evidence/cardarine.evidence.json` | 11 | yes | undetermined:2 | — | not-applicable (no product-label source) |
| Chorionic gonadotropin | `research/input/evidence/chorionic-gonadotropin.evidence.json` | 7 | yes | advisory:1, product-label:2, undetermined:1 | `dailymed-pregnyl-label`(research/input/evidence/chorionic-gonadotropin.evidence.json:35), `dailymed-pregnyl-organon-research-20260913`(research/input/evidence/chorionic-gonadotropin.evidence.json:107) | undetermined |
| CJC-1295 | `research/input/evidence/cjc-1295.evidence.json` | 7 | yes | advisory:1, other:1, undetermined:1 | — | not-applicable (no product-label source) |
| Clomiphene | `research/input/evidence/clomiphene.evidence.json` | 10 | yes | product-label:1 | `dailymed-clomid-label`(research/input/evidence/clomiphene.evidence.json:34) | undetermined |
| Creatine | `research/input/evidence/creatine.evidence.json` | 13 | yes | undetermined:3 | — | not-applicable (no product-label source) |
| Dulaglutide | `research/input/evidence/dulaglutide.evidence.json` | 8 | yes | approval-package:1, product-label:1 | `dailymed-trulicity-label`(research/input/evidence/dulaglutide.evidence.json:32) | undetermined |
| Elamipretide | `research/input/evidence/elamipretide.evidence.json` | 7 | yes | approval-package:1, other:1, product-label:1, undetermined:1 | `dailymed-forzinity-label`(research/input/evidence/elamipretide.evidence.json:35) | undetermined |
| Emideltide | `research/input/evidence/emideltide.evidence.json` | 6 | yes | advisory:2, other:1 | — | not-applicable (no product-label source) |
| Enclomiphene | `research/input/evidence/enclomiphene.evidence.json` | 2 | no | — | — | not-applicable (no product-label source) |
| Epitalon | `research/input/evidence/epitalon.evidence.json` | 12 | yes | advisory:1, undetermined:2 | — | not-applicable (no product-label source) |
| Exenatide | `research/input/evidence/exenatide.evidence.json` | 10 | yes | approval-package:1, product-label:2 | `dailymed-byetta-label`(research/input/evidence/exenatide.evidence.json:34), `dailymed-bydureon-bcise-label`(research/input/evidence/exenatide.evidence.json:58) | undetermined |
| GHK-Cu | `research/input/evidence/ghk-cu.evidence.json` | 5 | yes | advisory:1 | — | not-applicable (no product-label source) |
| GHRP-2 | `research/input/evidence/ghrp-2.evidence.json` | 12 | yes | undetermined:3 | — | not-applicable (no product-label source) |
| GHRP-6 | `research/input/evidence/ghrp-6.evidence.json` | 6 | yes | undetermined:2 | — | not-applicable (no product-label source) |
| Glutathione | `research/input/evidence/glutathione.evidence.json` | 12 | yes | advisory:4, product-label:1, undetermined:2 | `dailymed-glutathione-liquid-otc`(research/input/evidence/glutathione.evidence.json:57) | undetermined |
| Gonadorelin | `research/input/evidence/gonadorelin.evidence.json` | 12 | yes | product-label:1, undetermined:2 | `dailymed-factrel-veterinary`(research/input/evidence/gonadorelin.evidence.json:29) | undetermined |
| GSK2881078 | `research/input/evidence/gsk2881078.evidence.json` | 10 | yes | advisory:1 | — | not-applicable (no product-label source) |
| Hexarelin | `research/input/evidence/hexarelin.evidence.json` | 12 | yes | undetermined:1 | — | not-applicable (no product-label source) |
| Ibutamoren | `research/input/evidence/ibutamoren.evidence.json` | 5 | yes | advisory:1, undetermined:1 | — | not-applicable (no product-label source) |
| IGF-1 LR3 | `research/input/evidence/igf-1-lr3.evidence.json` | 9 | yes | product-label:1, undetermined:2 | `dailymed-increlex-label`(research/input/evidence/igf-1-lr3.evidence.json:35) | undetermined |
| Ipamorelin | `research/input/evidence/ipamorelin.evidence.json` | 4 | yes | advisory:1, undetermined:1 | — | not-applicable (no product-label source) |
| Kisspeptin-10 | `research/input/evidence/kisspeptin-10.evidence.json` | 5 | yes | advisory:1, undetermined:1 | — | not-applicable (no product-label source) |
| KPV | `research/input/evidence/kpv.evidence.json` | 7 | yes | advisory:1, undetermined:1 | — | not-applicable (no product-label source) |
| Lasofoxifene | `research/input/evidence/lasofoxifene.evidence.json` | 7 | yes | advisory:1, undetermined:1 | — | not-applicable (no product-label source) |
| LGD-3303 | `research/input/evidence/lgd-3303.evidence.json` | 12 | yes | undetermined:1 | — | not-applicable (no product-label source) |
| Ligandrol | `research/input/evidence/ligandrol.evidence.json` | 8 | yes | undetermined:1, warning-letter:1 | — | not-applicable (no product-label source) |
| Liraglutide | `research/input/evidence/liraglutide.evidence.json` | 16 | yes | advisory:1, approval-package:2, product-label:4, undetermined:1 | `dailymed-victoza-label`(research/input/evidence/liraglutide.evidence.json:32), `dailymed-saxenda-label`(research/input/evidence/liraglutide.evidence.json:44), `accessdata-fda-victoza-label-2025`(research/input/evidence/liraglutide.evidence.json:200), `accessdata-fda-saxenda-label-2026`(research/input/evidence/liraglutide.evidence.json:212) | undetermined |
| LL-37 | `research/input/evidence/ll-37.evidence.json` | 4 | yes | undetermined:1 | — | not-applicable (no product-label source) |
| Magnesium | `research/input/evidence/magnesium.evidence.json` | 2 | no | — | — | not-applicable (no product-label source) |
| Maridebart cafraglutide | `research/input/evidence/maridebart-cafraglutide.evidence.json` | 8 | no | — | — | not-applicable (no product-label source) |
| Mazdutide | `research/input/evidence/mazdutide.evidence.json` | 16 | yes | product-label:1, undetermined:1 | `dailymed-mazdutide-search-live`(research/input/evidence/mazdutide.evidence.json:95) | undetermined |
| Mecasermin | `research/input/evidence/mecasermin.evidence.json` | 7 | yes | product-label:1 | `dailymed-increlex-label`(research/input/evidence/mecasermin.evidence.json:33) | undetermined |
| Melanotan II | `research/input/evidence/melanotan-ii.evidence.json` | 5 | yes | undetermined:1 | — | not-applicable (no product-label source) |
| Melatonin | `research/input/evidence/melatonin.evidence.json` | 6 | yes | undetermined:1 | — | not-applicable (no product-label source) |
| MOTS-c | `research/input/evidence/mots-c.evidence.json` | 8 | yes | advisory:1, other:1, undetermined:1 | — | not-applicable (no product-label source) |
| Ospemifene | `research/input/evidence/ospemifene.evidence.json` | 12 | yes | approval-package:1, product-label:1 | `dailymed-osphena-label`(research/input/evidence/ospemifene.evidence.json:32) | undetermined |
| Ostarine | `research/input/evidence/ostarine.evidence.json` | 3 | yes | undetermined:2 | — | not-applicable (no product-label source) |
| Oxytocin | `research/input/evidence/oxytocin.evidence.json` | 10 | yes | product-label:1, undetermined:1 | `dailymed-pitocin-label`(research/input/evidence/oxytocin.evidence.json:33) | undetermined |
| PEG-MGF | `research/input/evidence/peg-mgf.evidence.json` | 4 | yes | other:1, undetermined:1 | — | not-applicable (no product-label source) |
| Pemvidutide | `research/input/evidence/pemvidutide.evidence.json` | 18 | yes | other:3, undetermined:1 | — | not-applicable (no product-label source) |
| Pramlintide | `research/input/evidence/pramlintide.evidence.json` | 8 | yes | approval-package:1, product-label:1, undetermined:1 | `dailymed-symlinpen-label`(research/input/evidence/pramlintide.evidence.json:33) | undetermined |
| RAD-150 | `research/input/evidence/rad-150.evidence.json` | 10 | yes | undetermined:2 | — | not-applicable (no product-label source) |
| Raloxifene | `research/input/evidence/raloxifene.evidence.json` | 7 | yes | product-label:1 | `dailymed-evista-label`(research/input/evidence/raloxifene.evidence.json:33) | undetermined |
| Retatrutide | `research/input/evidence/retatrutide.evidence.json` | 2 | no | — | — | not-applicable (no product-label source) |
| S-23 | `research/input/evidence/s-23.evidence.json` | 11 | yes | undetermined:2 | — | not-applicable (no product-label source) |
| Selank | `research/input/evidence/selank.evidence.json` | 10 | yes | undetermined:3 | — | not-applicable (no product-label source) |
| Semaglutide | `research/input/evidence/semaglutide.evidence.json` | 3 | yes | product-label:2, undetermined:1 | `dailymed-ozempic`(research/input/evidence/semaglutide.evidence.json:13), `dailymed-wegovy`(research/input/evidence/semaglutide.evidence.json:14) | undetermined |
| Semax | `research/input/evidence/semax.evidence.json` | 8 | yes | undetermined:1 | — | not-applicable (no product-label source) |
| Sermorelin | `research/input/evidence/sermorelin.evidence.json` | 11 | yes | product-label:1, undetermined:2 | `dailymed-search-sermorelin`(research/input/evidence/sermorelin.evidence.json:47) | undetermined |
| Setmelanotide | `research/input/evidence/setmelanotide.evidence.json` | 8 | yes | product-label:1, undetermined:1, warning-letter:1 | `dailymed-imcivree-label`(research/input/evidence/setmelanotide.evidence.json:34) | undetermined |
| Somatropin | `research/input/evidence/somatropin.evidence.json` | 7 | yes | advisory:2, product-label:1, undetermined:1 | `dailymed-genotropin-label`(research/input/evidence/somatropin.evidence.json:36) | undetermined |
| Spermidine | `research/input/evidence/spermidine.evidence.json` | 3 | no | — | — | not-applicable (no product-label source) |
| Stenabolic | `research/input/evidence/stenabolic.evidence.json` | 6 | yes | undetermined:1 | — | not-applicable (no product-label source) |
| Survodutide | `research/input/evidence/survodutide.evidence.json` | 17 | yes | other:1, product-label:1, undetermined:1 | `dailymed-survodutide-search`(research/input/evidence/survodutide.evidence.json:140) | undetermined |
| Tamoxifen | `research/input/evidence/tamoxifen.evidence.json` | 7 | yes | product-label:2 | `dailymed-tamoxifen-citrate-label`(research/input/evidence/tamoxifen.evidence.json:29), `dailymed-soltamox-oral-solution-label`(research/input/evidence/tamoxifen.evidence.json:101) | undetermined |
| TB-500 | `research/input/evidence/tb-500.evidence.json` | 2 | no | — | — | not-applicable (no product-label source) |
| Tesamorelin | `research/input/evidence/tesamorelin.evidence.json` | 5 | yes | other:1, product-label:1, undetermined:1 | `dailymed-egrifta-sv-label`(research/input/evidence/tesamorelin.evidence.json:35) | undetermined |
| Testolone | `research/input/evidence/testolone.evidence.json` | 10 | yes | undetermined:2, warning-letter:1 | — | not-applicable (no product-label source) |
| Testosterone cypionate | `research/input/evidence/testosterone-cypionate.evidence.json` | 3 | yes | product-label:2 | `dailymed-depo-testosterone`(research/input/evidence/testosterone-cypionate.evidence.json:13), `fda-azmiro-label-2022`(research/input/evidence/testosterone-cypionate.evidence.json:15) | undetermined |
| Thymosin alpha-1 | `research/input/evidence/thymosin-alpha-1.evidence.json` | 10 | yes | undetermined:2 | — | not-applicable (no product-label source) |
| Tirzepatide | `research/input/evidence/tirzepatide.evidence.json` | 5 | yes | advisory:1, other:1, product-label:2, undetermined:1 | `dailymed-mounjaro`(research/input/evidence/tirzepatide.evidence.json:13), `dailymed-zepbound`(research/input/evidence/tirzepatide.evidence.json:14) | undetermined |
| Toremifene | `research/input/evidence/toremifene.evidence.json` | 8 | yes | approval-package:2, other:1, product-label:1 | `dailymed-fareston-label`(research/input/evidence/toremifene.evidence.json:33) | undetermined |
| Urolithin A | `research/input/evidence/urolithin-a.evidence.json` | 2 | no | — | — | not-applicable (no product-label source) |
| Vitamin D3 | `research/input/evidence/vitamin-d3.evidence.json` | 9 | yes | product-label:1 | `dailymed-cholecalciferol-25mcg-tab`(research/input/evidence/vitamin-d3.evidence.json:32) | undetermined |
| VK2735 | `research/input/evidence/vk2735.evidence.json` | 14 | yes | product-label:1, undetermined:1 | `dailymed-search-vk2735-2026`(research/input/evidence/vk2735.evidence.json:175) | undetermined |
| Vosilasarm | `research/input/evidence/vosilasarm.evidence.json` | 11 | yes | undetermined:2, warning-letter:1 | — | not-applicable (no product-label source) |
| YK-11 | `research/input/evidence/yk-11.evidence.json` | 8 | yes | undetermined:2 | — | not-applicable (no product-label source) |

## Classification counts (across all FDA/DailyMed-lane sources in all 78 packets)

| classification token | count |
|---|---|
| product-label | 40 |
| approval-package | 13 |
| warning-letter | 5 |
| advisory | 25 |
| other | 14 |
| undetermined | 80 |
| **total FDA/DailyMed-lane source records** | **177** |

Packets with an FDA/DailyMed lane cited: **69 / 78**

Packets with at least one `product-label`-classified source: **31 / 78**

Packets with a product-label source AND an `interactions-section-cited` hit per the method above: **0 / 78**

## Packets with NO FDA/DailyMed lane (9 of 78)

- Amycretin
- Caffeine
- Enclomiphene
- Magnesium
- Maridebart cafraglutide
- Retatrutide
- Spermidine
- TB-500
- Urolithin A

## Label-token co-occurrence matrix

Cell = number of evidence packets (of 78) in which both row and column classification tokens appear among that packet's FDA/DailyMed-lane sources (diagonal = packets where that token appears at all, irrespective of co-occurrence). Tokens are the literal classification strings produced by the Method above; this is a count of token co-presence per packet, not an interpretation of relationship between sources.

| token | product-label | approval-package | warning-letter | advisory | other | undetermined |
|---|---|---|---|---|---|---|
| product-label | 31 | 10 | 1 | 5 | 6 | 20 |
| approval-package | 10 | 10 | 0 | 1 | 2 | 5 |
| warning-letter | 1 | 0 | 5 | 0 | 0 | 5 |
| advisory | 5 | 1 | 0 | 19 | 4 | 16 |
| other | 6 | 2 | 0 | 4 | 11 | 9 |
| undetermined | 20 | 5 | 5 | 16 | 9 | 55 |

### Co-occurrence citations (packet names per off-diagonal pair with count > 0)

- `advisory` + `approval-package` (1): Liraglutide
- `advisory` + `other` (4): CJC-1295, Emideltide, MOTS-c, Tirzepatide
- `advisory` + `product-label` (5): Chorionic gonadotropin, Glutathione, Liraglutide, Somatropin, Tirzepatide
- `advisory` + `undetermined` (16): ACP-105, AOD-9604, BPC-157, CJC-1295, Chorionic gonadotropin, Epitalon, Glutathione, Ibutamoren, Ipamorelin, KPV, Kisspeptin-10, Lasofoxifene, Liraglutide, MOTS-c, Somatropin, Tirzepatide
- `approval-package` + `other` (2): Elamipretide, Toremifene
- `approval-package` + `product-label` (10): Afamelanotide, Bazedoxifene, Bremelanotide, Dulaglutide, Elamipretide, Exenatide, Liraglutide, Ospemifene, Pramlintide, Toremifene
- `approval-package` + `undetermined` (5): Afamelanotide, Bremelanotide, Elamipretide, Liraglutide, Pramlintide
- `other` + `product-label` (6): Cagrilintide, Elamipretide, Survodutide, Tesamorelin, Tirzepatide, Toremifene
- `other` + `undetermined` (9): CJC-1295, Cagrilintide, Elamipretide, MOTS-c, PEG-MGF, Pemvidutide, Survodutide, Tesamorelin, Tirzepatide
- `product-label` + `undetermined` (20): Afamelanotide, Bremelanotide, Cagrilintide, Chorionic gonadotropin, Elamipretide, Glutathione, Gonadorelin, IGF-1 LR3, Liraglutide, Mazdutide, Oxytocin, Pramlintide, Semaglutide, Sermorelin, Setmelanotide, Somatropin, Survodutide, Tesamorelin, Tirzepatide, VK2735
- `product-label` + `warning-letter` (1): Setmelanotide
- `undetermined` + `warning-letter` (5): Andarine, Ligandrol, Setmelanotide, Testolone, Vosilasarm

## Measured size (count summary, no advice)

- Evidence packets censused: 78
- Packets citing an FDA/DailyMed lane: 69
- Packets with no FDA/DailyMed lane cited: 9 (named above)
- Packets with ≥1 `product-label`-classified source: 31
- Packets with a product-label source and a literal `interaction`-substring citation in that
  source's own `extractedEvidence` entries: 0
- Total FDA/DailyMed-lane source records across all packets: 177
- Classification-token counts across those 177 records: product-label 40, undetermined 80,
  advisory 25, other 14, approval-package 13, warning-letter 5
