# CLOSURE — P3-A (Generic Extensible Parcel Schema)

Coordinator closure record (governed-delivery lifecycle), 2026-10-09.

| Item | Value |
|---|---|
| Spec | `parcels/P3-A.md` — review chain: round-1 dual REJECT/REJECT → rework 3 (#521) → round-2 dual REJECT/AWF → rework 3b (`7df5ef4`) → round-3 targeted AWF → rework 3c (`be81530`), spec amendment A1 (`6e497d1`) |
| Implementation | PR #525 (commit `c3b4007`, merged `4fe5825`) — 28 allowed surfaces: parcel-spec.schema.json (15 keys incl. `extensionSections: {}`), template set, EXTENSION-POINTS.md, extension registry, no-placeholder machinery (9-step normalization incl. `decode-html-entities`, ordinal term processing), 12 JSON fixtures + manifest + frozen-P2-census compatibility set, bounded README/INDEX appends, `verify-p3a.ps1` |
| Remediation | PR #527 (`fix/p3a-remediation-1`, merged `0fa8eec`) — closed both demonstrated verifier blockers (check 11 href resolution; check 7 per-subsection scoping), alias-column resolver (check 6/`Resolve-RequiredSections`), BaseCommit-frozen census (check 4), carry-over-item-3 disposition + check 10 assertion, README link correction |
| Review 1 | retrospective `p3a_impl_review_1` **PASS-WITH-FIXES** → re-verify `p3a_reverify_1` **PASS-WITH-FIXES**: all six fix rows `Closed`, no blockers |
| Review 2 | retrospective `p3a_impl_review_2` **PASS-WITH-FIXES** (two demonstrated BLOCKERs) → re-verify `p3a_reverify_2` **PASS-WITH-FIXES**: independent new adversarial variants rejected; clean PASS at remediated content |
| Coordinator reproduction | `verify-p3a.ps1` → `P3-A verification PASS` at merged content; both blocker code claims reproduced at lines 842–843 / 625 before remediation |

**Status: DONE.** The generic parcel-spec contract exists, is machine-checkable, and its verifier
rejects the adversarial classes its own reviewers constructed.

## Process note (re-verify F1 disposition — amendment option (a))

`verify-p3a.ps1` check 3's frozen `AllowedSurfaces` list is authored for a single-anchor
feature-branch comparison and cannot produce a formal "clean PASS" transcript against `main`'s
merged history. This is a **known, accepted, cross-parcel verifier constraint**, not a
remediation defect; the remediation PR disclosed it with a reproducible local-diagnostic
workaround rather than silently patching the committed artifact. Going forward:

- The authoritative "clean PASS" pair for `verify-p3a.ps1` is: `BaseCommit` = the parcel's
  dispatch-anchor SHA; `HEAD` = the parcel's own feature-branch tip (never `main`'s merged
  history).
- Carry-forward constraint (option (b)) for any future verifier modeled on this one: check-3-style
  allow-lists must tolerate a **named, pinned** set of out-of-scope coordinator paths, never an
  open-ended tolerance. Recorded for P4/P6/P7 verifier work and P0-B's own verifier planning.

## Carry-over items

- Carry-over item 3 (`coordinator-parcel`-shape exercised by no fixture) — dispositioned in
  `docs/specs/schemas/fixtures/p3a/REAL-SPEC-COMPATIBILITY-SET.md` and asserted by check 10:
  **P3-B's dispatch must discharge or re-affirm it.**
- P2 review-2's two LOW amendments and the P3-A spec's own self-identified gaps travel with the
  parcel record.

**P3-A unblocks P0-A's dispatch** per the charter dependency spine (`… -> P2 -> P3-A -> P0-A -> …`).
