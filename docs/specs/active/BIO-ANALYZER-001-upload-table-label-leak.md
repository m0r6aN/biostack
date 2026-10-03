---
ticket: BIO-ANALYZER-001
title: Protocol upload — table/field labels must not become compounds; lock graceful-failure guarantees
status: review-candidate
owner: clinton.morgan
created: 2026-10-03
updated: 2026-10-03
supersedes: null
superseded_by: null
risk: standard
delivery_classes: [standard]
guidance_classes: [not-applicable]
substance_function_risk: [not-applicable]
function_review_status: not-applicable
surfaces:
  - backend/src/BioStack.Application/Services/ProtocolParser.cs
  - backend/tests/BioStack.Application.Tests/Services/ProtocolUploadGracefulFailureTests.cs
  - backend/tests/BioStack.Api.Tests/Integration/AnalyzeEndpointsIntegrationTests.cs
routing_class: standard-fix
data_classification: internal
---

# BIO-ANALYZER-001 — Table/field labels leak as compounds; lock graceful-failure guarantees

## Goal

Charter: `docs/goals/protocol-upload-graceful-failure/CHARTER.md` (Gate 1 ratified 2026-10-03, D1–D5).

Close the one remaining release-blocking gap in the uploaded-protocol analyzer and lock both user guarantees with regression tests:

1. Ordinary PDF/DOCX/CSV/XLSX uploads fail gracefully (no crash/500, no wall of fake compounds; honest bounded result or clean 4xx with a friendly message).
2. Real protocol content parses without treating document headers, prose, citations, or table labels as compounds.

## Verified facts on the comparison base (coordinator, `1c8a16e5e950e3b75f559e9c8c0744f28db2f6a5`)

- Reproduced: CSV `Compound,Dose,Frequency,Route,Duration` with rows `BPC-157,500mcg,daily,SubQ,4 weeks` etc. → `SpreadsheetProtocolExtractor` emits segments `Compound: BPC-157 | Dose: 500mcg | Frequency: daily | ...`; `ProtocolParser.ParseAsync` returns entries `BPC-157`, **`Frequency` (freq=`daily`, Recognized=true)**, `Retatrutide`. `Frequency` is the literal column label.
- Mechanism: `ProtocolParser.ParseSegment` (≈ lines 172–191): segment `Frequency: daily` matches `FrequencyPattern`; `BuildNameSlice` returns `Frequency`; `IsLikelyCompoundName("Frequency")` is true (Title-case, ≥3 letters, ≤3 tokens, not a schedule code).
- `AnalyzeEndpoints.AnalyzeProtocol` already maps `ProtocolIngestionException` to `Results.BadRequest(new { message })` (both around `ParseRequestAsync` and `AnalyzeAsync`) and any other `Exception` to `Results.Problem`.
- Already tested on base (do not duplicate): `PdfProtocolExtractorFlateTests`, `ProtocolIngestionDocxStructureTests`, `ProtocolAnalyzerDocxPacketGoldenTests`, `ProtocolIngestionServiceTests` (CSV/DOCX/XLSX extraction shape).
- Baseline: `dotnet test backend/tests/BioStack.Application.Tests --filter "FullyQualifiedName~Protocol|FullyQualifiedName~PdfProtocol"` = 258 passed, 0 failed.

## Out of Scope

- Rewriting the regex PDF extractor, OCR hardening, row-aware table reconstruction (e.g. recovering the dose that lives in a sibling `Dose: 500mcg` cell), frontend work, any change to `AnalyzeProtocolResponse`, `IProtocolParser`, feature gating/billing, or the `ProtocolIngestionException` contract.
- Push, PR, merge, deploy, cloud/settings.
- Any change to production-readiness gate status.

## Change

### C1 — Structural-label rejection (the only production change)

In `ProtocolParser.cs`, add a closed, case-insensitive set of structural label words (table headers / field names) and make `IsLikelyCompoundName` return `false` when **every** token of the candidate name is in that set. Initial set (exact, may not be extended by the builder without coordinator ruling):

`Frequency, Compound, Compounds, Dose, Dosage, Route, Timing, Time, Duration, Administration, Directions, Schedule, Protocol, Goal, Goals, Note, Notes, Reference, References, Version, Tracking, Baseline, Evidence, Phase, Support, Stack, Materials, Blood, Work, Week, Weeks, Day, Days, Month, Months, Name, Item, Product, Amount, Unit, Units, Total`

Rule is token-wise "all tokens in set", so a real name containing any non-label token (e.g. `Blood Flow Peptide`, `Day One Stack`) is unaffected. Applies on every input path (paste and upload) because the gate is shared. No other production file changes.

### C2 — Regression tests (new class `ProtocolUploadGracefulFailureTests`, Application.Tests)

Write tests first; record the red run before the C1 change (builder evidence), then green.

| # | Test intent | Layer |
|---|---|---|
| T1 | CSV with header `Compound,Dose,Frequency,Route,Duration,Timing,Notes` + ≥2 real alias rows (`BPC-157`, `Retatrutide`): parsed entry names contain both compounds and contain **no** entry whose name is a header label. | `SpreadsheetProtocolExtractor` → `ProtocolParser` |
| T2 | Same shape as an in-memory XLSX (built with the existing `ZipArchive` helper pattern from `ProtocolIngestionServiceTests`): same assertions. | extractor → parser |
| T3 | `[Theory]` over every label in the C1 set, segments `"<Label>: daily"` and `"<Label>: 500mcg"`: zero entries. (Label list in the test is the C1 list verbatim.) | parser |
| T4 | Non-regression: `"Blood Flow Peptide 500mcg daily"`-style multi-token non-label name and `"BPC-157 500mcg daily"` still emit exactly one entry. | parser |
| T5 | Prose-only DOCX (headings, narrative, citation lines, no dosing): analyzer result has 0 protocol entries, no unknown-compound issues, `scored == false`/`parseConfidence == none` per existing response fields. Reuse the setup pattern of `ProtocolAnalyzerDocxPacketGoldenTests`. | analyzer service |
| T6 | Real-protocol PDF (uncompressed content stream) containing title line, prose, a citation, and `BPC-157 500mcg daily` / `Retatrutide 2mg weekly`: entries exactly {BPC-157, Retatrutide}; title/prose/citation never emitted. | ingestion → parser |
| T7 | PDF with no extractable text (image-only): `ProtocolIngestionService.IngestAsync` throws `ProtocolIngestionException` whose message is non-empty, user-presentable, and contains no stack/type text. | ingestion |

### C3 — Endpoint test (existing `AnalyzeEndpointsIntegrationTests`, Api.Tests)

T8: signed-in Operator multipart-uploads the no-text PDF from T7 to `/api/analyze/protocol` with field `file`; response is `400` with JSON body containing a non-empty `message`; not 500. Uses the class's existing sign-in/subscription helpers.

## Acceptance Criteria

1. AC1 — Before C1, T1/T2/T3 fail and the failure output names `Frequency` (or another label) as an emitted compound; after C1 they pass. Red and green runs saved as evidence.
2. AC2 — T1–T8 pass; the pre-existing 258 tests in the baseline filter still pass (total ≥ 258 + new Application tests; zero failures).
3. AC3 — T4 passes: no real-compound regression from C1.
4. AC4 — `Api.Tests` run `--filter "FullyQualifiedName~AnalyzeEndpointsIntegrationTests"` is green incl. T8.
5. AC5 — Diff touches only the surfaces listed in Allowed Files; no public contract/type/signature changes.
6. AC6 — Nothing pushed, no PR opened, branch exists only locally.

## Allowed Files

- `backend/src/BioStack.Application/Services/ProtocolParser.cs`
- `backend/tests/BioStack.Application.Tests/Services/ProtocolUploadGracefulFailureTests.cs` (new)
- `backend/tests/BioStack.Api.Tests/Integration/AnalyzeEndpointsIntegrationTests.cs`
- `docs/goals/protocol-upload-graceful-failure/EVIDENCE.md` (new, builder writes red/green outputs and counts)

Coordinator-owned (builder must not edit): this spec, `CHARTER.md`, `GATE2.md`, `HANDOFF.md`, `docs/specs/INDEX.md`.

## Forbidden

- Any other file; any change to `IProtocolParser`, response/request contracts, `AnalyzeEndpoints.cs`, extractors, feature gates, frontend.
- Extending the C1 label set, adding broader name heuristics, or loosening other `IsLikelyCompoundName` rules.
- Deleting or weakening existing tests. Mocks of `ProtocolParser`/extractors in T1–T7 (use the real classes).
- Push/PR/merge/deploy.

## Verification (deterministic, run from `backend/` in the named worktree)

1. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~ProtocolUploadGracefulFailureTests"` — red before C1 (T1–T3 only), green after.
2. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~Protocol|FullyQualifiedName~PdfProtocol"` — green, count ≥ 258 + new.
3. `dotnet test tests/BioStack.Api.Tests --filter "FullyQualifiedName~AnalyzeEndpointsIntegrationTests"` — green.
4. `git diff --check`; `git diff --stat origin/main...HEAD` lists only Allowed Files (plus coordinator docs).

## Stop-and-Report Rule

Builder stops and returns to coordinator if: C1 breaks any existing test; T5/T6/T7/T8 expose a defect requiring changes outside Allowed Files (record observed vs expected, do not fix); the label set proves insufficient for a new leak (report, do not extend); a required test needs a mock of the parser/extractor.

## Rollback

Single-commit revert of the `ProtocolParser.cs` hunk; tests are additive. No data, migration, or config impact.

## Existing Patterns To Follow

- `backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionDocxStructureTests.cs` (parser/extractor harness, `CreateParser`).
- `backend/tests/BioStack.Application.Tests/Services/ProtocolAnalyzerDocxPacketGoldenTests.cs` (analyzer-level assertions).
- `backend/tests/BioStack.Application.Tests/Services/PdfProtocolExtractorFlateTests.cs` (PDF byte construction).
- `backend/tests/BioStack.Api.Tests/Integration/AnalyzeEndpointsIntegrationTests.cs` (`SignInAsync`, `UpsertSubscriptionAsync`).
