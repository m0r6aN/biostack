# p0a_re_review_2 — P0-A spec review

**Verdict:** APPROVE-WITH-FIXES
**Spec file:** /home/cmorgan76/Repos/biostack-wt/p0a-rework-1/docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md
**Spec SHA-256:** 2ffa54311c93876eff52578b4f0afbe2e41f76e261221276db4525fc0922e6e9
**Date:** 2026-10-08

## Ranked findings

### F1 — MAJOR — CI-004 seed-finding quotation is not byte-for-byte accurate, and conflicts with the spec's own `seed-regression` + `Real-corpus grounding` rules
**Claim:** CI-004's `source_b` quotation of the guidance-content-contract's Class C rule renders
the source's curly-quoted embedded example as straight single quotes, which is not a verbatim
match, yet `seed-regression` requires this exact row text to be carried forward unchanged.
**Evidence:**
- Spec (P0-A.md line 377, CI-004 row, `source_b`): `*"Must not morph into 'you should start at X.'"*`
- Actual source (`docs/guidance/biostack-guidance-content-contract.v1.md` line 100, verified
  byte-for-byte via `python3` read): `Must not morph into “you should start at X.”` — curly
  double quotation marks (U+201C/U+201D), not straight single quotes, and no closing period
  outside the mark.
- Hard constraints, "Real-corpus grounding": *"A quotation that does not match the source file
  byte-for-byte (modulo leading/trailing whitespace) fails validation."*
- "Deterministic verification," `seed-regression`: *"every CI-001 through CI-010 row above is
  present in the shipped CONTRADICTION-INVENTORY.md, byte-for-byte unchanged in its
  source_a/source_b quotations as they read in this spec version... the builder may add further
  detail but may not remove or soften a seed finding without a named, reviewed reason."*

These two rules are in direct tension for this one row: preserving the row exactly as the spec
currently reads (to satisfy `seed-regression`) means shipping a quotation that does not match the
source file byte-for-byte (failing `Real-corpus grounding`/`unresolvable-citation`); correcting the
quotation to match the source (to satisfy `Real-corpus grounding`) means the shipped row is not
byte-for-byte identical to this spec version's text (arguably failing `seed-regression`'s literal
wording). The spec gives no explicit carve-out for typographic normalization (curly vs. straight
quotation marks), so a builder cannot mechanically satisfy both checks as currently written. This
is a real, demonstrated citation-accuracy defect reachable today with `grep`/byte comparison, not
a hypothetical one — and it is pre-existing from the prior (rejected) spec version, unchanged by
this rework (confirmed via `git show 0a902c5:...P0-A.md`, same mismatched quote present there).
**Smallest amendment:** add one sentence to either `seed-regression`'s definition or "Real-corpus
grounding" explicitly permitting typographic quote-mark normalization (curly ""/'' vs. straight
"/') as not a content change, and/or correct CI-004's `source_b` text to the curly-quote form
(`“you should start at X.”`) so the seed row itself already matches its source byte-for-byte.
**Does it change a locked decision?** No — cosmetic/mechanical fix to a citation string, no
substantive finding, rank, disposition, or handoff changes.

### F2 — MINOR — "Outside this worktree's checkout" `unreviewable` condition is not independently checkable the same way the other two conditions are
**Claim:** Health-boundary "Missingness" defines three `unreviewable` conditions and says
`unreviewable-claim-verified` "independently re-checks every such claim against the filesystem at
`BaseCommit`." For conditions 1 (deleted) and 2 (unreadable) the spec names the exact mechanical
check (`git cat-file -e <BaseCommit>:<path>`; an OS-level read failure). For condition 3
("outside this worktree's checkout... for example, a submodule or sparse-checkout exclusion"), no
equivalent mechanical check is named, and the obvious check for condition 1
(`git cat-file -e <BaseCommit>:<path>`, which inspects the git object store, not the working-tree
checkout) would *succeed* for a file that is genuinely present in the commit's tree but absent
from a sparse checkout — i.e. it would not distinguish condition 3 from "not actually missing,"
and could cause `false-unreviewable-claim` to misfire against a legitimate condition-3 claim, or
conversely let a builder mis-mark a readable file `unreviewable` under condition 3 with no
verifier catching it, since no check is specified for that branch.
**Evidence:** "Health-boundary," "Missingness" bullet list, item 3, and the paragraph defining
`unreviewable-claim-verified` immediately after.
**Smallest amendment:** name the exact check for condition 3 (e.g., `git ls-tree -r <BaseCommit>`
diffed against the worktree's sparse-checkout cone / `git sparse-checkout list`) alongside the
other two, mirroring the existing specificity.
**Does it change a locked decision?** No. This repository currently has no submodules or sparse
checkout in active use (confirmed: the worktree is a plain clone), so this is a low-probability,
forward-looking gap rather than a live bypass today — downgraded from the BLOCKER-level "bypass
the Source Manifest" concern I was tasked to hunt for, because no exploitable path exists against
the actual current corpus.

## Missing pieces / collisions / unknowns

- None found regarding (a) precedence-rule determinism: the three-property comparison rule,
  worked through for every pairing type I could construct (ratified vs. unratified, two ratified
  with different dates, two unratified with equal and unequal "scope breadth," two unratified with
  incomparable narrow scopes), always resolves to exactly one of `A-outranks-B`, `B-outranks-A`,
  or the explicitly named `open-tie` outcome — `open-tie` is itself a defined, non-failure answer,
  so apparent gaps in the binary "product-wide vs. narrower" scope test do not break totality; they
  fall through to `open-tie` by construction. I could not find a document pair that the rule fails
  to answer.
- None found regarding (c): I actively looked for any sentence asserting a product rule in the
  spec's own voice. The one place this risk concentrates — the paragraph following the CI-001 seed
  row, which states the D-B1(c) "staged split" as "the product's current posture now" — is
  explicitly attributed to `P0-B-DESIGN-GATE.md`'s D-B1 table and to Coordinator Decision D-I (both
  independently verified to exist and to say what the spec claims, see Verification notes), and the
  text explicitly disclaims amending any canon document's text. This reports an owner ruling that
  already exists outside this spec; it does not originate one. No product allowed-output decision
  is made or implied by P0-A itself.
- None found regarding (b) exhaustiveness-narrowing: diffed seed findings CI-001–CI-010 against
  the prior (rejected) spec version byte-range; all ten rows are present in both, same IDs, no
  row dropped. The one row that changed (CI-001) was *expanded* (a shorter D13 paraphrase restored
  to the full conditional sentence), consistent with the spec's own stated reason, not narrowed.
- None found regarding (e): see F2 above — a theoretical gap in verifier specificity for one of
  three `unreviewable` conditions, not an exploitable bypass against this repository's actual
  structure today.

## Verification notes

- Spec file SHA-256 recomputed directly: matches header above.
- `CHARTER.md` SHA-256 `4cd390d6...4efe22` — matches spec's cited
  `4CD390D631487DA8E97509205A186F242C4A83B762FFFA894BEEC8A21F4EFE22` (case-insensitive hex match).
- `PLAN-REVIEW.md` SHA-256 — matches spec's cited hash exactly.
- Ledger whole-file hash at `main@32280aa2219100e20520a275d19af2fbdd1f32be`
  (`git show <sha>:docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md | sha256sum`) —
  matches spec's cited "shaping-time" hash `EE506C3B...4CA7AE` (as `EE506C3B54FB57ABD09BBB5180CD704AABCA3EB73875B7E760EF84B538863D47`... — full 64-hex match confirmed).
- Current ledger whole-file hash (`sha256sum docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`
  on the worktree) — matches spec's cited "current" hash `908FAA59...E4CA7AE`.
- D-G span hash (`sed -n '114,123p' ... | sha256sum`) — matches spec's cited
  `3D46AC19AEF9A0F553239B6EC984E90216286C72918FFB2AA04EFA81B90BC4F9` exactly, and the quoted D-G
  text matches the ledger's actual D-G section verbatim (bold markup stripped only, substance
  identical).
- D-H heading at line 125, D-I heading at line 152, file is 170 lines (last content line 169) —
  spec's cited "lines 125-150" (D-H) and "lines 152-169" (D-I) both match the actual heading-to-
  next-heading spans exactly.
- D-I's quoted sentence (*"the §2 canon conflict is resolved in substance by D-B1(c), and P0-A's
  contradiction inventory records its disposition by this ruling"*) matches the ledger's D-I text
  verbatim.
- `P0-B-DESIGN-GATE.md` exists, its §2 heading text and D-B1 table content match the spec's
  description and paraphrase of the "staged split" resolution.
- `git merge-base --is-ancestor 32280aa2219100e20520a275d19af2fbdd1f32be HEAD` — confirmed
  ancestor, matching the spec's "Reconciled shaping base anchor" claim.
- `grep -n "P3-A" docs/specs/INDEX.md` — no match, confirming the spec's claim that P3-A has no
  row at all in the index (not merely "review-candidate" there).
- `docs/specs/INDEX.md`'s P2 row reads `done`, matching the spec's claim; `closures/P2.md` records
  PR #505, matching.
- Charter's dependency spine string (`grep` line 152) reads
  `... -> P1 -> P2 -> P3-A -> P0-A -> P0-B -> ...`, matching the spec's quoted spine exactly.
- All ten CI-001–CI-010 `source_a`/`source_b` quotations spot-checked against their cited files
  with `grep -n`/direct byte read: nine verified byte-for-byte exact (modulo the spec's own
  mid-quote `...` elisions, which are visibly marked, not silent); CI-004's `source_b` is the one
  exception, documented as F1 above.
- Diffed this rework's single commit (`7753861`) against its stated reconciled shaping anchor
  (`a25a658`): only `parcels/P0-A.md` changed — no frozen-surface file (charter, PLAN-REVIEW,
  closures, P2/P3-A specs, ledger) was touched by this rework, confirming the "Frozen surfaces"
  section's own claim about itself.
- `docs/specs/scripts/verify-p2.ps1` lines 642-643 do assert `coordinator-assigns-at-gate-2` for
  the Branch/worktree and Owner cells, matching the spec's citation supporting the P2 precedent for
  that literal.
- No `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/` directory and no
  `docs/specs/schemas/canon-precedence.md` / `docs/specs/scripts/verify-p0a.ps1` exist yet in the
  worktree — consistent with "builder dispatch blocked" status; the spec does not prematurely
  create any of its own allowed-surface deliverables.
- No `TBD`/`FIXME`/`{{`-style placeholder found anywhere in the spec body outside of the rule text
  that names those markers as forbidden.
- Searched the full spec text for any first-person product-rule assertion ("BioStack
  must"/"BioStack may" stated outside a quotation mark/citation); found none — every such
  sentence in the document is either a quoted citation or an explicit statement about what canon
  *says*, consistent with the "No product allowed-output decision" hard constraint.
