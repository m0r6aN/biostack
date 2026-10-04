---
ticket: BIO-ANALYZER-003
title: Protocol parser — an unrecognized single word plus a frequency word, with no dose, is prose and must not become a compound
status: active
revision: 3
owner: clinton.morgan
created: 2026-10-04
updated: 2026-10-04
supersedes: null
superseded_by: null
risk: standard
delivery_classes: [standard]
guidance_classes: [not-applicable]
substance_function_risk: [not-applicable]
function_review_status: not-applicable
surfaces:
  - backend/src/BioStack.Application/Services/ProtocolParser.cs
  - backend/src/BioStack.Application/Services/ProtocolFingerprintService.cs
  - backend/tests/BioStack.Application.Tests/Services/ProtocolSingleWordFrequencyGateTests.cs
  - docs/goals/protocol-upload-graceful-failure/EVIDENCE-BIO-ANALYZER-003.md
routing_class: implementation/standard
verification_class: equivalence-provable
permission_profile: reviewer-readonly
data_classification: internal
---

# BIO-ANALYZER-003 — Single-word + frequency prose gate (Residual A)

## Intent

Close Residual A from `docs/goals/protocol-upload-graceful-failure/HANDOFF.md`: a prose line consisting of one Title-case word plus a frequency word (`Review daily`, `Hydrate weekly`, `Stretch morning`) passes the recognition gate and is emitted as an unknown compound. This violates guarantee 2 of the P01-R charter (real protocol content parses without treating prose as compounds) and was accepted as a documented residual by BIO-ANALYZER-001 only because fixing it needed its own spec. After this parcel, an unrecognized single name token that carries no digit and no hyphen, with a frequency word and no dose, is not emitted; a known compound (alias path), a name with a dose, and a digit- or hyphen-bearing token still are. Consumers: every input path (paste, PDF, DOCX, CSV/XLSX), since the gate is shared.

## Prerequisites

- `BIO-ANALYZER-001` merged (this parcel builds on its `StructuralLabelWords` check in `IsLikelyCompoundName` and its `ParserVersion = "v4"`).

## Verified facts (coordinator + independent reviewer, branch `fix/protocol-upload-graceful-failure` @ `a12d580d`)

- `ProtocolParser.ParseSegment` (gate region ≈ lines 166-221; method spans 104-221): after the alias path (`ResolveAliasName`) fails, a segment with a dose match OR a `FrequencyPattern` match proceeds; `BuildNameSlice` takes the text before the first dose (or, with no dose, the first frequency match); `IsLikelyCompoundName` accepts names of length 2-40, ≤ 3 tokens; an alphabetic token must be longer than 2 letters and not all-lowercase; an ALL-CAPS token and a token containing a digit, hyphen, or other non-letter bypass the case/length checks. `Review daily` → name slice `Review` → emitted with frequency `daily`.
- `BuildNameSlice` returns an empty slice when the cut index is 0 (`cutIndex > 0` guard, line 277). A line that **starts** with a frequency word (`Morning Routine daily`, `Daily Hydration weekly`, `Daily Review weekly`) is therefore already rejected today and does not leak. Lines whose frequency word is not first (`Hydration Routine daily`, `Stretch Routine morning`) do leak.
- The `FrequencyPattern` vocabulary includes `morning`, `evening`, `nightly`, `pre/post-workout`, so `Stretch morning`, `Hydrate nightly` leak the same way.
- `LocalKnowledgeSource` (the test knowledge source) holds exactly five compounds: `BPC-157`, `TB-500`, `MOTS-C`, `NAD+`, `Retatrutide`. It does **not** contain Semaglutide. (`LocalKnowledgeSource.cs` lines 132, 157, 178, 206, 228.) The production source is the database.
- `ParserVersion` is `"v4"` after BIO-ANALYZER-001 (`ProtocolFingerprintService.cs:10`); parse cache key = `analyzer:parse:parser-{ParserVersion}:{ParseFingerprint}` with 7-day TTL, so the fix needs a version bump to stop serving cached leaked parses. `ParserVersion` has no test dependents.
- Existing comments become stale after this parcel: the gate comment block (≈ lines 166-175, branch (b)), the note at ≈ lines 189-191 that cites `Semaglutide weekly` as a valid dosing line, and the `IsLikelyCompoundName` header comment (≈ lines 286-292) listing all-caps `NAD, KPV` as valid names.
- BIO-ANALYZER-001 forbade `Review daily`-style lines in its T5/T6 fixtures; this parcel makes them legal in tests and does not edit those tests. Across `backend/tests`, no existing test relies on a frequency-only, dose-less line for an unrecognized single word (reviewer search).
- The new test class cannot reuse a shared DOCX/analyzer helper: `BuildDocx` exists only as a private static in `ProtocolUploadGracefulFailureTests.cs` and `ProtocolIngestionDocxStructureTests.cs`, and the analyzer wiring (`CreateAnalyzer`) is private in its test classes. The builder copies what it needs into the new class; it does not edit those files.

## Constraints

- Rule (developer-ratified policy "reject unless alias or dose", 2026-10-04): in `ParseSegment`, after `IsLikelyCompoundName(nameSlice)` passes, if there is **no dose match** and the name slice is a **single token that contains neither a digit nor a hyphen**, return no entry. The alias path (`ResolveAliasName`), the blend path, and every segment with a dose are untouched.
- A single token containing a digit or hyphen (`TB-500`, `B12`, `GHK-Cu`, `Zorb-12`) is "unambiguously compound-shaped" by the existing comment and is NOT affected (developer-ratified 2026-10-04). The exemption is exactly digit-or-hyphen: a token with any other punctuation (`Stretch.`, `Review!`, `Hydrate/Stretch`) is **not** exempt and is rejected. (Coordinator ruling from review; see Rulings.)
- Consequence of the ratified digit/hyphen exemption, accepted (Residual A3): hyphenated or digit-bearing prose words with a frequency and no dose (`Check-in daily`, `Follow-up weekly`, `Phase2 daily`) are still emitted.
- Multi-token names with a frequency and no dose where the frequency word is NOT first (`Hydration Routine daily`, `Stretch Routine morning`, `Blood Flow Peptide weekly`) are NOT affected by this parcel; this is a deliberate narrow reading of the ratified policy, recorded as Residual A2 (see Out of Scope). Lines that start with a frequency word are already rejected today.
- Pure reject placed immediately after the existing `IsLikelyCompoundName` check; no existing check is modified; no new word list. The builder updates the three stale comments listed in Verified facts so they state that a frequency-only, dose-less line is accepted only when an alias matches, or the name has a digit or hyphen, or the name has more than one token.
- Known cost, accepted by the developer: an unrecognized single-word compound written without a dose (`Zorbatide weekly`) is no longer surfaced; with a dose (`Zorbatide 2mg weekly`) it still is.
- `ParserVersion` `"v4"` → `"v5"`. No other production change. No contract, `IProtocolParser`, extractor, or response change.
- Tests written first; red run captured before the change.

## Change

### C0 — Parse-cache invalidation

`ProtocolFingerprintService.ParserVersion`: `"v4"` → `"v5"`. Deliberately not unit-tested (a constant pin); verified by diff review.

### C1 — Single-word frequency-only rejection

In `ProtocolParser.ParseSegment`, after the `IsLikelyCompoundName(nameSlice)` guard and before building the entry: `if (!singleDoseMatch.Success && IsSinglePlainToken(nameSlice)) return Array.Empty<ProtocolEntryResponse>();` with a small private static helper (exactly one token after whitespace split; no char is a digit or `-`). Update the stale comments (Constraints). Comment cites this ticket.

### C2 — Tests (new class `ProtocolSingleWordFrequencyGateTests`, Application.Tests)

Real `ProtocolParser` with `LocalKnowledgeSource`, no mocks of parser/extractors. Every assertion message prints the segment and the names emitted (`Segment 'X' emitted: a | b`) so the red run names the leaked word.

| # | Intent | Expectation |
|---|---|---|
| T1 | `[Theory]` prose lines: `Review daily`, `Hydrate weekly`, `Stretch morning`, `Meditate nightly`, `Journal evening`, ALL-CAPS `REVIEW DAILY` | zero entries each |
| T2 | Shape variants: bullet `- Review daily`; table-cell `Review daily | Notes`; colon `Review: daily`; trailing punctuation `Review daily.`; `Stretch. Morning`; `Hydrate/Stretch daily` | zero entries each (bullet stripping, pipe splitting, and non-digit/hyphen punctuation do not bypass) |
| T3 | Positive: unrecognized name WITH dose: `Zorbatide 2mg weekly` | one entry, name `Zorbatide`, dose 2, unit `mg`, frequency `weekly` |
| T4 | Positive: known compound, frequency only, from `LocalKnowledgeSource`: `Retatrutide weekly` and `retatrutide weekly` (and `BPC-157 daily`) | one entry with the canonical name |
| T5 | Positive: digit/hyphen unknown token, frequency only: `Zorb-12 weekly` | one entry (ratified behavior) |
| T6 | Not widened: `Blood Flow Peptide weekly` and `Hydration Routine daily` | one entry each (locks the ratified narrow reading; Residual A2 stays open) |
| T7 | End-to-end prose: DOCX built (helper copied locally) with headings, `Review daily`, `Hydrate weekly` lines and one real line `BPC-157 500mcg daily`, through the analyzer service wired as in `ProtocolUploadGracefulFailureTests`. Headings MUST NOT contain a frequency word (or be a single word followed by one), otherwise they add entries of their own | entries == {BPC-157}; no issue references `Review`/`Hydrate` |

## Acceptance Criteria

1. AC1 — Before C1, T1, T2, and T7 fail with output naming the leaked word (e.g. `Review`); after, all pass. T3-T6 pass both before and after (they lock non-regression); the builder records that. Red and green runs saved as evidence.
2. AC2 — All of `FullyQualifiedName~Protocol|FullyQualifiedName~PdfProtocol|FullyQualifiedName~ProtocolSingleWordFrequencyGateTests` green; count ≥ the baseline recorded at start plus new tests; zero failures. `ProtocolUploadGracefulFailureTests` (BIO-ANALYZER-001) passes unmodified.
3. AC3 — Full `BioStack.Application.Tests` green (compare to the BIO-ANALYZER-001 recorded run: 862 passed, 5 skipped, 0 failed, plus new).
4. AC4 — Diff touches only Allowed Files; `ParserVersion` is the only constant changed; no public signature change.
5. AC5 — Nothing pushed, no PR, branch local only.

## Out of Scope

- **Residual A2 (known gap, not fixed here):** multi-token Title-case prose with a frequency (not first) and no dose (`Hydration Routine daily`, `Stretch Routine morning`) still becomes an unknown compound. Widening the rule to "any unrecognized name needs an alias or a dose" is a larger false-negative trade (drops `Blood Flow Peptide weekly`); needs a developer decision and its own parcel.
- **Residual A3 (known gap, not fixed here):** hyphenated or digit-bearing prose words with a frequency and no dose (`Check-in daily`, `Follow-up weekly`, `Phase2 daily`) — the direct consequence of the ratified digit/hyphen exemption.
- Prose that carries a dose (`Take 2 mg of water daily`-style) — different gate path.
- Table header words leaking (`BIO-ANALYZER-004`), table-row dose binding (`BIO-ANALYZER-004`), XLSX package faults (`BIO-ANALYZER-002`).
- Interaction with `BIO-ANALYZER-004` (informational): a reconstructed spreadsheet row that has a name and frequency but a blank dose (`Zorbatide weekly`) is dropped by this parcel unless the compound is a known alias. Accepted trade, same as pasted text.
- Extending `StructuralLabelWords`, adding verb word lists, frontend work, a per-entry confidence field (frozen response contract — loop-stop if wanted), push/PR/merge/deploy, production-readiness verdict.

## Allowed Files

- `backend/src/BioStack.Application/Services/ProtocolParser.cs`
- `backend/src/BioStack.Application/Services/ProtocolFingerprintService.cs`
- `backend/tests/BioStack.Application.Tests/Services/ProtocolSingleWordFrequencyGateTests.cs` (new)
- `docs/goals/protocol-upload-graceful-failure/EVIDENCE-BIO-ANALYZER-003.md` (new; builder writes red/green outputs)

Coordinator-owned (builder must not edit): this spec, `docs/specs/INDEX.md`, `HANDOFF.md`, `EVIDENCE.md`, `CHARTER.md`, `loop-directive.md`, Gate 2 record.

## Forbidden

- Any other file; `IProtocolParser`; response/request contracts; extractors; `AnalyzeEndpoints`; `ScoringVersion`/`IngestionVersion`; editing `ProtocolUploadGracefulFailureTests` or other existing test classes; weakening or deleting existing tests; mocks of parser/extractors; push/PR/merge/deploy.

## Verification (run from `backend/` in the Gate-2-named worktree)

1. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~ProtocolSingleWordFrequencyGateTests"` — red (T1, T2, T7) before C1, green after.
2. `dotnet test tests/BioStack.Application.Tests --filter "FullyQualifiedName~Protocol|FullyQualifiedName~PdfProtocol|FullyQualifiedName~ProtocolSingleWordFrequencyGateTests"` — green.
3. `dotnet test tests/BioStack.Application.Tests` — green.
4. `git diff --check`; `git diff --stat` lists only Allowed Files.

## Stop-and-Report Rule

Builder stops and returns to the coordinator if: C1 breaks any existing test (record which line it dropped; do not "fix" the test); a T4 fixture compound is not in `LocalKnowledgeSource` (the five listed in Verified facts are confirmed; any other requires stopping); T7 needs a change outside Allowed Files; a new prose leak shape outside this rule appears (report it, do not extend the rule).

## Rollback

Revert the commit: removes the `ParseSegment` guard and the comment edits, and returns `ParserVersion` to `v4` (previously cached `v4` entries become live again within TTL, including the single-word leak). Tests are additive.

## Ratified Decisions (developer, 2026-10-04)

1. Digit/hyphen single tokens with a frequency and no dose (`Zorb-12 weekly`) are kept.
2. The rule is not widened to multi-token names; Residual A2 stays a documented gap.
3. Unknown all-caps single tokens without a dose (`KPV daily` when `KPV` is not in the knowledge source) are rejected: the rule applies regardless of case.

## Coordinator rulings from spec review (flagged for developer awareness; refine, do not contradict, the decisions above)

- The exemption is exactly "digit or hyphen", not "any non-letter": `Stretch.`/`Review!`/`Hydrate/Stretch` are rejected. This follows decision 1 literally.
- Residual A3 (`Check-in daily`) is accepted as the stated consequence of decision 1, not fixed.

## Spec review triage (Spec003Reviewer, rev 2 → rev 3; no blocking findings)

| Finding | Ruling |
|---|---|
| F1 major — T4 example `Semaglutide` not in `LocalKnowledgeSource` (5 entries) | fix — fixture `Retatrutide`/`BPC-157`; fact stated |
| F2 major — A2 examples (`Morning Routine daily`, …) are already rejected (cut index 0) | fix — examples replaced, fact added |
| F3 minor — punctuation tokens (`Stretch.`) bypass "letters only" | fix — ruling: exemption is exactly digit/hyphen; T2 rows added |
| F4 minor — `Check-in daily` stays | accept-as-documented — Residual A3 |
| F5 minor — stale comments (`Semaglutide weekly`, `NAD, KPV`) | fix — builder updates; Constraints |
| F6 minor — DOCX/analyzer helpers are private | fix — builder copies; stated |
| F7 minor — assertion-message format; extra shapes; C0 untested | fix — message format mandated; shapes added; C0 deliberately untested |
| F8 info — length 2-40 wording | fix — corrected |
| F9 info — interaction with 004 blank-dose rows | accept-as-documented — Out of Scope note |

## Context & References

- `docs/goals/protocol-upload-graceful-failure/HANDOFF.md` (Residual A; "e.g. require alias or dose for single-token names").
- `docs/specs/active/BIO-ANALYZER-001-upload-table-label-leak.md` (Out of Scope residual; C1 placement; ParserVersion/cache rationale).
- `backend/tests/BioStack.Application.Tests/Services/ProtocolUploadGracefulFailureTests.cs` (parser harness, `LocalKnowledgeSource`, DOCX builder to copy, analyzer wiring).
- `backend/tests/BioStack.Application.Tests/Services/ProtocolAnalyzerDocxPacketGoldenTests.cs` (assertion style; loads a fixture file, builds no DOCX).
- Sequencing: independent of `BIO-ANALYZER-002`; touches `ProtocolFingerprintService.cs` line 10 (`ParserVersion`), adjacent to `IngestionVersion` on line 8 that `BIO-ANALYZER-004` edits — merge 003 before 004 to avoid a textual conflict.
