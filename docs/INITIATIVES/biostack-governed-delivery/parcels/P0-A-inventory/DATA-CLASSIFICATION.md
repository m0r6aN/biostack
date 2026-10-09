# P0-A Data Classification

Schema: `biostack.p0a-data-classification.v1`

## Classification

Every artifact this parcel creates or modifies (`docs/specs/schemas/canon-precedence.md`, this
directory's four files, `docs/specs/scripts/verify-p0a.ps1`, and the two append-only
registrations to `docs/specs/README.md`/`docs/specs/INDEX.md`) is classified:

**`internal-engineering, non-personal, pre-publication`.**

This matches the P0-A parcel spec's Privacy section ("Data classification of the inventory
artifacts themselves"): these are not public product surfaces, are not marketing copy, and must
not be linked from any public route.

## Why this classification applies

- **Zero personal data.** Every artifact is built from already-committed repository text
  (charter, specs, product docs, audit, canon, contract files). No user record, profile, protocol
  entry, check-in, or any other end-user-originated data is read, queried, or referenced.
- **Internal, coordinator- and reviewer-facing.** Output is consumed only by the governed-delivery
  coordinator and by future P0-D subparcel builders; it is never served to an end user and
  carries no public route.
- **Pre-publication.** This parcel's artifacts are new repository documents pending dual review
  and the owner's Gate 3 merge decision; they are not yet merged, let alone published to any
  product surface.

## Grep-based verification method (re-runnable by a reviewer)

The deterministic verifier (`docs/specs/scripts/verify-p0a.ps1`, check `personal-data-token-found`)
scans every file this parcel creates or modifies for personal-data token patterns and asserts zero
matches outside the one permitted named human owner already public in existing governance records
(`Clint Morgan`, who appears throughout `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`,
`docs/guidance/RATIFICATION.md`, and other already-public governance records this parcel quotes
from).

A reviewer can re-run the equivalent scan directly:

```bash
# Email-address pattern (RFC-5322-ish, simplified) across every file this parcel's allowed
# surfaces list names:
grep -nEI '[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}' \
  docs/specs/schemas/canon-precedence.md \
  docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/README.md \
  docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/SOURCE-MANIFEST.md \
  docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CONTRADICTION-INVENTORY.md \
  docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/DATA-CLASSIFICATION.md \
  docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A-inventory/CORPUS-COVERAGE-MATRIX.md \
  docs/specs/scripts/verify-p0a.ps1

# Named-person pattern other than the one permitted public owner name (case-sensitive, two
# capitalized words forming a plausible human name), excluding "Clint Morgan":
grep -nE '\b[A-Z][a-z]+ [A-Z][a-z]+\b' <same file list> | grep -v 'Clint Morgan'
```

Expected result: zero matches from the first command (no email address anywhere in this parcel's
artifacts); the second command's only expected matches, if any, are the already-public owner name
`Clint Morgan` (excluded by the `grep -v`) and any other capitalized two-word spans that are not
personal names (for example, file-path components or proper nouns naming a document, class, or
parcel) — a reviewer inspects any residual match by hand, since this heuristic pattern can also
match non-person two-word proper nouns.

## Retention, export, deletion, access control

- **Retention:** permanent, versioned repository documents (same retention posture as every other
  merged spec/closure record) — there is no personal-data retention clock to set because there is
  no personal data.
- **Export/deletion:** `not-applicable` — nothing in this parcel's artifacts is ever subject to a
  user export or deletion request.
- **Access control:** these artifacts live under version control with the same repository access
  control as every other governed-delivery document; no additional access tier is introduced.
