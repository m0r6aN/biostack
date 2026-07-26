# Parcel: KEO-74-DURABLE-SOURCE-ACQUISITION-RUNTIME-015

Status: implementation and focused local verification complete; independent review and publish pending.

## Goal

Add one DB-free worker mode that turns the approved 70-request, seven-source
campaign into durable normalized source-candidate and minimal receipt artifacts.
It does not create evidence packets, write canonical data, promote claims, or
join the existing ResearchJob output pipeline.

## Boundary

- Input is exactly one research-request batch, source-decision batch, and source
  registry. Each file is read once into a fixed-size buffer; schema validation,
  planning, and SHA-256 binding use those same bytes.
- Activation requires the existing preflight to return exactly
  `70/490/490/0/7` for unique requests/intents/ready/blocked/sources.
- The catalog is fixed to the six merged API adapters. `nih-nccih` remains a
  manual-review lane and is never passed to an automated adapter.
- PubMed tool and contact configuration are required but never logged or
  persisted. API-key configuration is rejected.
- All automated acquisition is serial and uses adapter-owned request budgets.
  The runtime adds no retry loop, scheduler, or parallelism.
- Output is restricted to the configured existing
  `ResearchOutput/source-acquisition/v1/<cycleId>` tree. Traversal and existing
  reparse-point paths fail closed.
- The caller must provide a stable cycle ID and positive candidate/receipt
  retention values. There are deliberately no retention defaults.
- Only normalized `SourceAcquisitionCandidate` values and minimal status/error
  receipts are retained. Raw responses, full text, private data, credentials,
  PubMed runtime identity, exception stacks, and arbitrary exception messages
  are not persisted.

## Durable behavior

- One exclusive cycle lock prevents concurrent writers.
- Intent IDs are SHA-256 values over the stable cycle ID, exact input hashes,
  and deterministic intent projection.
- Each intent has one immutable `attempt.json` for the cycle and one atomic
  `checkpoint.json` containing the attempt hash.
- A restart validates an existing checkpoint/attempt pair and does not call an
  adapter again. An attempt without a checkpoint reconstructs its checkpoint
  without transport.
- Attempts are content-addressed by the SHA-256 of their exact bytes. Resume
  validates that address plus the complete input/intent/status/retention
  boundary before trusting an attempt.
- Corrupt attempt/checkpoint pairs and orphan checkpoints receive a flushed
  content-free quarantine marker before each suspect file is atomically moved
  into the contained cycle quarantine and removed. Only sanitized artifact
  names and bounded identifiers remain; candidate content is not retained.
  A crash may leave a quarantined payload, but the unresolved marker keeps the
  intent fail-closed and the next resume purges that payload before transport.
- When the caller-approved retention interval expires, the runner first writes
  and flushes an immutable content-free `tombstone.json` retaining only cycle,
  intent/source/request identifiers, original status, timestamps, and the
  removed attempt hash. Only after that terminal marker exists does it delete
  the attempt and checkpoint. A crash at any point resumes from the tombstone
  and repeats cleanup without transport. The same cycle therefore remains
  terminal after candidate or receipt removal. The manifest and resume halt
  logic preserve the tombstone's original status, so expired rate-limited,
  backpressure, error, truncated, and not-attempted outcomes remain incomplete.
- Manifest and dedicated source-acquisition review queue are atomically
  replaced derived views. They are not canonical evidence or database input.
- `429`/rate-limit, `503`/backpressure, and bounded source errors make the run
  incomplete. The runner performs no retry and records remaining automated
  intents as `not-attempted`; manual NCCIH intents still receive
  `manual-review-pending` receipts.
- A truncated automated batch is retained as bounded candidate output with
  status `truncated`, never `completed`; it halts later transport and keeps the
  cycle incomplete before and after expiry.
- API candidates cross a strict persistence boundary: substantive bounded
  fields, authorized-use subset, complete required provenance, source-specific
  provenance shape, rights attributions, reuse boundary, and null manual audit.
  Existing FDA output that lacks these governed fields fails closed as the
  explicit adapter-integration blocker; this parcel does not fabricate them.
- Cancellation is not converted into an error receipt; it propagates to the
  one-shot worker and process exit contract.

## Files

- `backend/src/BioStack.KnowledgeWorker/Pipeline/SourceAcquisitionRuntime.cs`
- `backend/src/BioStack.KnowledgeWorker/Jobs/SourceAcquisitionJob.cs`
- `backend/src/BioStack.KnowledgeWorker/Config/ProductionSafetyGuard.cs`
- `backend/src/BioStack.KnowledgeWorker/Config/RunMode.cs`
- `backend/src/BioStack.KnowledgeWorker/Config/WorkerOptions.cs`
- `backend/src/BioStack.KnowledgeWorker/Program.cs`
- `backend/src/BioStack.KnowledgeWorker/Workers/IngestionWorker.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/SourceAcquisitionRuntimeTests.cs`
- `research/routing-events/keo-74-durable-source-acquisition-runtime-20260726.json`
- this parcel record

## Configuration

Required under `Worker` when `RunMode=SourceAcquisition`:

- `SourceAcquisitionResearchRequestPath`
- `SourceAcquisitionDecisionPath`
- `SourceAcquisitionRegistryPath`
- `SourceAcquisitionCycleId`
- `SourceAcquisitionCandidateRetentionDays` (positive)
- `SourceAcquisitionReceiptRetentionDays` (positive)
- `SourceAcquisitionPubMedTool`
- `SourceAcquisitionPubMedContactEmail`
- an already-existing `ResearchOutputDirectory`

`SourceAcquisitionPubMedApiKey` must be absent or blank.

## Acceptance

- Invalid/missing configuration, oversized or schema-invalid input, registry
  byte-hash mismatch, non-exact preflight, catalog mismatch, path escape, or
  reparse-point traversal fails before adapter transport.
- Exact campaign output has 490 immutable intent attempts: 420 automated source
  results plus 70 NCCIH manual-review receipts.
- Restart of a completed cycle performs zero duplicate calls.
- Candidate identity/order and intent order are deterministic.
- Candidate count and serialized attempt size are fixed and bounded.
- A corrupted checkpoint or orphan checkpoint is quarantined without content
  echo and fails closed before transport.
- Legacy API candidates missing governed persistence metadata fail closed
  without retaining the candidate.
- Truncated output and expired incomplete tombstones remain incomplete and
  cannot reopen later API transport.
- A second live runner cannot acquire the cycle lock.
- No source exception message, stack, raw response, PubMed runtime identity, API
  key, database call, evidence packet, canonical record, or promotion output is
  emitted.
- Focused tests pass without live source requests.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~SourceAcquisitionRuntimeTests --no-restore
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter "FullyQualifiedName~SourceAcquisitionExecutionPreflightTests|FullyQualifiedName~SourceAcquisitionPlanningTests" --no-restore
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore
rtk git diff --check
rtk git status --short
```

## Non-goals

- No live call during tests or parcel verification.
- No database or canonical-ingest registration.
- No ResearchJob, evidence-packet, compiler, promotion, API, frontend,
  deployment, registry, decision, or adapter modification.
- No automatic retries or scheduling. Retention duration remains caller-owned
  with no default; this parcel enforces positive values up to the fixed
  ten-year safety ceiling and performs the generic crash-safe expiry lifecycle
  described above.
- No commit, push, PR, or deployment from this parcel session.
