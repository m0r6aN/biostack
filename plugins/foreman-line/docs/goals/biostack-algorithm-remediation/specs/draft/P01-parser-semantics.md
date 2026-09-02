# Parcel P01: Parser Semantics

Status: **active — coordinator lint passed 2026-09-02; dispatch remains contingent on exact worktree/base verification and Step 0 confirmation**

## Goal

Remediate only the six parser defects governed by locked decision D5 of the ratified BioStack Algorithm Remediation charter. Preserve the exact diagnostic reproduction class as a retained regression, make the smallest production change inside `ProtocolParser.cs`, and prove the result with offline targeted, adjacent, and aggregate verification. This parcel does not authorize a push, pull request, merge, deployment, release, publication, or mutation of any diagnostic branch.

## Initiative / Project / Wave

- Initiative: `biostack-algorithm-remediation`
- Project: `BioStack` / backend `BioStack.Application`
- Parcel: `P01-parser-semantics`
- Wave: Wave 1 — independent fixes
- Risk/routing: high correctness risk; standard implementation routing
- Production owner: `ProtocolParser` (module/class ownership only; the pinned base has no repository `CODEOWNERS` file)

## Branch / Worktree / Base

- Branch: `codex/biostack-remediation-p01`
- Isolated worktree: `C:\Users\clint\.codex\worktrees\biostack-remediation-p01\BioStack`
- Required starting commit: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Required starting tree: `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`
- Diagnostic evidence commit: `817f6f3331c2b7c3410da63289c02fb27a98ed74`
- Diagnostic test blob: `ab4bd922f2f17c05b37e05115245e52ef265ee98`
- Pinned-base/current-goal-worktree production blob: `eaad1b314db45753fa942adb0dc6f6a13600f873`

The parcel branch must be created from the required starting commit, not from a diagnostic branch. The diagnostic commit is an evidence source only. It is locally proven to descend from the base, but its history must not be merged, cherry-picked, rebased, or otherwise incorporated into this parcel.

## Dependencies and Preconditions

1. Gate 1 for D1-D14 was ratified on 2026-09-02. D5 is not reopened by pending Gate 1 Amendment 01, which is limited to D6-D9; P01 may therefore be shaped independently.
2. Gate 2 is contingent, not automatic. Before dispatch, the coordinator must lint this draft, verify that the parcel worktree and branch start at the pinned base, confirm the exact Allowed Files, and approve the builder's Step 0 restatement.
3. The coordinator, not the builder, owns any required `origin/main` comparison. The builder must not fetch. If the coordinator reports that `origin/main` moved materially from the pinned base, this parcel stops under the charter's base-change rule.
4. P01 has no dependency on Q04 or another implementation parcel and must not consume another parcel branch. No other live builder may edit either AF-P01 file.
5. The diagnostic reproduction file exists at the diagnostic commit and is absent at the pinned base. The SDK-style test project at `backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj` targets `net10.0`, is marked as a test project, and includes ordinary `.cs` files by default; no project-file change is required or allowed.
6. Required .NET SDK, NuGet cache, and restore assets must already be available locally. This parcel may not access a registry or other network service to obtain them. Missing local assets are an environment blocker, not authority to restore online or change package files.

## Security and Release Gates

- **SG-SCOPE applies:** only AF-P01 may change; fixtures are synthetic/local; no secrets, protected data, payload dumps, production or cloud access, external providers, or unapproved network activity.
- The imported tests construct `LocalKnowledgeSource`, `BlendDecomposerService`, and `MemoryCache`. On the pinned base, `LocalKnowledgeSource.GetAllCompoundsAsync` returns an in-memory copy at `backend/src/BioStack.Infrastructure/Knowledge/LocalKnowledgeSource.cs:24-27`; no external source is needed.
- D10 applies: all verification is offline. Do not run `dotnet restore`, `git fetch`, `git pull`, or any command that can contact a registry, provider, cloud resource, production database, or remote Git host. Test commands use `--no-restore`.
- D11 applies: a fresh independent read-only adversarial review is required on the exact candidate commit. P01 is not one of the parcels requiring two adversarial reviewers or a separate defensive security reviewer, but any security-relevant finding still blocks Gate 3 until dispositioned.
- Gate 3 is ungranted and human-only. No push, PR creation, merge, deployment, provider enablement, publication, release, or diagnostic-branch cleanup is authorized.
- Passing builder tests alone does not clear review, Gate 3, merge, or release.

## Allowed Files — AF-P01

Exactly these two repository paths may be created or modified:

1. `backend/src/BioStack.Application/Services/ProtocolParser.cs`
2. `backend/tests/BioStack.Application.Tests/Services/ProtocolParserReproductionTests.cs`

If either path is missing, insufficient, or cannot carry the complete D5 contract, stop for a charter/spec amendment. Do not substitute a nearby file.

## Forbidden and Out of Scope

- Every repository path outside AF-P01 is forbidden, including the solution, test project, package/lock files, build configuration, fixtures, generated files, goal documents, and handoff records.
- Do not import `backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionPdfReproductionTests.cs`, even though the diagnostic commit also added it. That file belongs to AF-P02.
- Do not merge or cherry-pick diagnostic commit `817f6f3331c2b7c3410da63289c02fb27a98ed74`; import only the one P01 test file's contents into the worktree.
- Do not amend, rebase, force-update, delete, clean, or otherwise mutate the diagnostic coordination branch or any diagnostic parcel branch.
- Do not rename, delete, skip, weaken, reorder into dependence, or replace any of the six diagnostic test scenarios or method names. Do not change the expected values merely to make current behavior pass.
- Do not add a package, dependency, schema, migration, configuration switch, feature flag, endpoint, response field, public contract, provider call, telemetry payload, or network-capable fixture.
- Do not refactor unrelated parser behavior, redesign protocol ingestion, or change interaction, evidence, sidecar, OCR, frontend, or consent behavior.
- Do not interpret D5 as authority for a general grammar rewrite, arbitrary punctuation splitting, locale-specific comma decimals, fuzzy alias matching, or recognition of new undosed compound forms.
- Do not create a `ShapingResult` JSON. BioStack does not include the Foreman emitter/linter required to author one.
- Do not stage or commit during shaping. A future builder may make one local candidate commit only after the coordinator confirms Step 0 and all required verification is green.

## Verified Existing Patterns on the Pinned Base

All line references in this section are to commit `339f259b1a467034db4f57cf9d774c292f11b53a`.

- `backend/src/BioStack.Application/Services/ProtocolParser.cs:11-13` defines the compiled dose regex. It accepts integer or digit-leading decimal doses and recognizes `mcg`, `microgram(s)`, `ug`, Greek mu `μg`, `mg`, and `milligram(s)`; it does not accept a leading decimal or U+00B5 micro-sign form.
- `backend/src/BioStack.Application/Services/ProtocolParser.cs:67-72` already composes `SplitIntoSegments` with `SelectMany(ParseSegment)` before grouping and merging entries. Preserve this ability for one input to yield multiple entries.
- `backend/src/BioStack.Application/Services/ProtocolParser.cs:84-200` is the narrow parsing path. The blend branch already yields multiple component entries at lines 102-133, while the non-blend alias branch returns one entry at lines 156-168. Extend only as needed for independently dosed clauses; do not disturb blend behavior.
- `backend/src/BioStack.Application/Services/ProtocolParser.cs:99` removes a broad leading character class that includes digits. A fix must distinguish a real list marker from a compound-leading digit rather than globally retaining or globally removing all leading numeric syntax.
- `backend/src/BioStack.Application/Services/ProtocolParser.cs:120-121` and `:142-143` convert blend and single-entry doses with culture-sensitive `double.TryParse`. Both conversion sites are within D5's invariant-culture obligation.
- `backend/src/BioStack.Application/Services/ProtocolParser.cs:241-250` preserves the existing longest-alias-wins rule. `ContainsAlias` at `:364-368` currently uses arbitrary substring containment after normalization; replace only that membership semantics with token-bounded matching.
- `backend/src/BioStack.Application/Services/ProtocolParser.cs:370-379` splits newlines, semicolons, spaced plus signs, and table pipes, but not commas. A repair for independently dosed comma clauses must not make every comma a compound boundary or break cycle prose.
- `backend/src/BioStack.Application/Services/ProtocolParser.cs:382-386` normalizes Greek mu before replacing punctuation with spaces. `NormalizeUnit` at `:395-402` maps Greek-mu, `ug`, `mcg`, and word forms to `mcg`; both places currently omit U+00B5.
- `backend/tests/BioStack.Application.Tests/Services/ProtocolAnalyzerServiceTests.cs:13-24` constructs the production parser with local knowledge, blend decomposition, and in-memory cache. Existing direct parser-integrated examples include simple parsing at `:62-67`, blend/cycle dose preservation beginning at `:165-170`, and compound-plus-cycle preservation beginning at `:302-307`.
- `backend/tests/BioStack.Application.Tests/Services/ProtocolAnalyzerDocxPacketGoldenTests.cs:35-39`, `:65-72`, `:125-130`, and `:171-179` exercise recognition gating and known-compound retention through the production parser against a local fixture.
- `backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionServiceTests.cs:13-19`, `:28-33`, and `:82-87` cover paste, spreadsheet, and local fake OCR ingestion paths. These are read-only adjacent tests for P01.
- `backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj:1-8`, `:10-23`, and `:29-32` establish the `net10.0` SDK-style xUnit test project and its application project reference. No explicit compile inclusion is needed for the new test file.

## Exact D5 Contract

The following six clauses are exhaustive for P01. They must all hold, and they must not be silently broadened into a new parser contract.

1. **Token-bounded alias matching — R-PARSE-01.** After the parser's existing lookup normalization, an alias matches only as a complete normalized token sequence: the sequence begins at normalized-string start or after a normalized space, and ends at normalized-string end or before a normalized space. A token may not match inside a larger alphanumeric token. Preserve case-insensitive comparison and longest-alias-wins. Therefore normalized `Tβ4` must not be inferred from the interior characters of `At 4000`, and `ParseAsync("At 4000 steps nightly")` yields no entries. Do not add fuzzy, prefix, stemming, or new normalization semantics.
2. **Every independently dosed compound survives — R-PARSE-02.** A text span containing multiple compound clauses, each with its own dose, may yield one entry per independently dosed compound. For the governed scenario `BPC-157 500mcg daily, TB-500 2mg twice weekly`, the result contains both canonical entries with their own doses and units: `BPC-157`/`500`/`mcg` and `TB-500`/`2`/`mg`. Do not treat arbitrary comma-delimited prose or undosed names as compounds, and do not change grouping/merge behavior beyond what is needed to preserve these independently dosed entries.
3. **Invariant-culture numeric conversion — R-PARSE-03.** Every dose conversion performed by this parser, including blend and non-blend paths, interprets the regex-captured dot-decimal grammar with invariant culture. Under `fr-FR`, `BPC-157 0.5mg daily` yields dose `0.5` and unit `mg`. This does not authorize locale-specific comma decimals or a broader numeric grammar.
4. **Compound-leading digits are data — R-PARSE-04.** Leading cleanup must not strip the leading digit from `5-Amino-1MQ 50mg daily`; the emitted compound name is exactly `5-Amino-1MQ`, dose `50`, unit `mg`. Existing bullet/list cleanup may remain only where syntax is demonstrably a complete marker separated from the compound text. Do not invent general markup parsing.
5. **Leading-decimal magnitude and precision are preserved — R-PARSE-05.** The dose grammar accepts a leading decimal only when at least one digit follows the dot. `BPC-157 .25mg daily` yields dose `0.25`, not `25`, with unit `mg`. Integer and ordinary digit-leading decimals must continue to work. No exponent, sign, locale comma, or bare-dot support is added.
6. **Both Unicode micro characters normalize to `mcg` — R-PARSE-06.** Greek small letter mu U+03BC (`μg`), micro sign U+00B5 (`µg`), ASCII `ug`, and `mcg` all parse as the existing `mcg` unit. `BPC-157 500\u00b5g daily` yields dose `500` and unit `mcg`. No other unit family is added or changed.

Preserve all parser behavior not necessary to satisfy these clauses, especially recognition gating, cycle-phrase handling, schedule-code rejection, blend decomposition, longest-alias selection, entry grouping/merge rules, frequency normalization, and existing units.

## Diagnostic Import Instructions

After Step 0 is confirmed and while `HEAD` is still the pinned base, import exactly one worktree file without merging diagnostic history:

```powershell
git restore --source=817f6f3331c2b7c3410da63289c02fb27a98ed74 --worktree -- backend/tests/BioStack.Application.Tests/Services/ProtocolParserReproductionTests.cs
git hash-object backend/tests/BioStack.Application.Tests/Services/ProtocolParserReproductionTests.cs
```

The hash command must return `ab4bd922f2f17c05b37e05115245e52ef265ee98`. If it does not, stop. Do not restore the other file from that commit. Keep the class name `ProtocolParserReproductionTests`, its helper, all six `[Fact]` attributes, method names, input strings, culture setup/cleanup, ordering behavior, and assertions byte-for-byte. New or changed scenarios require coordinator review and a spec amendment before implementation.

The six retained methods are:

1. `ParseAsync_AliasInsideUnrelatedTokens_DoesNotCreateCompound`
2. `ParseAsync_CommaSeparatedCompounds_PreservesEveryCompound`
3. `ParseAsync_DotDecimalDose_IsCultureInvariant`
4. `ParseAsync_UnicodeMicroSign_PreservesDoseAndNormalizesUnit`
5. `ParseAsync_LeadingDigitCompoundName_PreservesFullName`
6. `ParseAsync_LeadingDecimalDose_PreservesPrecision`

## Implementation Constraint

Make the smallest coherent change to `ProtocolParser.cs` that satisfies all six exact clauses. Prefer existing parser helpers and control flow where they can carry the contract. No test-only production branch, special-case input string, reflection hook, or weakened recognition gate is acceptable. If a clause cannot be implemented cleanly inside the one production file while retaining the diagnostic file unchanged, stop for amendment.

## Required Tests and Acceptance Criteria

- All six diagnostic methods compile, are discovered, and pass unchanged.
- The target class has exactly six discovered tests: six passed, zero failed, zero skipped.
- The adjacent `ProtocolAnalyzer*` and `ProtocolIngestion*` tests pass with the same discovered-test count as their clean-base receipt.
- The full `BioStack.Application.Tests` project passes. Relative to the clean-base receipt, its discovered total and passed count increase by exactly six, while failed remains zero and skipped remains unchanged.
- The backend solution aggregate passes offline from already-restored assets. Relative to its clean-base receipt, only the Application test assembly gains six discovered/passed tests.
- The final diagnostic test file hash remains `ab4bd922f2f17c05b37e05115245e52ef265ee98`.
- Exactly the two AF-P01 paths differ from the base; no staged, modified, or untracked path exists outside AF-P01.
- The candidate descends from the pinned base, contains no merge commit from the diagnostic lineage, and the diagnostic commit is not an ancestor of the candidate.
- `git diff --check` is clean.
- No test or command contacted an external service.

## Exact Verification Commands

`rtk` was not resolvable in the shaping worktree's PowerShell session. The raw commands below were syntax-checked against local Git 2.45.2 and the installed `dotnet test` help, which confirms `--filter`, `--list-tests`, `--logger`, `--no-restore`, and `--disable-build-servers`. If `rtk` is available in the parcel session, the builder may prefix each command with the matching `rtk` form; otherwise these raw commands are the repository AGENTS.md debugging fallback.

Run from the parcel worktree root. Do not remove `--no-restore` from parcel test commands.

### Baseline receipt after Step 0 confirmation, before import or production edits

```powershell
git rev-parse HEAD
git status --short
git rev-parse 339f259b1a467034db4f57cf9d774c292f11b53a:backend/src/BioStack.Application/Services/ProtocolParser.cs
git cat-file -e 339f259b1a467034db4f57cf9d774c292f11b53a:backend/tests/BioStack.Application.Tests/Services/ProtocolParserReproductionTests.cs
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Services.ProtocolAnalyzer|FullyQualifiedName~BioStack.Application.Tests.Services.ProtocolIngestion" --disable-build-servers --logger "console;verbosity=minimal"
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --disable-build-servers --logger "console;verbosity=minimal"
```

Expected static receipts: `HEAD` equals the pinned base; status is empty; the production blob is `eaad1b314db45753fa942adb0dc6f6a13600f873`; `git cat-file -e` exits `128` because the reproduction path is absent on base. Record the adjacent and full-project `Passed`, `Failed`, `Skipped`, and `Total` summaries as `B_adj` and `B_app`. A failing baseline or missing local restore assets stops the parcel; it does not authorize network access.

### Targeted regression

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Services.ProtocolParserReproductionTests" --disable-build-servers --logger "console;verbosity=minimal"
```

Expected candidate receipt: passed `6`, failed `0`, skipped `0`, total `6`.

### Adjacent parser and ingestion suites

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~BioStack.Application.Tests.Services.ProtocolAnalyzer|FullyQualifiedName~BioStack.Application.Tests.Services.ProtocolIngestion" --disable-build-servers --logger "console;verbosity=minimal"
```

Expected candidate receipt: the full summary equals `B_adj`; failed is `0`.

### Application-project and backend-solution aggregate

```powershell
dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --no-restore --disable-build-servers --logger "console;verbosity=minimal"
dotnet test backend/BioStack.sln --no-restore --verbosity minimal --disable-build-servers
```

Expected Application receipt: candidate total and passed are `B_app + 6`, failed is `0`, and skipped equals the baseline. The solution run is the P01 aggregate check; record each test assembly's summary and confirm that only `BioStack.Application.Tests` gains six tests.

The goal-level F11 matrix also names `dotnet test backend/BioStack.sln --verbosity minimal`. The coordinator may consume that exact command later only in an already dependency-ready, network-prohibited aggregate environment. P01 builder authority does not include an implicit restore or any network access.

### Diff, ancestry, file-identity, and cleanliness receipts

Run after the candidate commit exists:

```powershell
git merge-base --is-ancestor 339f259b1a467034db4f57cf9d774c292f11b53a HEAD
git merge-base --is-ancestor 817f6f3331c2b7c3410da63289c02fb27a98ed74 HEAD
git rev-list --merges 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --check 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git diff --name-only 339f259b1a467034db4f57cf9d774c292f11b53a..HEAD
git hash-object backend/tests/BioStack.Application.Tests/Services/ProtocolParserReproductionTests.cs
git status --short
```

Expected results: base-ancestor exits `0`; diagnostic-ancestor exits `1`; merge list is empty; diff check exits `0`; name-only output is exactly the two AF-P01 paths; test hash equals the diagnostic blob; final status is empty after the local candidate commit. Capture every exit code explicitly because an empty native-command output is not by itself proof of success.

## Expected Base/New Test Count Receipt Method

The diagnostic file has exactly six `[Fact]` methods and no data-driven rows. The pinned base has no file at that path, so the P01 targeted base count is `0` and the candidate targeted count is exactly `6` (`+6`). The builder must not infer the adjacent or project baseline from an old handoff:

1. On the clean pinned base after Step 0 confirmation, run the baseline adjacent and full Application commands above and copy their console summaries verbatim into the session handoff as `B_adj` and `B_app`.
2. After the production fix, run the targeted, adjacent, and full Application commands from the same SDK/cache environment.
3. Record a table with `Passed`, `Failed`, `Skipped`, `Total`, exit code, and command for base and candidate.
4. Require targeted `0 -> 6`; adjacent `B_adj -> B_adj`; Application total/passed `B_app -> B_app + 6`; Application failed `0 -> 0`; and Application skipped unchanged.
5. Record each solution assembly's baseline/candidate summary. Only the Application assembly may gain the six tests. Any other count drift, missing test, unexpected skip, or discovered extra test stops the parcel for reconciliation.

Do not add receipt files to the repository. Preserve the terminal output in the builder handoff/evidence channel for coordinator consumption.

## Evidence Required

- Starting branch, worktree, commit, tree, clean-status, and production-blob receipts.
- Diagnostic import command, diagnostic commit, imported blob hash, and final unchanged test-file hash.
- Candidate commit and `git show --stat --oneline` plus the complete diff restricted to AF-P01.
- Baseline/candidate count table and verbatim summaries for targeted, adjacent, Application-project, and solution verification, with commands and exit codes.
- `git diff --check`, exact changed-path list, clean final status, base ancestry, no-merge, and diagnostic-not-ancestor receipts.
- A no-network/no-external-service receipt naming the use of `--no-restore`, local synthetic fixtures, and any local process/network isolation the builder used. Do not include secrets, payload dumps, or protected data.
- Fresh read-only adversarial review of the exact candidate commit and the coordinator's disposition of every finding. Rework creates a new commit and invalidates earlier review acceptance.
- Residual risks, blockers, decisions requested, and next safe action.

## Collision Risk

Inter-parcel file collision is low because AF-P01 is unique in the ratified ownership map; no other authorized remediation parcel owns `ProtocolParser.cs` or `ProtocolParserReproductionTests.cs`. Behavioral collision is medium because P02 and existing analyzer/ingestion flows consume parser output. Do not run another live builder against either AF-P01 path. If another branch changes `ProtocolParser.cs` or the pinned base moves, stop, rebase only after coordinator direction, rerun every P01 and adjacent test, and obtain fresh review on the new candidate.

## Step 0 — Restate and Stop Gate

Before any import, edit, test build, dependency action, stage, or commit, the builder must inspect the named local objects and send the coordinator one restatement containing:

1. goal, initiative, parcel, Wave 1 routing, branch, isolated worktree, pinned base commit/tree, current `HEAD`, and clean `git status`;
2. the two exact AF-P01 paths and the statement that every other repository path is forbidden;
3. all six D5 clauses in this spec, including their exact input/output scenarios and the prohibition on widening them;
4. diagnostic commit, exact one-file restore command, diagnostic blob, all six retained method names, and the explicit prohibition on importing the PDF reproduction file or diagnostic history;
5. dependencies and the ruling that pending Amendment 01 does not block P01;
6. targeted, adjacent, Application, aggregate, count-delta, diff, ancestry, and cleanliness commands and expected receipts;
7. SG-SCOPE, offline/no-network/no-provider/no-production-data constraints, local-assets-only rule, fresh independent review, and ungranted Gate 3;
8. all stop conditions below and any ambiguity or mismatch found.

The builder then stops. Only an explicit coordinator confirmation of that restatement permits diagnostic import or implementation. Silence, prior Gate 2 language, a clean base, or this draft alone is not confirmation.

## PR Notes for a Future Authorized PR

No PR may be created now. If a later human authorization permits one, its notes must state:

- **What changed:** the exact D5 parser-semantics repair in `ProtocolParser.cs` and the unchanged six-scenario diagnostic regression class.
- **Why:** prevent substring alias false positives, retain every independently dosed compound, parse dot decimals invariantly, preserve compound-leading digits and leading-decimal magnitude, and normalize both U+03BC and U+00B5 micro units.
- **Scope:** exactly AF-P01; no package, schema, config, endpoint, provider, or public response-contract change.
- **Risk:** central parser semantics can affect analyzer/ingestion output; mitigated by unchanged regressions, existing adjacent suites, full Application/solution verification, exact diff review, and fresh adversarial review.
- **Verification:** list exact commands, exit codes, base/candidate counts, hashes, changed paths, and review disposition.
- **Rollback:** revert only the exact approved P01 candidate commit and rerun the same targeted, adjacent, and aggregate matrix; do not delete the diagnostic branch or rewrite history.
- **Release boundary:** merge or deployment is not implied; cite the exact future Gate 3 approval if one exists.

## Session Handoff Template

- Status: `BLOCKED`, `READY_FOR_REVIEW`, or `REWORK_REQUIRED`; never claim merged, deployed, or released without separate evidence.
- Goal / parcel: `biostack-algorithm-remediation` / `P01-parser-semantics`
- Branch / worktree:
- Starting commit / tree:
- Candidate commit:
- Base production blob / diagnostic test blob / final test-file blob:
- Allowed Files changed:
- Forbidden files changed: `none` or stop
- Diagnostic import command and preservation result:
- D5 clause-by-clause disposition:
- Baseline count table (`B_adj`, `B_app`):
- Candidate targeted / adjacent / Application / solution command results and exit codes:
- Diff-check / changed-path / ancestry / no-merge / clean-status receipts:
- Offline/no-network/no-external-service receipt:
- Adversarial reviewer identity, exact reviewed commit, verdict, findings, and coordinator dispositions:
- Tests failed or skipped:
- Residual risk:
- Decisions needed / blockers:
- Next safe action: coordinator inspection and fresh read-only review, or explicit rework direction
- Do not touch: diagnostic branches/history, any path outside AF-P01, remote Git, PRs, merges, deployments, releases

## Stop Rules

Stop immediately and report if any of the following occurs:

- the parcel branch/worktree is missing, dirty before work, not exactly at the pinned base, or derived from a diagnostic branch;
- the coordinator has not explicitly confirmed Step 0, reports material `origin/main` movement, or identifies another live owner for AF-P01;
- either AF-P01 path is missing/insufficient, the production base blob differs, or the imported/final test blob differs from the pinned diagnostic blob;
- implementation requires any file, dependency, package, config, schema, fixture, or public decision outside this spec;
- a diagnostic name/scenario must be changed, skipped, weakened, or deleted to pass;
- any D5 clause can pass only by weakening recognition gating, changing unrelated parser behavior, or broadening the numeric, alias, delimiter, or unit grammar beyond the exact contract;
- targeted, adjacent, Application, or solution counts drift from the expected receipt; a test fails unexpectedly; or a new skip appears;
- a command would require network, restore, provider, cloud, production database, protected data, secret, or external payload access;
- a security or adversarial finding cannot close inside AF-P01, or rework changes the reviewed candidate without obtaining fresh review;
- a forbidden file appears in the diff or status, the diagnostic commit becomes an ancestor, a merge commit appears, or `git diff --check` fails;
- the same tripwire or false-closure condition repeats twice;
- any push, PR, merge, deployment, publication, release, diagnostic-branch mutation, or cleanup is proposed before explicit human authorization and exact Gate 3.
