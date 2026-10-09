# p0a_re_review_3 — P0-A spec review

**Verdict:** APPROVE
**Spec file:** /home/cmorgan76/Repos/biostack-wt/p0a-rework-1/docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md
**Spec SHA-256:** cc4a526920dff139f041df2b353779587fa62abb82154e9cf6d3d251683d15e3
**Date:** 2026-10-08

## Ranked findings

### MINOR-1 — D-I line-range citation off by one line
**Claim:** The spec cites D-I's span as "lines 152-169" in two places ("Lineage and dependencies"
and "Required source list" item 12's cross-reference context), but `## D-I` actually runs
152-170 (the ledger file is 170 lines; line 170 — "contradiction inventory records its
disposition by this ruling." — is part of D-I's quoted sentence and is omitted from the stated
range).
**Evidence:** `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` (worktree checkout),
`wc -l` = 170; `## D-I` heading at line 152; no heading after it; the quoted sentence *"the §2
canon conflict is resolved in substance by D-B1(c), and P0-A's contradiction inventory records
its disposition by this ruling"* spans lines 169-170. Spec line ~45: "`## D-I` heading, lines
152-169". The quoted D-I **text itself**, reproduced in the spec body, is byte-for-byte correct —
only the numeric line-range citation undercounts by one line.
**Smallest amendment:** Change "lines 152-169" to "lines 152-170" (two occurrences).
**Does it change a locked decision?** No — cosmetic citation-range inaccuracy, not a
quotation-accuracy or authorization-boundary defect. Pre-existing in the prior reviewed hash
(not introduced by this delta — confirmed by diff against commit `7753861`), so it is not a new
defect introduced by this rework, but it was not caught by either Round 1 or Round 2 review and
is worth a one-line fix before Gate 2 dispatch.

No BLOCKER or MAJOR findings. No other discrepancy found after exhaustive verification (below).

## Missing pieces / collisions / unknowns

None identified. The two Round-2 `fix` items (CI-004 quote byte accuracy; `unreviewable`
checkout condition determinism) are both closed cleanly, and the diff against the prior reviewed
hash (`7753861`) is scoped exactly to those two fixes plus their necessarily-coupled verifier-
check language (`unreviewable-claim-verified`) — no unrelated line moved, no seed row content
changed, no allowed-surfaces/hard-constraints/stop-conditions text touched.

## Verification notes

**(1) Seed-finding quotation accuracy — CI-004 first, then full corpus:**
- CI-004 `source_a` (CHARTER.md line 33, "Make educated, profile-aware recommendations…
  longitudinal observations.") — **byte-exact**, including trailing period.
- CI-004 `source_b` Class C (`biostack-guidance-content-contract.v1.md` line 100, "Must not
  morph into “you should start at X.”") — **byte-exact**, including the source's curly
  quotation marks (`“`/`”`), which the prior hash had rendered as straight quotes (the defect
  Round 2 flagged). The spec's new "exact-glyph" rule (Hard constraints, "Real-corpus
  grounding") correctly requires this and the fix correctly applies it.
- CI-004 `source_b` Class D ("Declaring an amount safe for the user," "Declaring a protocol
  appropriate for the user" — lines 117-118) — byte-exact.
- Spot-checked all remaining nine seed rows (CI-001 through CI-003, CI-005 through CI-010)
  against their cited source files (`CHARTER.md` lines 32/34/35/52/101; `README.md` lines 79/87;
  `docs/canon/biostack-protocol-intelligence-canon.md` lines 32-41; `docs/product/knowledge-
  engine-capability-map.md` line 6; `BIOSTACK_FRONTEND_READINESS_AUDIT.md` line 42; `docs/specs/
  active/BIO-PAIRWISE-001-….md` line 50): every quotation is byte-exact at the cited location
  (ellipsis elisions, where present — CI-001's README quote — are honestly marked and the
  elided span is immaterial list items, not a softened qualifier).
- D-G quotation (lines 114-123) verified by re-hashing the exact span
  (`sed -n '114,123p' … | sha256sum` → `3D46AC19AEF9A0F553239B6EC984E90216286C72918FFB2AA04EFA81B90BC4F9`,
  matches the spec's cited hash exactly) and reading the text directly — verbatim.
- D-H (lines 125-150) and D-I (lines 152-170, see MINOR-1) text quoted in the spec body is
  verbatim against the ledger.
- Ledger whole-file hashes: shaping-time (`EE506C3B…63D47`, verified via
  `git show 32280aa2…:…COORDINATOR-DECISIONS-2026-10-07.md | sha256sum`) and current
  (`908FAA59…4CA7AE`, verified via direct `sha256sum` in the worktree checkout) both match
  exactly.
- Charter SHA-256 (`4CD390D6…21F4EFE22`) and PLAN-REVIEW.md SHA-256 (`FBE40053…B69E8E256`) both
  verified by direct `sha256sum` — exact matches.
- `main@32280aa2…` is confirmed an ancestor of `main@a25a658e…` via
  `git merge-base --is-ancestor`, both anchors resolve to the commits the spec names.
- P2 `done` status and closure record, P3-A's absent `docs/specs/INDEX.md` row, and
  `P0-B-DESIGN-GATE.md` §2's conflict framing were all independently confirmed to match the
  spec's description verbatim.

**(2) `unreviewable`/"outside this worktree's checkout" independently checkable form:**
Round 2's R2r2-F2 finding is closed correctly. Condition 3 is now defined as the conjunction of
two independently reproducible commands: `git cat-file -e <BaseCommit>:<path>` **succeeds**
(proves the path is tracked at the pinned commit, ruling out condition 1 — deleted) **and** a
direct filesystem read of the same path in the dispatch worktree **fails** with a hard OS-level
read error (proves checkout absence, distinct from a genuinely readable file). Both exact
command outputs (success evidence for the first, the OS error text for the second) plus the
pinned `BaseCommit` SHA must be recorded in `SOURCE-MANIFEST.md`. The `unreviewable-claim-
verified` deterministic check was updated consistently: it replays both commands and fails
closed (`false-unreviewable-claim`) on any of (a) the git check itself failing under a
condition-3 claim (reclassified as a condition-1 mismatch), or (b) the filesystem read
unexpectedly succeeding. This is a genuinely falsifiable, scriptable, reproducible test — not
merely asserted. No remaining nondeterminism or judgment call in this condition.

**(3) New defects introduced by the delta:** Diffed the current spec against the prior reviewed
hash (commit `7753861`, the Round-2 version). The diff touches exactly four regions: (a) the
CI-004 quote glyph fix, (b) the condition-3 definition rewrite, (c) the evidence-recording
paragraph rewrite (now requires `BaseCommit` SHA + exact command outputs for all three
conditions, not just conditions 1/2), and (d) the matching `unreviewable-claim-verified` verifier
check rewrite, plus the new "exact-glyph" sentence appended to "Real-corpus grounding." All four
are internally consistent with each other and with the rest of the spec (e.g., the Missingness
rule's three-condition definition, the verifier check table, and the acceptance criteria all
agree on the same condition-3 semantics). No unrelated text moved; no seed row content, allowed
surfaces, hard constraints, or stop conditions changed. One pre-existing MINOR citation-range
inaccuracy was found (MINOR-1, above) — present before this delta and not touched by it.

**(4) Strictly analytical, no product allowed-output decision:** Re-verified. "Hard constraints"
retains its unmodified "No product allowed-output decision" clause as the parcel's primary
tripwire; "Stop conditions" bullet 1 repeats it as the single most important tripwire; the CI-001
`resolved-by-owner-ruling` disposition (the one row touching the live P0-B design-gate ruling)
explicitly states the row "keeps `status: contradictory`… the ruling settles which posture
governs the product now; it amends neither document's text" and still only names a
`handoff_target` (P0-D1) rather than authoring or implying the resolution itself. No seed row,
deliverable, allowed-surface, or acceptance criterion states, implies, or encodes a product
allowed/degraded/refused/escalated behavior. P0-B's boundary remains intact.

Checked and found clean, not separately itemized above: row-schema field count (11, matches
table); `no-placeholder` self-references are meta-mentions of forbidden markers, not actual
placeholders; exact allowed-surfaces list vs. frozen-surfaces list has no overlap or
contradiction; dispatch precondition (P3-A must close before Gate 2) is stated once and applied
consistently in "Lineage and dependencies" and "Stop conditions."
