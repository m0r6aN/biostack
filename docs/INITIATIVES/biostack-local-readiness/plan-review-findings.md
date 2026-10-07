# Plan-Review Triage — biostack-local-readiness (coordinator)

**Review:** one fresh session, charter + repo canon only, zero coordinator context (dispatched 2026-09-19).
Charter reviewed inclusive of the D10 amendment at ratification (reviewer confirmed).
**Triage by:** coordinator session 2026-09-19 (owner of the queue per `LOOP-DIRECTIVE.md`).
**Load-bearing reproduction (coordinator, on disk — reproduction is the tie-breaker):**
- PR-01 arithmetic REPRODUCED: `CorpusIdentityInventoryBuilder.cs:39-44` loads ONLY
  `research/input/candidates/pilot-compound-candidates.json` (16 candidates); the 70-universe file
  (`research/input/candidates/peptide-serm-sarm-market-interest.v1.json`, verified 70 candidates) is never
  loaded. Frozen pins verified: overlap 12, CandidateOnly 4, MissingEvidence empty, EvidenceWithoutCandidate 62
  (`CorpusIdentityInventoryBuilderTests.cs:16-24`). Builder-visible new IDs = 4+62 = 66 < 93 needed;
  absolute bound 57+78 = 135 < 150. As written, 007 must verdict `unreachable`.
- Baseline frame REPRODUCED: seed 57 / pilot 16 / evidence 78 / registry 30 (parsed on disk).
- Contract shape REPRODUCED for 006 lint: v1.0.0, effective 2026-07-13, monthly-only USD 0/1200/2900¢,
  paid access Active/Trialing, grace 0, aliases `/onboarding→/start` + `/map→/tools/analyzer`,
  health `/health` + `/health/keon` (`contracts/product-contract.v1.json`).
- Zero `OQ1|OQ2|OQ3|recommended default|assumption` hits across all eleven specs (re-ran) — PR-08 REPRODUCED.
- `docs/specs/INDEX.md`, `docs/specs/done/`, `evidence/` all absent — PR-06/PR-11 context REPRODUCED.
- 006 AC5 (line 73) cites 001/002 transcripts while 006 runs in the first batch — PR-09 REPRODUCED from spec text.
- `PARCELS.md:21` vs charter `:65` + 009/010 rebase specs — PR-04 REPRODUCED from spec text.
- `RELEASE-GATES.md:10` ("OQ1–OQ3 ruled at Gate 1") vs charter `:80` (carried OPEN) — PR-07 REPRODUCED.

## Dispositions (fix / accept-as-documented / informational)

| ID | Severity | Disposition | Coordinator ruling / action |
|---|---|---|---|
| PR-01 | blocking (008-chain) | accept-as-documented | No decision changed; charter stop path already armed: 007 proceeds as written and will likely verdict `unreachable-<n>` → 008–011 STOP, owner rules (KEO-73/74). NOT queue-blocking: 001–006 orthogonal, proceed. No Gate 1 re-open. |
| PR-02 | blocking (content) | accept-as-documented | D10 stands as ratified (stretch risk owned by owner). In-parcel controls (trace tables, unknown-honest, dual review, stops) are the mitigation; 007-first kept as cheapest falsifier, NOT treated as content clearance — recorded here. Owner handoff notes likely 011-stop. No Gate 1 re-open. |
| PR-03 | major | fix (deferred) | Shaping at 010 head: either 007 output gains a reserve list or 010's reserve-list bound is deleted. Moot if 007 verdicts unreachable. |
| PR-04 | major | fix (applied 2026-09-19) | `PARCELS.md:21` projection corrected to charter `:65` semantics (008→009→010 rebase onto prior merge; all others fork `origin/main`). Projection fix, not a charter change. |
| PR-05 | major | refer-to-owner | Exit item 1 wording (001..006) is charter text — coordinator cannot amend. Loop practice: all 001..011 run the full loop per D8 extension + `PARCELS.md`. Owner confirms exit wording at handoff; no re-open now, orthogonal work proceeds. |
| PR-06 | major | accept-as-documented + clarification | Spec `active/→done/` moves are coordinator Stage-F close-out (loop step 9), never parcel Allowed Files — no spec change needed. INDEX regen is coordinator-owned IF OQ3 adopted; OQ3 stays OPEN so no INDEX work exists. |
| PR-07 | major | fix (applied 2026-09-19) | `RELEASE-GATES.md` `open-decisions-resolved` description corrected to the Gate 1 record (OQ1–OQ3 OPEN; parcels proceed on spec-stated defaults; flip needs owner ruling/waiver in `DECISIONS.md`). Status cell untouched. |
| PR-08 | major | fix (applied 2026-09-19, partial) | OQ-assumption lines added to 006 + 001 specs (activates charter `:80` rule). Remaining parcels shaped at head. Verdict (g): workable after addendum — see rulings below. |
| PR-09 | major | fix (applied 2026-09-19) | 006 AC5 reworded: code assertions now; runtime cross-cite deferred to 002 transcripts (follow-up link). |
| PR-10 | major | fix (deferred) | Shaping at 011 head: enumerate every editable literal class (counts, ID lists, collision keys/owners, derived overlap lists — observed-only, itemized). |
| PR-11 | major | fix (applied 2026-09-19) | 001 Allowed Files gains `evidence/BIO-LOCAL-001-config-note.md` (gate's "001 config note" now authorized). |
| PR-12 | minor | accept-as-documented + ruling | **Ruling:** LS3 closes ONLY on 002 AND 005 both green (mirrors LS13 all-close rule). |
| PR-13 | minor | fix (deferred) | Shaping at 003/004 head: pin exact `--filter` values as 005 does. |
| PR-14 | info | informational | Passing baseline recorded; no action. |

## Coordinator OQ addendum (charter `:80` rule activated — OQs stay OPEN, no rulings)

- OQ1: 006 runs focused contract/entitlement/tier tests + `--check` (full backend suite NOT required);
  001 runs zero automated tests (manual boot verification per spec). Full-vs-focused for later parcels pinned at their head.
- OQ2: docker-compose boot REQUIRED for 001 (host runs supplementary only). 011's DB choice pinned at 011 head.
- OQ3: NO index work in any parcel until owner rules. Exit item 4's INDEX clause dormant.

## Gate 1 re-open record

None. Triage changed no locked decision (D1–D10 intact; OQ1–OQ3 remain OPEN).

## Verdicts adopted from review

- (e) collisions: rebase model holds (PR-04 fixed); 009/010 worktrees never concurrent with pending prior merges;
  011 sole test writer (wording fixed at head); 007-vs-011 no vicious circle, stop-and-reconcile stands.
- (f) inventory-first kept as falsifier only, never as content clearance.
- (g) OQ rule workable post-addendum (above).

(End of triage — 2026-09-19, coordinator)
