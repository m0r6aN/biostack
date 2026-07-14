# KEO-74 source registry v2 gates

## Deterministic scaffold

The executable source registry now requires each source to declare:

- identity and exact aliases;
- rights review state and use boundaries;
- operational and acquisition state;
- evidence and provenance policies;
- refresh and remediation procedures; and
- data storage, redistribution, privacy, and training boundaries.

The worker resolves only an exact `identity.sourceId` or an explicitly declared
`identity.aliases` value. The three headline activation states must all be satisfied:

1. `rights.reviewStatus` is `approved`;
2. `operations.status` is `active`; and
3. `acquisition.enabled` is `true`.

Those flags are necessary but not sufficient. Runtime authorization and the v2
schema also require recorded legal basis and terms, verification evidence,
allowed uses, operational and security owner roles, a reviewed non-`none`
acquisition plan, provenance fields, an active refresh cadence, substantive
remediation procedures and contact role, at least one permitted content class,
and at least one authorized evidence use. Empty or malformed activation evidence
fails closed even if the three headline flags are set.

The 13 pilot entries are deliberately `pending-human-legal`, disabled, and
non-acquiring. Their retained evidence classifications and technical capability
notes are provisional metadata, not permission to retrieve, store, transform,
cite, redistribute, train on, or promote source content.

## Human activation gates

Before changing any real-world source to an enabled state, humans must record:

- the legal basis or license and applicable terms URL;
- allowed and prohibited uses, including redistribution and derived-data limits;
- robots, API-terms, account, authentication, and rate-limit decisions;
- an operational owner role and security owner role;
- required source-specific provenance and citation identifiers;
- refresh cadence and staleness behavior;
- correction, retraction, and removal procedures; and
- the permitted content boundary for storage, display, export, and training.

No named owner or legal conclusion is supplied by this scaffold.

## Canonical ingest and intake binding

Binding is intentionally not implemented in this lane.

The admin canonical ingest endpoint currently accepts a bulk substance file plus
an override receipt, but the request has no source-registry identity or
per-record provenance envelope. The transcript intake lane persists source type
and URL, but no exact registry ID, registry-version/hash snapshot, or rights
state. Adding a partial check at either endpoint would create an apparent gate
that downstream records can bypass.

A safe follow-up must change and test the complete contract:

- require an exact registry ID for intake and canonical promotion;
- persist the registry version/hash and rights state used at retrieval;
- require every promoted record to carry the registry provenance envelope;
- reject disabled, stale, unknown, and ambiguous sources before external calls
  and before canonical writes;
- add the same evidence to durable governance receipts; and
- migrate or explicitly quarantine existing rows that lack those fields.

Until that contract is complete, the v2 pilot registry remains default-deny and
does not enable acquisition or canonical ingestion.
