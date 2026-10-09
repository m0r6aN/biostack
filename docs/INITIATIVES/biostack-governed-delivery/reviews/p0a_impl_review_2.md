# p0a_impl_review_2 — P0-A implementation review

**Verdict:** PASS-WITH-FIXES
**Commit reviewed:** `c0d8642cd74b701bca710d73ab8aad8560b69e2c` (main, merge of PR #528,
`2031370d5d83ced0dd97f36c4568e1d65a30cb05` "feat(governed-delivery): P0-A canon precedence
manifest + contradiction inventory")
**Spec (build authority):** `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md`
**Gate 2 dispatch record:** `docs/INITIATIVES/biostack-governed-delivery/dispatch/GATE2-P0A-IMPLEMENTATION.md`
(`BaseCommit = b78e7de8463a3db410c45cf223cd722713fec816`, verified: this commit's own diff adds
the dispatch record file itself — "single anchor" pattern holds)
**Date:** 2026-10-09

Artifact SHA-256 (at reviewed commit):
- `docs/specs/schemas/canon-precedence.md`: `36ac835e06bb1d5db5154b8f58b220777d9f1b4369b8011d55be95e172abb22a`
- `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CONTRADICTION-INVENTORY.md`: `449df172dfe133c071ab7b6bd91343c2cb60f0987e91471f6cb2b8f2c9cda800`
- `docs/specs/scripts/verify-p0a.ps1`: `11f4c09b5cfe773e6628df08dcb65fffc0571c7a3e995e923906d4b0c74e2e56`

## Scope and method

Read: `P0-A.md` (full), all 9 artifact files at `c0d8642`, the Gate 2 dispatch record. Extracted
every Required-source document at `BaseCommit` (`b78e7de`) into `/tmp/p0a_review` and byte-compared
a sample of `source_a`/`source_b` quotations (CI-001 through CI-005, CI-009, CI-011, the D-I quote,
the P0-B-DESIGN-GATE §2 quote) against the actual file text at that commit. Ran `verify-p0a.ps1`
read-only in `/home/cmorgan76/Repos/biostack-wt/p0a-impl`, then ran further adversarial mutations
against a disposable clone in `/tmp/p0a_adv` (never touching the reviewed worktree or main). No
repository file was written except this output file; no git write command was issued against
`biostack` or `biostack-wt/p0a-impl`.

## Ranked findings

### F1 — BLOCKER (candidate): the precedence manifest's own total order, mechanically applied to
two of the inventory's still-open (`fix`-disposition) rows, inverts the safety-restrictive canon
and nothing in the delivered artifacts warns against that misuse

**Claim.** `canon-precedence.md`'s ranked registry (section 2) ranks `CHARTER.md` at **rank 3**,
`docs/canon/biostack-protocol-intelligence-canon.md` at **rank 5**, and
`docs/product/knowledge-engine-capability-map.md` at **rank 8** — i.e. `compare(CHARTER.md,
protocol-intelligence-canon)` and `compare(CHARTER.md, capability-map)` both resolve to
**"CHARTER.md outranks."** `CONTRADICTION-INVENTORY.md`'s CI-002 and CI-005 rows cite exactly
this pair (CHARTER's "may" list — "Design and compare protocol options... compounds, combinations,
sequencing, timing, frequency, schedules" — versus protocol-intelligence-canon's "must not...
Design SARM cycles... Create protocol-builder flows for SARMs, SERMs, investigational peptides,
gray-market compounds, or other high-risk substances," and the capability map's "It does not
authorize... individualized dosing... sourcing guidance," respectively) and both remain
`proposed_disposition: fix` — i.e. *not* resolved by the D-I owner ruling CI-001 cites, per the
inventory's own text: "D-I's ruling does not, by its own text, resolve this narrower
protocol-builder-flow prohibition; a later P0-D1 builder must confirm this narrower text is
brought into line with the D-B1(c) posture, not merely assume it is already settled" (CI-002 row).

The Objective section of the governing spec states the precedence manifest's entire purpose is
"the comparison rule that makes any two canon documents' relative authority answerable by lookup,
not argument" — i.e. it is designed and delivered as the mechanism a later P0-D1 builder (or any
reader) consults to resolve exactly this shape of conflict. Mechanically applying it to CI-002/
CI-005 as delivered yields: CHARTER.md's broad permission to "design and compare protocol
options" and to not be restricted on "individualized dosing" **outranks** the narrower,
purpose-built safety prohibitions on SARM cycles, PCT instructions, protocol-builder flows for
high-risk substances, and sourcing guidance — the *opposite* of the restrictive posture the owner
actually ruled in D-I ("Deterministic math on user-entered values + guidance-contract v1.0.0
Class A/B/C surfaces are the product's current posture... `biostack-recommended` origination...
publicly enabled only after guidance-content-contract v2.0.0 re-ratification"). This happens
precisely *because* the ranking rule's property 1→2→3 priority (ratification-formality, then
recency, then scope-breadth) rewards CHARTER's undated, broad-scope permissive text over a
narrower, purpose-specific safety prohibition that carries no formal ratification event either —
property 3 ("scope breadth... product-wide... outranks the narrower one") breaks the tie in favor
of the *more permissive, broader* document for this specific pair, with no safety-direction
override anywhere in the rule.

Contrast: for CI-001/CI-004 (CHARTER vs. `biostack-guidance-content-contract.v1.md`), the
contract's dated/named ratification event makes it rank 1, *above* CHARTER's rank 3 — the
opposite directional outcome, decided by a different, unrelated technicality (whether a
ratification stamp exists), not by any consistent safety principle. A mechanical reader applying
`compare()` gets *inconsistent* safety outcomes across CI-001/CI-004 (restrictive doc wins) versus
CI-002/CI-005 (permissive doc wins) for materially the same class of dosing/protocol-design
conflict, entirely because of an accident of which specific document happens to carry a dated
"Fully ratified" stamp.

**Why this matters under the review brief.** The spec's own single most important tripwire
("Stop conditions," bullet 1) is: "Any artifact would state, imply, or could reasonably be read as
stating a product allowed-output... this is P0-B's unauthorized territory." A total order whose
designed purpose is cross-document conflict resolution, and which — when applied exactly as
designed to two of the inventory's own open rows — resolves in favor of "SARM protocol-builder
flows and individualized dosing are permitted because CHARTER.md outranks the document that
forbids them," is a reasonable reading of an artifact deciding a product allowed-output. Neither
`canon-precedence.md`'s "Scope boundary statement" (section 4, which only disclaims
*within-document* sentence-level conflicts, not *cross-document* rank-driven resolution — the
manifest's actual stated purpose) nor any other artifact in this delivery contains a caveat
blocking this reading. CI-002 and CI-005's own `conflict`/`proposed_disposition` text never invoke
the manifest's rank to resolve themselves (good — the inventory itself stays disciplined), but the
manifest is a free-standing, independently citable deliverable (`docs/specs/schemas/canon-precedence.md`,
linked from `docs/specs/README.md`'s new section) that a future P0-D1 builder, reviewer, or
external reader could legitimately apply on its own terms to reach exactly that conclusion.

**Attribution.** The three-property ranking rule (ratification-formality → recency → scope-breadth,
in that fixed priority, with no safety-direction override) is mandated verbatim by `P0-A.md`
("Precedence manifest," Design, properties 1-3) — the builder had no spec-given discretion to
deviate from it, and the registry's actual rank assignments (CHARTER rank 3, protocol-intelligence-canon
rank 5, capability-map rank 8, guidance-contract rank 1) are each individually defensible applications
of that exact rule to the real corpus. This finding is therefore **substantially inherited from the
spec's own ranking-rule design**, not an implementation deviation from it. What *was* within the
builder's discretion — and is the implementation gap I am flagging — is that nothing in the
delivered `canon-precedence.md`, `CONTRADICTION-INVENTORY.md`, or `P0-A-inventory/README.md`
records this specific, concrete, foreseeable misreading risk (the same discipline the builder
*did* apply elsewhere, e.g. CI-005's explicit `priority: P0-D1-first` flag for the
`controlled-or-illegal-sourcing` label). A one-paragraph caveat in `canon-precedence.md` section 4
("the registry's rank order must not, by itself, be read as resolving any `CI-NNN` row's safety
question — see each row's own `proposed_disposition`/`handoff_target`; a lower-ranked document's
narrower safety-specific prohibition is not superseded merely because a higher-ranked document's
broader permission covers the same subject") would have closed this gap at near-zero cost and
without touching the mandated ranking algorithm itself.

**Smallest amendment:** add that one caveat paragraph to `canon-precedence.md` section 4 (or a new
section 4a), citing CI-002/CI-005 as the concrete example this parcel's own corpus read surfaced.
Does not require re-ranking any document or reopening the ranking algorithm.

**Does it change a locked decision?** No — it adds a caveat; it does not alter D-G, D-I, any rank,
or any `CI-NNN` disposition.

**Severity recommendation:** Given the finding is largely inherited from spec-mandated ranking
logic and the inventory itself never exercises the manifest to resolve CI-002/CI-005 (the actual,
committed artifacts do not themselves state a product allowed-output), I am not independently
rejecting the merge on this finding alone, but I flag it as the single highest-priority item for
the coordinator/owner to weigh, because it meets the letter of (d) in this review's charge
("any way the manifest's ordering could be read as deciding product allowed-outputs — that would
be a BLOCKER"). A reasonable second reviewer could call this BLOCKER rather than MAJOR; I record it
at that rank deliberately so it is not silently downgraded.

### F2 — MAJOR: `verify-p0a.ps1`'s `unresolvable-citation` check does not verify quoted content
against the cited source, only that the cited *path* exists — a fabricated quotation in a
non-seed row passes cleanly

**Claim.** The Hard Constraints section of `P0-A.md` ("Real-corpus grounding") requires: "Every
`CI-NNN` row's quotations must be verifiably present, verbatim, in the cited file at the cited
location at the pinned `BaseCommit`. A quotation that does not match the source file byte-for-byte
... fails validation." The Deterministic-verification fold table maps "provenance loss" (a
health-boundary `mandatoryStopConditions` entry) explicitly to this same constraint plus
`unresolvable-citation`. In the shipped `verify-p0a.ps1` (lines 504-513), `unresolvable-citation`
only extracts backtick-delimited `.md` paths from the inventory text and probes `git cat-file -e
<BaseCommit>:<path>` — it never extracts or compares the *quoted span itself* against the file's
actual content, for any row other than the ten seed rows (which `seed-regression`, lines 349-370,
separately verifies — but only against the *spec's own text*, not against the real source files
either).

**Reproduction (in `/tmp/p0a_adv`, a disposable clone of the reviewed worktree, no repository
state in `biostack`/`biostack-wt` touched):** I altered CI-011's `source_b` quotation from the
actual INDEX.md text `"TBD (coordinator to assign Gate 2)"` to a fabricated
`"TBD (coordinator to assign Gate 99, completely fabricated text)"`, committed the change, and
re-ran `verify-p0a.ps1 -BaseCommit b78e7de8463a3db410c45cf223cd722713fec816 ...`. All 18 checks
still reported PASS, including `unresolvable-citation (absence)` and `row-schema-conformance`. The
fabricated quotation was never caught. (I separately confirmed `seed-regression` *does* catch a
tampered **seed** quotation — altering CI-001's `"Give clinical dosing instructions"` to `"Give
clinical dosage instructions"` correctly fails the verifier — so the gap is specific to non-seed
rows, i.e. exactly the kind of row this parcel's own corpus-read extension (CI-011) and any future
P0-A rework's new rows would be.)

**Why this matters.** The spec frames "Real-corpus grounding" as a hard, verifier-checked
constraint, not a reviewer-only discipline (unlike, e.g., `no-unattributed-claim`, which the spec
itself calls "heuristic"). For the ten seed rows this is true in substance (seed-regression
indirectly anchors them to the spec text, and I independently hand-verified seed quotations
against the real files and found them accurate). For any row the builder adds beyond the seed set
— which is exactly the parcel's own stated purpose for `CI-011` and would be the norm for every
subsequent P0-A extension — no automated check closes the loop back to the actual file content.
Acceptance criterion 8 ("No file outside Exact allowed surfaces is touched... not solely a
verifier-script claim") already acknowledges some checks are human-diff-backed rather than
script-provable; this finding is that `unresolvable-citation`'s *name* and the spec's
"provenance loss" mapping both promise more automated coverage than the script delivers. I did
hand-verify CI-011's actual quotation and found it accurate (confirmed `TBD (coordinator to assign
Gate 2)` and `[TBD]` at `docs/specs/INDEX.md` lines 8-13 at `BaseCommit`) — this is not a finding
that the shipped inventory contains a false quotation, only that the verifier would not have
caught it if it had.

**Smallest amendment:** extend `unresolvable-citation` (or add a new check) to extract the
italic-quoted span(s) associated with each non-seed `CI-NNN` row's `source_a`/`source_b` citation
and assert each appears verbatim (same normalization rules as `seed-regression`) in the cited
file's content at `BaseCommit` — the same technique `seed-regression` already uses, generalized
from "the spec's seed table" to "every row's cited file."

**Does it change a locked decision?** No.

### F3 — MINOR: CI-001's source_b quotation of the guidance-content-contract's Class D elides the
class-level conditional qualifier on "Prohibited," inherited verbatim from the spec's seed text

**Claim.** `docs/guidance/biostack-guidance-content-contract.v1.md` states, immediately above the
"Prohibited:" bullet list CI-001 quotes from: "**Status:** **Prohibited** unless product and
regulatory posture deliberately change via a new contract version." (verified at `BaseCommit`,
file read directly). CI-001's `source_b` field and `conflict` paragraph (in both `P0-A.md`'s seed
table and the shipped `CONTRADICTION-INVENTORY.md`, byte-identical per `seed-regression`) quote
only the bare "**Prohibited**" bullet-list framing and never the class-level conditional
("unless... via a new contract version"). The individual quoted bullet items ("Selecting the
correct dose for a person," "Personalized titration schedules") are themselves byte-exact
substrings of the source, so this is not a byte-for-byte violation of the Hard Constraints'
"Real-corpus grounding" test — it is an elision of a *material qualifier that governs the entire
class* the quoted items belong to, and CI-001's own disposition text (citing D-I/D-B1(c) and
"guidance-content-contract v2.0.0 re-ratification") shows the builder/spec-author already
understood this conditionality; it is simply never surfaced as part of the quoted evidence itself.

**Attribution.** Byte-identical to the spec's own seed-table text (verified by direct comparison);
the `seed-regression` check requires the builder reproduce this exact wording unweakened. The
builder had no discretion to add the qualifier without "softening a seed finding," which the spec
explicitly forbids without a named, reviewed reason. This is therefore a spec-inherited elision,
not an implementation defect, and I record it at MINOR severity accordingly — flagged per this
review's task (c) regardless of origin.

**Smallest amendment:** none within this builder's discretion; the amendment belongs to a future
spec rework of the seed table (out of this review's scope), or a reviewer-noted caveat added
alongside the seed row (not itself prohibited, since it is new text, not a change to the seed
quotation).

**Does it change a locked decision?** No.

### F4 — MINOR: `canonical-write-fencing-violation` only diffs `BaseCommit...HEAD` (committed
history) plus untracked files; an uncommitted modification to a frozen file in the working tree is
invisible to the check

**Claim.** `verify-p0a.ps1` lines 148-156 compute `allChanged` from `git diff --name-only
$BaseCommit...HEAD` plus `git ls-files --others --exclude-standard` (untracked files) — both are
blind to a **tracked** file modified in the working tree but not yet committed. Reproduced in
`/tmp/p0a_adv`: appending a line to `CHARTER.md` without committing left `canonical-write-fencing-violation`
PASS; committing the identical change correctly failed the check with
`canonical-write-fencing-violation: docs/INITIATIVES/biostack-governed-delivery/CHARTER.md is
outside the allowed-surfaces list.`

**Why this matters less than it might.** Acceptance criterion 8 already states file-surface
discipline is "not solely a verifier-script claim" and requires direct diff review in addition.
The verifier is designed to run against a committed Gate-2-dispatch tip, not a dirty working tree,
so this is a theoretical gap under normal process discipline, not a practical bypass of the actual
merge-gate (the PR diff reviewers and the coordinator see is always the committed state).

**Smallest amendment:** optionally add a `git status --porcelain` dirty-worktree check that fails
closed if uncommitted changes exist outside `artifacts/p0a-verification/`, matching the spirit of
P2/P3-A's own verifiers (not independently confirmed whether they already do this — out of scope
to check here).

**Does it change a locked decision?** No.

## (a) Precedence pair the rule cannot answer, or answers twice

Checked: `canon-precedence.md`'s comparison function is a pure dense-rank lookup (section 3); no
pair in the current 14-row registry shares a rank, so no pair returns `open-tie`, and I found no
pair the rule leaves genuinely unanswered. I did not find a document pair that the rule answers
*twice* (contradictorily) in the shipped text. The closer issue I found is **F1** above — not two
answers to the same `compare(A,B)` query, but the same ranking *methodology*, applied consistently,
produces directionally opposite safety outcomes across structurally similar conflict pairs
(CI-001/CI-004 vs. CI-002/CI-005) because of an accident of which document happens to carry a
dated ratification stamp. I record this under (d)/F1 rather than as a strict (a) finding, since
the comparison function itself is total and unambiguous as specified.

## (b) Contradiction the inventory misses (spot-audit)

I spot-checked dosing/provenance/tier/safety-boundary claims across `CHARTER.md`, `README.md`,
`canon.md`, `guidance.md`, `capmap.md`, and the knowledge-engine-model-data-roadmap for an
uncatalogued conflict and did not find one the inventory clearly misses within my review window.
Candidates I checked and ruled out as non-findings: README's tier table (`## What is free, and
what is not`) versus CHARTER (CHARTER states no tier-gating doctrine to compare against — genuinely
`scope-disjoint`, consistent with `CORPUS-COVERAGE-MATRIX.md`'s treatment); the knowledge-engine
roadmap's "autonomous" language versus README's "It is not an autonomous authority" (both say the
same thing, no conflict — consistent with CI-010's broader treatment of the promotion-authority
question). I did not have time within this review's budget to exhaustively re-derive all 78
corpus-coverage-matrix pairs independently; I spot-checked roughly a dozen and found the recorded
outcomes defensible.

## (c) Byte-exactness / elided-qualifier audit

Spot-checked CI-001 (CHARTER.md lines 34-35, 101; canon.md lines 32, 41; README.md line 87;
guidance.md lines 114-115), CI-002 (canon.md lines 34-39), CI-003, CI-005 (capmap.md line 6), CI-009
(pairwise001.md line 50), the D-I quote (coord.md lines 152-169), and the P0-B-DESIGN-GATE §2
reference (p0b-gate.md lines 56, 74-75) against the real files at `BaseCommit`. All matched
byte-for-byte as quoted (modulo the spec's own stated exemption for citation-delimiter quote
marks). One material-qualifier elision found: **F3** above (Class D's class-level "unless... new
contract version" conditional, omitted from CI-001's quoted evidence) — inherited from the spec,
not introduced by the builder.

## (d) Product-allowed-output risk in the inventory's dispositions or the manifest's ordering

See **F1** (manifest ordering). Separately checked every `CI-NNN` row's `conflict` and
`proposed_disposition` prose for first-person product-rule assertions ("BioStack must/may/does
not"): all such occurrences I found are inside quotation marks attributing the claim to a named
source document, consistent with the "No product allowed-output decision" hard constraint and the
`no-unattributed-claim` check's intent. No row's own disposition text asserts a product rule in
P0-A's voice.

## (e) Breaking `verify-p0a.ps1` with adversarial inputs

Performed in `/tmp/p0a_adv` only (disposable clone; `biostack` and `biostack-wt/p0a-impl` never
mutated). Results:
- Tampered seed quotation (CI-001) → correctly caught by `seed-regression`.
- Injected literal `TBD` into an inventory-method file → correctly caught by `no-placeholder`.
- Committed edit to a frozen surface (`CHARTER.md`) → correctly caught by
  `canonical-write-fencing-violation`.
- **Uncommitted** edit to the same frozen surface → **not caught** (F4, MINOR, mitigated by
  acceptance criterion 8's human-diff-review requirement).
- Injected an email token into the inventory → correctly caught by `personal-data-token-found`.
- Falsely marked a required source `unreviewable` (claiming deletion) without matching evidence →
  correctly caught by `unreviewable-claim-verified`.
- **Fabricated a non-seed row's (CI-011) quoted content** → **not caught** by any check (F2,
  MAJOR — the most significant verifier gap found).
- Did not find a way to pass the verifier while leaving a required source missing from
  `SOURCE-MANIFEST.md`, or while mismatching `status: contradictory` with
  `proposed_disposition: not-applicable` — both are directly, correctly enforced by
  `row-schema-conformance`/`source-manifest-completeness` in the code path I read (lines 294-307,
  and the `source-manifest-completeness` check referenced in the pass log); I did not independently
  fuzz every code path.

## (f) Stale/wrong hashes

- `BaseCommit` (`b78e7de8463a3db410c45cf223cd722713fec816`) cited consistently across
  `canon-precedence.md`, `CONTRADICTION-INVENTORY.md`, `SOURCE-MANIFEST.md`,
  `CORPUS-COVERAGE-MATRIX.md` — verified this commit exists, is an ancestor of the reviewed merge
  commit, and its own diff adds `GATE2-P0A-IMPLEMENTATION.md` (confirming the "single anchor =
  the commit that adds THIS file to main" claim in the dispatch record). No stale or wrong
  `BaseCommit` hash found.
- Did not independently re-verify the charter hash
  (`4CD390D631487DA8E97509205A186F242C4A83B762FFFA894BEEC8A21F4EFE22`) or the two coordinator-ledger
  hashes cited in `P0-A.md`'s lineage section against the live repository — out of this
  implementation review's direct scope (those are spec-lineage claims, not implementation
  artifacts), but flagging that I did not check them so this is not silently treated as verified.

## Missing pieces / collisions / unknowns

- `P0-A.md`'s own file header still reads "Status: **REVIEW CANDIDATE — builder dispatch
  blocked**" even though dispatch has in fact occurred and the implementation is merged to `main`
  at `c0d8642`. `P0-A.md` is not in this parcel's "Exact allowed surfaces" list, so the builder
  correctly did not touch it; this is a coordinator/closure-process staleness, not an
  implementation defect, but it means a reader of `P0-A.md` alone (without also checking
  `docs/specs/INDEX.md` or the dispatch record) would be misled about the parcel's actual state.
- I did not independently re-verify all 78 `CORPUS-COVERAGE-MATRIX.md` pair outcomes or all ten
  `BIO-PAIRWISE-00N` file reads against the real corpus; spot-checks were consistent with the
  recorded outcomes.

## Verification notes

- `verify-p0a.ps1` run clean (18/18 PASS) against the unmodified worktree at
  `/home/cmorgan76/Repos/biostack-wt/p0a-impl` with
  `-BaseCommit b78e7de8463a3db410c45cf223cd722713fec816 -BuilderId test_builder -ReviewerIds r1,r2
  -EvidenceDirectory artifacts/p0a-verification` (artifacts dir is gitignored/untracked scratch,
  not committed).
- Allowed-surfaces diff for `c0d8642` matches `P0-A.md`'s "Exact allowed surfaces" list exactly:
  9 files, same paths, `docs/specs/README.md`/`docs/specs/INDEX.md` each carrying exactly one
  appended section/row and zero other diffs (confirmed by direct `git diff` against
  `BaseCommit`).
- Class-axis tags used throughout `CONTRADICTION-INVENTORY.md` (`health-boundary`, `privacy`,
  `legal-policy`, `knowledge-promotion`, `standard`, `personalized-protocol-recommendation`,
  `deterministic-calculation`, `curated-evidence-guidance`, `safety-escalation`,
  `injection-or-sterile-preparation`, `investigational-or-unapproved`,
  `gray-market-or-identity-uncertain`, `interaction-or-contraindication-signal`,
  `controlled-or-illegal-sourcing`) all verified present in
  `docs/specs/schemas/classification-axes.schema.json`'s closed vocabularies.
- `CORPUS-COVERAGE-MATRIX.md` row count verified: exactly 78 distinct `N-M` pairs present
  (`13 choose 2`), matching the required-source count.
- `DATA-CLASSIFICATION.md`'s grep method is re-runnable as documented; did not independently
  re-run it against the live artifacts (relied on `verify-p0a.ps1`'s `personal-data-token-found`
  pass plus my own adversarial email-injection test, which the same mechanism caught).
