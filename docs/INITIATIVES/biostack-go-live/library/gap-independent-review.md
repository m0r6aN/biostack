# Independent review of the 59 gap library records

Date: 2026-10-14
Reviewer: independent review agent (did not author the gap file)
Seed edited: `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`
Scope: the 59 objects whose `canonicalId` values are listed in `gap-report.md`. No other seed object was edited. None was deleted.

## Counts

- Records reviewed: 59
- Confirmed: 57
- Narrowed: 2 (`calcium-carbonate`, `iodine`)
- Deactivated as duplicate: 0
- Source URL replaced: 2 (`calcium-carbonate`, `iodine`)
- Source URL added without removing the original: 1 (`omeprazole` OTC label)
- FDA-approval statements left standing only where the opened label stated the indication: 9 drug records
- Dose, schedule, or sourcing sentences removed: 0 (none were present in the 59 objects)
- `ops.needsReview` left false: 59
- `ops.lastChangeType` left `manual-review`: 59

`omega-3-fatty-acids` is not a duplicate of `omega-3-ethyl-esters`. The older record is the prescription ethyl-ester product. The new record is the dietary EPA/DHA class, regulatory status `not-established`, and it says the two are not interchangeable. No older seed id is the same concept as any of the other 58.

## How URLs were checked

Every stored URL was requested on 2026-10-14.

- DailyMed `drugInfo.cfm` URLs were fetched directly (HTTP 200) and the page title was compared with the substance.
- PubMed HTML returned HTTP 403 to the script client. Each PMID was then requested through NCBI E-utilities `esummary` (`db=pubmed`). The returned title matched the stored title for all 38 PMIDs. No PMID was invented.
- NIH ODS HTML returned HTTP 403 to the script client. The iodine fact sheet was verified through a web request that returned the page text: iodine is described there as an essential component of thyroid hormones T4 and T3. URL: `https://ods.od.nih.gov/factsheets/Iodine-HealthProfessional/`.

## Rows

| canonicalId | disposition | URL verified | change |
|---|---|---|---|
| magnesium-oxide | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=dc57893c-068a-37d8-e053-2a95a90aa054 | None. Opened page is a magnesium oxide dietary-supplement tablet. No disease-treatment approval is claimed. |
| vitamin-b12 | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=3c810a40-9e90-4ca7-b364-20d9ac61b120 | None. Opened page is an oral cyanocobalamin dietary-supplement tablet. |
| vitamin-c | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=d74dd994-2520-4d17-8d9d-fa06e9e8de96 | None. Opened page is an oral ascorbic acid dietary-supplement tablet. |
| zinc-sulfate | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=b569e760-ee63-40e3-b425-8d498ec164a5 | None. Opened page is a zinc sulfate dietary-supplement tablet. |
| ferrous-sulfate | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=7693c763-401d-4665-a984-fbbea72da33e | None. Opened page is a ferrous sulfate dietary-supplement tablet and carries the pediatric iron-overdose warning. |
| folic-acid | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=0baa10dd-6d1d-4946-8236-566451132da3 | None. Opened page is folic acid tablets indicated for megaloblastic anemias due to folic acid deficiency. The record does not claim every strength is prescription-only. |
| vitamin-b6 | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=b46908f4-5a46-44a0-a1fb-fe47f8192a1a | None. Opened page is a pyridoxine hydrochloride dietary-supplement tablet. |
| riboflavin | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=78f9ddab-300e-486a-ab81-23f465280e33 | None. Opened page is a riboflavin dietary-supplement tablet. |
| niacin | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=6e604319-e164-4056-ab80-b78b98c519c7 | None. Opened page states U.S. Approval 1997 and lipid indications for extended-release niacin. Supplement niacin remains not established. |
| potassium-chloride | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=9d3f1972-8ff0-4fc5-86b4-daeb2ba83fb4 | None. Opened page is Klor-Con potassium chloride extended-release tablets for potassium replacement. Supplement potassium remains not established. |
| calcium-carbonate | narrowed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=f4786707-a0f7-4ba8-9656-06278d1b4b6c | Replaced the original setid. That page resolved, but it was a calcium dietary-supplement tablet, not the antacid label the regulatory sentence described. The opened Tums Ultra page is a calcium carbonate chewable antacid for heartburn, acid indigestion, and sour stomach. Claim, benefit, regulatory wording, title, and URL now follow that page. No dose or schedule was copied. |
| potassium-iodide | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=bab815ee-47ef-4c87-a60d-e0d7863ad871 | None. Opened page is IOSAT potassium iodide for thyroid blocking in a nuclear radiation emergency only. Not treated as a duplicate of dietary iodine. |
| phytonadione | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=66ada4f8-c5a9-49f1-848f-e172416892a1 | None. Opened page states U.S. Approval 1955 and vitamin K replacement for coagulation-factor deficiency due to vitamin K deficiency or interference. Vitamin K2 is not transferred onto this record. |
| acetylcysteine | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=5558a5f5-e821-473b-7d8a-5d33d09f0586 | None. Opened page describes inhalation mucolytic use and oral acetaminophen-antidote use. Supplement NAC remains not established. No overdose protocol was copied. |
| atorvastatin | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=a60cc18b-0631-4cf0-b021-9f52224ece65 | None. Opened Lipitor page states U.S. Approval 1996 and labeled lipid and cardiovascular-risk indications. |
| lisinopril | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=838c2d78-d2d8-4981-9ec9-e50ef9e1a5d8 | None. Opened Zestril page states hypertension, adjunct heart-failure, and acute myocardial infarction indications. |
| omeprazole | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=b6761f84-53ac-4745-a8c8-1e5427d7e179 and https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=77ed80a2-a482-4838-ac55-4865b5c31d9f | Added the opened Prilosec OTC page. The original prescription granule page supports acid-related indications (U.S. Approval 1989) but not the OTC frequent-heartburn statement. Regulatory wording now names both opened labels. No dose or duration was copied. |
| sertraline | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=fda754f6-d0f3-4dce-a17a-927d64f912f7 | None. Opened Zoloft page lists major depressive disorder, obsessive-compulsive disorder, panic disorder, posttraumatic stress disorder, and social anxiety disorder. |
| ibuprofen | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=d21129f8-4d96-4e36-bc39-a534306dd77e | None. Opened Motrin IB page is OTC ibuprofen for temporary minor aches and pains and fever. |
| acetaminophen | confirmed | https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=1622f694-4d63-4c56-8737-fae31f0ecfb7 | None. Opened Tylenol Regular Strength page is OTC acetaminophen for temporary pain and fever, with a liver warning. No amount was copied. |
| ashwagandha | confirmed | https://pubmed.ncbi.nlm.nih.gov/36017529/ | None. E-utilities title matches the stored anxiety and stress systematic review. Regulatory status remains not-established. |
| glycine | confirmed | https://pubmed.ncbi.nlm.nih.gov/39453231/ | None. E-utilities title matches the stored glycine performance review. Tier remains Insufficient. |
| collagen-peptides | confirmed | https://pubmed.ncbi.nlm.nih.gov/34491424/ | None. E-utilities title matches the stored collagen peptide systematic review. |
| lions-mane | confirmed | https://pubmed.ncbi.nlm.nih.gov/26244378/ | None. E-utilities title matches the stored *Hericium erinaceus* review. Tier remains Insufficient. |
| rhodiola | confirmed | https://pubmed.ncbi.nlm.nih.gov/30652331/ | None. E-utilities title matches the stored *Rhodiola* species review. Tier remains Insufficient. |
| l-theanine | confirmed | https://pubmed.ncbi.nlm.nih.gov/42410082/ | None. E-utilities title matches the stored L-theanine systematic review of 31 trials. |
| vitamin-k2 | confirmed | https://pubmed.ncbi.nlm.nih.gov/38890875/ | None. E-utilities title matches the stored vitamin K2 clinical perspective. Not merged into phytonadione. |
| biotin | confirmed | https://pubmed.ncbi.nlm.nih.gov/28701385/ | None. E-utilities title matches the stored biotin review. |
| l-tyrosine | confirmed | https://pubmed.ncbi.nlm.nih.gov/26424423/ | None. E-utilities title matches the stored tyrosine supplementation review. |
| l-tryptophan | confirmed | https://pubmed.ncbi.nlm.nih.gov/32272859/ | None. E-utilities title matches the stored L-tryptophan mood review. |
| 5-htp | confirmed | https://pubmed.ncbi.nlm.nih.gov/33375373/ | None. E-utilities title matches the stored 5-hydroxytryptophan review. Tier remains Insufficient. |
| same | confirmed | https://pubmed.ncbi.nlm.nih.gov/39339750/ | None. E-utilities title matches the stored SAMe central-nervous-system systematic review. |
| glucosamine | confirmed | https://pubmed.ncbi.nlm.nih.gov/38581640/ | None. E-utilities title matches the stored knee-osteoarthritis review of glucosamine and chondroitin. |
| chondroitin | confirmed | https://pubmed.ncbi.nlm.nih.gov/38581640/ | None. Same opened review, which evaluates chondroitin as well as glucosamine. Kept as a distinct salt concept. |
| msm | confirmed | https://pubmed.ncbi.nlm.nih.gov/28300758/ | None. E-utilities title matches the stored methylsulfonylmethane review. Tier remains Insufficient. |
| inositol | confirmed | https://pubmed.ncbi.nlm.nih.gov/34638926/ | None. E-utilities title matches the stored inositol review. |
| taurine | confirmed | https://pubmed.ncbi.nlm.nih.gov/39796489/ | None. E-utilities title matches the stored taurine lipid and glycaemic meta-analysis. |
| turmeric | confirmed | https://pubmed.ncbi.nlm.nih.gov/29890252/ | None. E-utilities title matches the stored curcumin nutraceutical overview. |
| ginger | confirmed | https://pubmed.ncbi.nlm.nih.gov/35147170/ | None. E-utilities title matches the stored ginger umbrella review. |
| garlic | confirmed | https://pubmed.ncbi.nlm.nih.gov/35193446/ | None. E-utilities title matches the stored garlic cardiovascular systematic review. |
| elderberry | confirmed | https://pubmed.ncbi.nlm.nih.gov/33827515/ | None. E-utilities title matches the stored elderberry respiratory systematic review. Tier remains Insufficient. |
| echinacea | confirmed | https://pubmed.ncbi.nlm.nih.gov/31126553/ | None. E-utilities title matches the stored echinacea upper-respiratory systematic review. |
| milk-thistle | confirmed | https://pubmed.ncbi.nlm.nih.gov/38579127/ | None. E-utilities title matches the stored silymarin NAFLD/NASH systematic review. |
| saw-palmetto | confirmed | https://pubmed.ncbi.nlm.nih.gov/34488251/ | None. E-utilities title matches the stored *Serenoa repens* systematic review. |
| black-cohosh | confirmed | https://pubmed.ncbi.nlm.nih.gov/37192826/ | None. E-utilities title matches the stored black cohosh menopausal-symptom meta-analysis. |
| valerian | confirmed | https://pubmed.ncbi.nlm.nih.gov/38359657/ | None. E-utilities title matches the stored valerian insomnia umbrella review. Tier remains Insufficient. |
| ginkgo | confirmed | https://pubmed.ncbi.nlm.nih.gov/12519586/ | None. E-utilities title matches the stored *Ginkgo biloba* cognitive-impairment review. Tier remains Insufficient. |
| st-johns-wort | confirmed | https://pubmed.ncbi.nlm.nih.gov/27589952/ | None. E-utilities title matches the stored St. John's wort depression systematic review. |
| ginseng | confirmed | https://pubmed.ncbi.nlm.nih.gov/39474788/ | None. E-utilities title matches the stored ginseng cognitive-function systematic review. |
| bacopa | confirmed | https://pubmed.ncbi.nlm.nih.gov/22747190/ | None. E-utilities title matches the stored *Bacopa monnieri* systematic review. |
| omega-3-fatty-acids | confirmed | https://pubmed.ncbi.nlm.nih.gov/36103100/ | None. E-utilities title matches the stored omega-3 cardiovascular-prevention meta-analysis. Left active because it is not the prescription ethyl-ester record. |
| choline | confirmed | https://pubmed.ncbi.nlm.nih.gov/19906248/ | None. E-utilities title matches the stored choline nutrient review. Tier remains Unknown and regulatory status not-established. |
| pantothenic-acid | confirmed | https://pubmed.ncbi.nlm.nih.gov/38187802/ | None. E-utilities title matches the stored pantothenic acid scoping review. Tier remains Unknown. |
| thiamine | confirmed | https://pubmed.ncbi.nlm.nih.gov/33305487/ | None. E-utilities title matches the stored thiamine deficiency review. Tier remains Unknown. No deficiency-treatment approval was added. |
| iodine | narrowed | https://ods.od.nih.gov/factsheets/Iodine-HealthProfessional/ | Replaced the IOSAT potassium iodide emergency label. That page is about a different product and is already the source for `potassium-iodide`. Claim, warning note, evidence gap, source type, title, publisher, and URL now follow the opened NIH ODS iodine fact sheet. Tier remains Limited. Regulatory status remains not-established. No intake amount was copied. |
| selenium | confirmed | https://pubmed.ncbi.nlm.nih.gov/22381456/ | None. E-utilities title matches the stored selenium review. Tier remains Unknown. |
| vitamin-a | confirmed | https://pubmed.ncbi.nlm.nih.gov/38030585/ | None. E-utilities title matches the stored vitamin A deficiency and excess-intake review. Tier remains Unknown. No approval was added. |
| vitamin-e | confirmed | https://pubmed.ncbi.nlm.nih.gov/19451807/ | None. E-utilities title matches the stored vitamin E and cardiovascular disease review. Tier remains Unknown. |
| passionflower | confirmed | https://pubmed.ncbi.nlm.nih.gov/24140586/ | None. E-utilities title matches the stored *Passiflora incarnata* review. Tier remains Insufficient. |

## Schema and safety checks

- Classification values stayed inside the allowed enum. No record uses Botanical.
- Evidence confidence stayed `low` or `moderate`. Evidence tier stayed inside Strong, Moderate, Limited, Insufficient, and Unknown.
- Provenance source types stayed inside the allowed enum. The iodine replacement uses `regulator`.
- Formulation routes stayed `oral`. Label statuses stayed inside the allowed enum.
- Interaction ids and slugs stayed kebab-case.
- `dosingGuidance` is empty on all 59. No sentence in the 59 objects tells a reader a dose, a schedule, or how to obtain the substance. Product-strength strings that appear inside three DailyMed titles were left because they are the page titles, not instructions.
- `ops.needsReview` is false, `ops.isActive` is true, and `ops.lastChangeType` is `manual-review` on all 59.
- Seed JSON parses and still contains 158 unique canonical ids.
