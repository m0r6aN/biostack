# Goal-Level Integration Verification

Status: **GREEN — awaiting fresh independent integration review; Gate 3 ungranted**

Date: 2026-09-02

## Custody

- Pinned base and detached HEAD: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Pinned base tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`
- Coordinator-controlled worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-integration\BioStack`
- Staged integration tree: `17b3ff60c74098f67edec222f1854f6486df7fe5`
- Integration form: exact aggregate base-to-candidate diffs applied to a detached pinned-base worktree; no commit, merge, branch mutation, push, PR, deployment, or release
- Scope: 27 unique staged paths, zero parcel collisions, zero unstaged paths, zero untracked paths, and `git diff --cached --check` exit 0
- Parity: every one of the 27 worktree blobs matches its owning accepted candidate blob exactly after verification

The staged integration worktree is preserved for review. Ignored build/dependency roots are environment artifacts and are not included in the integration tree.

## Exact accepted candidate inputs

1. P01 `751090eabf3ee6f066c17ee83861ae04711fd4a0`
2. P02 `1b5742051ee2b54a80dac9bbdf68c68e5a6b5992`
3. P03 `a69d945a3cd8a3655911707031386441fc55079f` / tree `7205d5ea11148eac865534cf5ae545634eb84be5`
4. P04 `38ebffb10411c763ed4f570fdcfece51ab177aa3`
5. P05 `2f4a092fb7f0ec534996e6ad1116e8b50d612b77`
6. P06 `c435caf3cf0cf95fb1bfbc51d2924ea960183957`
7. P07 `c35df75be835d26b4c37618874cf502335b9a24c`
8. P08 `47c2be0358e7ca024b524c7693af8b06846671d6`

P03 and P05 final candidates contain rejected parents in their parcel histories. Only their final aggregate trees are accepted. A future Gate 3 request must specify a merge method that does not promote an intermediate rejected tree as independently accepted custody.

## Deterministic integrated verification

All verification ran against the exact staged integration tree with local/synthetic fakes and no provider, cloud, production database/data, protected-data, secret, remote-Git, or external-payload access.

### Backend

Command:

```powershell
dotnet test backend/BioStack.sln --no-restore --verbosity minimal --disable-build-servers
```

Result: exit 0; exactly 2,065 passed and 5 expected live-Collective skips:

- `BioStack.Domain.Tests`: 7 passed
- `BioStack.Application.Tests`: 656 passed, 5 skipped, total 661
- `BioStack.ProtocolOperationsExportBundleVerifierCli.Tests`: 158 passed
- `BioStack.Api.Tests`: 378 passed
- `BioStack.KnowledgeWorker.Tests`: 866 passed

The result matches the arithmetic aggregate of the accepted parcel deltas. Existing unrelated compiler/analyzer warnings remained non-failing.

Paired outbound authority check:

```powershell
dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --no-restore --filter 'FullyQualifiedName~BioStack.Api.Tests.Integration.ConsentGateIntegrationTests' --disable-build-servers --logger 'console;verbosity=minimal'
```

Result: exit 0; 17 passed, 0 failed/skipped.

The new worktree received only ignored NuGet restore metadata copied from `D:\Repos\BioStack\backend`, whose tracked base is the identical pinned commit/tree and whose project/dependency files are unchanged by every candidate. Fourteen project `obj` roots received 73 generated NuGet files; every source/destination SHA-256 matched. The per-file receipt is `integration-nuget-seed-manifest.sha256`. No restore or registry access occurred.

### Research sidecar

Environment: `UV_OFFLINE=1`, `uv run --offline`, external environment `C:\Users\clint\AppData\Local\Temp\biostack-remediation-integration-sidecar-venv`.

```powershell
uv run --offline pytest -q -rs
```

Result: exit 0; 53 passed, 1 expected legacy-config skip, 0 failed/errors.

The exact combined AF lint command reported the unchanged 13 expected findings: P05's 8 plus P06's 5, with no test-file finding. Its nonzero exit is the ratified ambient baseline, not a regression.

### Frontend

The shell cleared `OPENAI_API_KEY`, `OPENAI_REVIEW_MODEL`, `RESEARCH_AI_SUGGEST_ENABLED`, `API_URL`, and `NEXT_PUBLIC_API_URL`. `npm ci --offline` exited 0 from local cache; no registry fallback was used.

- Exact P07 target: 2 files, 10 passed, exit 0
- Full deterministic Vitest suite: 136 files, 989 passed, exit 0
- Focused ESLint across all three AF-P07 files: zero findings, exit 0
- `NEXT_TELEMETRY_DISABLED=1 npm run build`: exit 0

Known jsdom navigation notices and the existing Next.js middleware deprecation warning were non-failing and unchanged in authority.

## Security and release custody

- P02, P07, and P08 retain accepted SG-OUTBOUND/SG-SCOPE reviews and local-fake zero/one-call controls.
- P04 retains accepted SG-EVIDENCE/SG-SCOPE review.
- P05 and P06 retain accepted SG-SIDECAR/SG-SCOPE reviews and their exact combined-candidate verification.
- P03's Amendment-05 final candidate has a fresh independent adversarial ACCEPT with no actionable findings.
- Diagnostic branches remain preserved and unmerged.
- Q01, Q03, and Q04 retain accepted evidence-backed dispositions; Q02 remains explicitly inconclusive and terminally stopped, not silently fixed.

No OS-level packet capture was used. Network denial is evidenced by the explicit offline flags, cleared provider-related shell variables, local-fake design, absence of network-capable live-test execution, and skipped live Collective cases.

## Gate boundary and next action

This green integration receipt authorizes no commit, merge, push, PR, deployment, publication, release, provider enablement, or diagnostic cleanup. Obtain a fresh independent review of the exact staged integration tree and this receipt. Only after acceptance may the coordinator present the exact candidate set, integration tree, merge method/order, rollback plan, and residual uncertainty for human Gate 3.
