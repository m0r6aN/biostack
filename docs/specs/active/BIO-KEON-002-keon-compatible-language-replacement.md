---
ticket: BIO-KEON-002
title: Replace duplicated generic verification language with Keon-compatible positioning
status: review-candidate
owner: clinton.morgan
created: 2026-10-07
updated: 2026-10-07
supersedes: null
superseded_by: null
risk: standard
delivery_classes: [standard]
guidance_classes: [not-applicable]
substance_function_risk: [not-applicable]
function_review_status: not-applicable
routing_class: documentation/standard
data_classification: internal
---

# BIO-KEON-002 — Replace duplicated generic verification language with Keon-compatible positioning

## Goal

Identify and replace duplicated generic language about receipt verification, offline inspection workflows, deterministic hash verification, and audit-kit conventions in BioStack documentation with precise language that credits the Keon-compatible model without implying BioStack owns the generic verifier philosophy or that a Keon dependency already exists.

The replacement language shall use phrases such as "Keon-compatible offline inspection" and "Keon-compatible deterministic verification" to position BioStack's current independent implementation as compatible with a shared pattern, not as the originator or consumer of Keon infrastructure.

Preserve all audit claims, contract language, protocol-specific ownership, and user-facing verification behavior. Implementation logic, verifier CLI, and test fixtures remain unchanged.

## Initiative

This parcel sits inside the Keon ownership boundary established by locked decision **D10** (BioStack Governed Delivery Goal Charter):

> **D10 — Keon ownership boundary.** General receipt canonicalization, hashing, chaining, and offline verification remain Keon-owned. BioStack emits privacy-minimized domain facts through a pinned Keon-compatible contract.

Keon owns the portable verifier patterns, canonical receipt/manifest conventions, offline CLI conventions, and auditor-kit structure. BioStack currently implements a domain-specific verifier that is compatible with this pattern. K-3 documents that compatibility without removing the independent implementation (which remains in place through K-4 readiness) and without creating a dependency. K-3 is a documentation-only positioning lane; it does not implement the adapter seam (K-2), nor does it consume a Keon package (K-4).

## Wave

Keon consolidation cycle, lanes K-1 and K-3, documentation-only positioning.

## Branch

`docs/keon-k3-language-replacement`

## Worktree

`/home/cmorgan76/Repos/biostack-wt/keon-k1-k3-docs-alignment`

## Dependencies

- D10 locked decision (completed, approved in CHARTER.md).
- K-1 baseline documentation updates (recommended to be merged before K-3 Gate 2, for clarity; not a hard blocker if partitioned).
- Current independent implementation of `ProtocolOperationsExportBundle`, verifier CLI, offline verification runbook, release checklist, smoke scripts, result-code catalog, and capstone tests (existing; no changes required).

## Constraints

From TODO.md, Keon consolidation lane K-3 constraints:

> **BioStack K-3: Replace duplicated generic language.** Use "Keon-compatible offline inspection" for the current independent implementation; do not imply that BioStack owns the generic verifier philosophy or that a Keon dependency already exists.

Consequence: This spec shall replace generic, duplicated descriptions of offline verification workflows with the term **"Keon-compatible offline inspection"** where those descriptions repeat patterns established by Keon (e.g., deterministic hash verification, receipt-only modes, auditor-kit structure, air-gap workflows). The replacement must make clear that:

1. BioStack's implementation is compatible with (not derived from) the Keon pattern.
2. Keon owns the generic pattern; BioStack owns the domain adapter.
3. No dependency on Keon code, packages, or infrastructure is implied.
4. The current independent implementation is correct and remains in place.

Additional constraints from Ownership boundary (TODO.md):

- **Preserve verifier-kit domain ownership.** Verifier CLI reference, offline verification runbook, release checklist, smoke scripts, result-code catalog, and capstone-guard contract remain BioStack-specific. Language must not remove attribution or imply Keon provides these.
- **Preserve protocol-specific implementation.** `ProtocolOperationsExportBundle`, protocol-specific redaction/provenance fields, health/protocol wording, and admin/user workflows remain BioStack-owned.
- **Preserve existing audit and safety claims.** Every claim in the auditor-packet-index, commercialization-boundary note, and capstone guard must remain unchanged in truth and force.
- **Preserve existing implementation behavior.** Verifier CLI, offline runbook, smoke scripts, tests, and capstone guard remain functionally identical before and after this spec's changes.

## Acceptance Criteria

1. **AC1 — Generic language identified and replaced.** Documentation review (manual + grep-based) identifies duplicated generic descriptions of "deterministic receipt verification", "offline inspection workflows", "canonical hash verification", "auditor-kit conventions", and "air-gap modes" and replaces them with "Keon-compatible" variants where appropriate.
2. **AC2 — No ownership-inversion language.** No phrase appears in updated docs that says "BioStack owns the offline verifier pattern", "BioStack's generic verification model", "Keon uses BioStack's approach", or similar claims that invert ownership. (Rationale: Keon owns the generic pattern; BioStack owns the domain adapter.)
3. **AC3 — No false-dependency language.** No phrase says "Keon verifier", "via Keon", "Keon package", or "depends on Keon" without an explicit note that the current implementation is independent and remains so.
4. **AC4 — Keon-compatible positioning preserved.** Every instance of "Keon-compatible offline inspection" or "Keon-compatible deterministic verification" is present and used consistently to describe the current independent implementation's compatibility with Keon patterns.
5. **AC5 — Contract and audit claims unchanged.** Verifier CLI reference, offline runbook, release checklist, smoke scripts, result-code catalog, commercialization-boundary allowed/forbidden claims, and capstone-guard contract contain no unintended changes.
6. **AC6 — Protocol-specific ownership reaffirmed.** Language clearly continues to attribute `ProtocolOperationsExportBundle`, redaction/provenance fields, health wording, and admin workflows to BioStack.
7. **AC7 — Consistency check.** A grep-based search for "generic\|offline.*verifi" (case-insensitive) across target docs shows every instance either preserves existing claims or uses "Keon-compatible" language. No generic claim is orphaned or contradicted.

## Out of Scope

- **K-1 positioning updates.** K-1 updates ownership and "Keon-style" positioning language. K-3 focuses on replacing duplicated generic language with "Keon-compatible" variants. Partitioning at Gate 2 prevents overlap.
- **K-2 adapter seam.** Isolating shared canonical serialization and hash conventions behind an adapter interface is a separate lane. This spec does not add, modify, or depend on an adapter interface.
- **K-4 package consumption.** Consuming a stable Keon package or tooling is a separate lane. This spec uses no external Keon package or reference implementation.
- **Implementation changes.** Verifier CLI, offline runbook, smoke scripts, release checklist, tests, capstone guard, result-code catalog, and verification logic remain unmodified in behavior.
- **Frontend or product UI.** This spec is developer/auditor-facing documentation; no user-visible product surface is changed.
- **Removal of independent implementation.** All BioStack offline verification code remains in place and unchanged.

## Allowed Files

Target files for generic-language replacement (verified to exist and contain replicable descriptions of offline verification patterns):

- `docs/protocol-operations-offline-auditor-packet-index.md` — entry point; generic descriptions of "offline verification kit", "verifier kit", "audit workflow" that repeat Keon-generic patterns.
- `docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md` — commercialization SKU descriptions; generic language about "offline verification", "audit support", "deterministic verification" that may apply Keon-compatible positioning.
- `backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/README.md` — CLI reference; descriptions of "offline modes", "deterministic verification", "receipt verification" that reflect Keon-compatible pattern.
- `backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/OFFLINE_VERIFICATION_RUNBOOK.md` — runbook; descriptions of offline workflow, air-gapped verification, receipt handling that match Keon patterns.
- `docs/development/biostack-product-audit.md` — audit documentation; generic language about verification workflows (if applicable).
- `docs/commercialization/05-glossary-architecture.md` — glossary terms related to offline verification, receipts, audit workflows.

Coordinator-owned (builder must not edit): this spec, `docs/specs/INDEX.md`, `docs/specs/active/README.md`, `docs/specs/done/README.md`, TODO.md, CHARTER.md, HANDOFF documents, Gate 2 record.

## Forbidden

- Any implementation file under `backend/src/` or `backend/tests/` except new docs-only closure evidence.
- Verifier CLI code, offline runbook scripts, smoke-script behavior, test logic, or capstone-guard conditions.
- Language that says "BioStack owns the offline verifier pattern" or "BioStack's generic verification model."
- Language that implies a Keon dependency, package, or infrastructure consumption that does not exist.
- Removal of existing audit claims, commercialization boundary constraints, or capstone test rationale.
- Removal or weakening of protocol-specific ownership attribution.
- Addition of forward references to K-2 or K-4 implementation or to a Keon dependency.
- Any merge, push, or PR until coordinator approval.

## Required Tests

Deterministic docs-consistency checks only (no implementation tests):

1. **T1 — Keon-compatible language presence.** Grep search: `grep -i "Keon-compatible" docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/*.md` returns at least one match per file. (Proves Keon-compatible positioning is present.)
2. **T2 — No false-ownership language.** Grep search: `grep -i "BioStack owns.*verifi\|generic.*verifi.*pattern" docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/*.md` returns zero matches. (Proves ownership is not inverted.)
3. **T3 — No false-dependency language.** Grep search: `grep -i "depends.*Keon\|via.*Keon\|Keon.*package" docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/*.md` returns zero matches unless each match is followed within 10 lines by "independent implementation" or "does not depend". (Proves no false dependency is claimed.)
4. **T4 — Protocol-ownership preservation.** Grep search: `grep -i "ProtocolOperationsExportBundle\|protocol.*redaction\|protocol.*provenance" docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md` returns matches attributing these to BioStack. (Proves protocol-specific ownership is preserved.)
5. **T5 — Commercial claims unchanged.** A line-by-line diff of `docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md` shows only generic-language replacements (e.g., "offline verification" → "Keon-compatible offline inspection"); no changes to allowed/forbidden claims or SKU descriptions beyond language substitution.

## Verification (from worktree root)

1. Run `grep -i "Keon-compatible" docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/*.md` — expect at least one match per file.
2. Run `grep -i "BioStack owns.*verifi" docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/*.md` — expect zero matches.
3. Run `grep -i "depends.*Keon\|via.*Keon" docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/*.md` — expect zero matches or matches followed by "independent" within 10 lines.
4. Run `git diff --stat` — expect only `.md` files in `docs/` and `backend/tools/` (no implementation files).
5. Run `git diff docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/README.md` — review for unintended removals of audit claims or commercialization constraints.

## Collision Risk

**Both K-1 and K-3 edit overlapping documentation** (notably `docs/protocol-operations-offline-auditor-packet-index.md` and `docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md`). They must be **serialized or file-partitioned** at Gate 2:

- **Serialization:** K-1 is approved and committed first, then K-3 is approved and committed (K-3 sees K-1's baseline).
- **File partitioning:** K-1 updates only ownership/positioning section introductions; K-3 updates only generic-language instances within sections. Partition at Gate 2 to ensure no merge conflicts or duplicate edits.

Coordinator must resolve the partition and sequencing decision before approval.

## PR Notes

- This spec shapes K-3 only; K-1 (ownership positioning) is a separate parcel and separate PR.
- No implementation code changes; documentation language replacement only.
- Merge is not authorized by this spec's approval. Gate 2 approval and coordinator dispatch are required.
- The parcel may be merged only after all tests pass and an independent reviewer approves the language replacement and constraint preservation.

## Session Handoff

- **Builder context:** Keon consolidation boundary (D10, TODO.md), current independent offline verification implementation (not a dependency, owned by BioStack), constraint that "Keon-compatible" language is allowed to describe the current independent implementation's compatibility with Keon patterns without implying ownership inversion or dependency.
- **Coordinator handoff:** Collision resolution between K-1 and K-3 at Gate 2 (file partitioning or serialization); any new docs discovered not in Allowed Files list; any generic-language instance that needs replacement but is outside approved files.

## Stop-and-Report Rule

Builder stops and returns to the coordinator if:

- A target doc (Allowed Files) is not found or is empty (report which doc and its expected path).
- A new generic-language instance is discovered that should be replaced but is in a file not listed in Allowed Files (report the file and the instance).
- A test discovers that an audit claim or commercialization constraint has been accidentally weakened or removed.
- A replacement of generic language would require removing or weakening a safety or audit claim (report the conflict and stop).
- K-1's positioning updates conflict with K-3's generic-language replacements in the same sentence or section (coordinate with K-1 lead; stop if partition cannot be agreed at Gate 2).

## Context & References

- `docs/TODO.md` — Keon consolidation, ownership boundary, lanes K-1 through K-4.
- `docs/INITIATIVES/biostack-governed-delivery/CHARTER.md` — locked decision D10, delivery classes, spec lifecycle.
- `docs/specs/README.md` — spec lifecycle, review-candidate status, no-TBD rule.
- `docs/protocol-operations-offline-auditor-packet-index.md` — primary target for generic-language replacement.
- `docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md` — secondary target; commercialization constraints must be preserved.
- Existing offline verifier implementation: `backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/`, tests, capstone guard (no changes to behavior or code).
