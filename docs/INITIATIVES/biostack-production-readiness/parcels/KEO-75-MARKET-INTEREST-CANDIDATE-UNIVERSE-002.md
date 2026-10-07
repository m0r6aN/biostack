# Parcel: KEO-75-MARKET-INTEREST-CANDIDATE-UNIVERSE-002

## Goal

Create a deterministic, metadata-only peptide, SERM, SARM, and commonly misclassified adjacent-compound universe, pair it one-to-one with offline research requests, and process it only through BioStack's non-database Research mode.

## Initiative and issue

- Initiative: BioStack Production Readiness & Monetization
- Issue: KEO-75
- Track: M3 - Data & Intelligence Coverage
- Base: `origin/main@9a74df2279383b3ea8f61094b5ef164c0c6a3950`
- Branch/worktree: `codex/keo-75-market-coverage-20260724` / `D:\Repos\BioStack-keo75-market-coverage-20260724`

## Allowed files

- `research/input/candidates/peptide-serm-sarm-market-interest.v1.json`
- `research/research-requests/market-interest-coverage-2026-07-24.v1.json`
- `backend/tests/BioStack.KnowledgeWorker.Tests/MarketInterestCandidateUniverseTests.cs`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-75-MARKET-INTEREST-CANDIDATE-UNIVERSE-002.md`

## Boundary

The 70 entries are search/identity and queue targets, not medical facts. Discovery source IDs are routing hints only. No source text, evidence claim, dosing or cycle guidance, license decision, source activation, database write, review decision, promotion authority, API/runtime change, or customer-facing output is included.

All 13 real registry sources remain `pending-human-legal`, operations-disabled, and acquisition-disabled. KEO-73 owners/targets and KEO-74 source-by-source legal, security, evidence, retention, remediation, and acquisition approvals remain mandatory.

## Routing record

- Deterministic repository and Linear inventory first.
- Local probe `rtk proxy ollama run qwen3.5:9b "Return exactly OK and nothing else."` produced no output and was terminated after about 80 seconds.
- Local probe `rtk proxy ollama run qwen2.5-coder:3b "Return exactly OK and nothing else."` produced no output and timed out after 64.1 seconds with exit 124.
- Both local routes were classified degraded/unverified and no model output was accepted.
- A frontier research subagent assembled the current public candidate universe; deterministic schema/tests and independent root review own acceptance.

## Acceptance

- Exactly 70 schema-valid candidates and 70 schema-valid requests.
- Case-insensitive canonical names are unique and correspond one-to-one.
- Required peptide, SERM, SARM, and adjacent representatives exist.
- Every candidate remains source-registry-pending and human-review-required.
- Ibutamoren, Cardarine, and Stenabolic are explicitly not classified as SARMs.
- BioStack Research mode validates and queues the artifacts offline without Postgres.
- Existing evidence remains review/authorization blocked; missing evidence becomes research-requested.
- No canonical promotion is eligible.
- Focused and full KnowledgeWorker tests pass; diff scope is exactly the four allowed files.

## Commands

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~MarketInterestCandidateUniverseTests
rtk proxy powershell -File tools/research/run-knowledge-research.ps1 -CandidateFile research/input/candidates/peptide-serm-sarm-market-interest.v1.json -SourceRegistryFile research/input/sources/pilot-source-registry.json -EvidenceDirectory research/input/evidence -ReviewDecisionDirectory research/review-decisions -ResearchRequestPath research/research-requests/market-interest-coverage-2026-07-24.v1.json -OutputDirectory research/output/market-interest-20260724
rtk proxy dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --logger "console;verbosity=minimal"
rtk git diff --check
```

## Status

Completed offline on 2026-07-24:

- Candidate universe: 70; paired research requests: 70.
- Existing evidence scanned: 16; created as drafts: 16; flagged for review: 16; failed: 0.
- Research summary: 78 compounds represented, including eight evidence-backed compounds outside this 70-item market-interest universe.
- Task queue: 78 total, 33 high priority, 45 normal, and eight resolved at the identity/request layer.
- Promotion state: 62 research-requested, 16 blocked, and zero candidates eligible or exported for canonical promotion.
- Focused candidate-universe tests passed.
- Full `BioStack.KnowledgeWorker.Tests` suite passed: 246 passed, 0 failed, 0 skipped.
- The run emitted existing high-severity advisory warnings for `System.Security.Cryptography.Xml 10.0.9`; dependency remediation is outside this data-only parcel.

The output under `research/output/market-interest-20260724` is generated/ignored evidence and is not part of the parcel diff. No Postgres connectivity, live API call, or canonical knowledge mutation occurred.

## Stop conditions

Stop before any source acquisition, source-state change, source content storage, evidence generation, review approval, promotion, database/API/live-environment mutation, or claim that coverage is complete or user-facing.
