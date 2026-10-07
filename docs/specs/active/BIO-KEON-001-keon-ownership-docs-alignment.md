---
ticket: BIO-KEON-001
title: Align verifier-kit and architecture docs to Keon ownership boundary
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
surfaces:
  - docs/protocol-operations-offline-auditor-packet-index.md
  - docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md
  - backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/README.md
  - backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/OFFLINE_VERIFICATION_RUNBOOK.md
---

# BIO-KEON-001 — Align verifier-kit and architecture docs to Keon ownership

## Goal

Update BioStack architecture, auditor-facing, and offline verification documentation to clearly state that BioStack uses a **Keon-style offline verification kit** for deterministic local verification of protocol-operation artifacts. This positioning documents BioStack's current independent implementation as Keon-compatible without implying that a Keon dependency already exists or that BioStack owns the generic verification model.

Preserve all current implementation truth, contract language, and audit/user-facing claims. The word change from "Protocol Operations offline verification kit" to "Keon-style offline verification kit" must respect the constraint that "Keon offline verification infrastructure" language (implying infrastructure ownership) shall appear only after a real Keon dependency is formalized.

## Initiative

This parcel sits inside the Keon ownership boundary established by locked decision **D10** (BioStack Governed Delivery Goal Charter):

> **D10 — Keon ownership boundary.** General receipt canonicalization, hashing, chaining, and offline verification remain Keon-owned. BioStack emits privacy-minimized domain facts through a pinned Keon-compatible contract.

BioStack owns the domain-specific `ProtocolOperationsExportBundle`, protocol-specific redaction and provenance fields, and admin/user workflows. Keon owns generic offline verification infrastructure, portable verifier patterns, receipt/manifest conventions, and auditor-kit structure. This parcel documents that ownership without changing implementation or creating a Keon dependency. K-1 is a positioning and documentation lane; it does not implement the adapter seam (K-2), nor does it consume a Keon package (K-4).

## Wave

Keon consolidation cycle, lanes K-1 and K-3, documentation-only positioning.

## Branch

**Proposed dispatch branch:** `docs/keon-k1-docs-alignment` (to be confirmed at Gate 2). **Current PR branch (shaping phase):** `docs/keon-k1-k3-shaping` (shared with K-3). K-1 and K-3 must be dispatched separately per collision risk; serialization or file-partitioning to be decided at Gate 2.

## Worktree

`/home/cmorgan76/Repos/biostack-wt/keon-k1k3-fixes` (shared with K-3 during shaping phase)

## Dependencies

- D10 locked decision (completed, approved in CHARTER.md).
- Current implementation of `ProtocolOperationsExportBundle`, verifier CLI, offline verification runbook, release checklist, smoke scripts, result-code catalog, and capstone tests (existing; no changes required).
- Existing auditor and architecture documentation (must be preserved in scope and contract).

## Constraints

From TODO.md, Keon consolidation lane K-1 constraints:

> **BioStack K-1: Align docs to Keon ownership.** Update verifier-kit, architecture, and auditor-facing docs to say BioStack uses a Keon-style offline verification kit. Change this to "Keon offline verification infrastructure" only after the dependency is real.

Consequence: This spec must use the term **"Keon-style offline verification kit"** when positioning BioStack's current independent implementation. The term "Keon offline verification infrastructure" (which would imply BioStack hosts or owns the infrastructure) is forbidden until an actual Keon dependency is merged.

Additional constraints from Ownership boundary (TODO.md):

- **Preserve verifier-kit domain ownership.** Verifier CLI reference, offline verification runbook, release checklist, smoke scripts, result-code catalog, and capstone-guard contract remain BioStack implementation surface. Language must not imply Keon provides these or that BioStack owns Keon's generic model.
- **Preserve protocol-specific ownership.** `ProtocolOperationsExportBundle`, protocol-specific redaction/provenance fields, health/protocol wording, and admin/user workflows remain BioStack-owned and must not be repositioned as Keon-generic.
- **Preserve audit and safety claims.** Every claim in the existing auditor-packet-index, commercialization-boundary note, and capstone guard must remain truthful and unweakened.

## Acceptance Criteria

1. **AC1 — Documentation alignment complete.** Every discoverable reference to the "Protocol Operations offline verification kit" in auditor-facing, architecture, and development docs has been reviewed and updated to use "Keon-style offline verification kit" where appropriate. (See Allowed Files for the target list.)
2. **AC2 — No unintended scope creep.** No phrase "Keon offline verification infrastructure" or "Keon owns" appears in any updated file. (Rationale: such language would imply a dependency that does not yet exist.)
3. **AC3 — Contract and implementation language unchanged.** Verifier CLI reference, offline runbook, release checklist, smoke scripts, result-code catalog, and capstone tests contain no changes unrelated to positioning language. API contracts, result codes, verification logic, and audit-path behavior are identical before and after.
4. **AC4 — Protocol-specific ownership preserved.** Documentation clearly continues to attribute `ProtocolOperationsExportBundle`, redaction/provenance fields, health wording, and admin workflows to BioStack. No language suggests these are Keon-generic or could migrate in future lanes.
5. **AC5 — Audit and commercialization claims unchanged.** The auditor-packet commercialization-boundary note, allowed/forbidden claims, and all SKU descriptions remain unchanged in substance and tone.
6. **AC6 — Consistency check.** A grep-based search for "offline.*verifi" (case-insensitive) across target docs shows no instances of "Keon offline verification" that lack the "style" or "compatible" qualifier.

## Out of Scope

- **K-2 adapter seam.** Isolating shared canonical serialization and hash conventions behind an adapter interface is a separate lane. This spec does not add, modify, or depend on an adapter interface.
- **K-4 package consumption.** Consuming a stable Keon package or tooling is a separate lane. This spec uses no external Keon package or reference implementation.
- **Language beyond "Keon-style" or "Keon-compatible".** This spec preserves current implementation truth and does not introduce infrastructure-ownership language (e.g., "Keon-owned infrastructure", "via Keon", "Keon tooling") without explicit D10-aligned rationale.
- **Implementation changes.** Verifier CLI, offline runbook, smoke scripts, release checklist, tests, capstone guard, and result-code catalog remain unmodified in behavior.
- **Frontend or product UI.** Auditor packet index and architecture docs are developer/auditor-facing; no user-visible product surface is changed.
- **Cryptographic or verification logic.** Hash algorithms, receipt format, canonical serialization, and verification behavior are not touched.

## Allowed Files

Target files for documentation updates (verified to exist and contain "offline verification kit" language):

- `docs/protocol-operations-offline-auditor-packet-index.md` — entry-point index for auditor review; "Protocol Operations offline verification kit" positioning language.
- `docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md` — commercialization boundary note; "offline kit" and verification SKU descriptions.
- `backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/README.md` — verifier CLI reference; positioning language only, no code changes.
- `backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/OFFLINE_VERIFICATION_RUNBOOK.md` — offline runbook title and intro section; positioning language only.

Coordinator-owned (builder must not edit): this spec, `docs/specs/INDEX.md`, `docs/specs/active/README.md`, `docs/specs/done/README.md`, TODO.md, CHARTER.md, HANDOFF documents, Gate 2 record.

## Forbidden

- Any implementation file under `backend/src/` or `backend/tests/` except new docs-only closure evidence.
- Verifier CLI code, offline runbook scripts, smoke-script behavior, test logic, or capstone-guard conditions.
- Language that implies "Keon owns the BioStack verifier" or "Keon owns the offline kit."
- Language that repositions protocol-specific fields (`ProtocolOperationsExportBundle`, health/redaction wording) as Keon-generic.
- Removal of existing audit claims, commercialization boundary constraints, or capstone test rationale.
- Addition of forward references to K-2 or K-4 implementation or to a Keon dependency that does not yet exist.
- Any merge, push, or PR until coordinator approval.

## Required Tests

Deterministic docs-consistency checks only (no implementation tests):

1. **T1 — Positional language audit.** Grep search: `grep -i "Keon-style offline.*verifi" docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md` returns at least one match per file. (Proves positioning language is present.)
2. **T2 — Forbidden-phrase detection.** Grep search: `grep -i "Keon.*infrastructure\|Keon owns.*verifi" docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/*.md` returns zero matches. (Proves forbidden language is absent.)
3. **T3 — Protocol-ownership preservation.** Grep search: `grep -i "ProtocolOperationsExportBundle\|protocol.*redaction\|protocol.*provenance" docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md` returns matches attributing these to BioStack. (Proves protocol-specific ownership is preserved.)
4. **T4 — Commercialization claims unchanged.** A line-by-line diff of `docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md` shows only positioning language changes (e.g., "offline verification kit" → "Keon-style offline verification kit"); no changes to allowed/forbidden claims sections or SKU descriptions.

## Verification (from worktree root)

1. Run `grep -i "Keon-style offline" docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md` — expect at least one match per file.
2. Run `grep -i "Keon.*infrastructure\|Keon owns" docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/*.md` — expect zero matches.
3. Run `git diff --stat` — expect only `.md` files in `docs/` and `backend/tools/` (no implementation files).
4. Run `git diff docs/protocol-operations-offline-auditor-packet-index.md docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md` — review for unintended removals of audit claims or commercialization constraints.

## Collision Risk

**Both K-1 and K-3 edit overlapping documentation** (notably `docs/protocol-operations-offline-auditor-packet-index.md` and auditor-facing docs). They must be **serialized or file-partitioned** at Gate 2:

- **Serialization:** K-1 is approved and committed first, then K-3 begins (depends on K-1's updates as baseline).
- **File partitioning:** K-1 updates only intro/positioning language in each file; K-3 updates only generic term replacements (e.g., "offline verification" → "Keon-compatible offline inspection" in specific sections). Partition at Gate 2 to avoid merge conflicts.

Coordinator must resolve the partition and sequencing decision before approval.

## PR Notes

- This spec shapes K-1 only; K-3 (generic-language replacement) is a separate parcel. Both are shaped together in PR #473 but will be dispatched separately per Gate 2 decision.
- No implementation code changes; documentation positioning only.
- Merge is not authorized by this spec's approval. Gate 2 approval and coordinator dispatch are required.
- The parcel may be merged only after all tests pass and an independent reviewer approves the docs alignment and constraint preservation.

## Session Handoff

- **Builder context:** Keon consolidation boundary (D10, TODO.md), current offline verification implementation (not a dependency, independent), constraint that "Keon-style" is allowed but "Keon infrastructure" is forbidden until a real Keon dependency exists.
- **Coordinator handoff:** Collision resolution between K-1 and K-3 at Gate 2 (file partitioning or serialization); any new doc discovered not in Allowed Files list.

## Stop-and-Report Rule

Builder stops and returns to the coordinator if:

- A target doc (Allowed Files) is not found or is empty (report which doc and its expected path).
- A new positioning issue is discovered (e.g., "Keon infrastructure" language in a doc not listed, or a contract/audit claim that needs repositioning but is not in the Allowed Files list).
- A test discovers that a protocol-ownership claim has been accidentally weakened or removed.
- The word "Keon-compatible" appears in the docs in a way that conflicts with K-3's "Keon-compatible offline inspection" positioning (coordinate with K-3 lead if parallel work is detected).

## Context & References

- `docs/TODO.md` — Keon consolidation, ownership boundary, lanes K-1 through K-4.
- `docs/INITIATIVES/biostack-governed-delivery/CHARTER.md` — locked decision D10, delivery classes, spec lifecycle.
- `docs/specs/README.md` — spec lifecycle, review-candidate status, no-TBD rule.
- `docs/protocol-operations-offline-auditor-packet-index.md` — primary target for positioning update.
- `docs/architecture/protocol-operations-auditor-packet-commercialization-boundary.md` — secondary target; commercialization constraints must be preserved.
- Existing offline verifier implementation: `backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/`, tests, capstone guard.
