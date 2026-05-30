# PR 7A Durable Staged Transcript Candidate Persistence Contract TODO

- [ ] Add Application-only staged candidate review record contract:
  - [ ] Create `Services/TranscriptCandidateReviewRecord.cs`
  - [ ] Include fields:
    - [ ] ArtifactId
    - [ ] Canonicality (must be `non_canonical`)
    - [ ] ReviewState
    - [ ] SourceType
    - [ ] SourceUrl
    - [ ] Provider
    - [ ] IsDeterministicFixture
    - [ ] SegmentCount
    - [ ] SegmentSnapshotSignature
    - [ ] SourceMetadata
    - [ ] CreatedAtUtc
    - [ ] UpdatedAtUtc
    - [ ] RowVersion (optional, infrastructure-agnostic `string?`)
  - [ ] Enforce deterministic invariants:
    - [ ] non_canonical only
    - [ ] ReviewState constrained to lifecycle constants
    - [ ] Required fields validated
    - [ ] No canonical linkage/promotion/extraction/summarization/safety/medical/network fields

- [ ] Add Application-only staged candidate review store interface:
  - [ ] Create `Services/ITranscriptCandidateReviewStore.cs`
  - [ ] Keep interface minimal/boring:
    - [ ] Upsert staged non-canonical candidate
    - [ ] Get by ArtifactId
    - [ ] Update review state only
    - [ ] Minimal list/query only if necessary
  - [ ] Ensure no method surface for:
    - [ ] canonical KnowledgeEntry writes
    - [ ] promotion execution
    - [ ] extraction/summarization/safety/medical/network behavior

- [ ] Add contract tests:
  - [ ] Create `Tests/Services/TranscriptCandidateReviewStoreContractTests.cs`
  - [ ] Assert:
    - [ ] Store contract accepts only `non_canonical`
    - [ ] ReviewState constrained to lifecycle constants
    - [ ] No canonical write method
    - [ ] No promotion execution method
    - [ ] Artifact identity deterministic and stable
    - [ ] SourceMetadata deterministic enough for persistence round-trip

- [ ] Extend existing Application tests:
  - [ ] Update `TranscriptCandidateArtifactReviewServiceTests.cs`
    - [ ] Reassert no forbidden behavior surface
    - [ ] Verify stable mapping compatibility with record contract
  - [ ] Update `TranscriptCandidateReviewLifecycleTests.cs`
    - [ ] Reassert terminal-state invariants
    - [ ] Reassert `approve_for_promotion` is state/eligibility only (not promotion execution)

- [ ] Add architecture gate doc:
  - [ ] Create `docs/architecture/video-channel-staged-candidate-persistence-gate.md`
  - [ ] Must state:
    - [ ] staged candidates separate from canonical KnowledgeEntries
    - [ ] PR7A is contract-only
    - [ ] PR7B is first eligible Infrastructure persistence/migration lane
    - [ ] API waits for PR7A+PR7B validation
    - [ ] approved_for_promotion != promotion execution
    - [ ] non-canonical boundary mandatory until explicit future promotion workflow

- [ ] Validation:
  - [ ] Focused tests for changed/new files
  - [ ] Full `BioStack.Application.Tests`
  - [ ] `git diff --check`
  - [ ] Report exact changed files, exact commands, and counts

- [ ] Hard boundary confirmations:
  - [ ] No DI changes
  - [ ] No API/endpoints
  - [ ] No Infrastructure implementation
  - [ ] No DbContext/DbSet/migrations
  - [ ] No persistence implementation / DB writes
  - [ ] No canonical KnowledgeEntries writes
  - [ ] No promotion workflow execution
  - [ ] No extraction/summarization/safety/medical interpretation
  - [ ] No network calls/transcript fetching/YouTube API
