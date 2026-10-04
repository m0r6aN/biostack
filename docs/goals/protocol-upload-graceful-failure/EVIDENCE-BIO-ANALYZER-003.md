# EVIDENCE — BIO-ANALYZER-003

Builder: BioAnalyzer003Builder. Branch `fix/analyzer-single-word-frequency-gate`, start commit `426b33ae1ce58b7e7d2f5a150afc356b6c3b9ce0`.

## Step 0 — restatement

- Spec rev 3 sha256 verified = `9d224c5a…337b68` (matches Gate 2 record, case-insensitive).
- Change: C0 `ParserVersion` v4 -> v5; C1 in `ProtocolParser.ParseSegment`, after `IsLikelyCompoundName` guard: `if (!singleDoseMatch.Success && IsSinglePlainToken(nameSlice)) return empty;` plus private static helper (one token, no digit, no `-`); update 3 stale comments; C2 new test class `ProtocolSingleWordFrequencyGateTests` (T1-T7, helpers copied locally).
- Allowed files: ProtocolParser.cs, ProtocolFingerprintService.cs, ProtocolSingleWordFrequencyGateTests.cs (new), this file. Everything else forbidden.
- Stop triggers: C1 breaks an existing test; T4 fixture not in LocalKnowledgeSource; T7 needs out-of-scope change; new leak shape outside the rule.
- Consistency check: spec internally consistent; no design guess required.

## RED (tests written, C0/C1 not applied)

Command (from `backend/`): `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~ProtocolSingleWordFrequencyGateTests"`
Result: `Failed! - Failed: 13, Passed: 7, Skipped: 0, Total: 20`.
- T1 (6/6 failed): `Segment 'Review daily' emitted: Review`; likewise Hydrate, Stretch, Meditate, Journal, REVIEW.
- T2 (6/6 failed): `- Review daily`→Review; `Review daily | Notes`→Review; `Review: daily`→Review; `Review daily.`→Review; `Stretch. Morning`→`Stretch.`; `Hydrate/Stretch daily`→`Hydrate/Stretch`.
- T7 failed: `Expected exactly {BPC-157} but got: Review | Hydrate | BPC-157`.
- T3, T4 (3), T5, T6 (2) = 7 passed before and after (non-regression locks).

## GREEN (C0 v5, C1 guard, comments applied)

1. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~ProtocolSingleWordFrequencyGateTests"` → `Passed! Failed: 0, Passed: 20, Skipped: 0, Total: 20`.
2. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~Protocol|FullyQualifiedName~PdfProtocol|FullyQualifiedName~ProtocolSingleWordFrequencyGateTests"` → `Passed! Failed: 0, Passed: 438, Skipped: 0, Total: 438`. (Start-of-parcel filter baseline was not captured; 438 includes the 20 new tests. `ProtocolUploadGracefulFailureTests` unmodified and passing.)
3. `dotnet test tests/BioStack.Application.Tests` → `Passed! Failed: 0, Passed: 882, Skipped: 5, Total: 887` (BIO-ANALYZER-001 baseline 862 passed / 5 skipped + 20 new).
4. `git diff --check` clean; changed files are exactly the four Allowed Files.

## AC
AC1 met (red/green above). AC2 met. AC3 met. AC4 met (only `ParserVersion` constant changed; no signature change). AC5 met (local commits only, no push).

## Notes
- Stale `IsLikelyCompoundName` header comment: kept its original text and appended a 3-line note that dose-less single all-caps names are rejected afterward (spec asked comments to state the new acceptance rule).
- No deviations or stop triggers observed.
