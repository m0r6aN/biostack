# Analyzer P1 (Score-Confidence Gating) + P2 (UI Grace) — Implementation Plan

**Status:** Proposed
**Owner:** TBD
**Depends on:** PR #111 (recognition gate, table-cell splitting, `ug`→`mcg` fix) — _merged/in review_
**Scope:** `/tools/analyzer` — make the score honest when recognition is weak, and stop rendering unrecognized lines as a wall of "Unknown" cards.

---

## 1. Why

PR #111 stopped the parser from emitting prose/headers/citations as fake compounds (the "102 found, 4 normalized" failure). Two problems remain from the original remediation plan:

- **P1 — the score lies under weak recognition.** The BioStack score is always computed from `InteractionIntelligence.CompositeScore` and always rendered as a confident number (e.g. "61/100 Mixed fit"), even when only a fraction of the parsed stack maps to known compounds — or none does. There is no notion of *parse confidence* feeding the score, the score band, or the narrative.
- **P2 — the UI presents low-value rows as first-class.** `ParsedProtocolSection` renders every entry in a table/cards, and the "Status" column conflates "has a dose" with "recognized" (`dose > 0 ? Canonicalized : Partial`). Unrecognized lines deserve a single honest summary, not individual cards.

**Guiding principle (unchanged):** the analyzer must degrade *honestly* — when it cannot recognize enough structure, it should say so rather than manufacture a precise-looking score.

---

## 2. Current behavior (anchors)

Backend:
- `ProtocolAnalyzerService.AnalyzeAsync` — `backend/src/BioStack.Application/Services/ProtocolAnalyzerService.cs:71`
  - `knownEntries` (distinct recognized `KnowledgeEntry`s) computed at `:95-99`.
  - `GetOrAnalyzeAsync` builds score/issues/unknowns at `:184-212`; score = `(int)Math.Round(interactionIntelligence.CompositeScore)`.
  - `DeriveIssues` at `:232-280` — `excessive_compounds` and `inefficiency` use **`parseResult.Entries`** (all entries, incl. unrecognized) for both the trigger count and the `Compounds` list.
  - `unknownCompounds` at `:198-202`; `BuildScoreExplanation` at `:282-289`; `BuildParserWarnings` at `:291-310`.
  - Response constructed at `:113-128`.
- Response contract: `AnalyzeProtocolResponse` — `backend/src/BioStack.Contracts/Responses/AnalyzeProtocolResponse.cs:3` (serialized camelCase). `ProtocolEntryResponse` at `:25` has **no recognition flag**.
- `NormalizationService.Normalize` already computes per-compound `IsKnown` (`backend/src/BioStack.Application/Services/ProtocolNormalizationService.cs:41`) but it is not surfaced to the response.

Frontend:
- Type: `ProtocolAnalyzerResult` — `frontend/src/lib/types.ts:684` (consumes camelCase directly; no manual mapper, so a new backend field appears automatically once added to the TS interface).
- View helpers: `analyzerView.ts` — `getScoreLabel:50`, `getScoreBand:59`, `getScoreInsight:68`, `getWhatThisMeans:113`, `confidenceLabel:239`, `buildAnalyzerFindings:267`, `buildParserWarnings:331`.
  - `getWhatThisMeans` flattens `result.issues[].compounds` into the narrative (`:118`) — the original title/subtitle leak path; depends entirely on what backend puts in issue compound lists.
- Components: `ScoreHero.tsx` (arc gauge + "what this means"), `ParsedProtocolSection.tsx` (table/cards + Status chip at `:77-83`).

---

## 3. P1 — Score-Confidence Gating

### 3.1 Backend data model

Add parse-confidence to the response (additive; no breaking changes). `Score` stays a non-null `int` for backward compatibility; a new `Scored` flag tells the UI whether to trust it.

`AnalyzeProtocolResponse` (new fields, appended):
```csharp
int RecognizedCompoundCount,     // distinct known compounds
int ParsedCompoundCount,         // total parsed entries
string ParseConfidence,          // "high" | "medium" | "low" | "none"
bool Scored                      // false when ParseConfidence == "none"
```

`ProtocolEntryResponse` (new field, appended) — also serves P2:
```csharp
bool Recognized                  // matched to a KnowledgeEntry
```

> Surfacing `Recognized` per entry replaces the misleading "Status = Canonicalized/Partial" heuristic and removes the need for the frontend to re-derive recognition from `unknownCompounds`.

### 3.2 Confidence rule

Compute in `AnalyzeAsync` from already-available data (`knownEntries`, `parseResult.Entries`):

```
recognized = knownEntries.Count
parsed     = parseResult.Entries.Count
ratio      = parsed == 0 ? 0 : recognized / (double)parsed

none   : recognized == 0
low    : ratio < 0.34  (and recognized >= 1)
medium : ratio < 0.67
high   : otherwise
```
Thresholds live as named constants in `ProtocolAnalyzerService` so they're tunable. (Calibrate against the golden packet — post-#111 that packet recognizes ~4 of a handful, so it should land at `medium`/`low`, **never** a confident `high`.)

### 3.3 Score + issues gated on recognition

- **`Scored = ParseConfidence != "none"`.** When `none`, the numeric score is not meaningful; the UI shows the low-confidence state (3.4 / §4).
- **`DeriveIssues` operates on recognized compounds only.** Change `excessive_compounds` and `inefficiency` to count and list **recognized** canonical names (derive from `knownEntries` / `KnowledgeByCompound`), not `parseResult.Entries`. This closes the narrative-leak path at its source: `getWhatThisMeans` can only echo recognized canonical names.
- **`BuildParserWarnings`** stays factual but de-duplicated against findings; when `ParseConfidence == "none"` emit a single clear line ("We couldn't confidently identify known compounds in this document") instead of "98 compounds could not be fully normalized".

### 3.4 Backend tests

Extend `ProtocolAnalyzerDocxPacketGoldenTests` and `ProtocolAnalyzerServiceTests`:
- Packet → `ParseConfidence` ∈ {`low`,`medium`}, `Scored == true`, and **no** issue `Compounds` entry contains a `NonCompoundMarker` (already asserted) **nor** an unrecognized name.
- A pure-prose input (no known compounds) → `ParseConfidence == "none"`, `Scored == false`.
- A clean 2-compound paste (`BPC-157` + `TB-500`) → `ParseConfidence == "high"`, all entries `Recognized`.
- `excessive_compounds`/`inefficiency` issues never list unrecognized names.

---

## 4. P2 — UI Grace

### 4.1 Type wiring

`frontend/src/lib/types.ts`: add `recognizedCompoundCount`, `parsedCompoundCount`, `parseConfidence`, `scored` to `ProtocolAnalyzerResult`; add `recognized: boolean` to `ProtocolAnalyzerEntry`. No mapper change needed (direct camelCase consumption).

### 4.2 `analyzerView.ts`

- `confidenceLabel` (`:239`): drive from `result.parseConfidence` (capitalize) instead of inferring from warning arrays.
- `getScoreLabel`/`getScoreBand` (`:50`,`:59`): return `Not confidently scored` / `unscored` when `!result.scored`.
- `getScoreInsight`/`getWhatThisMeans` (`:68`,`:113`): when `!scored`, return an honest "couldn't confidently structure this document" message; when `parseConfidence === 'low'`, prepend a caveat. Continue to only reference issue compounds (now guaranteed recognized by §3.3).
- Add `partitionProtocol(result)` → `{ recognized, unrecognized }` using `entry.recognized` (pure, unit-testable).

### 4.3 `ScoreHero.tsx`

- When `!result.scored`: replace the arc gauge + number with a neutral "Not confidently scored" card explaining that too few compounds were recognized, plus a CTA to review the extracted text / edit the stack. Suppress the `bandColor` red treatment (a low-confidence parse is not a "bad stack").
- When `scored` but `parseConfidence !== 'high'`: keep the gauge but render a confidence chip (reuse `confidenceLabel`) and a one-line caveat above "What this means".

### 4.4 `ParsedProtocolSection.tsx`

- Split into **Recognized compounds** (table/cards as today, Status chip now = Recognized vs Unrecognized from `entry.recognized`, not `dose > 0`).
- Collapse **unrecognized** entries into a single expander: _"N lines we couldn't map to a known compound"_, listing the raw names in a muted block — no per-line cards. Hidden entirely when count is 0.
- Keep the existing blend badge and the >6 collapse behavior for the recognized table.

### 4.5 Frontend tests

Extend existing `__tests__/components/analyzer/*` and `analyzerView.test.ts`:
- `partitionProtocol` splits by `recognized`.
- `ScoreHero`: `scored == false` renders the no-score card and **no** numeric "/100"; low confidence renders the chip + caveat.
- `ParsedProtocolSection`: unrecognized entries render in the expander, not as cards; recognized entries show the Recognized chip; section copes with 0 recognized / 0 unrecognized.
- `getWhatThisMeans`/`getScoreInsight` honest copy under `!scored`.

---

## 5. Sequencing

1. **PR A (backend, P1):** response fields + confidence rule + recognition-gated issues + tests. Ships independently; frontend ignores unknown fields until wired.
2. **PR B (frontend, P2):** type wiring + `analyzerView` helpers + `ScoreHero` + `ParsedProtocolSection` + tests. Depends on PR A being deployed.
3. Manual verification: re-run the original `BioStack_Protocol_Printable_Packet_v2.docx` upload and confirm (a) no confident score, (b) recognized compounds listed cleanly, (c) unrecognized lines collapsed.

---

## 6. Out of scope / follow-ups

- Per-entry **confidence tiers** beyond the binary `recognized` (e.g. alias-match vs heuristic dose+name from #111) — possible later refinement.
- Row-aware table reconstruction (binding a name in column 1 to a dose in column 3 of the materials table) — improves dose accuracy but is a larger parser change.
- Image/PDF extraction hardening (the hand-rolled PDF regex extractor) — tracked separately.

---

## 7. Acceptance criteria

- [ ] Uploading the v2 packet yields `parseConfidence` ≤ `medium` and a non-misleading score presentation (no confident "61/100").
- [ ] A prose-only document yields `scored == false` and the no-score UI.
- [ ] No issue/narrative text references an unrecognized or non-compound string.
- [ ] Unrecognized parsed lines appear only in the collapsed summary, never as individual cards.
- [ ] Clean compound pastes are unaffected (`high` confidence, all recognized).
- [ ] Backend + frontend suites green; golden packet test extended with the new assertions.
