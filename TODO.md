# TODO — Video/Channel Knowledge Worker Intake Extension

## Completed prior planning/docs
- [x] Draft architecture ADR for video/channel intake extension (observational-only boundary).
- [x] Add proposed backend C# contracts for intake + extraction output (review-only claims).
- [x] Add proposed frontend TypeScript contracts for admin intake and candidate artifacts.
- [x] Add minimal admin UI plan document (fields, workflow, validation posture).
- [x] Add targeted test plan document matching required scenarios.

## PR 1 — Intake endpoint + persistence (queued-only)
- [ ] Add review-bound intake entity and persistence mapping (queued initial status).
- [ ] Add intake create service/validation (source-type URL + channel bounds).
- [ ] Add admin endpoint accepting AdminKnowledgeSourceIntakeRequest and returning AdminKnowledgeSourceIntakeResponse.
- [ ] Add focused integration tests for happy/error/edge cases.
- [ ] Run focused backend/API tests and verify pass.
- [ ] Update docs/architecture/video-channel-ingestion-test-plan.md with PR 1 completion notes.
- [ ] Confirm extraction remains unimplemented and queued-only.
