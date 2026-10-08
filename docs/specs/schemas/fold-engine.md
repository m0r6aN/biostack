# D14 Fold Engine — Deterministic Fieldwise Composition

Status: normative specification for BioStack's risk-taxonomy composition mechanics (charter
decision D14). This document specifies the deterministic, fieldwise fold that turns a spec's
declared classification-axis labels into a single routing output. It does not implement any
product allowed-output decision.

The fold consumes:

- a declared label set (`deliveryClasses`, `guidanceClasses`, `substanceFunctionRisk`) plus
  `conditionalInputs`, shaped as in a fixture's `input` object;
- the closed label vocabularies and `applicabilityField` metadata in
  `classification-axes.schema.json`;
- the delivery-class control content in `delivery-class-controls.json`.

It produces a `routing-output.schema.json`-shaped object.

Recording a product-guidance-class or substance/function-risk label adds no required sections, checks, reviewer weight, standing-authorization change, or stop condition under P2.

A spec receives only the controls folded from the delivery-class labels it actually declares, never every delivery-class label's content by default.

## Field-type taxonomy

Every field in `delivery-class-controls.json` (plus the reserved synthetic field used only by the
`negative-synthetic-scalar-conflict` fixture) is bound to exactly one of these four field types:

- **`union-set`** — `requiredSpecAdditions`, `minimumChecks`, `mandatoryStopConditions`,
  `requiredClosureEvidence`, `additionalMergeGate`. For every selected delivery class, the fold
  collects these fields into a deduplicated set, dropping `null` values (an unset
  `additionalMergeGate` contributes nothing to the union). Each `union-set` input field is renamed
  on the routing output: `requiredSpecAdditions` stays `requiredSpecAdditions`, `minimumChecks`
  becomes `requiredChecks`, `mandatoryStopConditions` stays `mandatoryStopConditions`,
  `requiredClosureEvidence` stays `requiredClosureEvidence`, and `additionalMergeGate` becomes
  `mergeGates`. These are the only two renames in the fold
  (`minimumChecks`→`requiredChecks`, `additionalMergeGate`→`mergeGates`); every other field keeps
  its input name on the output.
- **`max-scalar`** — `reviewers`. For `migration`, first resolve its conditional sub-rule using
  the caller-supplied `conditionalInputs.userDataOrProductionSchemaAffected` boolean (missing
  input when `migration` is selected is a **stop**, not a default); then take the maximum resolved
  integer across all selected classes.
- **`intersection-boolean`** — `dispatchEligibility`/`mergeEligibility`. Resolve each selected
  class's literal to a boolean (`"eligible-when-named"` resolves to `true` once the parcel is in
  fact explicitly named at Gate 2, which the fold assumes as a precondition of being invoked at
  all). The fold computes two separate intersections: `dispatchIntersection` is the AND of every
  selected class's resolved `dispatchEligibility` boolean, and `mergeIntersection` is the AND of
  every selected class's resolved `mergeEligibility` boolean. The single output field
  `standingAuthorizationEligible` is then `dispatchIntersection AND mergeIntersection` — the fold
  never exposes `dispatchIntersection`/`mergeIntersection` separately; only their conjunction is
  emitted. Today all 8 classes resolve `true` on both fields, so both intersections and their
  conjunction are always `true` — this field type exists so a future class that is *not* eligible
  on either field can correctly veto standing authorization without a fold-engine redesign.
- **`scalar-exact-match-required`** — reserved for any future field where two selected classes
  must assert identical literal values (no such field exists in `delivery-class-controls.json`
  today, since the charter's only scalar is `reviewers`, which is `max-scalar`, not exact-match).
  This type exists for forward compatibility (e.g., a future per-label allowed-output
  determination, which is explicitly out of scope for P2) and is proven only by the synthetic
  fixture `negative-synthetic-scalar-conflict.json`, which exercises the engine directly with a
  fixture-only field named `testOnlyScalarField` that is not part of the real taxonomy.

## Algorithm

The fold proceeds in this exact order:

1. Validate every declared label against its axis's closed vocabulary (from
   `classification-axes.schema.json`); reject with `unknown-label` on any miss.
2. Reject with `empty-required-axis` if `deliveryClass` has zero declared labels (every spec must
   declare at least `standard`).
3. For every `scalar-exact-match-required` field present among the resolved per-class control sets
   (including the synthetic `testOnlyScalarField` in fixture-only contexts), if selected classes
   disagree, stop with `incompatible-controls` and compute nothing further.
4. Resolve `max-scalar` fields, stopping with `missing-required-field` if a required conditional
   sub-input is absent.
5. Union the `union-set` fields.
6. Intersect `intersection-boolean` fields — separately AND every selected class's
   `dispatchEligibility`, separately AND every selected class's `mergeEligibility`, then AND those
   two intersection results together to produce the single `standingAuthorizationEligible` value.
7. Pass `productGuidanceClass` and `substanceFunctionRisk` declared labels straight through into
   `recordedGuidanceClasses` / `recordedSubstanceFunctionRisk` without adding obligations.
8. Emit the routing output.

## Stop on incompatible controls

A stop halts the fold before any further field is resolved and produces a `"stopped"` routing
output with a `stopReason` object naming exactly one of `"unknown-label"`,
`"empty-required-axis"`, `"incompatible-controls"`, or `"missing-required-field"`, plus
reason-specific detail keys (for example the offending label and axis for `unknown-label`, or the
missing conditional-input field name for `missing-required-field`). "Most restrictive wins"
applies only to a genuine scalar conflict (`scalar-exact-match-required`); it is never used to
silently pick a winner for `union-set` or `max-scalar` fields, which compose deterministically by
union and maximum respectively instead.

## Guidance-class and substance/function-risk pass-through

`productGuidanceClass` and `substanceFunctionRisk` labels are recorded verbatim on the routing
output (`recordedGuidanceClasses`, `recordedSubstanceFunctionRisk`) and are never folded into
`requiredSpecAdditions`, `requiredChecks`, `mandatoryStopConditions`, `requiredClosureEvidence`,
`mergeGates`, `reviewerCount`, or `standingAuthorizationEligible`. Recording a
product-guidance-class or substance/function-risk label adds no required sections, checks,
reviewer weight, standing-authorization change, or stop condition under P2. Binding these two axes
to concrete controls is deferred to P3-B (`productGuidanceClass`) and P0-B
(`substanceFunctionRisk`) respectively; P2 proves only that the pass-through is lossless and
inert.
