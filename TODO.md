# PR10 TODO

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

