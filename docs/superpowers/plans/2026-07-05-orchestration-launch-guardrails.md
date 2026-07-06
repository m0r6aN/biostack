# BioStack Orchestration Launch Guardrails Implementation Plan

**For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) superpowers:executing-plans implement plan task-by-task. Steps use checkbox (`- [ ]`) syntax tracking.

**Goal:** Remove backend launch blockers for future source-lane orchestration without launching any long-running operation.

**Architecture:** Keep the source lane on existing intake, transcript resolution, staged review, promotion, and Governed Spine receipt seams. Add nullable provenance and lifecycle fields only where needed, use existing receipt taxonomy, and fence the legacy admin ingest bypass behind disabled-by-default configuration plus explicit override governance.

**Tech Stack:** C#, ASP.NET Core minimal APIs, EF Core migrations, xUnit integration/unit tests, SQLite test databases.

---

### Task 1: Intake lifecycle and staged provenance

**Files:**
- Modify: `backend/src/BioStack.Application/Services/QueuedIntakeTranscriptResolutionService.cs`
- Modify: `backend/src/BioStack.Application/Services/TranscriptCandidateReviewRecord.cs`
- Modify: `backend/src/BioStack.Infrastructure/Persistence/Entities/StagedTranscriptCandidateReviewEntity.cs`
- Modify: `backend/src/BioStack.Infrastructure/Persistence/BioStackDbContext.cs`
- Modify: `backend/src/BioStack.Api/Endpoints/AdminStagedTranscriptCandidateReviewResponse.cs`
- Modify: `backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs`
- Create: `backend/src/BioStack.Infrastructure/Persistence/Migrations/20260705000000_PR172_AddSourceLaneLaunchGuardrails.cs`

- [ ] Write failing tests for successful resolution status advancement and `IntakeRequestId` provenance.
- [ ] Write failing test for failed resolution status and `FailureReason`.
- [ ] Implement minimal lifecycle updates and nullable provenance mapping.
- [ ] Add migration and snapshot/model updates for `IntakeRequestId`.

### Task 2: Existing taxonomy receipts

**Files:**
- Modify: `backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs`
- Modify: focused API integration tests.

- [ ] Write failing tests for `source.intake.received`, `source.transcript.resolved`, `source.candidate.staged`, `source.review-state.changed`, and `source.artifact.promoted`.
- [ ] Issue receipts through `IRuntimeReceiptFactory` at existing source-lane API points with deterministic seeds and useful evidence refs.

### Task 3: Admin ingest bypass fence

**Files:**
- Modify: `backend/src/BioStack.Api/Endpoints/AdminEndpoints.cs`
- Modify: focused API integration tests.

- [ ] Write failing test that `/api/v1/admin/knowledge/ingest` is disabled by default.
- [ ] Require enabled feature flag and explicit admin override header before canonical writes.
- [ ] When enabled and override is present, issue `admin.override.performed` before the legacy canonical write.

### Task 4: Validation

- [ ] Run focused source intake/review/promotion tests.
- [ ] Run admin transcript intake resolution integration tests.
- [ ] Run receipt/admin integration tests.
- [ ] Run `ProtocolIntelligenceOfflineBoundaryTests`.
- [ ] Run broader backend test slice only as needed for touched contracts.
