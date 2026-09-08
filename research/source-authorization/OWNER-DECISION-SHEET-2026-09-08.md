# Owner decision sheet — source rights

**UNSIGNED PROPOSAL. This document records no approval and issues no authorization.**
Nothing here activates a source, reassigns a role, changes a binding, or clears a launch.

**Prepared by:** Claude Opus 5, 2026-09-08, for Clint Morgan, sole owner and operator, Keon Systems LLC.
**Basis:** [rights assessment](SOURCE-RIGHTS-ASSESSMENT-2026-09-08.md) ·
[addendum](RIGHTS-FOLLOWUP-ADDENDUM-2026-09-08.md) ·
[GSRS identity verification](GSRS-PUBLIC-IDENTITY-VERIFICATION-2026-09-08.md) (Codex) ·
[approval sheet](LEGAL-APPROVAL-REQUIRED-2026-09-08.md) · registry `248d8f02…`
**Standing constraints:** BioStack is commercial; Keon holds no paid content licences.

I drafted the registry's `rights` text and I am not qualified to give legal advice. The questions below
are decisions for you; the analysis under each is engineering evidence, not a legal opinion.

---

## A. Role succession — what the artifacts actually require

**Finding: no distinct-person or independent-review requirement exists in any governing artifact or in
the code.** This was asserted in earlier documents, including mine. It is not supported.

| Location | What it actually says |
|---|---|
| `backend/src/BioStack.KnowledgeWorker/Schemas/source-authorization-decision.schema.json` `$defs/owner` | Requires `roleId` (enum of the four roles), `personName`, `assignmentStatus: "assigned"`. **No constraint that `personName` differ between owners.** |
| Same schema, `owners` | `minItems: 4, maxItems: 4, uniqueItems: true`. `uniqueItems` compares whole objects; four entries with distinct `roleId`s are unique whatever the names. **One person may hold all four.** |
| `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceAcquisitionPlanning.cs:408` `ApprovalIsApproved` | Checks assignee name non-empty, decision scope, blocking stage, review status, decision value, timestamp, notes. **No comparison against any other role's holder.** |
| `SourceAcquisitionPlanning.cs:~300` | Requires `rights.reviewedBy` to **equal** `approvals.legalRights.assigneeName`, emitting `source-rights-reviewer-approval-assignee-mismatch` otherwise. This is a *sameness* requirement — the opposite of separation — for that pair. |
| `recommended-seven-source-decisions.v1.json` `assignmentDisclaimer` | "one person may hold multiple roles; that overlap does not satisfy any distinct-person or independent-review requirement." It **disclaims** an independence property; it does not **impose** one. It points at a requirement that would have to exist somewhere else, and none does. |

**Three things this separates that were previously conflated:**

1. **Policy versus law.** Nothing above is a legal requirement. It is a procedure the project wrote for
   itself, and the project may change it. No statute or regulation identified in this work requires a
   distinct approver for reusing published material.
2. **The departed representative is not mandatory.** No artifact names Johnathan Harper as required. The
   schema requires a `personName` for `legal-rights-approver`; any assigned name satisfies it.
3. **Role reassignment is not permission.** Taking the role lets you *record* decisions. It grants no
   right to reuse third-party material. Section C is unaffected by A.

### Decision A1

> Do you take `legal-rights-approver`, recording it as **owner review of permissions** rather than legal
> review, and accept that no independent reviewer exists for source activation?

- **Recommended: yes.** It reflects reality — you are the sole operator — and the artifacts permit it.
- If **no**: name another holder, or the source-activation gate stays blocked indefinitely.
- Either way: supersede the v1 `owners` block with a new record. Do not edit history or attribute new
  approvals to Harper.

**Not a decision:** whether to *state* the loss of independence. The `assignmentDisclaimer` already does,
and it stays.

---

## B. precisionFDA / GSRS (S1) — narrow route, tested against the actual excerpts

Codex verified all seven UNIIs as public GSRS records with names and versions
([note](GSRS-PUBLIC-IDENTITY-VERIFICATION-2026-09-08.md)). That is **identity evidence only**.

I compared each record's original packet excerpt against the narrow public **name / UNII / record
version** scope. The result is that the narrow route covers less than the seven-record count suggests.

| # | Record | Cited? | Stored excerpt | Within name/UNII/version scope? |
|---|---|---|---|---|
| 1 | `unii-srs-afamelanotide` | yes, 1 claim | `AFAMELANOTIDE` | **Yes.** Name only. Its locator also names synonyms (Melanotan I / NDP-α-MSH) which are **outside** the scope. |
| 2 | `fda-gsrs-unii-6y24o4f92s` | yes, 1 claim | `UNII: 6Y24O4F92S; CAS: 189691-06-3; Molecular Formula: C50H68N14O10` | **Partly.** UNII yes. **CAS Registry Number and molecular formula are not** — see below. |
| 3 | `fda-unii-srs-m9l22y19h9` | yes, 1 claim | `CAS Number: 143045-27-6` | **No.** Entirely a CAS Registry Number, and this claim carries `fieldAuthorityRequired: true`. |
| 4 | `fda-gsrs-unii-chorionic-gonadotropin` | **not cited** | — | n/a |
| 5 | `fda-gsrs-unii-creatine` | **not cited** | — | n/a |
| 6 | `fda-unii-somatropin` | **not cited** | — | n/a |
| 7 | `fda-gsrs-unii-yk11-z9748j6b0r` | **not cited** | — | n/a |

**Four of seven support no claim at all.** They are registered aliases with zero corpus usage, so for
them the narrow route grants nothing and costs nothing.

**CAS Registry Numbers need separate evidence.** They are identifiers from the CAS Registry, a product of
the American Chemical Society, not NCATS-authored content. GSRS displaying one does not place it under
GSRS's CC0 dedication, and the assessment's rule against inferring rights from a host applies exactly
here. Two of the three cited excerpts depend on CAS numbers.

**No packet records a GSRS record version.** Codex's versions (45, 34, 31, 56, 4, 80, 5) exist only in the
verification note. Proposing the narrow route means version provenance would have to be **added going
forward**, not backfilled — the historical precisionFDA references stay as they are.

### Decision B1

> Approve a **narrow public-GSRS scope** — displayed name, UNII, and record version, acquired from the
> public NCATS distribution — as a proposed source scope, separate from the `fda` lane's openFDA approval?

- **Recommended: yes**, for that scope only. It is the one route here with a browser-verified CC0 basis.
- **Explicitly excluded** unless separately decided: synonyms, classifications, structures, molecular
  formulae, external database identifiers (**including CAS**), references, clinical attributes.
- **Do not** extend the existing openFDA API approval to precisionFDA web records by inference.

### Decision B2

> The two excerpts resting on CAS Registry Numbers (#2 partly, #3 entirely) — re-source, drop, or hold?

- **Recommended: hold both**, pending a decision on CAS identifiers generally.
- #3 is the sharper case: it is *only* a CAS number, supporting a `fieldAuthorityRequired` claim.

**Not a decision, flagged as engineering:** #1's excerpt is the single word `AFAMELANOTIDE` supporting a
*mechanism* claim. Whatever its rights status, a name does not evidence a mechanism. That is an evidence-
quality item for the reviewer lane, not a rights question.

---

## C. Retained content and remaining lanes

**Do not approve in bulk.** The 23 dispositions are individually reasoned in the
[assessment](SOURCE-RIGHTS-ASSESSMENT-2026-09-08.md) — 7 scoped approvals, 5 conditional, 8 holds, 1
defer, 2 retire. Each names its own scope and conditions. A blanket approval would discard exactly the
per-item reasoning that makes them defensible.

### Decision C1 — DrugBank retained content

Current measured exposure: **24 identifiers, 24 URL groups, 30 referencing claims, 29 stored quote
fields, 3,501 characters.** No commercial licence is held; DrugBank's terms restrict commercial
applications. Connecting its MCP server this session granted no content rights and none was exercised.

> Which handling?

| Option | Effect |
|---|---|
| **(a) Contain and re-source** — recommended | Keep acquisition disabled, exclude the 29 excerpts from customer display and AI retrieval, re-anchor the underlying facts to licensed primary sources, remove what cannot be re-sourced |
| (b) Seek a commercial licence | Ask separately about already-retained material; a prospective licence may not cure prior use |
| (c) Rely on fair use | Needs counsel; the terms are contractual as well as copyright |

At least one record is not DrugBank-authored at all: `basaria-2013-jgerontol-rct` is DrugBank's index
page for a *Journals of Gerontology* paper (DOI `10.1093/gerona/gls078`) whose notice names the 2012
author as copyright holder, with OUP publishing for the Gerontological Society of America. It carries 6
excerpts / 814 characters and needs its own route and rightsholder recorded, whichever option you pick.

### Decision C2 — ChEMBL

Four unregistered records; **2 claims, 2 stored quote fields, 125 characters total** — a molecule record
(57 chars) and a molecule-search API response (68 chars). Codex confirms these are names and identifiers,
not article prose. Published licence is **CC BY-SA 3.0**, which does permit commercial use with
attribution and share-alike.

> The remaining question is narrow: **does share-alike reach anything BioStack redistributes?**

- **Recommended: approve the two retained excerpts** under CC BY-SA 3.0 with attribution recorded, and
  decide any broader derived dataset separately.
- Share-alike attaches to adaptations of the licensed material, not to unrelated BioStack software. It
  does bear on redistributing a derived dataset built from ChEMBL content.
- ChEMBL's FAQ also flags restrictions on certain values derived from commercial software — check before
  taking anything beyond names and identifiers.

### Decision C3 — the two retired placeholders

`peer-reviewed-paper` and `peer-reviewed-review` were set to `rejected` without authority. Twenty-one
claims still cite them.

> Confirm retiring them as *authorization classes* — an internal classification decision, not a finding
> about the underlying papers?

- **Recommended: yes.** Then re-anchor the 21 claims to concrete publishers, each carrying its own
  rights record. Retiring a placeholder is not a reason to delete literature.

---

## D. What is not on this sheet

**Agent work, no decision needed:** inventory-level duplicate grouping (50 ids / 10 groups, aliases and
provenance preserved); re-anchoring claims once C3 is settled; recording route and rightsholder
separately for the 41 aggregator-routed records.

**Deliberately excluded from this parcel:** registering the 284 unregistered ids, and expanding the
registry schema's rights fields.

**Unresolved factual uncertainty:**

1. Whether CAS Registry Numbers displayed on a CC0 government page carry ACS rights. Not resolved here.
2. `rights.legalBasisOrLicense` text throughout the registry is mine, not a legal finding, and remains so
   for every lane until replaced.
3. `approvedRightsSourceCount` reads 26 registry **declarations**. That is not 26 verified approvals, and
   no count in this repository proves permission.
4. Whether any Keon contract outside this repository imposes an independent-review obligation. Section A
   covers this repository only.

---

## The smallest decision that unblocks the most

**Decision A1 alone.** Without a `legal-rights-approver`, no source decision can be issued at all, so
every item in B and C stays blocked regardless of its merits. A1 needs no licence analysis, costs
nothing, and is reversible by a later superseding assignment. B1 is the natural second, because it is the
only route here with verified public-domain footing.

Neither resolves the DrugBank retained-content question, which is the largest standing exposure.
