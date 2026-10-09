# Parcel-spec templates

This directory holds one starting-point Markdown template per charter delivery class, plus this
README. Each template is a complete, standalone `coordinator-parcel`-shape scaffold that
independently validates against
[`../schemas/parcel-spec.schema.json`](../schemas/parcel-spec.schema.json) once its sanctioned
`[REPLACE: ...]` fill-in markers are replaced with concrete values. No other file may be added
under this directory by this parcel.

## Choosing a starting template

Pick the template whose filename matches your spec's primary delivery class:

- `parcel-template.standard.md`
- `parcel-template.health-boundary.md`
- `parcel-template.privacy.md`
- `parcel-template.migration.md`
- `parcel-template.trust-path.md`
- `parcel-template.provider-pilot.md`
- `parcel-template.legal-policy.md`
- `parcel-template.knowledge-promotion.md`

Copy the file, replace every `[REPLACE: ...]` marker with real content, and set `delivery_classes`
to the actual class or classes your spec declares.

## Declaring more than one delivery class

A spec may declare more than one `delivery_classes` label. `parcel-spec.schema.json`'s
`requiredSectionDerivation: "fold-live"` unions every declared class's `requiredSpecAdditions` set
(via `fold-engine.md`'s `union-set` resolution against `delivery-class-controls.json`) — the
required sections for a multi-label spec are the union of every applicable template's required
headings, not the headings of any single template alone. A template is a starting point, not a
ceiling: when declaring multiple classes, open each applicable template and combine every required
heading from each into the one spec, replacing duplicate headings (a term shared by two classes,
for example `rollback` in both `standard` and `migration`) with a single heading satisfying both.

## Guidance classes and substance/function risk never add a required section

Declaring any `guidance_classes` or `substance_function_risk` label never adds a required section
under this schema — this is the same pass-through stance P2's `fold-engine.md` already pins for
these two axes. These two fields are recorded for passthrough and closed-vocabulary membership
only; binding them to concrete content obligations is deferred to P3-B (`guidance_classes`) and
P0-B (`substance_function_risk`) respectively, per `parcels/P3-A.md`'s Hard constraints.

## Using the `domain-overlay-insertion` extension point

Every template ships a literal `## Extension points used` heading whose body is the literal
sentence `None.`, because no template presupposes a domain-overlay or future-class extension. A
spec author adds that section's real content only once a later parcel's own approved spec defines
an entry in `parcel-spec.schema.json`'s `extensionSections` registry object (currently `{}`) — see
[`../schemas/EXTENSION-POINTS.md`](../schemas/EXTENSION-POINTS.md)'s `domain-overlay-insertion`
subsection for the exact, bounded mechanic. Reference the extension-section key by name in your
spec's frontmatter only once it exists in that registry; a reference to a key not present in the
registry fails validation with `unknown-extension-point`.
