# P0-A — Product Doctrine Recovery: Canon Precedence and Contradiction Inventory Parcel Spec

Status: **REVIEW CANDIDATE — builder dispatch blocked**

Parcel ID: `P0-A`

Risk and routing: `health-boundary`, `privacy`, `legal-policy`, `knowledge-promotion`
(self-declared delivery classes, folded against `delivery-class-controls.json`); architecture
(charter parcel-tree designation, P0 row); one builder; **two independent read-only adversarial
reviewers** (D14 max-scalar fold over four classes each requiring 2 reviewers resolves to 2;
charter D8 also requires dual review for architecture/risk-sensitive parcels — the same
combination already carried by P2 and P3-A).

This parcel is **analytical only**. It decides canon precedence and catalogues canon
contradictions. It does not decide, author, or imply any product allowed-output, any
guidance-class/substance-function-risk applicability criterion, or any allowed/degraded/refused/
escalated behavior. Those decisions are P0-B's, under its own, separate, not-yet-granted owner
gate (Coordinator Decisions D-G, below).

## Lineage and dependencies

- Governed-delivery charter SHA-256:
  `4CD390D631487DA8E97509205A186F242C4A83B762FFFA894BEEC8A21F4EFE22` (unchanged since P1, P2, and
  P3-A shaping; re-verified at this shaping time).
- Owner-authorization ledger (`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`) is an
  **append-only, living log, not a frozen artifact** — a single whole-file hash is not a durable
  authorization anchor for it (the prior spec hash pinned to `EE506C3B54FB57ABD09BBB5180CD704AABCA3EB73875B7E760EF84B538863D47`
  was correct at its own shaping anchor but is stale today, because the ledger has since grown by
  two further owner-dated entries). This rework instead anchors on the entry's own text span plus
  two dated whole-file hashes, so growth is visible rather than silently invalidating:
  - **Shaping-time whole-file hash** (`main@32280aa2219100e20520a275d19af2fbdd1f32be`, verified by
    `git show 32280aa2219100e20520a275d19af2fbdd1f32be:docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md | sha256sum`):
    `EE506C3B54FB57ABD09BBB5180CD704AABCA3EB73875B7E760EF84B538863D47`.
  - **Current whole-file hash** (this rework's shaping anchor, verified by direct
    `sha256sum docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`):
    `908FAA5999526DE76CC7E21B6753D41BC8BDB9F29C5F1717A504D9103E4CA7AE`. The file grew between the
    two anchors by two further owner-dated entries: **D-H** ("Owner directive: Gate 3 posture +
    P0-B design gate opened," `## D-H` heading, lines 125-150) and **D-I** ("P0-B design gate
    RULED," `## D-I` heading, lines 152-169) — both now load-bearing for this parcel (see
    "Required source list" items 12-13 and the D-I cross-reference on CI-001 in "Seed findings,"
    below).
  - **D-G entry text anchor** — the section headed `## D-G — P0-A authorization (owner,
    2026-10-08: "P0-A authorized")` (lines 114-123 at both anchors above; the entry's own text has
    not changed between them, only later entries were appended after it). The actual proof of the
    D-G quotation below is the SHA-256 of exactly that heading-to-next-heading span —
    `sed -n '114,123p' docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md | sha256sum` —
    which is `3D46AC19AEF9A0F553239B6EC984E90216286C72918FFB2AA04EFA81B90BC4F9` at both anchors. A
    reviewer re-verifies this span-hash directly, independent of how much the whole file has grown
    since, which is why it — not the whole-file hash — is this parcel's sole standing-authorization
    anchor. D-G's text, quoted here verbatim because it is this parcel's sole standing-authorization
    source (the charter itself grants P0 no standing authorization):

  > D-G — P0-A authorization (owner, 2026-10-08: "P0-A authorized")
  >
  > The owner explicitly authorizes P0-A (Product Doctrine Recovery: canon precedence and
  > contradiction inventory) ONLY — the charter grants no standing authorization for P0, so this
  > is the required explicit human gate for P0-A's shaping → review → dispatch chain. Scope bound
  > to the analytical inventory (read-only against product behavior). P0-B remains UNAUTHORIZED and
  > returns to the owner for a fresh gate with its design presented (it decides product
  > allowed-outputs). P0-C/P0-D likewise await their own gates. Class controls for P0-A in full:
  > health-boundary + privacy + legal-policy + knowledge-promotion sections, dual review, and the
  > human-approval conditions those classes trigger at merge.

  D-H and D-I do not change or withdraw D-G's grant (P0-A's authorization stands unmodified); they
  record two further, later owner rulings this parcel's Required source list and seed findings
  must account for (D-H opens the P0-B design gate; D-I rules on it, resolving in substance the
  same conflict CI-001 catalogues — see below).
- Closed plan review: `docs/INITIATIVES/biostack-governed-delivery/PLAN-REVIEW.md`, SHA-256
  `FBE40053DAE9508AC498B1331D2AED565A4BB68025CA24E07E0DC7BB69E8E256`.
- Closed P1 spec: `docs/INITIATIVES/biostack-governed-delivery/parcels/P1.md` — closure status
  `DONE` (`docs/INITIATIVES/biostack-governed-delivery/closures/P1.md`).
- P2 spec (`docs/INITIATIVES/biostack-governed-delivery/parcels/P2.md`) is `done`/closed:
  `docs/specs/INDEX.md`'s current P2 row reads `done`, and its closure record
  (`docs/INITIATIVES/biostack-governed-delivery/closures/P2.md`, 2026-10-08) records a completed
  dual-review PASS chain and Gate 3 merge (PR #505). P3-A spec
  (`docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md`) remains `REVIEW CANDIDATE` per
  its own file header and has **no row at all** in `docs/specs/INDEX.md` — absent, not
  `review-candidate` there (`grep -n "P3-A" docs/specs/INDEX.md` returns no match, at the prior
  shaping anchor or today). The charter's dependency spine reads
  `... -> P1 -> P2 -> P3-A -> P0-A -> ...`. **D-G authorizes this parcel's shaping → review →
  dispatch chain on its own terms; it does not waive the charter's dependency spine.** This spec
  may therefore be shaped and reviewed now (exactly as D-G scopes), but its **dispatch** (Gate 2)
  additionally requires P3-A to reach merged, closed status (P2's own precondition is already
  satisfied), because P0-A's own deterministic verification posture (see "Deterministic
  verification" below) depends on P2's frozen
  `classification-axes.schema.json`/`delivery-class-controls.json` content remaining stable at the
  version this spec cites, and because P3-A's no-`TBD`/placeholder rule and generic parcel-spec
  shape are the structural contract this spec already follows by precedent. This is recorded as an
  explicit dispatch precondition, not silently assumed; the coordinator stops and does not dispatch
  P0-A until P3-A closes.
- Original shaping base anchor: `main@32280aa2219100e20520a275d19af2fbdd1f32be` (this parcel's
  first-shaped hash, `50C22F74569C2453952B56E372E40BFC159162B0DE56C583C1FDC6CA93E0BC10`, was
  reviewed against this anchor and rejected by both dual-review findings above).
- Reconciled shaping base anchor for this rework: `main@a25a658eee2b1cf6b58dc59a2b28f5876312264a`
  (`git merge-base --is-ancestor 32280aa2219100e20520a275d19af2fbdd1f32be HEAD` confirms the
  original anchor is an ancestor of this one). This later anchor is the commit every hash,
  citation, and lineage claim in this rework (D-H/D-I, the current ledger hash, P2's `done`
  closure, `P0-B-DESIGN-GATE.md`'s existence) was re-verified against. Neither anchor is
  `BaseCommit` (see "BaseCommit (defined term)" below) — both are only the commits this spec's own
  text was shaped and re-verified against, across its two review cycles.

## Authorization boundary (restated, because this is the load-bearing fact of this spec)

- **Authorized now:** P0-A shaping, dual independent review, and (after P3-A closes — P2 has
  already closed — and triage closes) dispatch of exactly the analytical inventory described
  below.
- **Not authorized by this spec or by D-G:** P0-B (Product Capability and Safety Contract,
  including per-label applicability and allowed/degraded/refused/escalated product behavior),
  P0-C (policy fixtures), and every P0-D reconciliation subparcel. Each returns to the owner for
  its own fresh gate, per D-G's explicit text above.
- **Merge is the owner's decision.** A green Gate 2 dispatch, green deterministic checks, and two
  PASS reviews make this parcel mergeable; they do not make it merged. Gate 3 (merge) is reserved
  to the owner, consistent with D-G's "human-approval conditions those classes trigger at merge."

## BaseCommit (defined term)

`BaseCommit` is the single commit SHA this parcel's builder pins at **Gate 2 dispatch** — it is
**not** either shaping anchor named above (`main@32280aa2219100e20520a275d19af2fbdd1f32be`,
superseded, or `main@a25a658eee2b1cf6b58dc59a2b28f5876312264a`, this rework's own anchor), because
both predate P3-A's close and therefore cannot themselves be the commit real-corpus citations are
verified against at dispatch time. The coordinator's Gate 2 dispatch record (made only once the
"P3-A must reach merged, closed status first" precondition above clears) names the exact
`BaseCommit` SHA. Every clause in this spec that reads "at `BaseCommit`" — "Real-corpus grounding"
(Hard constraints), the `canonical-write-fencing-violation`/`unresolvable-citation` check, the
Health-boundary "Missingness" rule, and `unreviewable-claim-verified` (Deterministic verification)
— binds to that one pinned commit for the whole of this parcel's dispatch: the builder does not
re-pin it mid-dispatch, and a citation verified against any other commit (including either shaping
anchor above) does not satisfy these checks. This mirrors how P2 and P3-A each bind a single
`BaseCommit` dispatch anchor in their own Gate 2 builder handoff requirements.

## Objective

Produce two artifacts that let every later P0-D reconciliation subparcel resolve any doctrine
conflict deterministically, without re-litigating authority order each time:

1. **Canon precedence manifest** — a single, explicit, **total order** over every document this
   parcel recognizes as BioStack product-doctrine canon, plus the comparison rule that makes any
   two canon documents' relative authority answerable by lookup, not argument. "Total" means: for
   any two documents both present in the manifest's registry, the manifest yields exactly one of
   `A outranks B`, `B outranks A`, or (only for two documents sharing one named tier) the tier's
   documented tie-break rule, deterministically and without exception.
2. **Contradiction inventory** — an exhaustive, citation-backed catalogue of every inconsistency
   this parcel's required source list (below) contains, each row naming its exact sources (file +
   section/heading or line range + quoted text), the conflict's class-axis tags (from P2's closed
   vocabularies), and a proposed disposition (`fix`, `accept-as-documented`, `informational`, or
   `resolved-by-owner-ruling`) for a later P0-D subparcel to execute (or, for
   `resolved-by-owner-ruling` rows, to encode an already-ruled resolution into the canon of
   record). P0-A proposes dispositions; it never executes one, and a proposed disposition is not
   itself a reconciliation. "Exhaustive" is a best-effort, reviewer-checked claim, not a
   verifier-provable one — no script can prove the negative "no contradiction was missed"; the two
   independent reviewers are the actual backstop against silent narrowing. To make *coverage* (as
   distinct from exhaustiveness of *findings*) checkable rather than merely asserted, the builder
   also produces a **corpus-coverage matrix** (deliverable 2, required document contract 4, below)
   recording, for every pair of required sources, whether they were explicitly compared and with
   what outcome; `corpus-coverage-matrix-complete` (Deterministic verification) fails if any
   required-source pair has no recorded comparison outcome at all.

P0-A is read-only against product behavior: it changes no runtime code, no product contract, no
guidance-content-contract class, no canon document, and no existing spec. It produces new,
additive analysis artifacts only.

## Required source list (the corpus this parcel's Source Manifest must cover, at minimum)

The builder's Source Manifest (deliverable 3, below) must catalogue at least every one of these
sources as `consistent`, `contradictory`, or `unreviewable` (with citation); it may add sources the
builder discovers are load-bearing canon, but may not omit any of these without recording
`unreviewable` and the exact reason (for example: file deleted, file unreadable, file outside this
worktree's checkout).

1. `docs/INITIATIVES/biostack-governed-delivery/CHARTER.md` — the ratified product doctrine
   (`may`/`must not` lists, the four product guidance classes, the closed substance/function-risk
   vocabulary, D1-D18). Its own text carries no internal, dated ratification event (the lineage
   phrase "the developer's product-purpose correction and explicit `100% Ratified` decision"
   names no date in the document itself); its precedence rank is determined by the dated-evidence
   rule in "Precedence manifest" below, not assumed here.
2. `docs/specs/INDEX.md` and `docs/specs/README.md` (the governed spec lifecycle) — procedural
   canon on how specs are ranked, registered, and closed; narrower in scope than product doctrine
   but itself a source of possible procedural contradiction (for example a spec's declared status
   versus its actual registry row).
3. `README.md` (repository root) — the primary product-facing and investor/partner-facing
   description of what BioStack is, its tiers, its "Safety and compliance boundary," and its
   "Not Medical Advice" paragraph.
4. `docs/product/knowledge-engine-capability-map.md` — capability-to-data-source mapping with its
   own stated authorization/prohibition list.
5. `docs/product/knowledge-engine-model-data-roadmap.md` — roadmap-level product-capability
   narrative.
6. `docs/product/product-ids.md` — product identifier canon.
7. `BIOSTACK_FRONTEND_READINESS_AUDIT.md` — the frontend audit's claim statements (readiness
   verdicts, "Ready for provider acquisition," resolved/open findings) as a distinct claim-bearing
   artifact, not itself doctrine, but a place doctrine-adjacent claims are made publicly
   referenceable.
8. `docs/canon/biostack-protocol-intelligence-canon.md` — "Status: Canonical product foundation,"
   dated 2026-06-18, its "Observational-Only Boundary" `may`/`must not` lists.
9. `docs/guidance/biostack-guidance-content-contract.v1.md` (Status "Fully ratified," version
   1.0.0, effective 2026-08-02) and its ratification record `docs/guidance/RATIFICATION.md` — the
   Class A/B/C/D output-class taxonomy, including Class D's "Prohibited" status.
10. `docs/specs/active/BIO-PAIRWISE-001-relationship-substrate-and-label-census.md` through
    `BIO-PAIRWISE-006-studied-combinations-surface.md` — the pairwise negative-relationship lane's
    harm/interaction framing (absence-of-label-is-not-absence-of-interaction doctrine, relationship
    family vocabulary).
11. `docs/INITIATIVES/biostack-local-readiness/FINAL-HANDOFF.md` — the claim-discipline precedent
    ("No public, revenue, deployment, or privacy claim below exceeds the evidence") this parcel's
    own claim-cataloguing method should be measured against.
12. `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` — the owner-authorization ledger (see
    "Lineage and dependencies" above for its anchor and the D-G/D-H/D-I entries). It now carries
    **D-G** (this parcel's own authorization), **D-H** (P0-B design gate opened), and **D-I**
    (P0-B design gate RULED — D-B1(c) staged split, which resolves CI-001's headline conflict in
    substance; see "Seed findings," CI-001, below).
13. `docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` — the decision package D-H
    opened and D-I ruled on. Its §2 ("The central conflict this gate must resolve") states, in the
    same terms CI-001 catalogues, the guidance-content-contract v1.0.0 Class D prohibition versus
    charter D13's "may," and its D-B1 table records the owner's staged-split resolution this
    parcel's seed findings cross-reference.

Sources 1-2 are governance/procedural canon; 3-9 are product-doctrine canon; 10-11 are
narrower-scope precedent the inventory uses to test whether product-doctrine contradictions have
already propagated into parcel-level specs and closure records; 12-13 are owner-decision/
design-gate records — procedural rulings about other canon, not canon ratification events
themselves (see "Precedence manifest," classification guidance for borderline document types,
below) — read and cross-referenced by the contradiction inventory but not entered into the
precedence registry.

## Precedence manifest (deliverable 1)

### Design

The manifest is a **total order**, not a partial preference list, because P0-D's later
reconciliation subparcels need a lookup, not a renewed argument, every time two canon statements
disagree. The order is derived from three objective, checkable properties of each source, applied
in this exact priority so the result is deterministic rather than a subjective ranking:

1. **Ratification formality** — an explicit, dated, named-owner ratification event (a "Status:
   Fully ratified," "100% Ratified," or equivalent recorded sign-off naming who ratified it and
   when) outranks a document with no recorded ratification event.
2. **Recency of ratification** — among documents with a ratification event, the later-dated event
   outranks the earlier one. This rule is applied against the registry's actual dated evidence, not
   pre-judged here: `CHARTER.md`'s own text carries no internal, dated ratification event (only the
   undated lineage phrase "the developer's product-purpose correction and explicit `100%
   Ratified` decision"; `git log -1 --format=%ad -- CHARTER.md` shows its last file touch, not a
   ratification date, and is not itself a ratification-event citation), while
   `docs/guidance/biostack-guidance-content-contract.v1.md` carries an explicit, dated, named-owner
   event ("Fully ratified | 2026-08-02 (Clint Morgan — all gates passed; no remaining
   blockers)"). This spec does not assert which of the two therefore ranks first under rule 2 — the
   builder applies rule 2 to the registry's actual dated evidence and records, in
   `canon-precedence.md`'s table, the specific dated ratification record each document's rank rests
   on. A rank assigned under this rule without naming the dated event it is based on fails
   `precedence-manifest-totality` (below); an undated document is ranked only under rule 1 (no
   recorded event) or rule 3 (scope breadth), never rule 2.
3. **Scope breadth** — among documents tied on both (1) and (2) (for example, two sources with no
   recorded ratification event at all), the document whose stated scope is product-wide outranks
   one whose stated scope is a single feature, pipeline, or parcel.

**Classification guidance for borderline document types** (so two builders converge on the same
registry inputs, not just the same comparison function): a **Coordinator Decisions ledger entry**
(for example `COORDINATOR-DECISIONS-2026-10-07.md`'s D-G/D-H/D-I) is a **procedural authorization
or ruling record**, not itself a product-doctrine ratification event under property (1) — it
records who authorized or ruled on something and when, but none of D-G/D-H/D-I states a "Status:
Fully ratified" or equivalent doctrine sign-off for itself. It is classified `no ratification
event` for precedence purposes, the same as an undated document, and is **not** entered into the
precedence registry at all — its content is read and cross-referenced by the contradiction
inventory instead (see Required source list items 12-13 and CI-001's cross-reference, below),
because it names which existing registry document's position the owner has ruled on, not because
the ruling record itself outranks or is ranked against a canon document. A **design-gate decision
package** (for example `P0-B-DESIGN-GATE.md`) is classified the same way: a decision-presentation
and ruling record, excluded from the registry, read and cross-referenced by the contradiction
inventory. Any other borderline document the builder's corpus read turns up is classified by the
same test: does the document's **own text** state a dated, named-owner ratification/sign-off event
for itself (property 1 applies, entered in the registry), or does it only record a decision,
authorization, or ruling **about** other documents (classified `no ratification event`, excluded
from the registry, and instead cross-referenced from the contradiction inventory, exactly as
`COORDINATOR-DECISIONS-2026-10-07.md` and `P0-B-DESIGN-GATE.md` are here)?

Any two documents tied on all three properties are **not** resolved by this parcel; P0-A records
them as a tied pair requiring an explicit owner or P0-D1 ruling (mirroring the D-D/D-C
owner-ruling precedent for unresolvable corpus questions) rather than inventing a fourth,
unprincipled tie-break criterion.

### Required document contract: `docs/specs/schemas/canon-precedence.md`

A single Markdown document containing, in this exact order:

1. **The three-property ranking rule above**, stated normatively (not just referenced), so the
   document is self-contained and does not require re-reading this spec to apply.
2. **A ranked registry table** with columns `Rank | Document | Ratification event (date, named
   owner, or "none recorded") | Stated scope | Precedence note`, covering every document in the
   required source list plus every additional canon document the builder's corpus read turns up
   (`docs/guidance/RATIFICATION.md`, `docs/guidance/GOVERNANCE-ENFORCEMENT-FINDINGS.md`,
   `docs/legal/*` if present and readable, etc.) **except** required source list items 12-13
   (`COORDINATOR-DECISIONS-2026-10-07.md`, `P0-B-DESIGN-GATE.md`), which the "Classification
   guidance for borderline document types" paragraph above excludes from the registry as
   procedural ruling records, not ratification events. Rank is a dense integer (no gaps); two
   documents may share a rank **only** when the tie-break rule above has been applied and the tie
   is recorded as open per the paragraph above — a shared rank is never silent.
3. **A comparison function definition**: given any two documents both present in the registry,
   state the exact lookup (`compare(A, B) -> {A-outranks-B | B-outranks-A | open-tie, ruling-ref}`)
   and prove by construction that it is total over the registry (every registered pair resolves to
   exactly one of those three outcomes — `open-tie` is a valid, named outcome, not a failure to
   answer).
4. **An explicit scope boundary statement**: the manifest ranks *documents*, not individual
   sentences; where a single document contains both higher-authority and lower-authority content
   (for example, a ratified product-doctrine section and an unratified aside in the same file),
   the contradiction inventory (not the precedence manifest) is the mechanism that surfaces the
   internal inconsistency, and the manifest's document-level rank applies only once the internal
   inconsistency is itself resolved (a P0-D concern, not this parcel's).
5. **An extension rule**: how a future document not yet in the registry is placed (apply the same
   three-property test; append at the computed rank; never renumber existing ranks except through
   an explicit, logged amendment).

## Contradiction inventory (deliverable 2)

### Row schema (required document contract: every inventory row, wherever recorded, has exactly
these fields)

| Field | Content rule |
|---|---|
| `id` | `CI-NNN`, dense, zero gaps, assigned in discovery order |
| `topic` | one-line human-readable subject |
| `source_a` | file path + section heading or line range + exact quoted text |
| `source_b` | file path + section heading or line range + exact quoted text |
| `conflict` | one paragraph naming exactly what `source_a` permits/asserts that `source_b` forbids/denies, or vice versa — no paraphrase substitutes for the quotations in `source_a`/`source_b` |
| `delivery_class_tags` | zero or more labels from `classification-axes.schema.json`'s `deliveryClass.labels` closed vocabulary, or `not-applicable` |
| `guidance_class_tags` | zero or more labels from `productGuidanceClass.labels`, or `not-applicable` |
| `substance_function_risk_tags` | zero or more labels from `substanceFunctionRisk.labels`, or `not-applicable` |
| `status` | exactly one of `contradictory`, `consistent` (recorded as a non-conflict row so the corpus-coverage count is auditable — see Acceptance criteria), or `unreviewable` |
| `proposed_disposition` | exactly one of `fix`, `accept-as-documented`, `informational`, or `resolved-by-owner-ruling` (an owner ruling already on record resolves this conflict in substance; the row cites the ruling and does not re-propose a disposition a later P0-D subparcel would decide) — required for every `contradictory` row; `not-applicable` for `consistent`/`unreviewable` rows |
| `handoff_target` | which future P0-D subparcel (`P0-D1` core doctrine/ADR, `P0-D2` evidence methodology/guardrails, `P0-D3` product contract/README/marketing/provider copy, `P0-D4` enforcement/regression tests) would execute the disposition, or `not-applicable` |

`status: contradictory` requires `proposed_disposition != not-applicable`. A row violating this
pairing fails validation (see Deterministic verification).

### Required document contracts

1. `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/README.md` — the method:
   the row schema above, the disposition vocabulary's exact meaning (`fix` = a later P0-D subparcel
   should change a canon document's text; `accept-as-documented` = the apparent tension is
   intentional/acceptable and should be cross-referenced rather than removed; `informational` = not
   a true conflict, recorded for completeness; `resolved-by-owner-ruling` = an owner ruling already
   on record — for example a Coordinator Decisions ledger entry or a ruled design-gate decision
   package — resolves the conflict in substance, so no P0-D execution is proposed, only a citation
   to the ruling that already governs it), the corpus-coverage method (how the builder proves every
   required source was actually read, not sampled), and the explicit statement that this parcel
   proposes dispositions and never executes one.
2. `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/SOURCE-MANIFEST.md` — one
   row per required source (and any additional sources the builder's read turns up), each marked
   `consistent` (fully read; no contradiction found against any other catalogued source),
   `contradictory` (fully read; at least one `CI-NNN` row cites it), or `unreviewable` (not read,
   with the exact reason), plus the date/commit the source was read at.
3. `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CONTRADICTION-INVENTORY.md`
   — every `CI-NNN` row, in a single table or one table per topic cluster, using the row schema
   above exactly.
4. `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CORPUS-COVERAGE-MATRIX.md`
   — one row per **combinatorial pair** of required sources (`n choose 2` over the Required
   source list items read into the corpus, items 12-13 included since they are read even though
   excluded from the precedence registry), each marked `compared-contradictory` (cites the
   `CI-NNN` row the pair produced), `compared-consistent` (cites the `CI-NNN` row, or the
   `SOURCE-MANIFEST.md` entry, the pair's comparison was recorded under), or `scope-disjoint` (a
   one-sentence reason the two sources could not plausibly conflict — for example, disjoint
   subject matter). No pair may be silently absent; `corpus-coverage-matrix-complete` (below)
   fails closed on any missing pair.

### Seed findings (shaping-time evidence — mandatory floor, not the full inventory)

These rows were identified during this spec's corpus read. They are evidence that the method
above is workable and non-trivial; the builder's inventory **must** carry every one of them
forward (verified, not merely copied) and extend coverage to the rest of the required source list.
CI-001 additionally demonstrates the `resolved-by-owner-ruling` disposition (see the paragraph
following the table) — reserved for a conflict an existing owner ruling already addresses in
substance, distinct from `fix` (still open, awaiting P0-D execution). Removing, softening, or
silently merging any of these rows without a named reason is a verifier
failure (see Deterministic verification, `seed-regression` check).

| id | topic | source_a | source_b | status | proposed_disposition | handoff_target |
|---|---|---|---|---|---|---|
| CI-001 | Personalized numerical dose/reconstitution/schedule recommendations | CHARTER.md ratified doctrine, "BioStack may": *"Originate evidence-bounded numerical recommendations, including dose targets, reconstitution choices, and schedules, when the applicable capability contract permits it. Such outputs are recommendations, not prescriptions, and must carry numeric provenance, rationale, evidence applicability, uncertainty, risk controls, and escalation behavior... Perform deterministic reconstitution, concentration, dose, split, volume, and syringe-unit calculations"*; D13: *"BioStack may originate profile-aware dose, reconstitution, schedule, and support recommendations when the function contract, evidence, provenance, validation, uncertainty, and escalation requirements are satisfied. This does not authorize diagnosis, prescribing, clinician impersonation, or unsupervised alteration of prescribed treatment."* | `docs/canon/biostack-protocol-intelligence-canon.md` "Observational-Only Boundary", `must not`: *"Give clinical dosing instructions... Recommend starting, stopping, tapering, combining, escalating, or substituting substances"*; `README.md` "Safety and compliance boundary": *"does not provide clinical diagnosis, prescribing, medical dosing recommendations, individualized dosing... start/stop/taper/escalation advice"*; `docs/guidance/biostack-guidance-content-contract.v1.md` Class D: *"Selecting the correct dose for a person"* and *"Personalized titration schedules"* are **Prohibited** | contradictory | resolved-by-owner-ruling | P0-D1 |
| CI-002 | Protocol-builder flows for high-risk substance categories | CHARTER.md ratified doctrine, "BioStack may": *"Design and compare protocol options, including compounds, combinations, sequencing, timing, frequency, schedules"* | `docs/canon/biostack-protocol-intelligence-canon.md`, `must not`: *"Design SARM cycles. Design SERM recovery protocols. Provide post-cycle therapy instructions... Create protocol-builder flows for SARMs, SERMs, investigational peptides, gray-market compounds, or other high-risk substances."* | contradictory | fix | P0-D1 |
| CI-003 | Syringe-visualization versus a blanket injection-instruction prohibition | CHARTER.md ratified doctrine: *"show what a selected value looks like on the syringe"* | `docs/canon/biostack-protocol-intelligence-canon.md`, `must not`: *"Give injection instructions."* | contradictory | fix | P0-D2 |
| CI-004 | Profile-aware recommendation language versus Class C/D's explicit ceiling | CHARTER.md ratified doctrine: *"Make educated, profile-aware recommendations using relevant factors such as age, weight, goals, prior experience, current protocol, medications, conditions, tolerance, symptoms, biomarkers, diet, activity, and longitudinal observations."* | `docs/guidance/biostack-guidance-content-contract.v1.md` Class C: *"Must not morph into “you should start at X.”"*; Class D: *"Declaring an amount safe for the user,"* *"Declaring a protocol appropriate for the user"* — both **Prohibited** | contradictory | fix | P0-D2 |
| CI-005 | Capability map's independent prohibition list versus CHARTER.md `may` list | `docs/product/knowledge-engine-capability-map.md`: *"It does not authorize medical authority, diagnosis, prescribing, individualized dosing, treatment planning, start/stop/taper/escalation instructions, cycles, PCT, injection instructions, or sourcing guidance."* | CHARTER.md ratified doctrine `may` list (same spans cited in CI-001/CI-002/CI-003) | contradictory | fix | P0-D3 |
| CI-006 | Warning-first framing versus "more evidence, not less information" governing principle | CHARTER.md governing principle: *"High risk requires more evidence, explanation, validation, review, and escalation. It does not automatically require less useful information."* | `docs/canon/biostack-protocol-intelligence-canon.md`: *"Warning-first for high-risk categories: high-risk substances are surfaced through risk, regulatory, and observability context before any benefit framing."* | consistent | not-applicable | not-applicable |
| CI-007 | Privacy/data-custody claim moratorium | README.md: *"`/privacy` and `/terms` are stubs pending approved policy. No data-custody, storage-location, or privacy guarantee may be claimed on any public surface until they are approved."* | (no contradicting source found in the required list; catalogued because it is an explicit, dated legal-policy claim-moratorium other canon must not silently violate) | consistent | not-applicable | not-applicable |
| CI-008 | Audit verdict language versus named function-review status | `BIOSTACK_FRONTEND_READINESS_AUDIT.md`: *"Ready for provider acquisition. Close to ready for paid conversion. Ready as a free tools + evidence destination."* | No named human reviewer, approval status, or D15 function-review vocabulary (`unreviewed`/`review-required`/`reviewed`/`not-applicable`) is recorded anywhere in the audit document for this verdict | unreviewable | not-applicable | P0-D3 |
| CI-009 | Pairwise negative-relationship absence doctrine versus CHARTER.md conflict-identification doctrine | `docs/specs/active/BIO-PAIRWISE-001-relationship-substrate-and-label-census.md`: *"Absence of a label is not evidence that no interaction exists"* | CHARTER.md ratified doctrine `may` list: *"Identify conflicts, duplication, source-quality problems, contraindication signals, attribution problems, monitoring gaps, and reasons to pause or seek qualified help."* | consistent | not-applicable | not-applicable |
| CI-010 | Knowledge-promotion authority description versus charter's deferred-promotion stance | `README.md` "How knowledge gets in": *"Approved sources are ingested and normalized deterministically, classified for evidence strength and risk, human-reviewed where the contract requires it, and only then promoted into canonical product knowledge... It is not an autonomous authority, and it does not promote knowledge on its own."* | CHARTER.md: promotion-authority binding for the knowledge-promotion delivery class is not asserted anywhere in the ratified doctrine (silent, not contradicted) | accept-as-documented | not-applicable | P0-D1 |

CI-006, CI-007, and CI-009 are deliberately included as **non-conflict** rows: the acceptance
criteria below require the inventory to prove corpus coverage, not just list disagreements, so a
`consistent` disposition must be as citation-backed and auditable as a `contradictory` one. CI-008
demonstrates the `unreviewable` outcome distinct from both: a claim exists, but this parcel cannot
determine consistency or contradiction without a function-review assignment that does not yet
exist — recording that gap *is* the finding, and it is explicitly not this parcel's job to assign
one (that crosses into legal-policy adjudication, which D-G does not authorize).

CI-001 carries `proposed_disposition: resolved-by-owner-ruling` rather than `fix`: Coordinator
Decision **D-I** (`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`, `## D-I` heading, lines
152-169) rules **D-B1 = (c) staged split** on exactly this conflict, which
`docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` §2 ("The central conflict this
gate must resolve," lines 56-76) states in the same terms CI-001 catalogues (guidance-content-
contract v1.0.0 Class D prohibition versus charter D13's "may"). D-I states explicitly: *"the §2
canon conflict is resolved in substance by D-B1(c), and P0-A's contradiction inventory records its
disposition by this ruling."* D-B1(c)'s resolution (`P0-B-DESIGN-GATE.md` D-B1 table): deterministic
math on user-entered values plus Class A/B/C surfaces are the product's current posture now;
`biostack-recommended` origination — the dosing/schedule/reconstitution-target behavior CI-001
names — is defined in the P0-B contract but remains publicly gated behind guidance-content-contract
**v2.0.0** re-ratification, a separate future owner event. This row keeps `status: contradictory`
because the underlying canon texts still disagree on their own terms — the ruling settles which
posture governs the product now; it amends neither document's text, and guidance-content-contract
v1.0.0's Class D prohibition remains written exactly as quoted above. `handoff_target: P0-D1`
therefore names the subparcel that encodes this already-ruled resolution into the frozen
canon-of-record, not one that re-adjudicates it. CI-002, CI-004, and CI-005 bear on the same
headline conflict at narrower scope (protocol-builder flows for high-risk categories,
profile-aware recommendation language, and the capability map's prohibition list, respectively);
each is cross-referenced to D-I here but keeps `proposed_disposition: fix`, because D-I's ruling
addresses the headline dose/reconstitution/schedule-origination question (D-B1) and does not by
its own text resolve these narrower prohibitions — a later P0-D1 builder must still confirm each
narrower text is brought into line with the D-B1(c) posture, not merely assume it is already
settled.

## Mandatory class sections (D14 fieldwise union of all four declared delivery classes)

### Health-boundary

- **Guidance class:** `not-applicable`. P0-A produces no user-facing guidance of any of the four
  charter product guidance classes; it catalogues where canon disagrees about them.
- **Intended use:** internal, coordinator- and reviewer-facing. Output is consumed only by the
  governed-delivery coordinator and by future P0-D subparcel builders; it is never served to an
  end user and carries no public route.
- **Claims:** P0-A asserts no product claim. Every claim appearing in an inventory row is a
  **quotation** attributed to its source document, never an assertion in P0-A's own voice. The
  verifier's `no-unattributed-claim` check (below) enforces this mechanically.
- **Evidence threshold:** every `contradictory` or `consistent` row's `source_a`/`source_b` must
  be a verbatim quotation plus an exact file path and a section heading or line range sufficient
  for a reviewer to locate the quote in under one minute of searching. Paraphrase without a
  quotation fails validation.
- **Numeric provenance:** `not-applicable` — this parcel originates no numeric value of any
  provenance category (`user-entered`, `label-or-prescription-transcribed`, `source-studied`,
  `biostack-recommended`, `deterministically-derived`). This is stated explicitly rather than
  silently omitted, because the health-boundary class requires the field to be addressed one way
  or the other.
- **Missingness:** `unreviewable` is reserved for exactly three deterministic, independently
  verifiable conditions — no other reason qualifies:
  1. **Deleted** — the path does not exist at `BaseCommit` (`git cat-file -e
     <BaseCommit>:<path>` fails).
  2. **Unreadable** — the path exists at `BaseCommit` but the filesystem denies read access at
     dispatch time (a hard OS-level read failure, not a parsing difficulty, format objection, or
     judgment call).
  3. **Outside this worktree's checkout** — the path is named in the Required source list,
     `git cat-file -e <BaseCommit>:<path>` **succeeds** (the path is a tracked entry in the
     pinned commit's tree, ruling out condition 1), and a direct filesystem read of that same
     `<path>` inside this dispatch worktree **fails** with a hard OS-level read error (for
     example, a submodule gitlink not checked out, or a sparse-checkout cone exclusion) — the
     same tree-present-but-unreadable-here combination that distinguishes "excluded from this
     checkout" from both condition 1 (absent from the commit entirely) and a genuinely readable
     file (which would fail this condition's second test), independent of whether the file exists
     elsewhere in the repository's history.
  A row marked `unreviewable` must name which of these three conditions applies and cite the exact
  evidence that proves it in `SOURCE-MANIFEST.md`'s reason field, together with the pinned
  `BaseCommit` SHA: for condition 1, the failing `git cat-file -e <BaseCommit>:<path>` invocation's
  exact exit status/error text; for condition 2, the OS-level read error text; for condition 3,
  **both** the successful `git cat-file -e <BaseCommit>:<path>` invocation's output (proving tree
  presence) **and** the exact OS-level read error text from the failed worktree filesystem read
  (proving checkout absence) — a vague reason ("not reviewed," "out of scope," "deprioritized")
  fails validation, and so does marking a file `unreviewable` without this evidence. Recording the
  `BaseCommit` SHA alongside the quoted git/OS error text makes every `unreviewable` claim,
  including condition 3, independently reproducible by re-running the same two commands against
  that pinned commit, not merely asserted. `unreviewable-claim-verified` (Deterministic
  verification, below) independently re-checks every such claim, by replaying exactly this
  evidence, against the filesystem at `BaseCommit` and fails closed if the named file is, in fact,
  present and readable (or, for condition 3 specifically, if the `git cat-file -e` step itself
  fails — that is condition 1, not condition 3, and is also a fail-closed mismatch). A required
  source missing entirely from the Source Manifest (neither read nor recorded `unreviewable`)
  fails validation.
- **Red flags:** if a cataloged contradiction itself implicates the `acute-red-flag-or-emergency`
  or `controlled-or-illegal-sourcing` substance/function-risk labels (for example, a canon
  disagreement about whether BioStack may ever direct sourcing, or about escalation during a
  red-flag state), that row is additionally flagged `priority: P0-D1-first` in its `handoff_target`
  note, because those two labels "always preempt"/"always suppress" under the charter and a later
  reconciliation parcel should not have to re-discover the urgency ordering.
- **Escalation:** a disputed `proposed_disposition` between the two independent reviewers, or any
  reviewer finding that a row's disposition would itself decide a product allowed-output (crossing
  into P0-B's unauthorized territory), stops the parcel and returns to the coordinator for owner
  escalation rather than being triaged silently (see "Stop conditions and tripwire").
- **Function-review status:** P0-A itself is not a user-facing function, so its own status is
  `not-applicable` under D15. Every *catalogued* legal/regulatory or clinical-safety statement
  found in canon must carry its **own** D15 status (`unreviewed`, `review-required`, `reviewed`,
  or `not-applicable`) as found in its source document today — P0-A records the status it finds
  (usually `unreviewed`, since none of the required-source canon documents name a per-function
  reviewer), and never upgrades a status to `reviewed` itself.

### Privacy

- **Data inventory:** zero personal data. Every artifact this parcel produces is built from
  already-committed repository text (charter, specs, product docs, audit, canon, contract files).
  No user record, profile, protocol entry, check-in, or any other end-user-originated data is
  read, queried, or referenced.
- **Purpose:** internal governance reconciliation preparation; no purpose touches an individual
  user's data.
- **Consent:** `not-applicable` — no personal data is processed, so no consent basis is required.
- **Minimization:** the Source Manifest and inventory quote only the minimum span needed to prove
  a contradiction or consistency finding (typically one to three sentences); it never reproduces
  an entire document.
- **Retention:** these artifacts are permanent, versioned repository documents (same retention
  posture as every other merged spec/closure record) — there is no personal-data retention clock
  to set because there is no personal data.
- **Export/deletion:** `not-applicable` for the same reason; nothing in this parcel's artifacts is
  ever subject to a user export or deletion request.
- **Access control:** these artifacts live under version control with the same repository access
  control as every other governed-delivery document; no additional access tier is introduced.
- **Data classification of the inventory artifacts themselves:** `internal-engineering,
  non-personal, pre-publication`. They are not public product surfaces, are not marketing copy,
  and must not be linked from any public route. A reviewer finding any personal-data token
  (name other than a named human owner already public in repository governance records, email,
  user identifier, free-text user content) anywhere in this parcel's artifacts is a privacy
  mandatory stop (`leakage`, below) — it would mean the builder read outside the allowed,
  non-personal corpus.

### Legal-policy

- **Policy owner:** the governed-delivery coordinator shapes and triages this parcel under the
  owner's D-G gate; the owner (Clint Morgan, the named human owner throughout
  `COORDINATOR-DECISIONS-2026-10-07.md`) is this parcel's policy owner for the purpose of approving
  its merge. This field records *who owns the decision to catalogue without adjudicating*, not an
  adjudication of any legal/regulatory question found inside the canon — P0-A performs no legal or
  regulatory determination of its own (see Hard constraints).
- **Jurisdiction/scope:** `not-applicable` for any legal/regulatory determination (none is made);
  the parcel's own procedural scope is this repository's governed-delivery process.
- **Version/effective date:** this spec's content is versioned by its own commit/hash in the PR
  this parcel's commit produces; it has no independent "effective date" because it enacts no
  policy of its own.
- **Enforcement surfaces:** none. P0-A enforces nothing against product behavior; its artifacts
  are read by humans and by later P0-D parcels, not by any runtime gate.
- **Approval status:** `review-candidate` pending two independent reviews and the owner's Gate 3
  merge decision, exactly as this document's header states.
- **Cataloguing discipline for legal/regulatory canon statements found inside the corpus:** every
  regulatory or legal-status statement the inventory catalogues (for example README's "stubs
  pending approved policy" moratorium, or the guidance-content-contract's "Fully ratified" status)
  is recorded with its **own** D15 function-review status exactly as currently stated in its source
  — never adjudicated, upgraded, downgraded, or interpreted by P0-A. Where a source states no
  status at all, the builder records `unreviewed` (the honest default) rather than inferring one.

### Knowledge-promotion

- **Source/license/provenance:** every inventory row's `source_a`/`source_b` **is** a provenance
  citation (file path + section/line) by construction of the row schema; no row may cite an
  unsourced claim. This parcel introduces no new licensed or external source — its only "sources"
  are already-committed repository canon, so license tracking is `not-applicable`.
- **Evidence grade:** `not-applicable` in the scientific-evidence-grading sense (this parcel
  grades canon-document authority, via the precedence manifest, not scientific evidence).
- **Review lifecycle:** this parcel's own artifacts move `review-candidate -> active -> done`
  exactly like any other governed-delivery parcel (`docs/specs/README.md`'s lifecycle); they carry
  no separate knowledge-promotion lifecycle of their own, because they promote no product-facing
  canonical claim.
- **Promotion authority:** P0-A promotes nothing into canonical product knowledge. It is
  explicitly fail-closed on promotion: its allowed surfaces (below) include no edit to any existing
  canon document, no edit to the knowledge-engine pipeline, and no new canonical-claim file outside
  its own named inventory directory. The knowledge-promotion class's "canonical-write fencing"
  check (adapted below) is satisfied by this parcel producing zero writes to any file outside its
  allowed-surfaces list.
- **Rollback:** every artifact this parcel creates is new and additive (no existing file is
  modified except the two append-only registrations described in "Exact allowed surfaces").
  Rollback is `git revert` of this parcel's merge commit; no downstream artifact depends on P0-A's
  output being present at runtime, because nothing in this parcel ships to production or to any
  running system.

## Exact allowed surfaces

The builder (at a future, separately gated dispatch) may create or modify only:

1. `docs/specs/schemas/canon-precedence.md` — new. The precedence manifest (deliverable 1).
2. `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/README.md` — new. The
   inventory method.
3. `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/SOURCE-MANIFEST.md` — new.
4. `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CONTRADICTION-INVENTORY.md`
   — new.
5. `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/DATA-CLASSIFICATION.md` —
   new. States the privacy data-classification of this parcel's own artifacts (see Privacy section
   above) and the grep-based method the verifier uses to prove zero personal-data tokens are
   present.
6. `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CORPUS-COVERAGE-MATRIX.md`
   — new. The checkable corpus-coverage exhaustiveness record (see "Deterministic verification,"
   `corpus-coverage-matrix-complete`).
7. `docs/specs/scripts/verify-p0a.ps1` — new. The deterministic verifier (see below).
8. `docs/specs/README.md` — modified. Exactly one appended section (`## Canon precedence and
   contradiction inventory (P0-A)`), zero removed or reordered lines.
9. `docs/specs/INDEX.md` — modified. Exactly one appended table row for `P0-A`, in the existing
   column order, zero removed lines, zero other-row changes, using the closed-vocabulary registry
   cell literal `coordinator-assigns-at-gate-2` for the `Branch/worktree`/`Owner` cells at dispatch
   (P2's realized precedent: P2.md's own document contract 8 and acceptance criterion 12 define
   and pin this literal, and P2's `docs/specs/INDEX.md` row carried it verbatim while `P2` was
   `review-candidate`; `verify-p2.ps1` lines 642-643 still assert it as P2's historical Gate-2-time
   contract. P2's *current*, closed/`done` `docs/specs/INDEX.md` row has since replaced that
   placeholder with real branch/owner links, per its Gate 3 closure — the same lifecycle transition
   this parcel's own row will undergo at its own eventual closure. P3-A describes the literal only
   as a planned future usage in its own spec text and has no `docs/specs/INDEX.md` row of its own
   yet) rather than an ad hoc `TBD` cell.

No other path may change.

### Frozen surfaces

The builder must not change or reinterpret:

- The governed-delivery charter, the plan-review record, and every closed/review-candidate P1,
  P2, and P3-A artifact.
- Every document in the required source list (above) and every other canon document the builder's
  corpus read discovers: `README.md`, `docs/product/**`, `BIOSTACK_FRONTEND_READINESS_AUDIT.md`,
  `docs/canon/**`, `docs/guidance/**`, `docs/legal/**`, `docs/specs/active/**`,
  `docs/specs/done/**`, every `docs/INITIATIVES/**/FINAL-HANDOFF.md` and closure record. P0-A
  **reads** these; it never edits, "fixes," annotates inline, or reconciles them. That is
  exclusively P0-D's job, after this parcel's precedence manifest is frozen and P0-D's own gate is
  granted.
- P2's five schema/fold/routing/regression-map files and every P2/P3-A fixture.
- `docs/specs/CORE-CONTEXT.md`, `docs/specs/active/README.md`, `docs/specs/done/README.md`.
- Root `AGENTS.md`.
- `docs/INITIATIVES/biostack-production-readiness/` and its `NO-GO / HOLD` verdict (charter D17).
- `frontend/`, `backend/`, `contracts/`, `.github/`.
- D1-D18, the ratified product doctrine, the four product guidance classes, the ten
  substance/function-risk labels, and the charter's parcel dependency spine, standing
  authorizations, stop conditions, and exit criterion.
- Every file any contradiction row's `proposed_disposition` names for future change — proposing a
  fix is not performing one.

If an allowed deliverable appears to require any frozen-surface or frozen-contract change, the
builder stops without editing it.

## Hard constraints

- **No product allowed-output decision.** This parcel never states, implies, or can be read as
  stating what BioStack's product may or must not do. It states what the *canon says* about that,
  including where the canon disagrees with itself. Any artifact phrased as a product rule
  ("BioStack must/must not do X") rather than as a cited catalogue entry ("Document A says X;
  Document B says not-X") fails review and is this parcel's clearest tripwire (see Stop
  conditions).
- **No adjudication.** `proposed_disposition` is a recommendation for P0-D, never an executed
  change. A row whose disposition is `fix` must not pre-author the fix text; it names the conflict
  and the responsible later subparcel only.
- **No new personal data, license, or external source.** Every citation is to already-committed
  repository text. No network fetch, no new external document, no user data of any kind.
- **No `TBD`.** No file created or modified by this parcel may contain the literal markers `TBD`,
  `TODO`, `FIXME`, or `{{...}}` placeholder syntax, or an unresolved decision. Every table cell is
  either a real value or an explicit, named `not-applicable`/`unreviewable` — never a blank or a
  placeholder.
- **Real-corpus grounding, not synthetic examples.** Every `CI-NNN` row's quotations must be
  verifiably present, verbatim, in the cited file at the cited location at the pinned
  `BaseCommit`. A quotation that does not match the source file byte-for-byte (modulo leading/
  trailing whitespace) fails validation. The straight double quotation marks (`"`) this spec uses
  as its own citation delimiter (opening/closing a quoted span) are not part of the quoted content
  and are exempt from the byte-for-byte test; any quotation mark that appears *inside* a cited
  span is source content and must reproduce the source's exact glyph (curly “/” or
  ‘/’ vs. straight "/'), with no typographic normalization permitted there.
  `seed-regression` (Deterministic verification, below) carries this same exact-glyph floor
  forward for the ten seed rows.

## Deterministic verification

P0-A has no runtime, no calculation surface, and no code path to fixture against in the
positive/degraded/refused/escalated sense the health-boundary class's `minimumChecks` assumes for
a product-shipping parcel. Per D14 ("No label erases another label's obligations"), these checks
are **adapted**, not waived, to this parcel's analytical shape. Each adaptation is recorded below
as a named, deterministic, scriptable check inside `docs/specs/scripts/verify-p0a.ps1`:

| Folded check (from the four classes' `minimumChecks` union) | P0-A adaptation | Verifier behavior |
|---|---|---|
| positive, degraded, refused, escalated fixtures (health-boundary) | the inventory's own row set must include at least one instance of each of four states of the *method itself*: a `consistent` row (positive), an `unreviewable` row (degraded — insufficient information to decide), a `contradictory` row with `proposed_disposition: fix` (refused — canon as written cannot both be followed), and at least one row flagged `priority: P0-D1-first` touching a red-flag/illegal-sourcing label (escalated) | fails `missing-method-state-coverage` if any of the four states has zero rows |
| calculation boundary tests where numeric (health-boundary) | asserted `not-applicable`: no file under this parcel's allowed surfaces may contain a formula, rounding rule, or numeric calculation | fails `unexpected-numeric-surface` if any allowed-surface file contains a computation pattern |
| authorization, consent, retention/export/deletion, log-redaction tests (privacy) | grep-based personal-data token scan (name/email/free-text-user-content patterns) across every file this parcel creates or modifies, asserting zero matches outside the one permitted named human owner already public in existing governance records | fails `personal-data-token-found` on any match |
| copy-to-enforcement consistency; version/consent linkage tests (legal-policy) | every legal/regulatory canon statement catalogued in `CONTRADICTION-INVENTORY.md` or `SOURCE-MANIFEST.md` carries a D15-vocabulary function-review status exactly as found in its source, never `reviewed` when the source itself does not say `reviewed` | fails `invented-review-status` if any catalogued status is stricter than its source states |
| provenance/license/freshness; fail-closed promotion; canonical-write fencing tests (knowledge-promotion) | diff this parcel's commit against the allowed-surfaces list (above); any path outside that list is a fencing violation; every `CI-NNN`/manifest row's citation resolves to a real file+location at `BaseCommit` | fails `canonical-write-fencing-violation` or `unresolvable-citation` |

### `mandatoryStopConditions` fold (per class, mirroring the `minimumChecks` table above)

Per D14, each of the four declared classes' `mandatoryStopConditions`
(`docs/specs/schemas/delivery-class-controls.json`) is folded and mapped explicitly to this
parcel's own tripwire language, the same rigor already applied to `minimumChecks` above:

| Class | `mandatoryStopConditions` entry | P0-A tripwire mapping |
|---|---|---|
| health-boundary | `unsupported certainty` | "Stop conditions," bullet 1 ("Any artifact would state, imply, or could reasonably be read as stating a product allowed-output...") — an unsupported-certainty product claim is exactly that drift. |
| health-boundary | `prescribed-treatment direction` | "Stop conditions," bullet 1 — directing alteration of prescribed treatment is a product allowed-output/behavior statement, this parcel's clearest tripwire. |
| health-boundary | `red-flag bypass` | Health-boundary "Red flags" rule (`priority: P0-D1-first` flag on any row implicating `acute-red-flag-or-emergency`/`controlled-or-illegal-sourcing`) plus "Stop conditions," bullet 7 (P0-B/P0-C/P0-D referenced as if already authorized) — bypassing the flag or the authorization boundary is a stop. |
| health-boundary | `provenance loss` | Hard constraints, "Real-corpus grounding" (every quotation verified byte-for-byte at `BaseCommit`) plus "Stop conditions," bullet 3 ("A required source is missing, unreadable, or its content at `BaseCommit` cannot be verified") — provenance loss of a citation is exactly that stop. |
| privacy | `new sensitive field without approved lifecycle` | `not-applicable` as a fresh occurrence — the Privacy section asserts zero personal data and no new field of any kind; if a builder's read nonetheless discovers one, "Stop conditions," bullet 5 ("Any personal-data token is found in a draft artifact") applies. |
| privacy | `leakage` | "Stop conditions," bullet 5, and the Privacy section's own named "leakage" mandatory stop (Privacy, "Data classification of the inventory artifacts themselves"). |
| privacy | `consent bypass` | `not-applicable` — Privacy, "Consent" states no consent basis is ever implicated; stated explicitly rather than silently omitted. |
| legal-policy | `unapproved policy presented as effective` | Hard constraints, "No product allowed-output decision," plus Legal-policy "Cataloguing discipline" (never upgrades a status to `reviewed`) — presenting an unapproved policy as effective would itself be an unauthorized product/claim statement and is covered by "Stop conditions," bullet 1. |
| legal-policy | `policy/enforcement mismatch` | Legal-policy "Enforcement surfaces: none" plus `no-unattributed-claim` — any enforcement mismatch found inside canon is catalogued as a `CI-NNN` row, never asserted as settled; "Stop conditions," bullet 2 ("A `proposed_disposition` would itself require a charter amendment...") covers the case where resolving it would require more than a catalogue entry. |
| knowledge-promotion | `missing source/license/review state` | "Stop conditions," bullet 3, and `source-manifest-completeness`/the `unreviewable` evidence discipline (Health-boundary "Missingness," above). |
| knowledge-promotion | `bypassed promotion` | Knowledge-promotion "Promotion authority" (P0-A promotes nothing; zero writes outside allowed surfaces) plus `canonical-write-fencing-violation`. |
| knowledge-promotion | `unreviewed public claim` | Health-boundary "Function-review status" (P0-A never upgrades a found status to `reviewed`) plus `invented-review-status`. |

Additional, parcel-specific deterministic checks `verify-p0a.ps1` must implement:

- `precedence-manifest-totality` — for every pair of documents in `canon-precedence.md`'s
  registry, `compare(A, B)` resolves to exactly one of the three defined outcomes; no pair is
  silently unresolved.
- `precedence-rank-density` — the rank column contains no gaps and no unexplained duplicate (a
  duplicate is valid only when accompanied by a recorded `open-tie` note).
- `no-placeholder` — the literal markers `TBD`, `TODO`, `FIXME`, `{{...}}` are absent from every
  file this parcel creates or modifies (reusing P3-A's `noPlaceholderPatterns`, read from
  `docs/specs/schemas/parcel-spec.schema.json` once P3-A has merged — see the dispatch
  precondition in "Lineage and dependencies").
- `row-schema-conformance` — every `CI-NNN` row in `CONTRADICTION-INVENTORY.md` has exactly the
  eleven fields of the row schema, and `status: contradictory` always pairs with
  `proposed_disposition != not-applicable`.
- `class-axis-vocabulary-conformance` — every `delivery_class_tags`/`guidance_class_tags`/
  `substance_function_risk_tags` value is either `not-applicable` or a literal member of the
  corresponding closed vocabulary in `docs/specs/schemas/classification-axes.schema.json`, read
  live (not copied), exactly as P2/P3-A already require elsewhere.
- `seed-regression` — every `CI-001` through `CI-010` row above is present in the shipped
  `CONTRADICTION-INVENTORY.md`, byte-for-byte unchanged in its `source_a`/`source_b` quotations as
  they read in **this** spec version (CI-001's `source_a` quotations were extended, with a named
  reason, during this spec's dual-review rework to restore the charter's conditional clause and
  D13's closing sentence — see CI-001's row and the paragraph following the seed table; that
  extension is the new byte-for-byte floor, not the prior spec hash's shorter quotation) (the
  builder may add further detail but may not remove or soften a seed finding without a named,
  reviewed reason recorded alongside it).
- `source-manifest-completeness` — every required source (above) appears in `SOURCE-MANIFEST.md`
  with a valid `consistent`/`contradictory`/`unreviewable` status and a reviewable citation or
  reason.
- `unreviewable-claim-verified` — for every `SOURCE-MANIFEST.md` row marked `unreviewable`, the
  verifier independently re-checks the named condition against `BaseCommit` by replaying the
  recorded evidence: for condition 1 (deleted), re-runs `git cat-file -e <BaseCommit>:<path>` and
  fails closed unless it fails identically; for condition 2 (unreadable), re-attempts the
  filesystem read and fails closed unless it fails identically; for condition 3 (outside this
  worktree's checkout), re-runs **both** `git cat-file -e <BaseCommit>:<path>` (must succeed) and
  the filesystem read (must fail) and fails closed (`false-unreviewable-claim`) unless exactly that
  pairing holds — a `git cat-file -e` failure under a condition-3 claim is a condition-1/condition-3
  mismatch, and a successful filesystem read under any condition is a `false-unreviewable-claim`
  — a bypass-prevention check, not a courtesy re-read.
- `corpus-coverage-matrix-complete` — every combinatorial pair of required sources (including
  items 12-13) has a recorded `compared-contradictory`/`compared-consistent`/`scope-disjoint`
  outcome in `CORPUS-COVERAGE-MATRIX.md`; any missing pair fails this check.
- `no-unattributed-claim` — no sentence in any artifact asserts a product rule in BioStack's own
  voice without an accompanying citation to a specific canon document.

## Acceptance criteria

1. `docs/specs/schemas/canon-precedence.md` exists, is non-empty, and `verify-p0a.ps1`'s
   `precedence-manifest-totality` and `precedence-rank-density` checks both pass.
2. The inventory (`CONTRADICTION-INVENTORY.md` + `SOURCE-MANIFEST.md`) covers one hundred percent
   of the required source list with a valid status for each, and `source-manifest-completeness`
   and `unreviewable-claim-verified` (no `false-unreviewable-claim`) both pass. The
   `CORPUS-COVERAGE-MATRIX.md` records an outcome for every combinatorial pair of required
   sources, and `corpus-coverage-matrix-complete` passes — the checkable floor for coverage;
   "exhaustive" findings beyond that floor remain a best-effort, reviewer-checked claim (see
   Objective, deliverable 2).
3. `seed-regression` passes: all ten seed findings above are present, unweakened relative to this
   spec version's own text (see `seed-regression`'s definition for the CI-001 extension this
   rework made).
4. Every `contradictory` row names a `proposed_disposition` (including `resolved-by-owner-ruling`
   where an owner ruling already governs the conflict, cited by name) and a `handoff_target`;
   every `consistent`/`unreviewable` row names `not-applicable` for both —
   `row-schema-conformance` passes.
5. `class-axis-vocabulary-conformance`, `no-placeholder`, `canonical-write-fencing-violation`
   absence, `unresolvable-citation` absence, `personal-data-token-found` absence,
   `invented-review-status` absence, and `false-unreviewable-claim` absence all pass.
6. `DATA-CLASSIFICATION.md` states the `internal-engineering, non-personal, pre-publication`
   classification and the grep method used to prove it; the method is re-runnable by a reviewer.
7. `docs/specs/README.md` carries exactly one appended section and zero other diffs; `docs/specs/
   INDEX.md` carries exactly one appended row and zero other diffs.
8. No file outside "Exact allowed surfaces" is touched (diff review confirms this directly; it is
   not solely a verifier-script claim).
9. Two independent, fresh, read-only reviewers return PASS, and the health-boundary
   (`green-dual-review-required`) and legal-policy (`recorded-human-approval`) merge gates folded
   from D14 are both satisfied before Gate 3 is even requested.

## Review/gates

- **Reviewer count:** 2 (D14 max-scalar fold over four classes each at 2; also charter D8 for
  architecture/risk-sensitive parcels).
- **Reviewer independence:** fresh-session, frontier-capable, read-only, each receiving only this
  approved spec plus the necessary canon/repo state — neither reviewer sees the other's findings
  before submitting (charter D7).
- **Merge gates (D14 union of `additionalMergeGate` across the four classes):**
  `green-dual-review-required` (health-boundary) **and** `recorded-human-approval` (legal-policy)
  both apply; privacy and knowledge-promotion contribute no additional merge gate beyond dual
  review. Both conditions must be satisfied before Gate 3; satisfying one does not waive the
  other.
- **Human-approval condition:** per D-G, the owner's recorded approval is required at merge, in
  addition to (not instead of) the two independent PASS reviews. **Gate 3 merge is the owner's
  decision.**
- **Disagreement handling:** if the two reviewers disagree on any finding (including a disputed
  `proposed_disposition`, a disputed precedence rank, or a disputed `status`), the coordinator
  reproduces the disputed fact directly against the cited source text before triage, per charter
  D8; an irreproducible disagreement is a stop (below), not a coin-flip resolution.

## Stop conditions and standing-authorization tripwire

The builder, reviewers, or coordinator stop and return to the owner when:

- Any artifact would state, imply, or could reasonably be read as stating a product
  allowed-output, an applicability criterion for any substance/function-risk label, or any
  allowed/degraded/refused/escalated behavior — **this is P0-B's unauthorized territory and the
  single most important tripwire this parcel carries.** Drift toward deciding product outputs
  stops the parcel immediately, regardless of how far dispatch has progressed.
- A `proposed_disposition` would itself require a charter amendment (changes D1-D18 or the
  ratified product doctrine) rather than a P0-D execution — per the charter's plan-review
  triage rule, this reopens Gate 1 for that decision rather than proceeding as ordinary P0-D
  handoff.
- A required source is missing, unreadable, or its content at `BaseCommit` cannot be verified —
  recorded `unreviewable`, never guessed.
- Two reviewers disagree and the coordinator cannot reproduce and resolve the disputed fact.
- Any personal-data token is found in a draft artifact.
- P2 or P3-A have not reached merged/closed status by the time dispatch (Gate 2) would occur (see
  "Lineage and dependencies" dispatch precondition).
- P0-B, P0-C, or any P0-D subparcel is referenced as if already authorized — each remains gated on
  its own, separate, not-yet-granted owner decision; this spec's existence is not that gate.

## Rollback

This parcel's artifacts are new, additive files plus two append-only registrations
(`docs/specs/README.md`, `docs/specs/INDEX.md`). Rollback is a direct `git revert` of the merge
commit; nothing downstream depends on these artifacts at runtime, because P0-A ships no code, no
configuration, and no product-facing content. No migration, data backfill, or compatibility window
is implicated.

## What this parcel does not close

Closing P0-A closes the canon-precedence/contradiction-inventory step only. It does not authorize
P0-B, P0-C, or any P0-D subparcel; does not waive, mark passing, or alter the production-readiness
`NO-GO / HOLD` verdict (charter D17); and does not itself reconcile a single contradiction it
catalogues — reconciliation is P0-D's work, under P0-D's own future gate, governed by the
precedence manifest this parcel freezes.
