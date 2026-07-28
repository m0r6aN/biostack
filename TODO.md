# PR10 TODO

## Admin sign-in production fix (/admin magic-link start)

- [x] Add frontend API proxy endpoint for `POST /api/v1/auth/start` to forward to backend auth service
- [x] Preserve JSON payload and cookie forwarding behavior in proxy response
- [ ] Run focused frontend build/type check to validate route compiles
- [ ] Mark fix tasks complete after verification

- [x] Add configurable YouTube transcript provider options (disabled by default)
- [x] Add MCP client abstraction interface for YouTube transcript retrieval
- [x] Implement YouTubeTranscriptSourceMaterialProvider behind ITranscriptSourceMaterialProvider
- [x] Ensure disabled path is deterministic (`transcript_provider_disabled`) and makes no MCP/network call
- [x] Wire DI/config in API startup safely when provider config is absent
- [x] Preserve fake-provider tests (no behavior regressions)
- [x] Add provider tests: disabled-by-default no-call behavior
- [x] Add provider tests: enabled path maps mocked MCP output to TranscriptSourceMaterialResult
- [x] Add provider tests confirming no canonical/promotion/extraction/safety behavior
- [x] Run focused provider tests
- [x] Run full BioStack.Application.Tests
- [x] Run BioStack.Api.Tests (startup/DI safety)
- [x] Run `git diff --check`
- [x] Prepare final report with changed files, test commands/results, and forbidden-surface confirmations

## Keon offline verification consolidation

### Ownership boundary

Keon owns generic offline and air-gapped deterministic verification
infrastructure: canonical JSON and hashing, receipts/manifests/verifier posture,
portable verifier patterns, offline CLI and auditor-kit conventions,
claims-boundary/non-authority language, negative-fixture strategy, kit manifest
format, and auditor handoff structure.

BioStack owns the domain-specific `ProtocolOperationsExportBundle`,
protocol-specific redaction and provenance fields, health/protocol wording, and
admin/user workflows. BioStack consumes, showcases, and stress-tests the Keon
substrate; it does not become the source of the generic verification model.

### Existing vertical implementation (do not duplicate or remove yet)

- [x] Protocol Operations export bundle and deterministic local verifier exist.
- [x] Receipt-only verification, offline CLI modes, runbook, release checklist,
  smoke scripts, and auditor packet index exist.
- [x] Negative fixtures, stable result-code catalog, air-gap guards, and capstone
  coverage stress-test the current vertical implementation.
- [ ] Preserve BioStack-owned redaction, provenance, observational health wording,
  safety boundaries, and admin/user workflow behavior throughout consolidation.

### Future consolidation lanes

- [ ] **BioStack K-1: Align docs to Keon ownership.** Update verifier-kit,
  architecture, and auditor-facing docs to say BioStack uses a Keon-style offline
  verification kit. Change this to "Keon offline verification infrastructure"
  only after the dependency is real.
- [ ] **BioStack K-2: Add a Keon-compatible adapter seam.** Isolate shared
  canonical serialization and hash conventions behind an adapter while keeping
  `ProtocolOperationsExportBundle`, protocol-specific redaction/provenance, and
  BioStack result wording domain-owned.
- [ ] **BioStack K-3: Replace duplicated generic language.** Use
  "Keon-compatible offline inspection" for the current independent
  implementation; do not imply that BioStack owns the generic verifier
  philosophy or that a Keon dependency already exists.
- [ ] **BioStack K-4: Consume stable Keon package/tooling.** Replace duplicated
  mechanics only after Keon publishes a versioned package/tooling contract,
  compatibility fixtures, migration guidance, and rollback path. Keep the
  BioStack verifier as the domain adapter and retain positive, negative, and
  air-gapped conformance tests.

### Dependency and closeout

- K-1 and K-3 are documentation/positioning lanes and must preserve the current
  implementation truth.
- K-2 depends on a versioned Keon adapter/compatibility contract.
- K-4 is blocked until stable Keon package/tooling exists and BioStack's current
  golden bundle, negative fixtures, receipt modes, and air-gap tests pass through
  the adapter with no domain-contract regression.

