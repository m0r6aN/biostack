# Scenarios

## Parser

- Alias matching must respect token boundaries and preserve multi-compound segments.
- Extraction regexes must be bounded and culture-independent.
- Dose parsing must preserve supported units and decimal precision.

## Interaction

- Avoid/review relationships must not be preempted or weakened.
- Canonical self-pairs must be eliminated.
- Confidence must remain finite and within its declared range.

## Evidence/provenance

- Failed research jobs must not become observational/source-attributed evidence.
- Synthetic identifiers alone must not satisfy source attribution.

## Sidecar lifecycle

- Terminal job state must be monotonic across timeout and worker completion.
- Request constraints must actually bound submitted work.

## Outbound-data boundary

- Raw document bytes, compound/goal data, and research prompts require explicit authenticated/consented outbound controls.
- Tests use intercepting fakes and must prove that no real network call occurs.
