# Coordinator Loop Directive — BioStack Algorithm Remediation

## Ownership

**Queue owner:** Codex goal task `01a061dd-255b-7942-83bb-b6ad82450ac3` (`/root`).

One goal has one coordinator. Ownership may transfer only at a parcel boundary through an explicit update to this block. If another live coordinator claims this queue, stop and report; never assume shared ownership.

## Current state

- Goal branch: `codex/goal-biostack-algorithm-remediation`
- Remediation base: `main` / `origin/main` at `339f259b1a467034db4f57cf9d774c292f11b53a`
- Gate 1: D1-D14 ratified 2026-09-02
- Plan review: complete; triage commit `ef4376b0a186dd0c0fe9e4b7772bde07e39756eb`
- Narrow Gate 1 Amendment 01: ratified 2026-09-02; D6-D9 replacements active
- Narrow Gate 1 Amendment 02: ratified 2026-09-02; D3/D4/D11/D12 replacements and D15 active
- Narrow Gate 1 Amendment 03: ratified 2026-09-02; D8 timeout-terminalization supplement and replacement AF-P05 active
- Narrow Gate 1 Amendment 04: ratified 2026-09-02; replacement AF-P03 and exact reviewed-fixture correction active
- Narrow Gate 1 Amendment 05: ratified 2026-09-02; P03 artifact-identity/provenance supplement and exact 11-case census active
- Gate 2: contingent authorization granted for Q01-Q04 and P01-P08
- Gate 3: granted 2026-09-03 by ratification of `gate-3-request.md` at goal commit `ebb3318e3fb5f1b5186d23736719150d28a5fede`; Narrow Gate 3 Retry 01 now authorizes exactly one isolated unchanged `day7Review.test.ts` rerun, then conditionally one full frontend rerun, focused lint/build, and resumption of the existing Gate 3 sequence
- Diagnostic coordination and five parcel branches: preserved and unmerged
- P01 candidate `751090eabf3ee6f066c17ee83861ae04711fd4a0`: deterministic chain green; one fresh adversarial ACCEPT; held unmerged
- P02 candidate `1b5742051ee2b54a80dac9bbdf68c68e5a6b5992`: deterministic chain green; two fresh adversarial ACCEPTs plus SG-SCOPE/SG-OUTBOUND ACCEPT; held unmerged
- P04 candidate `38ebffb10411c763ed4f570fdcfece51ab177aa3`: deterministic chain green; two fresh adversarial ACCEPTs plus SG-EVIDENCE/SG-SCOPE ACCEPT; held unmerged
- P07 candidate `c35df75be835d26b4c37618874cf502335b9a24c`: deterministic chain green; two fresh adversarial ACCEPTs plus SG-OUTBOUND/SG-SCOPE ACCEPT; held unmerged
- P08 candidate `47c2be0358e7ca024b524c7693af8b06846671d6`: deterministic chain green; two fresh adversarial ACCEPTs plus SG-OUTBOUND/SG-SCOPE ACCEPT; held unmerged
- Q01 evidence `83d1235b3a964ddf7130f67900f4dcd3b38dcf42`: fresh review ACCEPT; reproduced config-only Collective transmission; P08 remediation candidate is review-complete and held unmerged
- Q03: fresh review ACCEPT; `INCONCLUSIVE_NO_DETERMINISTIC_SEAM`; zero repository changes
- Q04: final transcript fresh review ACCEPT; `READY`; P07 remediation candidate is review-complete and held unmerged
- Q02: v1 evidence commit `dea721288521a5789350e2398d0d7292b01ad10f` is preserved but review-rejected; v2 stopped terminally before transcript/test execution when the Baseline procedure encountered an unsupported `New-Item -LiteralPath`; no retry/v3 is authorized and endpoint-wide coverage remains inconclusive
- P05 candidate `2f4a092fb7f0ec534996e6ad1116e8b50d612b77`: deterministic chain green; two fresh adversarial ACCEPTs plus SG-SIDECAR/SG-SCOPE ACCEPT; rejected parent `923900c66d7046c282d397ba60d85b9eebcf1a58` preserved in history; P06 shaping may proceed against the exact accepted hash
- P03 candidate `a69d945a3cd8a3655911707031386441fc55079f` / tree `7205d5ea11148eac865534cf5ae545634eb84be5`: Amendment-05 rework chain green and fresh independent adversarial review ACCEPT with no actionable findings; rejected parent `7e8087a00722e38c2a090d49826fa6289c880356` preserved in history; held unmerged.
- P06 candidate `c435caf3cf0cf95fb1bfbc51d2924ea960183957`: standalone and exact P05+P06 combined chains green; two fresh adversarial ACCEPTs plus SG-SIDECAR/SG-SCOPE ACCEPT; held unmerged
- Goal-level integration tree `17b3ff60c74098f67edec222f1854f6486df7fe5`: exact blobs from all eight accepted candidates, 27 unique paths and zero collisions; backend `2,065 passed / 5 expected skips`, sidecar `53 passed / 1 expected skip`, frontend `989/989`, focused lint/build and backend consent `17/17` green; fresh independent integration review ACCEPT with no blocking findings; Gate 3 ratified and release execution active
- Cerebras routing inventory: user-scope credential presence is confirmed without reading its value, but the current Codex process does not inherit it, no Cerebras CLI or reviewed callable adapter is present, and the existing governed Cerebras shadow route is public-only/candidate-only with Coordinator and verifier roles prohibited. State is `configured_unverified`; no BioStack source, test payload, secret, or provider request has been sent.

## Standing authorizations

The developer granted exactly:

> Gate 1: I ratify D1-D14 as written and grant contingent Gate 2 dispatch authorization for Q01-Q04 and P01-P07. Gate 3 remains ungranted.

> Narrow Gate 1 Amendment 01: I ratify the replacement text for D6, D7, D8, and D9 as written. All other decisions and the existing contingent Gate 2 authorization remain unchanged. Gate 3 remains ungranted.

> Narrow Gate 1 Amendment 02: I ratify the D3, D4, D11, and D12 replacements, add D15, approve AF-P08 and P08 as written, and grant contingent Gate 2 dispatch authorization for P08. Amendment 01 is concurrently ratified by the preceding statement; all other decisions and existing authorizations remain unchanged. Gate 3 remains ungranted.

> Narrow Gate 1 Amendment 03: I ratify the D8 timeout-terminalization supplement and replace AF-P05 with the three files listed, including `jobs/runner.py`. P05 retains contingent Gate 2 authorization subject to its shaped spec and Step 0; P06 remains downstream of the verified P05 candidate. All other decisions and authorizations remain unchanged. Gate 3 remains ungranted.

> Narrow Gate 1 Amendment 04: I replace AF-P03 with the three files listed and authorize only the stale API graph-positive fixture change from `provisional` to exactly `reviewed`. Ratified D6 and all other decisions and authorizations remain unchanged. Gate 3 remains ungranted.

> Narrow Gate 1 Amendment 05: I ratify the D6 artifact-identity/provenance supplement and replacement P03 regression census and verification counts as written. AF-P03 remains the same three files, and the Amendment 04 API fixture restriction remains unchanged. P03 retains contingent Gate 2 authorization subject to its amended shaped spec and fresh Step 0. All other decisions and authorizations remain unchanged. Gate 3 remains ungranted.

> Gate 3: I ratify `gate-3-request.md` at goal commit `ebb3318e3fb5f1b5186d23736719150d28a5fede` as written.

> Narrow Gate 3 Retry 01: I authorize one isolated rerun of the unchanged `day7Review.test.ts`, followed—only if green—by one full frontend-suite rerun using the ratified runner. If both are green, authorize completing focused lint/build and resuming the already-ratified Gate 3 sequence. Any repeated failure or other drift stops execution. No production or test modification, timeout change, scope expansion, or sidecar deployment is authorized.

Operational effect:

1. Q01-Q04 and P01-P08 may be shaped and dispatched only after their specs exactly match the ratified charter and ratified amendments, name an isolated branch/worktree, pass Step 0, and retain exact Allowed Files.
2. Amendment 01 unblocks P03-P07; Amendment 02 authorizes P08; Amendments 03 and 04 resolve the scoped P05 and initial P03 blockers; Amendment 05 reactivates only P03's cross-artifact provenance rework after amended-spec lint and fresh Step 0. P05 must precede final P06 integration verification, and Q04's accepted `READY` disposition remains a prerequisite for P07.
3. No further new parcel, changed Allowed Files, external data/provider use, production/cloud data access, secret disclosure, publication, migration, billing action, cleanup, reset, or force-push is authorized. Gate 3 authorizes only the exact normalized branch, PR, merge-commit, API/frontend deployment, monitoring, and guarded normal-revert rollback described in the ratified request.
4. The research sidecar remains undeployed. Every Gate 3 stop condition and exact tree/blob/count requirement remains active.

## Queue

The coordinator advances this queue in dependency order while allowing only collision-free work to run concurrently.

1. Ratification receipts for Amendments 01 and 02: complete.
2. Q01-Q04 adjudication: complete; Q02 remains a durable inconclusive terminal stop.
3. P01 and P02 implementation/review chains: complete and held unmerged.
4. Amendments 03-05 ratification receipts and P03 Amendment-05 implementation/review chain: complete; held unmerged.
5. P04, P07, and P08 implementation/review chains: complete and held unmerged.
6. P05 and P06 implementation/review chains: complete and held unmerged.
7. For each shaped parcel: Gate 2 contingency check, isolated dispatch, Step 0 ruling, build, closure check, deterministic evidence, required adversarial/security review, and rework loop.
8. Exact green candidate set and independently accepted integration tree assembled; human Gate 3 ratified on 2026-09-03. Complete.
9. Active under Narrow Gate 3 Retry 01: run exactly one isolated unchanged `day7Review.test.ts` retry; only if green, run exactly one full frontend retry; only if both are green, complete focused lint/build and resume the already-ratified push/PR/checks/merge/deploy/Stage-F sequence. Any repeated failure or drift stops.

## Per-iteration algorithm

1. Read the charter, `plan-review-findings.md`, this directive, active specs, and handoffs. Reconcile them with Git/worktree state before trusting carryover.
2. Fetch `origin` before each dispatch wave. If `origin/main` moves materially from the pinned base, stop affected dispatch and apply the charter's base-change rule.
3. Shape one parcel in a fresh frontier session. The shaper may write only its spec in this goal's coordination directory.
4. Coordinator-lint every factual claim on disk: file existence, test-project inclusion, source ownership, dependencies, exact commands, baseline counts, collision points, and security gates.
5. Confirm the Gate 2 contingencies. Create or verify one `codex/` branch and one isolated worktree named in the spec. No parcel branches from a diagnostic branch.
   - If a fresh .NET worktree lacks ignored restore metadata, the coordinator may copy only `project.assets.json`, `project.nuget.cache`, and generated NuGet props/targets from a clean ambient worktree at the identical base/tree. Record relative paths plus SHA-256 before/after, keep tracked status clean, and permit no restore or network access.
6. Dispatch a fresh builder/investigator. Step 0 restates goal, base, exact Allowed Files, forbidden work, invariants, tests, security/network limits, and stop conditions, then stops for coordinator confirmation before any edit.
7. Rule on flags. Any missing decision, new file, production dependency, changed invariant, or new reproduced Q defect stops the affected parcel for a narrow amendment.
8. On completion claim, inspect the exact commit and diff before rerunning anything. Wrong-shaped claims are presumptively empty. Verify all required files and no forbidden files.
9. Consume deterministic evidence: targeted regression, adjacent suites, exact count delta, aggregate scope, `git diff --check`, ancestry, clean status, and network/local-fake receipts.
10. Dispatch fresh read-only adversarial review on the exact candidate commit. Use two independent adversarial reviewers plus a separate defensive security reviewer for P02, P04, P05, P06, and P07. Reviewers never fix or commit.
11. Triage every finding as fix, accept-as-documented, or informational. Reproduce disputed findings before ruling. Any rework changes the candidate commit and invalidates earlier acceptance.
12. Hold all candidate commits unmerged until the complete goal-level chain is green and the human grants exact Gate 3.

## Worktree and branch convention

- Branch: `codex/biostack-remediation-<parcel-lowercase>`
- Worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-<parcel-lowercase>\BioStack`
- Every worktree starts from the Gate-2-verified base and records its starting commit.
- Existing diagnostic worktrees and branches are read-only evidence sources and are never used as parcel bases.
- BioStack does not vendor the Foreman permission-profile emitter. The coordinator therefore verifies each local worktree and dispatches agents with exact Allowed Files and explicit no-network/no-external-provider constraints.

## Universal stop conditions

Stop the affected parcel and report when:

- a pending or reopened human gate controls the next action;
- current `origin/main` differs materially from the verified base;
- an exact Allowed File is missing or insufficient;
- a production/public-contract decision is absent;
- an invariant can pass only by weakening or deleting its regression;
- a real external provider, network registry, cloud resource, production database, protected data, secret, or payload dump would be required;
- a security finding cannot close within the parcel;
- the same tripwire or false-closure condition fires twice;
- any push, PR, merge, deployment, publication, release, or diagnostic-branch mutation is proposed before exact Gate 3.

## Stage-F requirements

Stage F records exact merged commits, verification counts and commands, review/security dispositions, Q-lane outcomes, residual risk, lessons, and branch custody. It verifies that the original diagnostic coordination and parcel branches still exist and remain unmerged. Remediation worktree cleanup occurs only after approved merges; diagnostic cleanup is never implied.
