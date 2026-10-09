# EXTENSION-POINTS — P3-A's three named, closed-vocabulary extension points

This document declares exactly three named extension points for the generic parcel-spec contract
(`parcel-spec.schema.json`). Each extension point names precisely which file(s) a future parcel
may append to and what it may never touch. No extension point defines, binds, or implies any
product capability, claim, evidence, or allowed/degraded/refused/escalated semantic — that
remains P3-B's (guidance class) and P0-B's (substance/function risk) exclusive territory, per
`parcels/P3-A.md`'s Frozen-surfaces and Hard-constraints sections.

Each extension point's subsection states, verbatim, the sentence: "This extension point adds no required section, check, reviewer weight, standing-authorization change, or stop condition by existing; it only becomes active when a future parcel's own approved spec uses it."

## `delivery-class-extension`

For a future charter amendment adding a ninth+ delivery class.

**Exact mechanic:** the new class is added to `classification-axes.schema.json`'s
`deliveryClass.labels` and a new top-level key in `delivery-class-controls.json` (both P2-owned,
frozen to P3-A, and requiring their own charter-amendment review — P3-A does not perform this).
Because `parcel-spec.schema.json` derives required sections live (fold-live), no P3-A file changes
automatically once those two P2-owned files are amended, **except** that any genuinely new
`requiredSpecAdditions` term the new class introduces must be appended, additively, as one new
row in `SECTION-HEADING-MAP.md` — this is the one sanctioned, bounded edit this extension point
permits to an otherwise-frozen P3-A file, and it is additive-only (no existing row may be
removed, renamed, or reordered).

This subsection pins, verbatim, the sentence: "A newly appended `SECTION-HEADING-MAP.md` row's normalized term (same normalization as document contract 2, including the anti-heading-soup constraints) must not duplicate any term already present in the live `requiredSpecAdditions` union, and no existing row may be removed, renamed, value-mutated, or reordered by this mechanic; the amending parcel's own deterministic verifier must assert this append-only, non-duplicating invariant as a named check, failing `extension-point-not-additive` on violation."

This extension point adds no required section, check, reviewer weight, standing-authorization change, or stop condition by existing; it only becomes active when a future parcel's own approved spec uses it.

## `domain-overlay-insertion`

The single named seam where a thin domain overlay (D2) attaches without redesigning
`parcel-spec.schema.json`; which future parcel(s) use this seam, and for what purpose, is
determined entirely by that future parcel's own approved spec, not by this one.

**Exact mechanic:** `parcel-spec.schema.json` declares a reserved, currently-empty closed registry
object `extensionSections: {}` (present in the schema file as an explicit empty object, not an
unresolved placeholder). A future parcel operating under its own approved spec may append a new named key to this
object (for example a generic, non-product-semantic delivery-class-adjacent label, to be named and
defined entirely by that future parcel's own approved spec — not a product capability, claim,
guidance-class, or substance/function-risk binding, which remains P3-B/P0-B's exclusive territory
per this parcel's own Frozen-surfaces and Hard-constraints sections) binding it to an additional
required-section term, but may not alter any other key already present. **Non-binding topology
note:** the one-key-to-one-required-section-term shape just described is this extension point's
minimal, illustrative default only, not a structural ceiling this parcel freezes. A future
parcel's own approved spec may extend or redesign the internal shape of what an `extensionSections`
key binds to (for example binding one key to more than one required-section term, or to a
conditional or cross-axis rule) without that redesign being treated as a violation of this
parcel's additive-only, frozen-surface guarantee for `EXTENSION-POINTS.md` — the guarantee this
parcel freezes is the *existence* of the `extensionSections` seam and the
disjointness/non-removal invariant below, not the internal shape of a future binding. A spec that
declares a reference to an extension-section key **not present** in this registry fails validation
with the named error `unknown-extension-point` — this is the mechanic the
`negative-unknown-extension-point` fixture proves.

This subsection pins, verbatim, the sentence: "A newly appended `extensionSections` key's normalized form (same normalization as `SECTION-HEADING-MAP.md`) must not equal any term already present in the live `requiredSpecAdditions` union at append time, nor equal any other `extensionSections` key; no existing `extensionSections` key may be removed, renamed, or value-mutated by any future append; and the appending parcel's own deterministic verifier must assert this disjointness-and-non-removal invariant as a named check, failing `extension-point-not-additive` on violation."

This extension point adds no required section, check, reviewer weight, standing-authorization change, or stop condition by existing; it only becomes active when a future parcel's own approved spec uses it.

## `template-set-extension`

The mechanic for adding a new per-delivery-class template.

**Exact mechanic:** a new file named exactly `docs/specs/templates/parcel-template.<label>.md`,
where `<label>` must already exist in `classification-axes.schema.json`'s `deliveryClass.labels`
at the time of addition (closed-vocabulary gate — an invented, non-charter label is rejected, not
silently accepted). No existing template file is reordered or renamed by this mechanic.

This extension point adds no required section, check, reviewer weight, standing-authorization change, or stop condition by existing; it only becomes active when a future parcel's own approved spec uses it.
