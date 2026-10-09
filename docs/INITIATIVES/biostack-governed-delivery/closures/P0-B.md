# CLOSURE — P0-B (Product Capability and Safety Contract — what the product may say)

Coordinator closure record (governed-delivery lifecycle), 2026-10-09.

| Item | Value |
|---|---|
| Spec | `parcels/P0-B.md` — dual review AWF/AWF (matrix fidelity byte-identical) → fix 1 (`9b440a9`) → targeted re-review APPROVE/AWF → **D-J** amendment (`d684254`, dose-context definition) → amendment #529 |
| Implementation | PR #531 (`feat/p0b-product-capability-contract`, merged `725a47e`) — machine-readable contract: 10×4 matrix (byte-faithful), per-label applicability criteria, preemption order, D-B3..D-B6 semantics, `enablementState` (D-B1(c) staging split, `publiclyEnabled: false` hard), schema + validator + fixtures |
| Hardening | fix 1 (`95252e6`) → re-verify **FAIL** (blocklist marketed as paraphrase-proof) → fix 2 (`67dd246`, closed-world citation rule + honest claims) → GLM re-verify **FAIL** (ruled text unpinned; merged-clause smuggle) → fix 3 (`922ec87`): **byte-pin ALL normative text** in both artifacts + reviewer-set pinning |
| Review | dual impl review PASS-WITH-FIXES ×2 (contract CONTENT clean: matrix byte-faithful, zero scope creep); re-verify ×1 FAIL (closed in fix 2/3); GLM penetration re-verify FAIL (closed in fix 3); final bounded GLM confirmation **PASS** (all 16 edit attacks held) |
| Coordinator reproduction | stem-free paraphrase smuggle → REJECTED; T4i merged-into-pinned-sentence smuggle → REJECTED; in-place rule-text corruption → REJECTED (exact expected-vs-found diff); clean baseline **19/19 PASS**; single-reviewer invocation → fails closed |

**Status: DONE.** The product's speech is now governed by an enforceable, byte-pinned contract:
every matrix cell, rule, and enablement literal in both artifacts is compared exact against the
owner-ruled frozen design gate (D-B1..D-B6), and any edit that would let the product say more
than the owner approved fails validation loudly.

## Doctrine of record

- **D-I**: D-B1(c) staged split — Class A/B/C + deterministic math live; `biostack-recommended`
  origination behind guidance-content-contract **v2.0.0** re-ratification + `legal_product_ratification`.
- **D-J**: dose-context operational definition (the `R (dose-context)` cell's scope semantics).
- **D-K**: precedence directional constraint (travels into P0-D1's reconciliation).
- Known limits (disclosed, honest): semantic "behavior smuggle" detection is a review-layer
  control; the static layer blocks the structural classes (uncited clauses, unpinned text,
  merged-clause edits). The dual-review + owner-merge gate is the named compensating control.

**Next on the dependency spine:** `P0-B -> P3-B -> P4 -> P6 -> P7 -> P0-C`. P3-B binds the
parcel schema (P3-A) to the frozen contract's capability/claim/provenance/missingness/
function-review/escalation fields; P0-C then proves the allowed/degraded/refused/escalated
behavior with fixtures — including that useful guidance survives enforcement (charter D12).
