# BioStack Governed Delivery Goal Charter

Status: **RATIFIED REVIEW CANDIDATE — implementation dispatch blocked pending plan-review closure**

Review candidate: `GDC-1`

## Normative lineage

- BioStack Governed Delivery Upgrade Directive, SHA-256 `0BDA60A325B5E2520E8ACBB4404C83540AA3F5B55CF60F3CE1E1FE6CF5458BA1`.
- The Coordinator Pattern, SHA-256 `5B537030C2B39E82025630BC534B4CB6647BA701B2207558083C425E7E4CF82D`.
- The Parcel-Driven Development and initiative-coordination skill contracts referenced by the Coordinator Pattern.
- The developer's product-purpose correction and explicit `100% Ratified` decision in the governing task.

`BioStack_spec-driven-development-demo-rev2.html` is explanatory only and is not normative.

This candidate is based on the reconciled repository history containing local `main@012fd5db4596625a7a8e2e98296970abaf9f5afb` and `origin/main@9a74df2279383b3ea8f61094b5ef164c0c6a3950`. The untracked ambient `docs/legal/` content is outside this branch and must be preserved.

## Objective

Adopt the shared Coordinator Pattern and Parcel-Driven Development workflow as BioStack's governed software-delivery system, using a thin BioStack domain overlay for claims, health risk, privacy, provenance, migration, provider, legal, and knowledge-promotion controls.

The adoption must prove that BioStack can carry one material goal from concept through ratification, independent plan review, approved parcel specs, isolated builds, deterministic verification, independent adversarial review, merge, closure, and sealed evidence without creating a competing orchestrator or weakening BioStack's product purpose.

## Ratified product doctrine

BioStack is an evidence-guided harm-reduction and protocol-intelligence product for self-directed users operating in a confused, noisy, and often careless information environment. It is not merely a substance library. Its purpose is to filter noise, preserve relevant context, and provide the most useful, transparent, risk-calibrated information reasonably available to people who may proceed regardless.

BioStack may:

- Curate and rank scientific, regulatory, clinical, mechanistic, and clearly labeled experiential information.
- Separate established, emerging, mechanistic, anecdotal, contradictory, and unsupported claims.
- Design and compare protocol options, including compounds, combinations, sequencing, timing, frequency, schedules, diet, supporting supplements, monitoring, and lower-risk alternatives.
- Make educated, profile-aware recommendations using relevant factors such as age, weight, goals, prior experience, current protocol, medications, conditions, tolerance, symptoms, biomarkers, diet, activity, and longitudinal observations.
- Originate evidence-bounded numerical recommendations, including dose targets, reconstitution choices, and schedules, when the applicable capability contract permits it. Such outputs are recommendations, not prescriptions, and must carry numeric provenance, rationale, evidence applicability, uncertainty, risk controls, and escalation behavior.
- Perform deterministic reconstitution, concentration, dose, split, volume, and syringe-unit calculations and show what a selected value looks like on the syringe.
- Identify conflicts, duplication, source-quality problems, contraindication signals, attribution problems, monitoring gaps, and reasons to pause or seek qualified help.
- Reassess guidance as user context, evidence, or observed outcomes change.

BioStack must not:

- Diagnose a disease or condition.
- Prescribe, claim clinical authority, or impersonate a medical professional.
- Direct a user to alter prescribed treatment without appropriate professional involvement.
- Present uncertain, indirect, stale, or population-mismatched evidence as established fact.
- Present a studied, common, calculated, or recommended value as automatically safe for a specific person.
- Guarantee safety, efficacy, or outcomes.
- Hide uncertainty, material contraindication signals, or source-quality limitations.
- Facilitate illegal sourcing, evasion, or concealment.
- Continue ordinary protocol guidance through an emergency or red-flag state when escalation is required.
- Treat an “at your own risk” disclaimer as a substitute for evidence, validation, privacy, or safety controls.

Governing principle: **High risk requires more evidence, explanation, validation, review, and escalation. It does not automatically require less useful information.**

## Product guidance classes

These classes describe product behavior. They are separate from delivery-governance risk classes and substance/function risk labels.

1. `deterministic-calculation`: math from declared inputs, with formula, units, validation, rounding behavior, and numeric provenance.
2. `curated-evidence-guidance`: evidence-ranked context, ranges, comparisons, uncertainties, and source-quality conclusions.
3. `personalized-protocol-recommendation`: transparent profile-aware recommendations, including eligible numerical, scheduling, support, monitoring, and lower-risk options.
4. `safety-escalation`: deterministic degradation, suppression, urgent warning, or qualified-professional escalation when missing inputs, conflicts, red flags, prescribed-treatment involvement, or evidence limits require it.

Every user-facing function must declare its guidance class, intended user and use, input provenance, output type, substance/function risk, evidence threshold, missing-input behavior, automation level, review status, personal-data use, red-flag behavior, and whether it ranks options or selects a recommended target.

Numeric values must identify one of these origins: `user-entered`, `label-or-prescription-transcribed`, `source-studied`, `biostack-recommended`, or `deterministically-derived`. Presentation must not allow a prefilled or recommended value to masquerade as neutral arithmetic.

The closed initial substance/function-risk vocabulary is:

- `ordinary`
- `prescription-treatment-involved`
- `investigational-or-unapproved`
- `gray-market-or-identity-uncertain`
- `injection-or-sterile-preparation`
- `interaction-or-contraindication-signal`
- `minor-or-age-uncertain`
- `pregnancy-or-lactation`
- `acute-red-flag-or-emergency`
- `controlled-or-illegal-sourcing`

More than one label may apply. P2 defines the schema and fieldwise composition mechanics for label applicability but does not decide product allowed outputs. After P0-A freezes canon precedence, P0-B defines deterministic applicability criteria and allowed, degraded, refused, and escalated behavior for each label; P0-C proves that behavior with fixtures. No parcel may add, remove, or redefine a label in a way that changes allowed product behavior without a charter amendment and review. `acute-red-flag-or-emergency` always preempts ordinary guidance. `controlled-or-illegal-sourcing` always suppresses sourcing, evasion, and concealment assistance. Other labels calibrate evidence, explanation, validation, review, and escalation rather than automatically suppressing useful guidance.

## Locked decisions

### Shared delivery architecture

- **D1 — Shared mechanics remain generic.** The Coordinator Pattern and PDD mechanics remain shared; BioStack does not fork or duplicate them.
- **D2 — Thin BioStack overlay.** BioStack owns only domain rules, contexts, templates, checks, classifications, and evidence requirements.
- **D3 — Repository canon.** Goal Charters, parcel specs, reviews, triage, and closure records are versioned repository artifacts, not raw chat dependencies.
- **D4 — No production dependency.** Production BioStack projects do not depend on orchestration or spec-governance tooling.
- **D5 — One coordinator.** One long-running coordinator owns this goal; ownership transfers only through an explicit written handoff at a parcel boundary.
- **D6 — Isolated work.** Every builder receives one approved parcel, one named branch, and one isolated worktree. Ambient branch or worktree selection is prohibited.
- **D7 — Independent verification.** No agent verifies or approves its own work. Reviewers are fresh-session, frontier-capable, read-only, and receive only the approved spec plus necessary canon and repo state.
- **D8 — Dual review.** Architecture and risk-sensitive parcels receive two independent adversarial reviews. Review disagreement is reproduced by the coordinator before triage.
- **D9 — Written delegation only.** Dispatch and merge can be delegated only through written, scoped standing authorization. Any failed or incomplete verification voids authorization for that parcel.
- **D10 — Keon ownership boundary.** General receipt canonicalization, hashing, chaining, and offline verification remain Keon-owned. BioStack emits privacy-minimized domain facts through a pinned Keon-compatible contract.
- **D11 — Adopt through parcels.** Except for the Stage Zero charter and its plan-review record, this adoption is implemented only through approved parcels.

### Product and safety architecture

- **D12 — Useful guidance is in scope.** Safety governance calibrates useful guidance; it does not collapse BioStack into a passive library.
- **D13 — Personalized numerical guidance is allowed.** BioStack may originate profile-aware dose, reconstitution, schedule, and support recommendations when the function contract, evidence, provenance, validation, uncertainty, and escalation requirements are satisfied. This does not authorize diagnosis, prescribing, clinician impersonation, or unsupervised alteration of prescribed treatment.
- **D14 — Three orthogonal classification axes.** Delivery risk, product guidance class, and substance/function risk are independently recorded and may all be multi-label. Controls compose field by field: union required sections, checks, stop conditions, and evidence; take the maximum reviewer count; allow standing authorization only when every applicable label permits it; apply every triggered human-approval condition; and stop on incompatible controls. “Most restrictive wins” applies only to a genuine scalar conflict. No label erases another label's obligations.
- **D15 — Function-specific review.** Regulatory and legal status is assessed per user-facing function, intended use, user, automation, output, and claim. Broad disclaimers or product-level labels do not settle function-level status. Active specs record `unreviewed`, `review-required`, `reviewed`, or `not-applicable` and name the human owner when review is required.
- **D16 — Evidence namespaces are distinct.** `scientific-evidence`, `recommendation-rationale`, `delivery-evidence`, and Keon receipt facts use distinct contracts and must not be conflated.
- **D17 — Existing release hold is preserved.** This goal cannot supersede, waive, or mark passing any gate in `docs/INITIATIVES/biostack-production-readiness/`. Governed-delivery closure is not production-readiness evidence unless a release gate explicitly accepts it.
- **D18 — Capstone is a governed integration goal.** P9 is an umbrella capstone with subordinate parcels. It may advance a real product initiative, but it may not hide feature expansion merely to demonstrate the framework or mutate the production-readiness verdict.

## BioStack governance overlay

Risk classification is multi-label. Composition uses D14's deterministic fieldwise fold: unions for additive obligations, maximum reviewer count, intersection for authorization eligibility, all triggered human approvals, and stop on incompatibility.

| Delivery class | Required spec additions | Minimum deterministic checks | Reviewers | Standing authorization eligibility | Mandatory stop conditions | Required closure evidence |
|---|---|---|---:|---|---|---|
| `standard` | objective, surfaces, contracts, acceptance criteria, tests, rollback | focused tests; lint/build as affected | 1 | dispatch and merge eligible when named | scope/contract drift; red check | checks, review, acceptance map, diff |
| `health-boundary` | guidance class; intended use; claims; evidence threshold; numeric provenance; missingness; red flags; escalation; function-review status | positive, degraded, refused, escalated fixtures; calculation boundary tests where numeric | 2 | eligible only when explicitly named; green dual review required | unsupported certainty; prescribed-treatment direction; red-flag bypass; provenance loss | scientific-evidence references, rationale fixture, safety results, dual reviews, human review record when triggered |
| `privacy` | data inventory; purpose; consent; minimization; retention; export; deletion; access control | authorization, consent, retention/export/deletion, log-redaction tests | 2 | eligible only when explicitly named | new sensitive field without approved lifecycle; leakage; consent bypass | data map, test results, migration/rollback proof, dual reviews |
| `migration` | compatibility window; forward/backward behavior; rollback; data-loss analysis; environment plan | migration, downgrade/rollback or restore, compatibility tests | 2 when user data or production schema is affected; otherwise 1 | eligible only with explicit base and environment scope | destructive or irreversible path; unknown production state | schema diff, compatibility matrix, backup/restore or rollback evidence, environment receipt |
| `trust-path` | trust boundary; fail-open/closed behavior; issuer/verifier ownership; redaction; external contract version | contract fixtures; failure-path; tamper; privacy-minimization tests | 2 | eligible only when explicitly named | synthetic/local receipt represented as authoritative; Keon ownership breach | pinned contract, consumer compatibility, failure evidence, dual reviews |
| `provider-pilot` | pilot population; role/consent; permitted workflow; retention; SLA/owner; prohibited clinical behavior | role, consent, rate-limit, retention/deletion, audit tests | 2 | dispatch eligible when named; merge requires explicit human owner acknowledgment | clinical-workflow expansion; missing retention/SLA owner; consent failure | pilot contract, operator ownership, privacy evidence, dual reviews |
| `legal-policy` | policy owner; jurisdiction/scope; version/effective date; enforcement surfaces; approval status | copy-to-enforcement consistency; version/consent linkage tests | 2 | dispatch eligible when named; merge requires recorded human approval | unapproved policy presented as effective; policy/enforcement mismatch | approved version, owner record, effective-date mapping, dual reviews |
| `knowledge-promotion` | source/license/provenance; evidence grade; review lifecycle; promotion authority; rollback | provenance/license/freshness; fail-closed promotion; canonical-write fencing tests | 2 | eligible only when explicitly named | missing source/license/review state; bypassed promotion; unreviewed public claim | source manifest, promotion decision, receipts, rollback evidence, dual reviews |

Specialized context is injected only when a parcel's declared classes and surfaces require it. Always-loaded context remains minimal.

## Parcel tree and dependency order

`P0` denotes product-doctrine recovery priority, not execution order. Its implementation must wait for the minimum governance spine that will govern it.

1. **P1 — Governance bootstrap** (`standard`, architecture): materialize Goal Charter lifecycle, directory conventions, `docs/specs/INDEX.md`, active/done directories, minimal core pointers, and repository-agent pointers.
2. **P2 — Risk taxonomy and routing mechanics** (architecture, risk-sensitive): define all three classification-axis schemas without inheriting every delivery-class label; encode the closed label vocabulary, applicability fields, D14's fieldwise fold, required sections, checks, reviewer counts, authorization eligibility, stops, and closure evidence. P2 does not bind substance/function labels to product allowed outputs. It requires two independent reviews and remains within the P1-P7 standing authorization only while it changes governance mechanics and no product allowed-output decision.
3. **P3 — Parcel contract and templates** (`standard`, architecture), split at the capability-contract boundary:
   - **P3-A:** generic extensible parcel schema, explicit no-`TBD` rule, templates, and extension points. It must not invent product capability semantics.
   - **P3-B:** after P0-B freezes the Product Capability and Safety Contract, bind the parcel schema to required capability, claim, provenance, missingness, function-review, and escalation fields.
4. **P4 — Deterministic validation** (`standard`, architecture): dependency-light spec linter plus focused positive and negative fixtures, including multi-label and missing-field cases.
5. **P6 — Closure and evidence contracts** (`trust-path`, architecture): acceptance-to-delivery-evidence mapping, review-record contract, closure manifest, and distinct evidence namespaces.
6. **P7 — Independent review enforcement** (`trust-path`, architecture): one- and two-review enforcement, reviewer independence/read-only records, disagreement reproduction, and rework tripwires.
7. **P0 — Product Doctrine Recovery** (`health-boundary`, `privacy`, `legal-policy`, `knowledge-promotion`, architecture), split into independently dispatchable subparcels:
   - **P0-A:** canon precedence and contradiction inventory.
   - **P0-B:** machine-readable Product Capability and Safety Contract, including per-label applicability and allowed/degraded/refused/escalated behavior, numeric provenance, and function-review status.
   - **P0-C:** policy fixtures proving the P0-B allowed, degraded, refused, and escalated behavior and that useful guidance survives enforcement.
   - **P0-D:** reconciliation umbrella governed by P0-A's frozen precedence manifest; it is not dispatched to one builder. Its non-overlapping serialized parcels are:
     - **P0-D1:** core product doctrine, accepted ADR, and canonical protocol-intelligence policy.
     - **P0-D2:** scientific-evidence methodology and safety guardrails, after P0-D1.
     - **P0-D3:** product contract plus README, marketing, provider, and other user-facing copy, after P0-D2.
     - **P0-D4:** enforcement and regression tests, after P0-D3.
8. **P5 — CI governance** (`standard`, architecture): active-spec dispatch guard and serialized governance checks on shared CI surfaces.
9. **P8 — Keon-compatible BioStack receipt adapter** (`trust-path`, `privacy`): consume a pinned Keon-owned contract fixture/version and map only privacy-minimized BioStack domain facts. No generalized receipt infrastructure.
10. **P9 — End-to-end capstone goal** (multi-class umbrella): run one real BioStack initiative through the complete governed chain. The ratified product north star is profile-aware protocol guidance to evidence-ranked options, schedule/support planning, reconstitution/dose calculation, syringe visualization, monitoring, and reassessment. **P9-0 capability inventory and gap classification precedes target scope and Gate 2.** Existing capabilities may enter the capstone directly. Missing capabilities enter only when they have independent product value, explicit acceptance criteria, non-overlapping subordinate specs, and explicit Gate 2 approval; they do not become governed-delivery exit requirements merely because they appear in the north star. Subsequent subordinate parcels cover only the approved target: profile/privacy, scientific-evidence and rationale, numeric provenance/calculation, escalation, frontend/backend integration, and environment scenarios. P9 is never one builder parcel.

Dependency spine:

`plan-review closure -> P1 -> P2 -> P3-A -> P0-A -> P0-B -> P3-B -> P4 -> P6 -> P7 -> P0-C -> P0-D1 -> P0-D2 -> P0-D3 -> P0-D4 -> P5`

`P8` may begin after P7 when its pinned external contract and privacy mapping are approved. `P9-0` begins only after P5 and P8 close. The remaining P9 target is fixed only after P9-0 and explicit Gate 2 approval of its non-overlapping subordinate parcel set.

## Standing authorizations

The developer's ratification carries forward the previously proposed standing authorization for **P1 through P7 only**, subject to all of these contingencies:

- Formal plan review of this exact candidate hash is closed.
- Each parcel has an approved spec, named isolated branch/worktree, builder, required checks, and independent reviewer count.
- Dispatch remains inside the named parcel surfaces and carries no external production effect.
- Merge is permitted only when every deterministic check, required review, rework check, acceptance-evidence item, and coordinator reproduction is green and complete.
- Any failed, missing, disputed, stale, or wrong-environment verification voids authorization for that parcel.

No standing dispatch or merge authorization is inferred for P0, P8, P9, production deployment, legal-policy effectiveness, regulatory determinations, data deletion, provider-pilot expansion, or changes to the existing production-readiness verdict. Those require an explicit human gate.

## Stop conditions

The coordinator stops and returns to the developer when:

- A locked decision or frozen contract would change.
- A parcel needs surfaces, permissions, or external effects outside its approved spec or standing authorization.
- A product function would cross diagnosis, prescribing, clinician impersonation, prescribed-treatment alteration without professional involvement, illegal sourcing/evasion, or emergency/red-flag bypass boundaries.
- A required legal, regulatory, clinical-safety, privacy, security, or production owner is absent.
- A security or privacy finding cannot close inside the parcel.
- A deterministic tripwire fires twice on the same parcel.
- Two reviews disagree and the coordinator cannot reproduce and resolve the disputed fact.
- A verification step is red, incomplete, stale, synthetic where live evidence is required, or run in the wrong environment.
- The pinned Keon contract is missing or would require BioStack to own generalized receipt mechanics.
- The reconciled base, branch, worktree, or preservation list is uncertain.
- The authorized queue is empty.

## Explicit exclusions

- No duplicate Foreman, coordinator runtime, or generalized verifier in BioStack.
- No production dependency on governance tooling.
- No raw chat transcript as repository canon.
- No self-review or reviewer fixes.
- No ambient worktree or branch selection.
- No vague acceptance criteria or unresolved `TBD` in active specs.
- No broad refactor without an approved parcel.
- No merge from a red or incomplete chain.
- No production feature expansion whose only purpose is to demonstrate the framework.
- No alteration of existing production-readiness, legal, privacy, consent, retention, provider, or security gate status without that gate's named owner and environment-specific evidence.

## Measurable goal exit

This governed-delivery goal closes only when all of the following are true:

1. P1-P8 and every required P0 subparcel are merged with approved specs, isolated build records, deterministic checks, required independent reviews, acceptance maps, and closure manifests.
2. The eight delivery classes and three classification axes are deterministically enforced in active specs and CI.
3. P9 completes one material initiative through idea, charter, plan review, subordinate parcel specs, isolated builds, deterministic verification, adversarial review, rework where required, PR, merge, closure, and sealed evidence.
4. Positive, degraded-input, high-risk, numeric-provenance, and red-flag scenarios prove both usefulness and safety boundaries.
5. P8 compatibility evidence proves BioStack emits only domain facts through a pinned Keon-compatible contract without owning Keon's generalized receipt mechanics.
6. Production BioStack builds and runs without orchestration/spec-governance dependencies.
7. The final report distinguishes governed-delivery success from the still-independent production-readiness release verdict.

## Plan-level adversarial review and triage

Before P1 shaping or dispatch, two fresh, independent, read-only frontier reviews must inspect this exact file hash, the normative lineage, and current repository canon. Each review must attempt to disprove decomposition coherence, parcel independence, dependency order, classification composition, product-purpose preservation, claim/evidence/privacy controls, authorization scope, stop conditions, and exit measurability.

Each reviewer returns ranked findings, exact evidence, smallest amendment, whether a locked decision changes, missing parcels, collisions, and unknowns. Reviewers do not see each other's work.

The coordinator reproduces disputed findings and records every item as `fix`, `accept-as-documented`, or `informational`. If a fix changes D1-D18 or the ratified product doctrine, Gate 1 reopens only for that decision. Otherwise the candidate may be amended without re-ratification and reviewed again at its new hash. P1 may be shaped only after all blocking plan findings are closed.
