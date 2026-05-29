# PR 3 Queued Intake Transcript Resolution TODO

- [x] Add application resolver service for queued-intake transcript material resolution.
  - [x] Accept intake id path.
  - [x] Accept intake entity path.
  - [x] Enforce queued-only before provider call.
  - [x] Enforce transcript-shaped source type only; reject unsupported source types before provider call.
  - [x] Return transcript source material only.
  - [x] Keep resolution read-only (no canonical writes).

- [x] Keep fake transcript provider test-only.
  - [x] No production fake-provider registration.
  - [x] No network calls.

- [x] Add focused resolver tests.
  - [x] queued intake id resolves transcript material via fake provider.
  - [x] queued intake entity resolves transcript material via fake provider.
  - [x] non-queued intake rejects.
  - [x] unknown intake id rejects.
  - [x] unsupported/non-transcript source type rejects before provider call.
  - [x] no canonical knowledge writes during resolution.

- [ ] Verify existing tests still pass.
  - [ ] TranscriptSourceMaterialProviderTests
  - [ ] KnowledgeSourceIntakeServiceTests
  - [ ] AdminKnowledgeSourceIntakeIntegrationTests
  - [ ] Full Application tests only if contracts changed.

- [ ] Produce PR3 validation report.
  - [ ] exact changed files
  - [ ] whether production DI was added
  - [ ] whether endpoint/API changed
  - [ ] exact commands and pass/fail/skip counts
  - [ ] explicit confirmations:
    - [ ] no-network
    - [ ] no-extraction/orchestrator/candidate/safety paths
    - [ ] no-canonical-knowledge-writes
    - [ ] fake-provider-test-onlyUs