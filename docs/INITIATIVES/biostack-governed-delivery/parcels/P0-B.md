# P0-B — Product Capability and Safety Contract Parcel Spec

Status: **REVIEW CANDIDATE — builder dispatch blocked**

Parcel ID: `P0-B`

Risk and routing: `health-boundary`, `privacy`, `legal-policy`, `knowledge-promotion`
(self-declared delivery classes, folded against `delivery-class-controls.json`); architecture
(charter parcel-tree designation, P0 row); one builder; **two independent read-only adversarial
reviewers** (D14 max-scalar fold over four classes each requiring 2 reviewers resolves to 2;
charter D8 also requires dual review for architecture/risk-sensitive parcels — the same
combination already carried by P0-A, P2, and P3-A).

This parcel **decides what the product may say** — the machine-readable Product Capability and
Safety Contract: deterministic applicability criteria for each of the ten closed
substance/function-risk labels, and allowed/degraded/refused/escalated behavior for each label
across the four product guidance classes, plus numeric provenance, missing-input, function-review,
and escalation semantics. It decides these things **only** within the boundary the owner has
already ruled (Coordinator Decision **D-I**, `D-B1..D-B6`, below) — it transcribes and
operationalizes that ruling into a machine-readable contract; it does not re-litigate, soften,
strengthen, or extend any ruled item. **Any apparent need to deviate from the ruled matrix is a
STOP-AND-REPORT to the owner, not a design choice this parcel or its builder may make.**

## Lineage and dependencies

- Governed-delivery charter SHA-256:
  `4CD390D631487DA8E97509205A186F242C4A83B762FFFA894BEEC8A21F4EFE22` (unchanged since P0-A, P1,
  P2, and P3-A shaping; re-verified at this shaping time by direct
  `sha256sum docs/INITIATIVES/biostack-governed-delivery/CHARTER.md`).
- Owner-authorization ledger (`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`) whole-file
  hash at this shaping time, verified by direct `sha256sum`:
  `908FAA5999526DE76CC7E21B6753D41BC8BDB9F29C5F1717A504D9103E4CA7AE` — unchanged since P0-A's own
  current (reconciled) shaping anchor (P0-A.md's "Lineage and dependencies," "Current whole-file
  hash"), confirming no further owner-dated entry has landed between P0-A's shaping and this
  parcel's. Per the same append-only-ledger caution P0-A records, this whole-file hash is a
  freshness check, not this parcel's sole standing-authorization anchor; the load-bearing anchor is
  the **D-I entry text span** below.
- **D-I entry text anchor** — the section headed `## D-I — P0-B design gate RULED (owner,
  2026-10-08: "D-B1: c · D-B2–D-B6: as recommended")` (lines 152-169 of
  `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` at the hash above). The span-hash a
  reviewer re-verifies directly, independent of the ledger's future growth, is the SHA-256 of
  exactly `sed -n '152,169p' docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md | sha256sum`.
  D-I's text, quoted here verbatim because it is this parcel's design-ruling source of record:

  > D-I — P0-B design gate RULED (owner, 2026-10-08: "D-B1: c · D-B2–D-B6: as recommended")
  >
  > The owner rules on the design presented in
  > `docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md`:
  >
  > 1. **D-B1 = (c) staged split.** Deterministic math on user-entered values + guidance-contract
  >    v1.0.0 Class A/B/C surfaces are the product's current posture. `biostack-recommended`
  >    origination (dose targets, schedules, profile-aware picks) is defined in the P0-B contract
  >    but publicly enabled only after guidance-content-contract **v2.0.0** re-ratification +
  >    `legal_product_ratification` — a separate owner event.
  > 2. **D-B2–D-B6 = as recommended.** The per-label behavior matrix (D-B2), fail-closed numeric
  >    provenance rules (D-B3), missing-input ladder (D-B4), function-review/public-enablement
  >    rules (D-B5), and escalation semantics (D-B6) are adopted exactly as presented.
  >
  > No locked §1 item is changed; no charter amendment is required. Effects: the D-B1..D-B6 matrix
  > is now **frozen as P0-B's normative design input** and will be encoded verbatim in the P0-B
  > parcel spec. **P0-B dispatch remains gated** on P0-A freezing canon precedence (charter
  > dependency spine); the §2 canon conflict is resolved in substance by D-B1(c), and P0-A's
  > contradiction inventory records its disposition by this ruling.

  D-I rests on **D-G** (`## D-G`, lines 114-123: *"P0-B remains UNAUTHORIZED and returns to the
  owner for a fresh gate with its design presented"*) and **D-H** (`## D-H`, lines 125-150, the
  owner directive opening this gate: *"On to P0-B design gate — the one where we decide what the
  product may say"*). D-G, D-H, and D-I together are this parcel's full authorization chain for
  **shaping and review**; none of the three grants dispatch (see "Authorization boundary," below).
- Design-gate decision package SHA-256:
  `34928AC8C54FE8AF9CB7B3A01D36820FE435F2ADF4F02F56C20E7CF548D57F5B`
  (`docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md`, verified by direct
  `sha256sum` at this shaping time). This document's §1-§6 (lines 23-215) is the **entire
  normative design input** this spec's Deliverables section transcribes; "Deliverables," below,
  cites exact section headings and line ranges for every clause it carries forward.
- Guidance-content-contract v1.0.0 SHA-256:
  `60729197B1609AB489B921AE6A9840C1DA485CBA6F9B4B1A5CA6BD0FE4E8291F`
  (`docs/guidance/biostack-guidance-content-contract.v1.md`, Status "Fully ratified," 2026-08-02).
  **Remains the governing public-surface contract in full** — Class A/B/C permitted, Class D
  prohibited — until a distinct, future **v2.0.0** re-ratification event. This parcel defines the
  `biostack-recommended`-origination contract content that v2.0.0 would eventually govern; it does
  not ratify v2.0.0, does not change v1.0.0's text, and does not enable any public surface v1.0.0
  currently prohibits (see "Hard constraints," `biostack-recommended` staging gate).
- P2 substrate (both files unchanged since P2's `done` closure, re-verified by direct `sha256sum`
  at this shaping time):
  - `docs/specs/schemas/classification-axes.schema.json`, SHA-256
    `49F9FCFA18AF08202069812BA74BEBD0ADF1A836ADC62DA2E5063CAC80FA0A14`. Its `substanceFunctionRisk`
    axis carries `"controlBindingStatus": "deferred-to-P0-B"`,
    `"allowedOutputBindingStatus": "out-of-scope-for-P2"`, and each of the ten labels'
    `applicabilityField` entries carry `"determinationAuthority":
    "deferred-to-P0-B-deterministic-criteria"` and `"status": "deferred"`. This parcel is the
    deferral's named destination; "Exact allowed surfaces," item 3, binds these fields.
  - `docs/specs/schemas/delivery-class-controls.json`, SHA-256
    `E1E545CC01D1EC316FC3F137E80792ABAC1F5D071BD09E15C8B5C804B536B068` — the four declared classes'
    `requiredSpecAdditions`/`minimumChecks`/`reviewers`/`additionalMergeGate`/
    `mandatoryStopConditions`/`requiredClosureEvidence` this spec's "Mandatory class sections" and
    "Deterministic verification" fold against, unchanged since P2's closure.
- P0-A spec (this rework's prerequisite under the charter dependency spine
  `... -> P3-A -> P0-A -> P0-B -> ...`): `docs/INITIATIVES/biostack-governed-delivery/parcels/
  P0-A.md`, current content SHA-256 `21EAE29F3319A210CD4DD1919695C1C3A20A34B5E0AB910E4CC4913DFAC54165`,
  status **`REVIEW CANDIDATE — builder dispatch blocked`** per its own header at this shaping time
  (not yet merged, not yet closed; `docs/specs/INDEX.md` carries no `P0-A` row). P0-A's own
  contradiction inventory (once built) will record CI-001's `resolved-by-owner-ruling` disposition
  citing this same D-I entry (P0-A.md, seed finding `CI-001`); **this spec does not wait for that
  inventory to exist to be shaped or reviewed** — D-I already rules the underlying conflict in
  substance, independent of when P0-A's own artifacts are produced — but this spec's **dispatch**
  does wait for P0-A's canon precedence to actually freeze (merged, closed), per the charter
  dependency spine and the explicit boundary below.
- P3-A spec: `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md`, status
  `REVIEW CANDIDATE` per its own header; `docs/specs/INDEX.md`'s current `P3-A` row reads
  `review-candidate`. Not yet merged or closed. P3-A precedes P0-A on the dependency spine and is
  therefore also a dispatch precondition for this parcel (transitively, through P0-A).
- Shaping base anchor: `main@13c5c3f535918938095afc2b9f629f2f3b003e47` (this worktree's current
  `HEAD`, branch `docs/p0b-shaping`, clean working tree at shaping time). Not `BaseCommit` (see
  "BaseCommit (defined term)," below) — only the commit this spec's own text and every hash and
  citation above were shaped and verified against.

## Authorization boundary (restated, because this is the load-bearing fact of this spec)

- **Authorized now:** P0-B **shaping** and **dual independent review**, under D-G's explicit grant
  that P0-B "returns to the owner for a fresh gate with its design presented" and D-H/D-I's
  completion of exactly that gate (the design was presented in `P0-B-DESIGN-GATE.md`; the owner
  ruled on it in D-I). D-I is the fresh, explicit human gate D-G reserved for P0-B's doctrine
  content; it authorizes this spec to exist, be shaped, and be reviewed.
- **Not yet authorized by D-I or by this spec: P0-B dispatch (Gate 2).** D-I's own text states the
  precondition explicitly: *"P0-B dispatch remains gated on P0-A freezing canon precedence (charter
  dependency spine)."* This spec may therefore be **shaped and reviewed now** (exactly as D-G/D-H/
  D-I scope), but its **dispatch** additionally requires, in this order: (1) P3-A reaching merged,
  closed status (P3-A precedes P0-A on the dependency spine and is itself not yet closed); (2)
  P0-A reaching merged, closed status, with its canon-precedence manifest frozen (P0-A is not yet
  merged — see "Lineage and dependencies," above); (3) this spec's own dual review passing and
  Gate 3 merge of *this* spec, per the ordinary parcel-spec lifecycle. None of these three has
  occurred as of this shaping. The coordinator stops and does not dispatch P0-B until all three
  clear, in order.
- **This spec decides product allowed-outputs ONLY within the owner-ruled D-B1..D-B6 matrix.** Every
  applicability criterion, allowed/degraded/refused/escalated cell, provenance rule, missing-input
  rule, function-review rule, and escalation rule this spec's Deliverables section requires the
  builder to encode is either (a) a verbatim transcription of a ruled D-B1..D-B6 clause, cited to
  its exact `P0-B-DESIGN-GATE.md` section and line range, or (b) a deterministic, narrowly-scoped
  **operationalization** of an already-closed label name into a checkable applicability test,
  explicitly flagged as such and bounded by the charter's own closed vocabulary (charter: *"No
  parcel may add, remove, or redefine a label in a way that changes allowed product behavior
  without a charter amendment and review"*). **Any content in a future `product-capability-
  safety-contract` artifact that cannot be traced to (a) or (b) is itself a deviation and a stop
  condition** (see "Stop conditions," below) — this is the spec-level control that keeps this
  parcel inside D-G's and D-I's actual grant.
- **Merge is the owner's decision.** A green Gate 2 dispatch (once its own preconditions clear),
  green deterministic checks, and two PASS reviews make this parcel mergeable; they do not make it
  merged. Gate 3 (merge) is reserved to the owner, consistent with D-G's "human-approval conditions
  those classes trigger at merge," restated by D-I's preservation of that same posture.
- **This spec does not authorize production release.** Even after P0-B merges, D13's
  `biostack-recommended`-origination capability remains **publicly disabled** until the
  guidance-content-contract v2.0.0 re-ratification + `legal_product_ratification` event D-B1(c)
  names — a wholly separate, future owner event this spec does not schedule, does not approximate,
  and does not treat as a formality. Nothing in this spec waives, marks passing, or alters the
  production-readiness `NO-GO / HOLD` verdict (charter D17).

## BaseCommit (defined term)

`BaseCommit` is the single commit SHA this parcel's builder pins at **Gate 2 dispatch** — it is
**not** `main@13c5c3f535918938095afc2b9f629f2f3b003e47` (this spec's own shaping anchor, named
above), because that anchor predates P3-A's and P0-A's close and therefore cannot itself be the
commit the "Deliverables" section's verbatim citations are re-verified against at dispatch time.
The coordinator's Gate 2 dispatch record (made only once the three-step dispatch precondition in
"Authorization boundary" above clears) names the exact `BaseCommit` SHA. Every clause in this spec
that reads "at `BaseCommit`" — the "Real-corpus grounding" hard constraint, the
`canonical-write-fencing-violation`/`unresolvable-citation` checks, and the `matrix-fidelity`
check (Deterministic verification) — binds to that one pinned commit for the whole of this
parcel's dispatch, mirroring P0-A's and P2's own `BaseCommit` discipline.

## Objective

Produce the single machine-readable artifact (plus its human-readable companion) that lets every
user-facing BioStack function answer, deterministically and without re-litigation: *which
substance/function-risk labels apply to this output, and what must this output do — allow,
degrade, refuse, or escalate — given its product guidance class and every applicable label's
obligations composed under D14's fieldwise fold?*

This is the **Product Capability and Safety Contract** the charter names at P0-B's parcel-tree row
(*"machine-readable Product Capability and Safety Contract, including per-label applicability and
allowed/degraded/refused/escalated behavior, numeric provenance, and function-review status"*) and
the contract P3-B will later bind the generic parcel schema to, and P0-C will later prove with
fixtures. P0-B is the sole, authoritative place this binding is defined; it is defined exactly
once, here, under the owner's D-I ruling — not re-derived or re-argued downstream.

P0-B changes no runtime code. It produces a new, additive governance/policy artifact; no product
surface reads it or enforces it until a later parcel (P0-C for fixtures; an eventual, separately
gated product-integration parcel for runtime enforcement) wires it in. **This spec's own merge
does not enable any product behavior.**

## Normative input and authority sources

1. **`docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md`** (hash above) — the owner-
   ruled design, §§1-6. This is the **sole source** for every ruled clause the Deliverables section
   transcribes; no other document may be substituted or blended in for a ruled clause's content.
2. **`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`**, entries **D-G**, **D-H**, **D-I**
   (lines 114-169 at the hash above) — the ruling record and this parcel's authorization-to-shape
   chain (see "Lineage and dependencies").
3. **`docs/INITIATIVES/biostack-governed-delivery/CHARTER.md`** (hash above) — the ratified product
   doctrine's `may`/`must not` lists, the four product guidance classes, the closed
   substance/function-risk vocabulary, and **D12-D16** (quoted where load-bearing, below). This
   spec's content must be traceable to the charter's own `may` list plus D-I's ruling — it invents
   no new product capability beyond what D13 already names as possible and D-I already stages.
4. **`docs/guidance/biostack-guidance-content-contract.v1.md`** (hash above) — the still-governing
   Class A/B/C/D taxonomy, copy-guard terms, warning/uncertainty markers, and approval-level
   vocabulary this spec's Deliverables section reuses verbatim for the "now" posture (D-B1(c)'s
   first clause) rather than re-authoring an equivalent vocabulary from scratch.
5. **`docs/specs/schemas/classification-axes.schema.json`** and **`delivery-class-controls.json`**
   (hashes above) — the P2 substrate this parcel's deliverable binds (classification-axes.schema.
   json's deferred `substanceFunctionRisk` applicability fields) and folds against
   (delivery-class-controls.json's four-class union, for "Mandatory class sections" and
   "Deterministic verification").

Charter clauses this spec relies on verbatim (quoted here once, so later sections may cite by
name rather than re-quoting):

> D12 — Useful guidance is in scope. Safety governance calibrates useful guidance; it does not
> collapse BioStack into a passive library.

> D13 — Personalized numerical guidance is allowed. BioStack may originate profile-aware dose,
> reconstitution, schedule, and support recommendations when the function contract, evidence,
> provenance, validation, uncertainty, and escalation requirements are satisfied. This does not
> authorize diagnosis, prescribing, clinician impersonation, or unsupervised alteration of
> prescribed treatment.

> D14 — Three orthogonal classification axes. Delivery risk, product guidance class, and
> substance/function risk are independently recorded and may all be multi-label. Controls compose
> field by field: union required sections, checks, stop conditions, and evidence; take the maximum
> reviewer count; allow standing authorization only when every applicable label permits it; apply
> every triggered human-approval condition; and stop on incompatible controls. "Most restrictive
> wins" applies only to a genuine scalar conflict. No label erases another label's obligations.

> D15 — Function-specific review. Regulatory and legal status is assessed per user-facing function,
> intended use, user, automation, output, and claim. Broad disclaimers or product-level labels do
> not settle function-level status. Active specs record `unreviewed`, `review-required`,
> `reviewed`, or `not-applicable` and name the human owner when review is required.

> D16 — Evidence namespaces are distinct. `scientific-evidence`, `recommendation-rationale`,
> `delivery-evidence`, and Keon receipt facts use distinct contracts and must not be conflated.

> Product guidance classes ... `acute-red-flag-or-emergency` always preempts ordinary guidance.
> `controlled-or-illegal-sourcing` always suppresses sourcing, evasion, and concealment assistance.
> Other labels calibrate evidence, explanation, validation, review, and escalation rather than
> automatically suppressing useful guidance.

## Deliverables

### Required document contract 1: `docs/specs/schemas/product-capability-safety-contract.json`

The primary, machine-readable artifact. A single JSON document, `"schema":
"biostack.product-capability-safety-contract.v1"`, containing at minimum the following top-level
keys, each populated exactly as this section specifies (verbatim transcription where marked
**[RULED — verbatim]**; deterministic operationalization where marked **[OPERATIONALIZED —
bounded]**; no other key's content may deviate from either without failing `matrix-fidelity`,
below):

1. `"contractVersion"` — this contract's own version, starting `"1.0.0"` at first dispatch,
   independent of (and never conflated with) the guidance-content-contract's own
   `v1.0.0`/`v2.0.0` versioning. **[OPERATIONALIZED — bounded]**: a version field is structurally
   required by this parcel's own "Objective" (a reusable, citable artifact); its value is not a
   ruled clause.
2. `"normativeInputs"` — an object naming, by exact file path and the hash each carries in this
   spec's "Lineage and dependencies," every source in "Normative input and authority sources"
   above, so the artifact is self-describing about what it transcribes and from where.
3. `"enablementState"` — the **D-B1(c) staging split**, made machine-explicit
   **[RULED — verbatim, structurally encoded]** (`P0-B-DESIGN-GATE.md` §3, "D-B1," lines 81-91;
   Coordinator Decision D-I, item 1, quoted in "Lineage and dependencies" above):
   ```json
   {
     "biostackRecommendedOrigination": {
       "definedInContract": true,
       "publiclyEnabled": false,
       "currentPosture": "deterministic-math-on-user-entered-values-plus-guidance-contract-v1.0.0-class-A-B-C",
       "governingGuidanceContractVersion": "1.0.0",
       "requiredGuidanceContractVersionForPublicEnablement": "2.0.0",
       "requiredApprovalLevelForPublicEnablement": "legal_product_ratification",
       "publicEnablementEvent": "not-yet-occurred-separate-future-owner-event",
       "rulingReference": "COORDINATOR-DECISIONS-2026-10-07.md#D-I"
     }
   }
   ```
   `"publiclyEnabled"` **must** be the literal boolean `false` in every dispatch of this contract
   until a future, separately gated parcel records the v2.0.0 re-ratification +
   `legal_product_ratification` event and flips it — this parcel does not flip it, schedule its
   flipping, or define the mechanism that will flip it (that is the separate future owner event's
   own gate). A builder who sets `"publiclyEnabled": true`, omits this object, or weakens any of
   its field values fails `enablement-field-fidelity` (Deterministic verification) and is a hard
   stop (see "Hard constraints").
4. `"preemptionOrder"` — **[RULED — verbatim]**, transcribing `P0-B-DESIGN-GATE.md` §3, "D-B2,"
   the paragraph following the per-label table (lines 117-120): *"Multi-label composition follows
   D14's fieldwise fold with this preemption order: `acute-red-flag-or-emergency` →
   `controlled-or-illegal-sourcing` (surface-scoped) → refusal-cap labels
   (`minor-or-age-uncertain`, `pregnancy-or-lactation`, `prescription-treatment-involved` for the
   prescribed treatment) → calibrating labels union their obligations. 'Most restrictive wins'
   only on a genuine scalar conflict; no label erases another's obligations."* Encoded as an
   ordered array of four composition stages exactly matching this text's own four-stage order, plus
   the two trailing sentences stored verbatim as `"compositionNote"`.
5. `"numericProvenance"` — **[RULED — verbatim]**, transcribing `P0-B-DESIGN-GATE.md` §3, "D-B3"
   (lines 121-131), the five locked origins (`user-entered`, `label-or-prescription-transcribed`,
   `source-studied`, `biostack-recommended`, `deterministically-derived` — themselves charter-
   locked, restated at "Product guidance classes," charter, not this gate's own invention) plus the
   four numbered presentation/fail-closed rules verbatim:
   1. Every displayed number carries a machine-readable origin from the five locked values and a
      visible origin marker on dosing-context surfaces.
   2. `biostack-recommended` values additionally require rationale, evidence applicability,
      uncertainty, and risk controls rendered alongside — never as bare numbers.
   3. No prefilled or recommended value may render in a neutral-arithmetic frame (e.g., inside a
      calculator input without its provenance marker).
   4. Missing provenance → the numeric output is **refused** (fail-closed), not downgraded.
6. `"missingInputLadder"` — **[RULED — verbatim]**, transcribing `P0-B-DESIGN-GATE.md` §3, "D-B4"
   (lines 133-140), the three-rung ladder verbatim:
   1. Arithmetic on declared inputs: refuse silently-invalid math (units, plausibility bounds,
      decimal-error flags per Class B); never guess an input.
   2. Recommendation origination (post-v2.0.0): required-input set is function-declared; any
      missing required input → degraded output naming the missingness, or refusal when the
      missingness is safety-material (age, pregnancy status, identity/concentration,
      prescribed-treatment scope).
   3. `PARTIAL_PACKET` / `OUTSIDE_REVIEWED_CONTEXT` markers fire per the v1.0.0 marker vocabulary
      (`docs/guidance/biostack-guidance-content-contract.v1.md`, "Required warning and uncertainty
      language" table — reused by reference, not re-defined).
7. `"functionReviewStatus"` — **[RULED — verbatim]**, transcribing `P0-B-DESIGN-GATE.md` §3,
   "D-B5" (lines 142-150), the four numbered rules verbatim:
   1. `unreviewed` functions are **internal staging only** (mirrors `automated_candidate`) — never
      public-facing output.
   2. Public dosing-context UX requires `legal_product_ratification` against the governing
      contract version (existing v1.0.0 rule, preserved).
   3. `review-required` names the human owner and blocks public enablement until `reviewed`.
   4. Class-triggered merge approvals (health-boundary, privacy, legal-policy, knowledge-promotion)
      continue to fire regardless of any standing Gate 3 authorization (D14 fold, D9).
8. `"escalationSemantics"` — **[RULED — verbatim]**, transcribing `P0-B-DESIGN-GATE.md` §3, "D-B6"
   (lines 152-163), the four numbered rules verbatim:
   1. `acute-red-flag-or-emergency`: output stops ordinary guidance immediately; surfaces urgent
      professional/emergency language; suppresses calculators and dose context.
   2. `prescription-treatment-involved`: alteration-of-treatment surfaces refuse; the professional-
      involvement template escalates; evidence comparison remains available.
   3. Escalation is a distinct output type (class `safety-escalation`), never a footnote on an
      otherwise ordinary recommendation.
   4. Escalation language is drawn from approved templates (v1.0.0 approval levels: high-impact
      safety wording requires `clinical_safety_copy_review`).
9. `"labels"` — one object per closed substance/function-risk label (exactly the ten from
   `classification-axes.schema.json`'s `substanceFunctionRisk.labels`, no more, no fewer), each
   carrying:
   - `"locked"` — `true` for `acute-red-flag-or-emergency` and `controlled-or-illegal-sourcing`
     (charter-locked, "not open at this gate" per `P0-B-DESIGN-GATE.md` §1); `false` for the other
     eight.
   - `"applicabilityCriterion"` — **[OPERATIONALIZED — bounded]**, the deterministic test (below,
     "Per-label applicability criteria") that decides whether this label attaches to a given
     function invocation's declared inputs/outputs. Bounded by the charter's closed vocabulary; it
     operationalizes the label's own name and the D-B2 "Calibration required" column text, and
     invents no new product capability.
   - `"behavior"` — an object keyed `C1`/`C2`/`C3`/`C4` (the four product guidance classes, in the
     charter's own declared order: `deterministic-calculation`, `curated-evidence-guidance`,
     `personalized-protocol-recommendation`, `safety-escalation`), each value one of
     `"allowed"`/`"degraded"`/`"refused"`/`"escalated"`, transcribed **[RULED — verbatim]** from
     `P0-B-DESIGN-GATE.md` §3, "D-B2" table (lines 99-115) — see "Per-label behavior matrix,"
     below, for the full table reproduced in this spec's own text as the required-content floor.
     Per D-B1(c), every `C3` cell in the table below is **additionally** gated by
     `enablementState.biostackRecommendedOrigination.publiclyEnabled`: while `false`, no `C3`
     behavior for any label may be exposed on a public surface regardless of its table value — the
     table records the *designed* behavior, the enablement flag records whether it may *ship*
     publicly yet. This gating relationship is itself `"enablementGatesC3": true` on every label
     object, not asserted once globally, so a partial or future per-label carve-out cannot silently
     bypass it.
   - `"calibrationRequired"` — **[RULED — verbatim]**, the exact "Calibration required" cell text
     from the D-B2 table for this label.

### Per-label behavior matrix (required content floor — transcribed verbatim from
`P0-B-DESIGN-GATE.md` §3, "D-B2," lines 93-120; the builder's JSON/Markdown artifacts must carry
every row forward unweakened, exactly as `seed-regression` does for P0-A's seed findings)

Guidance-class order: **C1** `deterministic-calculation`, **C2** `curated-evidence-guidance`,
**C3** `personalized-protocol-recommendation` (gated by `enablementState`, see above), **C4** is
not a column here because `safety-escalation` is itself the escalated output type D-B6 defines,
not a fourth behavior column on the other three classes — cells read **A** = allowed, **D** =
degraded, **R** = refused, **E** = escalate.

| Label | C1 | C2 | C3 (post-v2.0.0) | Calibration required |
|---|---|---|---|---|
| `ordinary` | A | A | A | none beyond base contract |
| `prescription-treatment-involved` | D | D | **R** (for the prescribed treatment) | no alteration directives; "discuss with prescriber" template; comparison to reviewed evidence allowed |
| `investigational-or-unapproved` | A | D | D | `EVIDENCE_LIMITED`/`EVIDENCE_CASE_REPORT` markers; no "established" framing; mandatory monitoring + uncertainty blocks |
| `gray-market-or-identity-uncertain` | D | D | D | identity/purity uncertainty surfaced; potency-dependent math refused when identity/concentration unverified (missing-input rule) |
| `injection-or-sterile-preparation` | A | D | D | sterility/prep safety context section required; refusal when sterility inputs missing |
| `interaction-or-contraindication-signal` | A | D | D→E | conflict disclosure mandatory (never hidden); "review before proceeding" Class C template; escalate on strong signal |
| `minor-or-age-uncertain` | D | D | **R** | age-dependent inputs refused; `POPULATION_MISMATCH` markers; professional escalation language |
| `pregnancy-or-lactation` | R (dose-context) | D | **R** | no "proceed anyway" framing; mandatory professional escalation |
| `acute-red-flag-or-emergency` | R | R | **R** + **E** | LOCKED — preempts ordinary guidance; urgent professional/emergency language |
| `controlled-or-illegal-sourcing` | A¹ | A¹ | A¹ | LOCKED — suppresses sourcing/evasion/concealment surfaces only; evidence content unaffected |

¹ Except any surface that would source, evade, or conceal — those are refused with no fallback.

`interaction-or-contraindication-signal`'s `C3` cell, `D→E`, is encoded as `"degraded-escalates-
on-strong-signal"` in the JSON artifact (not a fifth behavior literal) — the calibration column's
own text ("escalate on strong signal") is the deterministic trigger: a strong interaction/
contraindication signal (as defined by the function's own declared evidence-source match
threshold) upgrades the cell from `D` to `escalated` at evaluation time; the table's static value
is the floor, not the ceiling, for this one cell, exactly as written in the ruled table.

### Per-label applicability criteria (required content — authored by this parcel, bounded by the
closed vocabulary; **[OPERATIONALIZED — bounded]**, not a ruled D-B1..D-B6 clause)

Each criterion is a deterministic test over a function's own declared capability-contract fields
(per the charter: *"Every user-facing function must declare its ... substance/function risk"*)
evaluated against the function's actual declared inputs/outputs at invocation time. Multiple
criteria may be true simultaneously (multi-label, per D14); `ordinary` is the residual label.

| Label | Deterministic applicability test |
|---|---|
| `ordinary` | True when none of the other nine labels' tests below are true for this invocation. |
| `prescription-treatment-involved` | True when a declared input or output names a substance/treatment that the function's own capability contract, or a `label-or-prescription-transcribed` input, marks prescription-status, or the user has declared it as a currently prescribed/clinician-directed treatment. |
| `investigational-or-unapproved` | True when the cited evidence source's own regulatory-status field states investigational, unapproved, or off-label for the declared use, or no approved-use record exists in the evidence source for the declared use. |
| `gray-market-or-identity-uncertain` | True when a required identity/concentration/manufacturing-source provenance field (per `numericProvenance` and the function's declared inputs) is absent, unverified, or not resolvable to one of the five locked numeric origins. |
| `injection-or-sterile-preparation` | True when the function's declared output type or route is injection, reconstitution, or any sterile-preparation step. |
| `interaction-or-contraindication-signal` | True when a matching interaction/contraindication record exists in the cited evidence source for the user's declared concurrent substances, medications, or conditions. |
| `minor-or-age-uncertain` | True when the user's declared age is below the function's declared minimum-age threshold, or age is a function-declared required input and is missing or unverified. |
| `pregnancy-or-lactation` | True when the user has declared current pregnancy or lactation status, or that status is a function-declared required input and is missing/unverified for a function whose substance or output carries a source-labeled pregnancy/lactation signal. |
| `acute-red-flag-or-emergency` | **LOCKED.** True when declared symptoms, vitals, or context match any function-declared red-flag/emergency criterion; the triggering criterion set is itself function-declared (per D15's function-specific-review model), not invented by this contract. |
| `controlled-or-illegal-sourcing` | **LOCKED.** True whenever the declared request or context seeks sourcing, acquisition, legal/regulatory evasion, or concealment of a controlled-or-illegal substance or its acquisition — evaluated against the request/context, never against the mere mention of a controlled substance's evidence content. |

A criterion that cannot be evaluated deterministically from a function's own declared fields at
invocation time (for example, an undeclared capability-contract field a later function needs)
defers to that function's own missing-input ladder behavior (`missingInputLadder`, rule 2) — it
never causes this contract itself to guess an applicability determination.

### Required document contract 2: `docs/specs/schemas/product-capability-safety-contract.md`

A human-readable companion, exact content-mirror of the JSON artifact (every key, every ruled
clause, every table, in the same order), so a reviewer can verify fidelity by reading prose and
tables rather than parsing JSON. States, at its top, in its own words: *"This document and
`product-capability-safety-contract.json` must never diverge; `matrix-fidelity` (Deterministic
verification) fails closed if they do."* Includes a short "How to read this contract" section
naming the `[RULED — verbatim]` vs. `[OPERATIONALIZED — bounded]` provenance marker convention
this spec's Deliverables section uses, carried forward into the artifact itself so a future reader
(P0-C's builder, a runtime-integration parcel's builder) can tell which clauses are owner-ruled and
immutable without this spec and which are this parcel's own bounded operationalization, reviewable
and amendable at P0-B's own review tier without reopening the owner gate.

## Mandatory class sections (D14 fieldwise union of all four declared delivery classes)

### Health-boundary

- **Guidance class:** `not-applicable` to this parcel's own artifacts (the contract document is
  not itself a user-facing output; it is the definition other functions will declare against). The
  four product guidance classes (`deterministic-calculation`, `curated-evidence-guidance`,
  `personalized-protocol-recommendation`, `safety-escalation`) are, however, the exact subject
  matter the Deliverables section's `"behavior"` object defines for every other function — stated
  explicitly so the field is addressed, not silently conflated with this parcel's own non-status.
- **Intended use:** internal/governance artifact, consumed by future P0-C fixture builders, a
  future runtime-integration parcel, and P3-B's parcel-schema binding. Not served to an end user
  directly; no public route of its own.
- **Claims:** this parcel asserts no product claim beyond what D-I already rules and the charter
  already permits. Every `[RULED — verbatim]` clause is a citation to `P0-B-DESIGN-GATE.md`, never
  an assertion in this spec's own voice; every `[OPERATIONALIZED — bounded]` clause is marked as
  such and is reviewable on its own bounded terms (it decides *how a declared label is detected*,
  never *what the product may do once detected* — that remains the ruled matrix).
- **Evidence threshold:** `not-applicable` in the scientific-evidence-grading sense — this parcel
  grades no scientific claim; "Numeric provenance" and "missing-input ladder," above, are this
  parcel's analogous threshold machinery, for numeric values, not claims.
- **Numeric provenance:** this is the deliverable's own subject matter (`"numericProvenance"`,
  above) — stated here as the field this class requires every health-boundary spec to address, not
  omitted because the content lives in a different section.
- **Missingness:** `unreviewable` (in P0-A's three-condition sense) is reserved for the same three
  deterministic conditions P0-A defines (deleted / unreadable / outside this worktree's checkout)
  and applies identically here to any Normative-input-and-authority-sources file the builder cannot
  read at `BaseCommit`; "Deterministic verification," `source-availability-verified`, re-checks any
  such claim exactly as P0-A's `unreviewable-claim-verified` does.
- **Red flags:** `acute-red-flag-or-emergency` and `controlled-or-illegal-sourcing` are both
  **locked** inputs to this contract (see Deliverables, "labels," `"locked": true`); this parcel
  transcribes their already-ruled, charter-locked preemption/suppression behavior, it does not
  define it anew, and any apparent conflict between a locked label's charter text and this spec's
  transcription is itself a stop condition (below), not a judgment call.
- **Escalation:** a disputed `[RULED — verbatim]`-vs-`[OPERATIONALIZED — bounded]` classification
  between the two independent reviewers, or any reviewer finding that an `"applicabilityCriterion"`
  or `"behavior"` cell is not actually traceable to D-I's ruling or the charter's closed
  vocabulary, stops the parcel and returns to the coordinator for owner escalation (see "Stop
  conditions").
- **Function-review status:** this parcel's own artifacts are `not-applicable` under D15 (not a
  user-facing function). `functionReviewStatus` (the deliverable's own transcribed D-B5 rules) is,
  again, the subject matter this field requires every other function to carry, not this parcel's
  own status.

### Privacy

- **Data inventory:** zero personal data. Every artifact this parcel produces is built from
  already-committed repository canon (the charter, the design-gate decision package, the
  guidance-content-contract, the P2 schema substrate, the coordinator-decisions ledger). No user
  record, profile, protocol entry, or check-in is read, queried, or referenced.
- **Purpose:** internal governance/policy-definition preparation; no purpose touches an individual
  user's data.
- **Consent:** `not-applicable` — no personal data is processed, so no consent basis is required.
- **Minimization:** the artifact quotes ruled clauses in full (they are the contract's own
  normative text, not excerptable without losing meaning) but reproduces no other document beyond
  what "Normative input and authority sources" names.
- **Retention:** this artifact is a permanent, versioned repository document (same posture as every
  other merged spec) — no personal-data retention clock applies because there is no personal data.
- **Export/deletion:** `not-applicable` for the same reason.
- **Access control:** version-controlled with the same repository access control as every other
  governed-delivery document; no additional access tier.
- **Data classification of this parcel's own artifacts:** `internal-engineering, non-personal,
  pre-publication, policy-defining`. The last tag (distinct from P0-A's purely analytical
  classification) records that, unlike P0-A, this parcel's artifacts **are** the thing a later
  runtime surface will enforce against — they remain non-public and non-personal, but they are not
  merely descriptive. A reviewer finding any personal-data token anywhere in this parcel's
  artifacts is a privacy mandatory stop (`leakage`), identical to P0-A's own rule.

### Legal-policy

- **Policy owner:** the owner (Clint Morgan, named throughout
  `COORDINATOR-DECISIONS-2026-10-07.md`) is this parcel's policy owner — D-I is the owner's own
  ruling on this parcel's substantive content, which is a materially stronger policy-owner
  posture than P0-A's "catalogue without adjudicating" framing: **P0-B's content is itself the
  owner's adjudication**, transcribed, not merely read and catalogued.
- **Jurisdiction/scope:** `not-applicable` for any independent legal/regulatory determination of
  this parcel's own (none is made beyond what D-I already rules); this parcel's procedural scope is
  the governed-delivery process defining the product's own output-capability contract.
- **Version/effective date:** `product-capability-safety-contract.json`'s own `"contractVersion"`
  field (`"1.0.0"` at first dispatch) is this artifact's version; it has no "effective date" of its
  own distinct from this parcel's own Gate 3 merge date, because the contract itself enforces
  nothing at runtime until a later, separately gated integration parcel wires it in (see
  "Rollback").
- **Enforcement surfaces:** none, at this parcel. P0-C (fixtures) and an eventual, separately gated
  runtime-integration parcel are the surfaces that will enforce this contract; this parcel defines
  it.
- **Approval status:** `review-candidate` pending two independent reviews and the owner's Gate 3
  merge decision, exactly as this document's header states, and pending the three-step dispatch
  precondition in "Authorization boundary."
- **Cataloguing discipline for the still-governing guidance-content-contract v1.0.0:** this parcel
  never states or implies that v1.0.0's Class D prohibition has changed, been superseded, or been
  weakened. Every reference to `biostack-recommended` origination is explicitly and consistently
  qualified `publiclyEnabled: false` / "post-v2.0.0" / "a separate future owner event" — a
  reviewer finding any sentence in this parcel's artifacts that could be read as claiming public
  Class D availability today is a legal-policy mandatory stop (`unapproved policy presented as
  effective`, folded from `delivery-class-controls.json`).

### Knowledge-promotion

- **Source/license/provenance:** every clause in the Deliverables section cites its exact source
  (file + section/line range) by construction, exactly as P0-A's row schema requires for its own
  citations; this parcel introduces no new licensed or external source.
- **Evidence grade:** `not-applicable` in the scientific-evidence-grading sense; this parcel grades
  no scientific claim.
- **Review lifecycle:** `review-candidate -> active -> done`, the ordinary governed-delivery
  lifecycle (`docs/specs/README.md`), no separate knowledge-promotion lifecycle of its own.
- **Promotion authority:** `product-capability-safety-contract.json`/`.md` are the **sole**
  authoritative encoding of per-label applicability and allowed/degraded/refused/escalated
  behavior; this parcel's "Exact allowed surfaces" fences every write to exactly the files named
  below, and `classification-axes.schema.json`'s three-field update (item 3) is the **only**
  mechanism by which P2's deferred `substanceFunctionRisk` binding becomes `defined` — no other
  file may carry a competing or duplicate binding.
- **Rollback:** every artifact this parcel creates is new and additive, plus the one tightly scoped
  `classification-axes.schema.json` field update and the two append-only registrations
  (`docs/specs/README.md`, `docs/specs/INDEX.md`). Rollback is `git revert` of the merge commit;
  nothing downstream depends on this parcel's output being present at runtime yet, because no
  runtime surface reads or enforces this contract until a later, separately gated integration
  parcel exists.

## Exact allowed surfaces

The builder (at a future, separately gated dispatch) may create or modify only:

1. `docs/specs/schemas/product-capability-safety-contract.json` — new. Required document contract
   1 (above).
2. `docs/specs/schemas/product-capability-safety-contract.md` — new. Required document contract 2
   (above).
3. `docs/specs/schemas/classification-axes.schema.json` — modified, exactly these fields and no
   others: for each of the ten `substanceFunctionRisk.labels` entries under
   `axes.substanceFunctionRisk.applicabilityField.<label>`, `"determinationAuthority"` changes from
   `"deferred-to-P0-B-deterministic-criteria"` to
   `"defined-by-product-capability-safety-contract-v1.0.0"` and `"status"` changes from
   `"deferred"` to `"defined"`; at `axes.substanceFunctionRisk`, `"controlBindingStatus"` changes
   from `"deferred-to-P0-B"` to `"bound-at-this-parcel"` (the literal `deliveryClass` already
   carries) and `"allowedOutputBindingStatus"` changes from `"out-of-scope-for-P2"` to
   `"bound-at-P0-B"`. Zero other diffs: no label added, removed, renamed, or redefined; the
   `deliveryClass` and `productGuidanceClass` axis objects are untouched; `productGuidanceClass`'s
   own `"controlBindingStatus": "deferred-to-P3-B"` is untouched (P3-B's own future job, not this
   parcel's).
4. `docs/specs/scripts/verify-p0b.ps1` — new. The deterministic verifier (see below).
5. `docs/specs/README.md` — modified. Exactly one appended section (`## Product Capability and
   Safety Contract (P0-B)`), zero removed or reordered lines.
6. `docs/specs/INDEX.md` — modified. Exactly one appended table row for `P0-B`, in the existing
   column order, zero removed lines, zero other-row changes, using the closed-vocabulary registry
   cell literal `coordinator-assigns-at-gate-2` for the `Branch/worktree`/`Owner` cells at dispatch
   (P2's realized precedent and P0-A's own current-text usage, both cited in P0-A.md's own "Exact
   allowed surfaces" item 9).

No other path may change.

### Frozen surfaces

The builder must not change or reinterpret:

- The governed-delivery charter, the plan-review record, and every closed/review-candidate P1,
  P2, P3-A, and P0-A artifact.
- `docs/guidance/biostack-guidance-content-contract.v1.md` and `docs/guidance/RATIFICATION.md` —
  this parcel **reuses** v1.0.0's vocabulary and status by reference; it does not edit v1.0.0's
  text, does not ratify v2.0.0, and does not create a v2.0.0 document (a separate, future, owner-
  gated event).
- `docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` and
  `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` — read and cited, never edited; this
  parcel transcribes D-I's ruling, it does not amend the ruling record itself.
- `docs/specs/schemas/delivery-class-controls.json`, `docs/specs/schemas/fold-engine.md`,
  `docs/specs/schemas/routing-output.schema.json`, `docs/specs/schemas/AXIS-REGRESSION-MAP.md`,
  `docs/specs/schemas/SECTION-HEADING-MAP.md`, `docs/specs/schemas/EXTENSION-POINTS.md`, and every
  P2/P3-A fixture — P2's closed substrate beyond the one named `classification-axes.schema.json`
  field update above.
- `docs/specs/schemas/parcel-spec.schema.json` — P3-A's schema; binding it to this parcel's
  capability/claim/provenance/missingness/function-review/escalation fields is **P3-B's** job
  (charter parcel-tree row: *"P3-B: after P0-B freezes the Product Capability and Safety Contract,
  bind the parcel schema to required capability, claim, provenance, missingness, function-review,
  and escalation fields"*), not this parcel's.
- `docs/specs/CORE-CONTEXT.md`, `docs/specs/active/README.md`, `docs/specs/done/README.md`.
- Root `AGENTS.md`.
- `docs/INITIATIVES/biostack-production-readiness/` and its `NO-GO / HOLD` verdict (charter D17).
- `frontend/`, `backend/`, `contracts/`, `.github/` — no runtime code, no product surface, no CI
  workflow. This parcel ships zero code.
- D1-D18, the ratified product doctrine, the four product guidance classes, the ten
  substance/function-risk labels (the vocabulary itself — this parcel defines applicability and
  behavior *for* the closed vocabulary; it does not add, remove, or rename a label), and the
  charter's parcel dependency spine, standing authorizations, stop conditions, and exit criterion.

If an allowed deliverable appears to require any frozen-surface or frozen-contract change, the
builder stops without editing it.

## Hard constraints

- **No deviation from the ruled D-B1..D-B6 matrix.** Every `[RULED — verbatim]` clause in
  Deliverables must match its cited `P0-B-DESIGN-GATE.md` span byte-for-byte (modulo leading/
  trailing whitespace, per the same quotation-mark exemption P0-A's "Real-corpus grounding"
  constraint states). A builder who softens, strengthens, extends, or narrows any ruled cell,
  rule, or order **stops and reports to the owner** — this is never a "reasonable interpretation"
  judgment call for the builder or either reviewer to resolve themselves.
- **No premature public enablement.** `enablementState.biostackRecommendedOrigination
  .publiclyEnabled` must be the literal boolean `false` in this parcel's entire dispatch; no file
  this parcel creates or modifies may state, imply, or be readable as stating that
  `biostack-recommended` origination, or any `C3` behavior cell gated by it, is available on a
  public surface today.
- **No redefinition of the closed label vocabulary.** The ten `substanceFunctionRisk` labels are
  exactly the ten `classification-axes.schema.json` already names; this parcel adds none, removes
  none, and renames none. `"applicabilityCriterion"` operationalizes a label's own name; it does
  not change what the label means.
- **No new personal data, license, or external source.** Every citation is to already-committed
  repository text named in "Normative input and authority sources." No network fetch, no new
  external document, no user data of any kind.
- **No `TBD`.** No file created or modified by this parcel may contain the literal markers `TBD`,
  `TODO`, `FIXME`, or `{{...}}` placeholder syntax, or an unresolved decision. Every field is
  either a real value or an explicit, named `not-applicable` — never a blank or placeholder.
- **Real-corpus grounding, not synthetic examples.** Every `[RULED — verbatim]` quotation must be
  verifiably present, verbatim, in `P0-B-DESIGN-GATE.md` at the cited location at the pinned
  `BaseCommit`. A quotation that does not match byte-for-byte fails validation.
- **`[OPERATIONALIZED — bounded]` traceability.** Every `"applicabilityCriterion"` must cite, in a
  code comment or adjacent prose field, which label name and/or D-B2 "Calibration required" text it
  operationalizes; an applicability criterion with no traceable basis fails `unbounded-
  operationalization` (Deterministic verification).

## Deterministic verification

P0-B has no runtime, no live calculation surface, and no code path to fixture against in the
positive/degraded/refused/escalated sense the health-boundary class's `minimumChecks` assumes for
a product-*shipping* parcel — this parcel *defines* the contract P0-C will later fixture-prove, it
does not itself prove runtime behavior. Per D14 ("No label erases another label's obligations"),
these checks are **adapted**, not waived, to this parcel's contract-defining shape, mirroring
P0-A's own adaptation discipline. Each adaptation is recorded below as a named, deterministic,
scriptable check inside `docs/specs/scripts/verify-p0b.ps1`:

| Folded check (from the four classes' `minimumChecks` union) | P0-B adaptation | Verifier behavior |
|---|---|---|
| positive, degraded, refused, escalated fixtures (health-boundary) | the contract's own label×class matrix must include at least one cell of each of the four states (`allowed`, `degraded`, `refused`, `escalated`/`degraded-escalates-on-strong-signal`) across the ten-label table | fails `missing-behavior-state-coverage` if any of the four states has zero cells |
| calculation boundary tests where numeric (health-boundary) | asserted structural requirement: `numericProvenance` must name exactly the five locked origins and the four numbered rules; no file under this parcel's allowed surfaces may contain an actual formula, rounding rule, or runtime calculation (this parcel defines provenance rules, it performs no calculation) | fails `unexpected-numeric-surface` if any allowed-surface file contains a computation pattern |
| authorization, consent, retention/export/deletion, log-redaction tests (privacy) | grep-based personal-data token scan across every file this parcel creates or modifies, asserting zero matches outside the one permitted named human owner already public in existing governance records | fails `personal-data-token-found` on any match |
| copy-to-enforcement consistency; version/consent linkage tests (legal-policy) | every reference to `biostack-recommended` origination or any `C3` cell is paired with its `enablementState` gate; no sentence asserts public Class D/C3 availability today | fails `enablement-field-fidelity` on any unpaired or contradicted reference |
| provenance/license/freshness; fail-closed promotion; canonical-write fencing tests (knowledge-promotion) | diff this parcel's commit against the allowed-surfaces list; any path outside that list is a fencing violation; every `[RULED — verbatim]` citation resolves to a real file+location at `BaseCommit` | fails `canonical-write-fencing-violation` or `unresolvable-citation` |

Additional, parcel-specific deterministic checks `verify-p0b.ps1` must implement:

- `matrix-fidelity` — `product-capability-safety-contract.json` and `.md` never diverge in
  content; every `[RULED — verbatim]` clause in both files matches this spec's own Deliverables
  text byte-for-byte; every per-label table row (ten labels × the behavior/calibration columns) is
  present, unweakened, relative to the "Per-label behavior matrix" table in this spec's own text
  (mirroring P0-A's `seed-regression`) — the builder may add explanatory detail but may not remove
  or soften a ruled row without a named, reviewed reason recorded alongside it, and such a reason
  is itself a stop condition trigger (below), not a silent edit.
- `enablement-field-fidelity` — `enablementState.biostackRecommendedOrigination.publiclyEnabled`
  is the literal boolean `false`; every other field in that object matches this spec's required
  JSON fragment exactly; no file states or implies public availability today.
- `unbounded-operationalization` — every `"applicabilityCriterion"` cites a traceable basis (label
  name and/or D-B2 calibration text); a criterion with no cited basis, or one that introduces a
  product-behavior decision beyond detection (for example, smuggling an allowed/refused rule into
  an applicability test), fails this check.
- `vocabulary-closure` — the `"labels"` object contains exactly the ten
  `classification-axes.schema.json` `substanceFunctionRisk.labels` values, no more, no fewer, in
  no altered spelling.
- `no-placeholder` — the literal markers `TBD`, `TODO`, `FIXME`, `{{...}}` are absent from every
  file this parcel creates or modifies, reusing P3-A's `noPlaceholderPatterns`
  (`docs/specs/schemas/parcel-spec.schema.json`, read live).
- `axis-binding-diff-scoped` — the diff to `classification-axes.schema.json` touches only the
  exact fields named in "Exact allowed surfaces," item 3; any other diff to that file fails this
  check.
- `source-availability-verified` — every file in "Normative input and authority sources" is
  readable at `BaseCommit`; any that is not is recorded `unreviewable` with the same three-
  condition evidence discipline P0-A's `unreviewable-claim-verified` defines, and independently
  re-checked the same way.
- `no-unattributed-claim` — no sentence in any artifact asserts a product rule in BioStack's own
  voice without a `[RULED — verbatim]`/`[OPERATIONALIZED — bounded]` provenance marker and its
  citation.

## Acceptance criteria

1. `docs/specs/schemas/product-capability-safety-contract.json` and `.md` both exist, are
   non-empty, and `matrix-fidelity` passes (zero divergence between the two files and this spec's
   own required-content floor).
2. `enablementState.biostackRecommendedOrigination.publiclyEnabled` is `false`, every other
   `enablementState` field matches this spec's required fragment, and `enablement-field-fidelity`
   passes.
3. The `"labels"` object carries exactly the ten closed labels, each with `"locked"`,
   `"applicabilityCriterion"`, `"behavior"` (all four `C1`/`C2`/`C3` cells plus the `C4`/
   `safety-escalation` relationship via `escalationSemantics`), and `"calibrationRequired"`
   populated per this spec's required content, and `vocabulary-closure` passes.
4. `preemptionOrder`, `numericProvenance`, `missingInputLadder`, `functionReviewStatus`, and
   `escalationSemantics` are each present and match this spec's required content verbatim, per
   `matrix-fidelity`.
5. `unbounded-operationalization`, `missing-behavior-state-coverage` absence,
   `unexpected-numeric-surface` absence, `personal-data-token-found` absence,
   `canonical-write-fencing-violation` absence, `unresolvable-citation` absence,
   `axis-binding-diff-scoped`, `source-availability-verified`, `no-placeholder`, and
   `no-unattributed-claim` all pass.
6. `docs/specs/schemas/classification-axes.schema.json`'s diff touches only the exact fields named
   in "Exact allowed surfaces," item 3, confirmed both by `axis-binding-diff-scoped` and by direct
   diff review (not solely a verifier-script claim).
7. `docs/specs/README.md` carries exactly one appended section and zero other diffs; `docs/specs/
   INDEX.md` carries exactly one appended row and zero other diffs.
8. No file outside "Exact allowed surfaces" is touched (diff review confirms this directly).
9. Two independent, fresh, read-only reviewers return PASS, and the health-boundary
   (`green-dual-review-required`) and legal-policy (`recorded-human-approval`) merge gates folded
   from D14 are both satisfied before Gate 3 is even requested.
10. The three-step dispatch precondition in "Authorization boundary" (P3-A closed; P0-A closed
    with canon precedence frozen; this spec's own dual review passed) is independently confirmed by
    the coordinator before Gate 2 dispatch — recorded, not assumed.

## Review/gates

- **Reviewer count:** 2 (D14 max-scalar fold over four classes each at 2; also charter D8 for
  architecture/risk-sensitive parcels).
- **Reviewer independence:** fresh-session, frontier-capable, read-only, each receiving only this
  approved spec plus the necessary canon/repo state (`P0-B-DESIGN-GATE.md`, the D-G/D-H/D-I ledger
  entries, the charter, the guidance-content-contract v1.0.0, the P2 schema substrate) — neither
  reviewer sees the other's findings before submitting (charter D7).
- **Merge gates (D14 union of `additionalMergeGate` across the four classes):**
  `green-dual-review-required` (health-boundary) **and** `recorded-human-approval` (legal-policy)
  both apply; privacy and knowledge-promotion contribute no additional merge gate beyond dual
  review. Both conditions must be satisfied before Gate 3; satisfying one does not waive the
  other.
- **Human-approval condition:** per D-G/D-I, the owner's recorded approval is required at merge, in
  addition to (not instead of) the two independent PASS reviews. **Gate 3 merge is the owner's
  decision**, and nothing in D-I's "D-B2-D-B6 as recommended" ruling substitutes for that separate
  merge-time approval.
- **Disagreement handling:** if the two reviewers disagree on any finding (including a disputed
  `[RULED — verbatim]`-vs-`[OPERATIONALIZED — bounded]` classification, a disputed applicability
  criterion's traceability, or a disputed matrix cell), the coordinator reproduces the disputed
  fact directly against `P0-B-DESIGN-GATE.md`'s cited text before triage, per charter D8; an
  irreproducible disagreement is a stop (below), not a coin-flip resolution.

## Stop conditions and standing-authorization tripwire

The builder, reviewers, or coordinator stop and return to the owner when:

- Any artifact would state, imply, or could reasonably be read as stating a product
  allowed-output, applicability criterion, or allowed/degraded/refused/escalated behavior **not
  traceable** to a `[RULED — verbatim]` D-I/`P0-B-DESIGN-GATE.md` clause or a bounded
  `[OPERATIONALIZED — bounded]` criterion under this spec's own traceability rule — this is this
  parcel's clearest tripwire: deciding something D-I did not rule, or deciding it differently than
  D-I ruled it.
- Any file states or implies that `biostack-recommended` origination, or any `C3` cell it gates, is
  publicly available today, or that guidance-content-contract v2.0.0 has been ratified.
- A locked label (`acute-red-flag-or-emergency`, `controlled-or-illegal-sourcing`) would have its
  charter-locked preemption/suppression behavior altered, softened, or made conditional.
- A required normative-input source is missing, unreadable, or its content at `BaseCommit` cannot
  be verified — recorded `unreviewable`, never guessed.
- Two reviewers disagree and the coordinator cannot reproduce and resolve the disputed fact against
  `P0-B-DESIGN-GATE.md`'s cited text.
- Any personal-data token is found in a draft artifact.
- P3-A or P0-A have not reached merged/closed status by the time dispatch (Gate 2) would occur (see
  "Authorization boundary" dispatch precondition).
- A proposed matrix change or applicability-criterion change would itself amount to a charter
  amendment (altering D1-D18, the ratified product doctrine, or the closed label vocabulary) rather
  than a bounded operationalization — per the charter's plan-review triage rule, this reopens
  Gate 1 for that decision.
- P0-C, P3-B, or any P0-D subparcel, or any runtime-integration parcel, is referenced as if already
  authorized or already dispatched — each remains gated on its own, separate, not-yet-granted
  decision; this spec's existence is not that gate.
- Any suggestion, in any artifact, that this parcel's merge constitutes, approximates, or
  advances the production-readiness verdict (charter D17) or any production release decision.

## Rollback

This parcel's artifacts are new, additive files plus one tightly scoped field update to
`classification-axes.schema.json` and two append-only registrations (`docs/specs/README.md`,
`docs/specs/INDEX.md`). Rollback is a direct `git revert` of the merge commit; nothing downstream
depends on these artifacts at runtime, because no product surface reads or enforces this contract
until a later, separately gated parcel (P0-C for fixtures; an eventual runtime-integration parcel)
exists and is itself dispatched. No migration, data backfill, or compatibility window is
implicated.

## What this parcel does not close

Closing P0-B closes the Product Capability and Safety Contract's **definition** only. It does not:

- Enable `biostack-recommended` origination on any public surface (a separate, future,
  guidance-content-contract-v2.0.0-plus-`legal_product_ratification` owner event).
- Prove, with fixtures, that the defined allowed/degraded/refused/escalated behavior is actually
  achievable or that useful guidance survives enforcement — that is P0-C's job, under P0-C's own
  future gate.
- Bind the generic parcel schema to this contract's required fields — that is P3-B's job.
- Wire any runtime surface to read or enforce this contract — no such parcel is named or
  authorized by this spec.
- Waive, mark passing, or alter the production-readiness `NO-GO / HOLD` verdict (charter D17).
- Authorize P0-C, P0-D, P3-B, P9, or any other not-yet-gated parcel.
