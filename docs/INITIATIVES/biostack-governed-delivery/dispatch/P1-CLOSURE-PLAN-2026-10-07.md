# P1 Closure Plan — 2026-10-07

Coordinator-owned record. Does not modify the frozen `P1-GATE2.md`.

## Reproduced state (coordinator finding-reproduction, 2026-10-07)

P1 was shaped (`905527e`), dispatched (`d230ce9`, adds `P1-GATE2.md` = dispatch anchor), and built
(`456fed2` "docs: bootstrap governed delivery", the `p1_builder` tip) on 2026-07-16, and its eight
surfaces are already merged in `main` history. The parcel was never verified, never reviewed, and
never closed: registry row remains `active` / `not-yet-closed`. The builder branch
`codex/biostack-governance-p1` no longer exists anywhere, but the chain is linear and complete:

- `comparisonBase` = `8206dd6501912ac41ca003e3dbab208477211837` ("docs: close P1 spec review")
- `dispatchAnchor` = `d230ce9a328c85a7ec7810aea2cfb463f6f840da` ("docs: authorize P1 dispatch")
- `builderTip` = `456fed2` ("docs: bootstrap governed delivery"; parent = dispatch anchor)

`git log 8206dd6..456fed2` shows exactly the dispatch-record commit and the builder-surface
commit, satisfying the verifier's changed-set contract (check 3).

## Environment reconciliation (Windows → Linux)

The frozen Gate 2 record names worktree `D:/Repos/BioStack-governance-p1` and a PowerShell
verifier. The reconciled execution machine is Linux (`/home/cmorgan76/Repos/biostack`). Rulings:

1. Verification runs in a clean worktree at `456fed2` at
   `/home/cmorgan76/Repos/biostack-wt/p1-closure`.
2. `verify-p1.ps1` is executed **unchanged** under `pwsh` installed machine-locally (dotnet global
   tool). No repository file is modified to accommodate the environment.
3. Reviewers inspect the eight surfaces at `456fed2` (the builder tip), not current `main`,
   because several surfaces have changed in the 725 commits since.

## Closure sequence

1. **p1_verifier** (haiku-4-5): run the spec's deterministic verifier with the literal anchors,
   `BuilderId p1_builder`, `ReviewerIds p1_impl_review_1,p1_impl_review_2`, evidence directory
   `artifacts/p1-verification` (intentionally untracked per check 14); report full transcript.
2. **p1_review_1 / p1_review_2** (sonnet-4-5, independent, read-only): adversarial review of the
   eight surfaces at `456fed2` against `parcels/P1.md` required document contracts and acceptance
   criteria. Ranked findings, exact evidence, PASS/FAIL.
3. **Coordinator triage**: reproduce disputed findings; `fix` / `accept-as-documented` /
   `informational`. Any `fix` reopens a builder cycle at the reconstructed tip (amendment if the
   changed set would leave the verifier contract).
4. **Closure record + registry transition**: on a fully green chain, the coordinator writes
   `CLOSURE-P1.md`, embeds the verifier transcript reference and both review verdicts, and changes
   only the P1 registry row to `done` in the same governed change (spec lifecycle rule 7).

No merge Gate 3 is pending for P1: the built surfaces are already in `main` history. This plan
closes the evidence gap only. P2 dispatch remains blocked until step 4 lands.
