# GSRS public identity verification — 2026-09-08

Codex inspected the rendered NCATS GSRS pages for all seven UNIIs named in the rights addendum's S1 scope. Each page displayed the requested UNII, **Public record**, **Public definition**, and record status **Validated (UNII)**. This closes the question of whether those identities can be found in the public NCATS distribution. It does not establish equivalence of every historical excerpt, clear third-party fields, approve automated acquisition, or issue a BioStack source decision.

| UNII / public record | Displayed name | Displayed record version |
|---|---|---:|
| [QW68W3J66U](https://gsrs.ncats.nih.gov/ginas/app/ui/substances/QW68W3J66U) | AFAMELANOTIDE | 45 |
| [6Y24O4F92S](https://gsrs.ncats.nih.gov/ginas/app/ui/substances/6Y24O4F92S) | BREMELANOTIDE | 34 |
| [20ED16GHEB](https://gsrs.ncats.nih.gov/ginas/app/ui/substances/20ED16GHEB) | CHORIONIC GONADOTROPIN | 31 |
| [MU72812GK0](https://gsrs.ncats.nih.gov/ginas/app/ui/substances/MU72812GK0) | CREATINE | 56 |
| [M9L22Y19H9](https://gsrs.ncats.nih.gov/ginas/app/ui/substances/M9L22Y19H9) | LONG-(ARG3)INSULIN-LIKE GROWTH FACTOR-I | 4 |
| [NQX9KB6PCL](https://gsrs.ncats.nih.gov/ginas/app/ui/substances/NQX9KB6PCL) | SOMATROPIN | 80 |
| [Z9748J6B0R](https://gsrs.ncats.nih.gov/ginas/app/ui/substances/Z9748J6B0R) | YK-11 | 5 |

The interface identified GSRS version 3.1.2. Verification used the publicly rendered record pages, without signing in, downloading a full dataset, following linked third-party databases or invoking the BioStack acquisition pipeline. The older `/ginas/app/beta/substances/` afamelanotide URL redirected to `/ginas/app/ui/substances/`; search-index reports of a missing older route were not treated as evidence that the live record was absent.

The [GSRS data licence](https://gsrs.ncats.nih.gov/licensing), already verified in a rendered browser in the rights addendum, states CC0/public-domain data availability unless otherwise noted. The observed afamelanotide page also contains external classifications, commercial-database identifiers and linked references. Its public-record label should therefore not be expanded into a blanket permission claim for every linked work or external vocabulary.

Recommended next scope to prepare: the exact displayed name, UNII, record version and public identity provenance, with the specific acquisition route and any field-level exclusions documented. Review broader synonyms, classifications, structures, references or clinical attributes separately if BioStack actually needs them. Compare original packet excerpts against this proposed scope before proposing a provenance migration. Preserve the original precisionFDA references as historical provenance.

UNII validation concerns substance identity. It does not itself imply regulatory review or approval, as FDA's [UNII notice](https://precision.fda.gov/uniisearch/srs/unii/000705ZASD) explains. No medical use, efficacy, safety or regulatory-status claim is cleared by this verification.
