# P4 Remediation 1 — Gate 2 Dispatch Record (D-M / A-P4-2 pins + F1 blocker fix)

Coordinator-owned record. Remediation dispatched 2026-10-10 after the P4 implementation review
split (R1 `PASS` vs R2 `FAIL`); record written at re-verify dispatch time (2026-10-10) with the
anchor pair pinned — this retrospective placement is disclosed, not hidden. Trigger and pins are
the coordinator-ratified amendment **D-M** (`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`
## D-M). Owner may override any pin.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "P4-REMEDIATION-1",
  "builderId": "p4_remediation_builder",
  "branch": "fix/p4-remediation-1",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/p4-remediation-1",
  "baseCommit": "e092e71176c5d800c8eba33877a83c6da95efdcf (D-M ledger commit on main; dispatch anchor; equals main tip at dispatch and at re-verify dispatch)",
  "builderTip": "45d5995f0d4d4335f1f1519b45bdd963f05da442 (PR #539 head)",
  "amendmentCommit": "f90fe0bd7b7b47f9a129a64408d03c84c2fe5764 (spec-only, committed alone before code)",
  "permissionEnvelope": "docs-only:p4-remediation-surfaces",
  "builderSurfaces": "exactly 4 paths across 2 commits — commit 1 (spec-only): parcels/P4.md; commit 2 (code-only): docs/specs/scripts/validate-spec.ps1, docs/specs/scripts/verify-p4.ps1, docs/specs/schemas/fixtures/p4/negative-missing-required-field-migration-conditional.json",
  "reviewerIds": ["p4_reverify_1", "p4_reverify_2"],
  "risk": "standard; architecture — dual independent blind re-verification (fresh adversarial runs)",
  "verificationContract": "the spec's 12-check Deterministic verification passes at the remediated hash AND every demonstrated/adversarial evasion class — especially the R2 F1 body-prose placeholder PoC and fresh variants — fails closed against it"
}
```

## Required fixes being re-verified (D-M, exactly)

1. **F1 (BLOCKER, reproduced at code level):** `validate-spec.ps1` stage 5 must scan the real
   file's Markdown **body prose** (`$parsed.Body`), not only headings + frontmatter, in `-SpecPath`
   disk mode. The reviewer's ATTACK.md PoC must flip from `valid` to `invalid`/`placeholder-violation`.
2. **A-P4-2a (F2 ratification):** non-self-contradictory `pilot-rollback-alias` example (now
   `pilot wind down plan`); `positive-real-spec-p3b-cross-check.json` pinned `expected.result` =
   `invalid`/`missing-required-section`; AC-P4-05/check 7 cross-verifier-agreement scope restored
   to FULL agreement (not the builder's narrowed form).
3. **A-P4-2b (F3):** `missing-required-field` is the 13th closed-vocabulary `reason` literal and
   has its own fixture (`negative-missing-required-field-migration-conditional.json`).
4. **A-P4-2c (F4):** `extension_section` pinned in the spec as the named P4 frontmatter convention.
5. **F5:** fixture `expected` outcomes SHA-256 hash-pinned in the verifier, independent of its own
   live re-derivation.

## Re-verify dispatch (this record's live purpose)

Two blind, read-only, independent reviewers (`p4_reverify_1`, `p4_reverify_2`,
`anthropic/claude-sonnet-5`; provider switch to `accounts/fireworks/models/glm-5p3` on model
refusal per precedent) run fresh adversarial verification against the pinned worktree. Mandatory
provisions:

- **Anchor discipline (scar tissue):** pin the anchor pair — BaseCommit = `e092e71176c5d800c8eba33877a83c6da95efdcf`
  (dispatch anchor), HEAD = `45d5995f0d4d4335f1f1519b45bdd963f05da442` (builder tip). Is-ancestor
  check the base against every reviewed SHA. Confirm PR #539's live head still equals the builder
  tip at review time (race guard). Never hand-type a SHA; compute it.
- **Blind/independent:** never read another reviewer's output or the builder's log in
  `.audit/reviews/`; verify claims by execution, not attestation (D8 reproduction standard).
- **Body-prose adversarial set (beyond the original PoC):** paraphrased placeholders in body
  prose (no literal stem), HTML-entity/homoglyph placeholders in body prose, placeholder only in
  fenced code/inline code spans, empty body, empty file, malformed markdown — each must fail
  closed or be honestly disclosed in scope, never silently pass.
- **Pin-evasion set:** fixture-`expected` tampering must fail the hash pin; in-place weakening of
  ruled P4.md text must fail check 4/7 surfaces it governs; the amendment-commit carve-out must
  not become a smuggling path (spec edits in the code commit, or code edits in the amendment
  commit, must both fail).
- **A-P4-2b scope:** no 14th `reason` literal may be emitted by any check on any fixture or
  adversarial input (closed vocabulary is closed).
- **Determinism:** two runs, identical output; full `verify-p4.ps1` run with the pinned anchor
  pair must print `P4 verification PASS` (all 12 checks) — record the exact invocation.
- Verdict vocabulary: `PASS` / `PASS-WITH-FIXES` / `FAIL`, ranked findings (BLOCKER/MAJOR/MINOR),
  exact file+line evidence, smallest amendment, locked-decision impact. Output file:
  `.audit/reviews/p4_reverify_<n>.md` (the ONLY file a reviewer may write). Repo is read-only;
  no git write commands; never touch the coordinator checkout's git state (D-L standing rule).

Delivery (post-review, coordinator-owned): merge of PR #539 is a Gate 3 green-chain merge under
D-H (P4 carries no class-triggered human approval: standard; architecture) — but only after both
re-verifies are green and coordinator reproductions hold. Closure record
`closures/P4.md` + reviews canon copy + INDEX row → `done` on merge.
