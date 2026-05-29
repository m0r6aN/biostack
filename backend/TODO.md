# PR 4 Transcript Candidate Artifact Staging TODO

- [x] Add deterministic candidate descriptor model in Application.
- [x] Add staging service contract in Application.
- [x] Implement pure in-memory staging service (no persistence, no promotion).
- [x] Add focused staging tests:
  - [x] Stage_FromResolvedTranscriptMaterial_ReturnsDeterministicCandidateDescriptor
  - [x] Stage_IncludesSourceReferenceProviderAndSegmentSnapshotMetadata
  - [x] Stage_DoesNotSummarizeOrExtractClaimsOrSafetyOrMedicalFields
  - [x] Stage_DoesNotPromoteOrWriteCanonicalKnowledge
  - [x] Stage_IsDeterministicForSameInput
- [ ] Run validation test slices:
  - [ ] TranscriptCandidateArtifactStagingServiceTests
  - [ ] QueuedIntakeTranscriptResolutionServiceTests
  - [ ] TranscriptSourceMaterialProviderTests
  - [ ] KnowledgeSourceIntakeServiceTests (if needed by touched contracts)
  - [ ] Full Application tests (if broad contract impact)
- [ ] Run `git diff --check` and ensure clean output.
- [ ] Prepare final PR4 validation report with explicit boundary confirmations.
