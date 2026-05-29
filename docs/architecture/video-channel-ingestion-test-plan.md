# Test Plan — Video/Channel Knowledge Worker Intake Extension

## Scope

Covers the minimal extension for admin-submitted `video_url` / `channel_url` intake into Knowledge Worker candidate extraction flow.

## Non-goals

- No full media downloading
- No canonical knowledge writes
- No prescription/dosing recommendation generation
- No review bypass

---

## 1) Video URL intake job creation

## Test
Submit admin intake payload with:

- `sourceType=video_url`
- valid `sourceUrl`
- requested outputs populated

## Expected
- API accepts request (`202/200` per endpoint style).
- Request persisted/queued with status `queued`.
- Record has source type/url correctly captured.
- No canonical `KnowledgeEntry` changes occur.

## Assertions
- Response includes `intakeRequestId`.
- Intake record exists with status `queued`.
- `KnowledgeEntries` count unchanged.

---

## 2) Channel URL intake job creation

## Test
Submit payload with:

- `sourceType=channel_url`
- valid channel URL
- `channelOptions.maxVideos`
- optional date filters

## Expected
- API accepts request.
- Status `queued`.
- Channel options persisted exactly.
- Worker later fans out bounded extraction tasks (bounded by options).

## Assertions
- Request row/json contains `maxVideos`, date bounds.
- Invalid bounds rejected (e.g., after > before).

---

## 3) Optional instructions persistence

## Test
Submit both video and channel requests with `optionalInstructions`.

## Expected
- Text persists unchanged (trim policy explicit and consistent).
- Worker receives instructions in extraction agent invocation payload.

## Assertions
- Intake storage contains instructions.
- Extraction invocation logs/artifacts include same instruction payload.

---

## 4) Extraction output stored as candidate artifacts

## Test
Run extraction processing for queued request.

## Expected
- Normalized extraction output persisted in candidate artifact store/file.
- Raw artifact refs persisted.
- Review queue candidates created from extracted claims/reasons.
- No canonical writes.

## Assertions
- Artifact exists and includes required sections:
  - sourceMetadata
  - transcriptQuality
  - coreThesis
  - claims
  - compoundsMentioned
  - biomarkersOrLabsMentioned
  - protocolPhases
  - safetyFlags
  - evidenceGaps
  - rawArtifactRefs
- `KnowledgeEntries` unchanged by extraction step.

---

## 5) Claims remain review-only

## Test
Inspect generated extracted claim candidates and review queue.

## Expected
- Every claim marked as review candidate.
- Every claim carries source attribution.
- Claim origin explicitly indicates source claim (`source-claim`).

## Assertions
- `claimOrigin == source-claim` for all claims.
- review status defaults to pending/review state.
- no auto-promotion flags set.

---

## 6) Medical/dosing/disease claims are flagged

## Test
Use extraction fixture containing dosing language, disease-treatment phrasing, and safety statements.

## Expected
- Claims flagged:
  - `isDosingClaim`, `isDiseaseClaim`, `isSafetyClaim`
  - `requiresMedicalReview=true` for relevant claims
- UI/review artifact includes warning banner/flag text that these are source claims only.

## Assertions
- Relevant boolean flags present and true for matching claims.
- review queue includes explicit reasons for medical/safety review.
- no guidance reclassification occurs.

---

## 7) Failed extraction produces safe failure state

## Test
Simulate extraction adapter failure (timeout/provider error/invalid transcript).

## Expected
- Intake status transitions to `failed`.
- `failureReason` captured safely/sanitized.
- No partial canonical updates.
- Retry remains manual or queue-driven per policy.

## Assertions
- failure state persisted.
- artifact references either absent or marked partial with diagnostics.
- canonical tables unchanged.

---

## Additional recommended tests

- Contract serialization tests for C# and TS models.
- Validation tests:
  - invalid URLs,
  - unsupported source type,
  - channel options out of bounds,
  - empty requested outputs (if disallowed).
- Idempotency test for duplicate submit handling (if dedupe policy exists).
- Attribution integrity test: each claim must have a non-empty source URL.

---

## Exit criteria

Feature considered acceptable when all required scenarios above pass and demonstrate:

1. Intake creation works for both source types.
2. Extraction remains artifact/review-only.
3. Dosing/disease/safety claims are correctly flagged as source claims.
4. Failures terminate safely without canonical side effects.
