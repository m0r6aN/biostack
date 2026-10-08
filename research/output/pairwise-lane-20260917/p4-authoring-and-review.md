# BIO-PAIRWISE-005 — P4 authoring and review record (first sourced negative pair)

Builder: `bio_pairwise_005_builder`. Branch: `feat/bio-pairwise-005-sourced-negative-pair`.
Tested/base SHA: `b558ac843b169572280ae2ceb3e14466b178830b` (origin/main at worktree creation).
Session start (UTC): `2026-10-08T18:53:39Z`.

Delivery class: `knowledge-promotion` (D-E(2)). Sourcing split: D-E(1) — derivation from
already-authorized, already-cited lane sources only; no new external source acquisition performed
or required by this parcel.

Record produced: `research/input/relationships/pairwise-p4-first-negative.relationship.json`,
one relationship: `rel-tamoxifen-warfarin-001` (Tamoxifen × Warfarin / coumarin-type
anticoagulants, `contraindicated`, indication-specific).

---

## 1. Candidate pair selection (measured grounds)

Per the spec's Constraints: "a compound already promoted and live, whose interaction is stated in
an authorized source."

**Measured method used (reproducible):**

1. Enumerated the "already promoted and live" substrate two ways and required a candidate to
   appear in at least the richer, authoritative one:
   - `data/compounds.json` (16 served compounds) — a narrower, user-facing live set.
   - `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` (99 substance records) —
     filtered to `ops.isActive == true` (38 compounds, including `Tamoxifen`). Records with
     `ops.isActive == false` (e.g. `Pramlintide`, a Batch-C draft explicitly marked
     `"reviewStatus": "draft"`) were excluded as not "live" regardless of what their evidence
     packets contain.
2. For each live, active compound that also has a `research/input/evidence/*.evidence.json`
   packet already in the repository (the sourcing split's "already-cited lane source"), searched
   that packet's `claims[]` for any claim naming a **second, specific compound** (not a drug
   class or population) in a negative/safety relationship — i.e. an actual pairwise interaction,
   not a single-compound contraindication/warning.
3. Cross-checked candidates against P0's census
   (`research/output/pairwise-lane-20260917/p0-label-interaction-census.md`), which already
   established that **0/78** evidence packets have a product-label source with a literal
   `interaction`-substring hit in `extractedEvidence` — i.e. no packet surfaces a claim pre-tagged
   as an "interaction" claim type. This parcel's search was therefore not restricted to
   `claimType: interaction`; it inspected `contraindication` and `warning` claim text directly,
   since P1's bar governs `relationshipType`/`assertionClass` of the *relationship record this
   parcel produces*, not the claim-type taxonomy of the upstream evidence packet.

**Candidates inspected and rejected (single-compound only, no second named compound):**
Semaglutide, Tirzepatide, Testosterone cypionate, Vitamin D3, Tesamorelin, Melatonin — each has a
product-label (A1/A2) source in its evidence packet, but every contraindication/warning claim in
these packets names only a population or a disease state (e.g. "personal or family history of
MTC"), never a second compound. (Full per-packet review recorded by direct reading of
`research/input/evidence/{semaglutide,tirzepatide,testosterone-cypionate,vitamin-d3,tesamorelin,melatonin}.evidence.json`
during this session.)

**Candidate selected:** `Tamoxifen` (`backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`,
canonicalName `Tamoxifen`, `ops.isActive: true`). Its evidence packet
(`research/input/evidence/tamoxifen.evidence.json`) contains claim
`tamoxifen-contraindication-001`, which names a second, specific compound class
(coumarin-type anticoagulants, of which warfarin is the named, current-label member — see §3) in
a contraindication, sourced to an A1 DailyMed product label already cited in the packet before
this parcel began. This is the first and only candidate found that satisfies every clause of the
selection method above; per the spec's instruction, the first such pair found on measured grounds
is the one authored — no second candidate was pursued once this one qualified.

Note on `substances-seed.json`'s own `interactions[]` stub for Tamoxifen
(`tamoxifen-warfarin`, `target: "warfarin"`, `sources: ["src:tamoxifen"]`): this stub is
**not itself evidence** — `src:tamoxifen` is a placeholder, not a resolvable source — and was used
only as a selection-method cross-check (it independently pointed at the same pair this parcel's
evidence-packet search found). No content from the stub was copied into the relationship record;
every field in the record traces to the verbatim-anchored evidence-packet citation in §3.

---

## 2. Sourcing split compliance table

| Evidence item | Cited source | Sourcing-split side | Already cited pre-parcel? |
|---|---|---|---|
| Contraindication claim text, scope (risk-reduction indications only), and verbatim quote fragment | `dailymed-tamoxifen-citrate-label` (DailyMed SPL, Andrx Pharmaceuticals, setid `8f642753-9e12-433c-a0bc-ab33dac41ddf`, 2007-04-20), `research/input/evidence/tamoxifen.evidence.json:29-39` (source snapshot), `:203-234` (claim) | **Permitted — derivation** from an already-authorized, already-cited lane source | Yes — present at base SHA `b558ac843b169572280ae2ceb3e14466b178830b`, last touched by commit `b35b2b4` (pre-dates this branch) |
| Source registry membership check | `research/input/sources/pilot-source-registry.json:482` lists `dailymed-tamoxifen-citrate-label` in the authorized registry | Confirms authorized-lane status | Yes — pre-existing registry entry |
| "Coumarin-type anticoagulant" vs. "warfarin" naming note | `research/input/evidence/tamoxifen.evidence.json:205` reviewFlag (2026-09-07 correction note) | Disclosed, **not used as evidence** (unverified in this packet) | N/A — explicitly excluded from this record's `sourceRefs` |
| `substances-seed.json` Tamoxifen interaction stub (selection cross-check only) | `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`, Tamoxifen record, `interactions[0]` | Not evidence; used only to independently corroborate the selection, not copied into the record | Yes — pre-existing |

**No new external source acquisition occurred or was required.** The second-source citation in
the evidence packet (`dailymed-soltamox-oral-solution-label`) was deliberately **excluded** from
this relationship record's `sourceRefs`, because its `extractedEvidence` entries carry
`"quote": null` — not yet verbatim-anchored in the cited packet — and citing it here would smuggle
an unverified claim into a "derivation" step. This exclusion is itself required by the sourcing
split's verbatim-citation discipline, not a gap this parcel needed to stop and report: the single
remaining source (`dailymed-tamoxifen-citrate-label`) is independently sufficient (A1 tier,
verbatim quote present) to satisfy P1's sourcing bar and the spec's Required Tests.

---

## 3. Page-image / verbatim verification record (T5)

| Field | Value |
|---|---|
| Source file | `research/input/evidence/tamoxifen.evidence.json` |
| Source id | `dailymed-tamoxifen-citrate-label` |
| Page/section locator | "Contraindications" section, DailyMed SPL setid `8f642753-9e12-433c-a0bc-ab33dac41ddf` (Andrx tamoxifen citrate tablet label, displayed update 2007-04-20) |
| Verbatim text used in this record | `"require concomitant coumarin-type anticoagulant therapy"` |
| File:line of that verbatim fragment | `research/input/evidence/tamoxifen.evidence.json:229-231` |
| Verbatim subheading fragment establishing indication scope | `"Reduction in Breast Cancer Incidence in High Risk Women and Women with DCIS"` — `research/input/evidence/tamoxifen.evidence.json:224-226` |
| Verification date recorded in the cited packet | 2026-09-05 (independent review pass that re-fetched and recorded these two fragments as verbatim, distinct from the 2026-08-29 extraction pass's compressed paraphrase at `:219-223`, which is explicitly marked "NOT verbatim label text" and is **not** used as evidence in this record) |
| This parcel's verification action | Read the cited fragments directly from `research/input/evidence/tamoxifen.evidence.json` at the line numbers above (`Read` tool, this session) and copied them into the relationship record's `reviewFlags`/`context` unmodified. This parcel has no live-fetch/browser tool available and did not attempt to re-fetch the DailyMed page; per the sourcing split, re-fetching an already-cited source to re-verify it would still not be "new acquisition," but it was not necessary here because the cited packet already carries a dated, independently-recorded verbatim fragment (not a fetch-extraction-layer quote) — the exact assurance T5 requires. |
| Source authority tier | `A1` (`research/input/evidence/tamoxifen.evidence.json:31`) |

The claim text explicitly distinguishes the verbatim-anchored class term ("coumarin-type
anticoagulant therapy") from the unverified, not-yet-fetched single-drug term ("warfarin") that a
later label revision reportedly uses (`:205`, `:234` reviewFlag) — that later text was never
retrieved by the cited packet and is excluded from this record's evidentiary basis. The relationship
record's `objectCompound` field names `"Warfarin (coumarin-type anticoagulants)"` for legibility
(warfarin is the class's prototypical, most commonly cited member, consistent with the
`substances-seed.json` interaction stub), but every `reviewFlags` entry and the `context` field
make explicit that only the class term is verbatim-anchored by this parcel's cited evidence —
this is the "paraphrase, exact page locators" discipline the source-rights doctrine requires where
a single-drug name is not itself proven in the cited text.

---

## 4. Evidence grade and basis

- `evidenceTier`: `Strong` — matches the cited evidence packet's own `claims[].evidenceTier` for
  `tamoxifen-contraindication-001` (`research/input/evidence/tamoxifen.evidence.json:210`).
- `assertionClass`: `authoritative-caution` — a contraindication issued by an FDA-regulated
  product label (DailyMed SPL), per publication-contract §2a.
- `sourceRefs[0]` (`dailymed-tamoxifen-citrate-label`) `authorityTier`: `A1`, satisfying P1's bar
  clause 5 and this parcel's T2.
- Negative-safety framing is evidence-graded and population/context-qualified, never universal:
  the record's `context.population`, `effectDomain`, and three separate `reviewFlags` entries all
  state that the contraindication applies **only** to tamoxifen's risk-reduction indications
  (DCIS / high-risk breast cancer prevention), not to tamoxifen use generally — this qualifier is
  copied directly from the cited label text itself (`:205`), not invented by this parcel.

---

## 5. Review lifecycle (T6, T8, T9) — status: NOT YET SATISFIED, by design

The record's `relationshipReviewStatus` is set to `"review-required"`, and `ops.needsReview` is
`true`. **No `research/review-decisions/*.json` file was created by this parcel.**

This is deliberate, per the mandate's promotion-lifecycle instruction ("you mark
`needsReview`/pending promotion, NEVER self-approve") and the spec's own author/reviewer
separation requirement ("the author does not review their own record... reviewer 2 is a different
person from reviewer 1"). The Gate 2 dispatch record
(`docs/INITIATIVES/GATE2-BIO-PAIRWISE-005.md`) names two distinct, separately dispatched reviewer
identities — `bio_pairwise_005_review_1` and `bio_pairwise_005_review_2` — neither of which is
this builder. Writing review-decision files under those identities, or under any identity, from
inside the builder parcel would fabricate the independent attestations T8/T9 exist to require and
would be exactly the self-attestation T9 is designed to catch. The builder's correct action is to
leave the record at `review-required`/`needsReview: true` and stop there.

Consequently, as of this parcel's closure:

- **T6 (review-status gate):** not yet applicable — `relationshipReviewStatus` is `review-required`,
  not `accepted-as-evidence-backed`, and no review-decision files exist yet for this relationship.
  Verified: `relationship-packet.schema.json`'s `relationshipReviewStatus` enum includes
  `review-required` as a legal value, and the record's field is set to that value, not
  self-promoted to `accepted-as-evidence-backed`.
- **T8 (dual review recorded):** not yet satisfied — zero review-decision entries exist for this
  relationship. This is expected at this stage of the lifecycle; it is the next, separate step
  (`bio_pairwise_005_review_1`, `bio_pairwise_005_review_2`), not part of this parcel.
- **T9 (anti-self-attestation cross-check):** trivially holds in the only sense available to this
  parcel — there is nothing to cross-check yet, and specifically there is no reviewer identity
  equal to the builder identity recorded anywhere, because no review-decision file exists. The
  cross-check becomes meaningfully executable once `bio_pairwise_005_review_1` and
  `bio_pairwise_005_review_2` file their decisions; at that point a validator (or a human) must
  confirm: (a) exactly two `research/review-decisions/*.json` entries reference this
  relationship, (b) both decide the equivalent of `accepted-as-evidence-backed`, (c) their
  `reviewerId`s are distinct from each other, and (d) neither `reviewerId` equals
  `bio_pairwise_005_builder` (this record's author identity, per `packet.agentId`).

This gap is **not** a STOP-AND-REPORT condition under this parcel's mandate — it is the expected,
designed boundary between the builder step and the dual-review step the Gate 2 record dispatches
separately. The PR this parcel opens states this plainly.

---

## 6. Promotion authority (owner-only)

No live Refresh was run. No promotion occurred or was attempted. §T7 below demonstrates, via the
real (unmodified) `CompoundGraphBuilder` / `CompoundGraphPersistenceMapper` projection path, that
the record **does not currently project** to the knowledge graph (gated correctly on
`relationshipReviewStatus`), and separately previews — in-memory only, nothing written to disk or
to any P2 source file — what the projection would look like if a future, separate dual-review step
sets `relationshipReviewStatus: accepted-as-evidence-backed`. That preview is not a promotion and
does not substitute for the owner's later promotion sign-off, which remains a separate, later,
human act outside this parcel's closure evidence per the knowledge-promotion delivery class.

---

## 7. Rollback path

If the record is rejected in review, fails schema/sourcing verification, or a future dry-run
surfaces unexpected blast radius: delete
`research/input/relationships/pairwise-p4-first-negative.relationship.json` and this file
(`research/output/pairwise-lane-20260917/p4-authoring-and-review.md`). No other file is touched by
this parcel (verified: `git status` / `git diff --stat` against this branch's base shows changes
confined to exactly these two paths — see §8, T7). Because no live Refresh ever ran, rollback
never touches the live knowledge projection.

---

## 8. Required Tests — results

| Test | Result | Evidence |
|---|---|---|
| T1 — schema validation | **PASS** | `jsonschema` (Draft 2020-12) validated `research/input/relationships/pairwise-p4-first-negative.relationship.json` against `backend/src/BioStack.KnowledgeWorker/Schemas/relationship-packet.schema.json`: zero errors. |
| T2 — source authority (A1/A2) | **PASS** | `sources[0].authorityTier == "A1"` in the record; matches `research/input/evidence/tamoxifen.evidence.json:31`. |
| T3 — evidence tier not Unknown | **PASS** | `relationships[0].evidenceTier == "Strong"`. |
| T4 — sourced-pair provenance (derivation vs. acquisition) | **PASS** | `research/input/evidence/tamoxifen.evidence.json` is present at base SHA `b558ac843b169572280ae2ceb3e14466b178830b` (last touched by commit `b35b2b4`, pre-dating this branch) — see §2. |
| T5 — page-image verification | **PASS** | See §3: source file, page/section locator, verification date (2026-09-05, recorded in the cited packet), and verified text all recorded; the verbatim-anchored fragment is distinguished from the packet's own non-verbatim paraphrase. |
| T6 — review-status gate | **NOT YET APPLICABLE (by design)** | See §5. `relationshipReviewStatus: review-required`; no premature `accepted-as-evidence-backed`. |
| T7 — dry-run projection without P2 modification | **PASS** | Ran the actual record file through the real `CompoundGraphBuilder`/`CompoundGraphPersistenceMapper` (via a temporary, reverted-before-commit `[Fact]` in `RelationshipProjectionTests.cs`, executed with `dotnet test --filter TEMP_BioPairwise005_DryRunReceipt`): as-authored, **0** relationships project (correctly gated on `review-required`); an in-memory-only preview with `relationshipReviewStatus` flipped to `accepted-as-evidence-backed` projects **1** relationship, `tamoxifen -> warfarin-coumarin-type-anticoagulants (avoid_with)`, confined to that single pair. The temporary test method was fully reverted (`git checkout`) before this parcel's commit; `git diff --stat HEAD -- backend/ frontend/ data/` shows **0** changed lines — zero P2 modification. |
| T8 — dual review recorded | **NOT YET SATISFIED (by design)** | See §5 — next step, outside this parcel. |
| T9 — review-status cross-check (anti-self-attestation) | **NOT YET EXECUTABLE (by design)** | See §5 — becomes executable once T8's two decisions exist. |

---

## 9. What remains unproven (AC7)

One sourced negative relationship record, author-built and schema-valid, is not coverage, and is
not yet promotion-eligible:

- This proves the chain *can* carry one genuine, derivation-only, verbatim-cited negative pair
  from an already-authorized evidence packet through to a schema-valid relationship record and a
  correctly-gated (currently zero-blast-radius) projection preview.
- It does **not** prove dual independent review will accept the record, that the owner will
  promote it, or that any other compound pair in the 78-packet corpus has comparably strong,
  verbatim-anchored, compound-specific interaction evidence — the P0 census and this parcel's own
  candidate search both found this to be a narrow, not a general, pattern across the corpus.
- The "warfarin" naming in `objectCompound` is a legibility choice over a verbatim-anchored class
  term ("coumarin-type anticoagulants"); reviewers should treat that naming choice, not just the
  underlying contraindication, as part of what they are reviewing.
