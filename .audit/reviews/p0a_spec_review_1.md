# p0a_spec_review_1 — P0-A spec review

**Verdict:** REJECT
**Spec file:** docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md
**Spec SHA-256:** 50c22f74569c2453952b56e372e40bfc159162b0de56c583c1fdc6ca93e0bc10
**Date:** 2026-10-09 (repository-local date; no live clock available to this reviewer)

## Ranked findings

### F1 — BLOCKER — Cited SHA-256 for the sole standing-authorization source is wrong
**Claim:** The spec's "Lineage and dependencies" section cites
`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` SHA-256 as
`EE506C3B54FB57ABD09BBB5180CD704AABCA3EB73875B7E760EF84B538863D47` and calls it "this parcel's
sole standing-authorization source," then quotes D-G "verbatim" from it.
**Evidence:** `sha256sum docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` →
`908faa5999526de76cc7e21b6753d41bc8bdb9f29c5f1717a504d9103e4ca7ae` — completely different from the
cited hash (not a case or whitespace artifact; a full content mismatch). `git status --short`
shows no uncommitted changes to that file, and the charter and plan-review hashes cited in the
same section *do* verify correctly (`4cd390d6...`, `fbe40053...`), so this is not a global
tooling error — the COORDINATOR-DECISIONS hash specifically is wrong.
**Smallest amendment:** Correct the cited hash to the current committed value, or anchor the
citation to a specific commit of that file (it is an append-only growing log, not a frozen
artifact) rather than asserting a single pinned hash for "this parcel's sole standing-authorization
source."
**Changes a locked decision?** No — but it means the cited proof of D-G's exact text is currently
unverifiable as stated, which matters because this is the parcel's *only* standing-authorization
citation.

### F2 — BLOCKER — Required source list omits the document that already contains a later, directly relevant owner ruling on this parcel's flagship conflict
**Claim:** The spec quotes D-G from `COORDINATOR-DECISIONS-2026-10-07.md` as the sole
authorization source, but does **not** list that file (or
`docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md`) in the "Required source list."
That same decisions file, as it exists on disk today, also contains **D-H** ("P0-B design gate
OPEN") and **D-I** ("P0-B design gate RULED... D-B1 = (c) staged split... the §2 canon conflict is
resolved in substance by D-B1(c), **and P0-A's contradiction inventory records its disposition by
this ruling**"). D-I is dated the same day as D-G (2026-10-08) and explicitly assigns this
parcel the job of recording its disposition for exactly the conflict the spec's seed findings
CI-001/CI-002/CI-004/CI-005 catalogue (the charter's personalized-dosing/protocol-builder "may"
language versus the guidance-content-contract's Class D prohibitions). The spec's seed findings
carry no reference to D-H/D-I, and `seed-regression` locks them "byte-for-byte unchanged... without
a named, reviewed reason."
**Evidence:** `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` lines 114 (D-G), 122-133
(D-H), 135-150 (D-I, quoting "the §2 canon conflict is resolved in substance by D-B1(c), and
P0-A's contradiction inventory records its disposition by this ruling"); P0-A.md "Required source
list" (11 numbered items, neither COORDINATOR-DECISIONS-2026-10-07.md nor P0-B-DESIGN-GATE.md
appears); P0-A.md CI-001/CI-002/CI-004/CI-005 rows (no mention of D-I or the staged-split ruling).
**Smallest amendment:** Add `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` and
`docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` to the required source list, and
require the builder to cross-reference D-I's ruling in the disposition/conflict text of every
seed row it bears on (at minimum CI-001, CI-002, CI-004, CI-005), rather than leaving them framed
as purely open conflicts awaiting a future P0-D1 decision the owner has, in substance, already
made.
**Changes a locked decision?** No — this is additive source coverage, not a reversal of any
P0-A decision — but it is a correctness/completeness gap that undermines criterion (a) (source-list
defensibility) and (c) (exhaustiveness: a builder following the spec literally would never be
required to discover D-H/D-I at all).

### F3 — BLOCKER — The precedence manifest's own worked example is unverifiable and conflicts with available evidence, breaking the determinism goal
**Claim:** The "Precedence manifest / Design" section asserts the charter "is, at this shaping
time, the most recent ratification event touching product-doctrine scope found in the required
source list, which is why it provisionally ranks first." This is the spec's only worked
application of its own rank rule #2 ("recency of ratification"), and it sits directly upstream of
CI-001's "fix → P0-D1" disposition. But CHARTER.md contains **no internally recorded date** for
its "100% Ratified" decision, while two other required-source documents carry explicit, dated
ratification events that are later or equally well-evidenced:
`docs/guidance/biostack-guidance-content-contract.v1.md` ("Fully ratified | 2026-08-02 (Clint
Morgan — all gates passed; no remaining blockers)") and
`docs/canon/biostack-protocol-intelligence-canon.md` ("Status: Canonical product foundation,"
dated 2026-06-18 in its own text, file last touched 2026-08-02 in git). The charter's own file
history (`git log -1 --format=%ad -- CHARTER.md` → `2026-07-16`) predates the guidance-content-
contract's stated ratification date. Two honest builders applying the stated three-property rule
could therefore reach materially different rankings for the exact comparison CI-001 depends on
(charter-first vs. guidance-contract-first), which breaks the "total order... deterministically and
without exception" guarantee the spec requires of the manifest. This is reinforced, not
contradicted, by D-I (see F2): the owner's actual ruling (D-B1(c)) treats guidance-content-contract
v1.0.0 as the product's *current* authoritative posture and gates any charter-"may" dosing output
behind a **future** v2.0.0 re-ratification — i.e., the owner's own resolution leans toward
guidance-content-contract outranking the charter's unconditional reading today, not the reverse the
spec's worked example assumes.
**Evidence:** `grep -n "2026-1\|2026-0" CHARTER.md` → no matches (no internal date anywhere in the
charter); `docs/guidance/biostack-guidance-content-contract.v1.md:10` → "Fully ratified |
2026-08-02..."; `git log -1 --format=%ad -- CHARTER.md` → `2026-07-16`; P0-A.md "Precedence
manifest / Design," rule 2's paragraph (provisional charter-first ranking).
**Smallest amendment:** Either (a) require the builder to source the charter's ratification date
from a specific, citable, dated record before applying rule 2 (not assert it in the spec itself),
or (b) remove the spec's own "provisional rank first" pre-judgment and let the manifest's registry
table alone decide, so the spec does not bake an unverified, outcome-determinative ranking into the
builder's starting point.
**Changes a locked decision?** No — it is a request to make the manifest's own reasoning
verifiable, not a ruling on the correct rank.

### F4 — MAJOR — Lineage section's P2/P3-A registry-status claim is stale/inaccurate as the spec reads today
**Claim:** "P2 spec... and P3-A spec... are, at this shaping anchor... still recorded
review-candidate in `docs/specs/INDEX.md`, not yet merged or closed." This was true for P2 at the
cited anchor commit, but P2 is now `done`/closed in the live `docs/specs/INDEX.md`, and P3-A has
**never** had a row in `docs/specs/INDEX.md` at all — neither at the anchor commit nor today — so
"recorded review-candidate" misdescribes P3-A's actual registry state (absent, not
review-candidate).
**Evidence:** `git show 32280aa2219100e20520a275d19af2fbdd1f32be:docs/specs/INDEX.md | grep P2` →
`review-candidate` (true at anchor); current `docs/specs/INDEX.md` P2 row → `done`; `grep -n
"P3-A" docs/specs/INDEX.md` (current and at the anchor commit) → no match in either.
**Smallest amendment:** Update the lineage text to state P2's current (`done`) status and correct
P3-A's description from "recorded review-candidate" to "spec exists at `REVIEW CANDIDATE` status
in its own file header but has no `docs/specs/INDEX.md` row yet." This does not change the
dispatch precondition's substance (dispatch is still correctly blocked pending P3-A's close) but
the spec's factual framing, read today, is wrong.
**Changes a locked decision?** No.

### F5 — MINOR — Seed-finding quotations elide material qualifying language, locked in by seed-regression
**Claim:** CI-001's `source_a` quotation omits (via "...") the charter's conditional clauses
"when the applicable capability contract permits it" (charter line 34) and D13's closing sentence
"This does not authorize diagnosis, prescribing, clinician impersonation, or unsupervised
alteration of prescribed treatment" (charter line 101) — both of which bear directly on whether
the charter's "may" is actually unconditional versus the paired "must not" lists. Because
`seed-regression` locks these rows "byte-for-byte unchanged... without a named, reviewed reason,"
this selective framing becomes the fixed floor of the inventory.
**Evidence:** `docs/INITIATIVES/biostack-governed-delivery/CHARTER.md` lines 34-35, 101; P0-A.md
line 247 (CI-001 row).
**Smallest amendment:** Quote the conditional clauses in full (or add an unquoted note in the
`conflict` field explaining why the condition does not resolve the tension), rather than eliding
them with an ellipsis that hides their scope-limiting effect.
**Changes a locked decision?** No.

### F6 — MINOR — Misattributed precedent for the `coordinator-assigns-at-gate-2` literal
**Claim:** "Exact allowed surfaces" item 8 calls the `coordinator-assigns-at-gate-2` cell literal
"P3-A's precedent," but P3-A has never merged and has no `docs/specs/INDEX.md` row; the literal's
actual live precedent is P2's own merged `docs/specs/INDEX.md` row (which already uses it twice)
and P2's own spec text.
**Evidence:** `grep -rn "coordinator-assigns-at-gate-2" docs/` → hits in `P2.md` (both as spec text
and live `verify-p2.ps1` assertions) and the live P2 `INDEX.md` row; P3-A.md only describes the
literal as a planned future usage, not yet realized in the registry.
**Smallest amendment:** Attribute the precedent to P2, not P3-A.
**Changes a locked decision?** No.

### F7 — MINOR — `mandatoryStopConditions` fold is not made explicit per class, unlike `minimumChecks`
**Claim:** The "Deterministic verification" table explicitly folds and adapts each of the four
classes' `minimumChecks` line by line. No equivalent explicit table exists for the four classes'
`mandatoryStopConditions` (health-boundary: "unsupported certainty," "prescribed-treatment
direction," "red-flag bypass," "provenance loss"; privacy: "new sensitive field without approved
lifecycle," "leakage," "consent bypass"; legal-policy: "unapproved policy presented as effective,"
"policy/enforcement mismatch"; knowledge-promotion: "missing source/license/review state,"
"bypassed promotion," "unreviewed public claim"). The generic "Stop conditions and
standing-authorization tripwire" section plausibly covers the spirit of most of these, but there is
no line-by-line proof each maps to a named P0-A stop condition the way `minimumChecks` got.
**Evidence:** `docs/specs/schemas/delivery-class-controls.json` `mandatoryStopConditions` per class
(verified directly); P0-A.md "Deterministic verification" (folds only `minimumChecks`); "Stop
conditions and standing-authorization tripwire" (7 generic bullets, no per-class attribution).
**Smallest amendment:** Add one line per class-specific `mandatoryStopConditions` entry mapping it
to existing tripwire language, mirroring the rigor already applied to `minimumChecks`.
**Changes a locked decision?** No.

## Missing pieces / collisions / unknowns

- Neither `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` nor
  `docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` is in the required source list
  or the frozen-surfaces list, despite the former being quoted as the parcel's sole
  standing-authorization source and both containing content (D-H, D-I) directly load-bearing for
  this parcel's own seed findings (see F2). A later P0-D1 builder reading only this spec's required
  source list would never be pointed at D-I's existing ruling.
- The spec's dispatch precondition ("P2 and P3-A must reach merged, closed status first") is
  correctly still enforced today (P3-A remains open/unregistered), but the lineage text describing
  *why* is stale (F4) — worth re-anchoring before dispatch is requested, since the spec will be
  read again at that time.
- `docs/specs/schemas/parcel-spec.schema.json`, which `verify-p0a.ps1`'s `no-placeholder` check
  says it will read "once P3-A has merged," does not exist yet — consistent with the spec's own
  acknowledgment and not a defect, but confirms the `no-placeholder` check cannot be instantiated
  until P3-A actually closes, reinforcing the dispatch precondition's necessity.

## Verification notes

- Computed and compared all three cited lineage hashes: charter hash matches (case-insensitive
  hex), plan-review hash matches, COORDINATOR-DECISIONS hash does **not** match (F1).
- Confirmed charter D8 (dual review for architecture/risk-sensitive parcels) and D14 (fieldwise
  union fold, max reviewer count, "no label erases another label's obligations") text matches the
  spec's characterization.
- Confirmed the charter's dependency spine text
  (`plan-review closure -> P1 -> P2 -> P3-A -> P0-A -> ...`) and the "no standing dispatch or merge
  authorization is inferred for P0" clause match the spec's claims about the authorization
  boundary.
- Confirmed `docs/specs/schemas/classification-axes.schema.json`'s three axis keys
  (`deliveryClass`, `productGuidanceClass`, `substanceFunctionRisk`) exist as the spec assumes.
- Confirmed `docs/specs/schemas/delivery-class-controls.json`'s `requiredSpecAdditions` for all
  four declared classes (health-boundary, privacy, legal-policy, knowledge-promotion) are each
  fully present as named subsections in the spec's "Mandatory class sections." `minimumChecks` are
  explicitly folded and adapted in the "Deterministic verification" table.
  `mandatoryStopConditions` are not folded with the same explicitness (F7).
- Confirmed all 11 required-source-list files exist on disk, plus the six `BIO-PAIRWISE-00x` files
  item 10 references.
- Spot-verified seven seed-finding quotations (CI-001 through CI-010, sampling charter, protocol-
  intelligence-canon, README, guidance-content-contract, capability-map, pairwise-001, and local-
  readiness README text) against source files; all matched verbatim except for the selective
  ellipsis issue noted in F5. No fabricated quotation found.
- Confirmed no instance of "BioStack may/must/should/will" appears in the spec's own voice outside
  quotations or the hard-constraint rule text itself — the no-product-allowed-output discipline
  (criterion d) is honored in the spec's own prose.
- Confirmed P2 is `done` and P3-A has no `docs/specs/INDEX.md` row, in the live repository state
  (criterion e), and traced this back to the anchor commit to establish F4's stale/inaccurate
  framing rather than an outright fabrication.
- Did not find any sentence in the spec that states, implies, or could reasonably be read as
  deciding a product allowed-output, guidance-class applicability, or
  allowed/degraded/refused/escalated behavior in P0-A's own voice — the hard-constraint tripwire
  appears honored at the text level. The deeper problem (F2/F3) is that the spec's *analytical
  machinery* (precedence rank, seed dispositions) is not provably correct/determinstic/complete
  given already-existing canon, not that it illegitimately crosses into P0-B's territory.
