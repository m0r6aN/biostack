# Placeholder Publication Date Cleanup Receipt

Date: 2026-09-07. Base: `915e26f6eb40a21d4b19b1bc0e0ec9cf5be0a992` (origin/main after PR #269).
Scope: **removal of fabricated `publishedAt` values only**. No source added, no source removed, no authority,
tier, confidence, claim, conflict, or review-flag change. No promotion, no clearance, no re-review.

This executes the metadata half of the authority finding in
[`wave006-rereview-2026-09-05.md`](wave006-rereview-2026-09-05.md), which directed that "Unsupported January 1
publication dates need replacement with verified dates or honest uncertainty." This pass supplies the **honest
uncertainty** branch and only that branch. Verified dates remain outstanding. The registry/policy half of that
same finding (NIH ODS A2-vs-C2, GRAS-submission authorship, controlled-study and structured-database tiering)
is untouched and still open.

## What Was Changed

67 source snapshots across 44 evidence packets in `research/input/evidence/` carried a `publishedAt` value of
the form `YYYY-01-01T00:00:00Z`. Every one of the 67 was set to `null`.

| Metric | Count |
|---|---|
| Source entries with a `YYYY-01-01T00:00:00Z` `publishedAt` at base | 67 |
| Source entries nulled | 67 |
| Evidence packets affected | 44 |
| Evidence packets in the corpus | 78 |
| Residual `YYYY-01-01T00:00:00Z` values after this pass | 0 |

Implied years spanned 1984 to 2026. The affected sources are journal articles, systematic reviews, FDA
labels, PubMed/PMC and Europe PMC records, a WADA standard, and one vendor-marketing page — document classes
whose real publication dates are days in the calendar year, not January 1.

For each affected packet, exactly two additions were made under `ops`:

- one `ops.reviewReasons` entry recording how many placeholder dates were nulled on 2026-09-07, why, and that
  verified dates remain outstanding;
- one `ops.qualityFlags` entry, `placeholder-date-cleanup-2026-09-07`.

Those two additions plus the `publishedAt` nulls are the complete set of mutations. Nothing else in any packet
changed.

## Why `null` And Not A Guessed Date

`YYYY-01-01T00:00:00Z` is not a date anyone verified. It is year-only metadata widened into a false exact
calendar day by the packet-authoring pass. A downstream consumer cannot distinguish it from a genuine January 1
publication, so the corpus was asserting 67 specific publication days it had no evidence for.

The packet schema (`backend/src/BioStack.KnowledgeWorker/Schemas/evidence-packet.schema.json`,
`$defs.sourceSnapshot`) types `publishedAt` as `["string", "null"]`. `null` is therefore the schema-sanctioned
representation of "publication date not established," and it is the only value in this pass that is true.

Supplying a **verified** date is deliberately out of scope. It is re-review work: it requires retrieving each
source and reading its publication metadata, which needs source authorization this pass does not hold. No web
retrieval was performed and no publication date was inferred, reconstructed, or looked up. Replacing one
fabricated date with another unverified one would have preserved the original defect under a fresher
timestamp.

## Nothing Was Lost

`$defs.sourceSnapshot` sets `additionalProperties: false`, so the known year could not be preserved as a new
in-packet field. It is preserved out of band instead. Every nulled value is recorded, with the packet, packet
id, compound, `sourceId`, `sourceType`, implied year, exact removed timestamp, title, publisher, URL, DOI, and
PMID, in:

`research/review-decisions/receipts/placeholder-dates-2026-09-07/removed-placeholder-dates.json`

That artifact is committed alongside this receipt and makes the change fully reversible: when a real
publication date is verified for a given `sourceId`, the artifact identifies exactly which packet and source
entry to write it back to, and what year the original metadata implied.

## Flagged For Reviewer Attention

Two entries cite the same document, the WADA Prohibited List 2026, and both carried `2026-01-01T00:00:00Z`:

- `andarine.evidence.json` / `wada-prohibited-list-2026`
- `s-23.evidence.json` / `wada-2026-prohibited-list-official`

January 1 is that List's **entry-into-force** date, not its publication date. The cited PDF URL is itself
dated September 2025 (`.../2025-09/2026list_en_final_clean_september_2025.pdf`), so the document was published
months before the timestamp asserted. Both were nulled with the rest, because `publishedAt` is a publication
date field and January 1 is demonstrably not this document's publication date. If the corpus wants to record
an effective date for regulatory standards, that is a schema question and not something this pass decided.

## What Was Deliberately Not Done

- **No verified publication date was supplied** for any of the 67 sources. All 67 remain `null`.
- **No web retrieval** and no inference of a real publication date from any source.
- **No `authorityTier`, `evidenceTier`, `confidence`, `fieldAuthorityRequired`, `claimType`, `context`,
  `sourceRefs`, claim `statement`, `reviewFlags`, or `conflicts` value was changed** in any packet. Verified
  mechanically by diffing parsed packet objects against `HEAD`.
- **No `accessedAt` value was touched**, including the several that are themselves `T00:00:00Z` midnight
  timestamps. They are out of scope for this directive.
- **No `publishedAt` that was not exactly `YYYY-01-01T00:00:00Z` was touched.** Other midnight timestamps on
  non-January-1 days (for example `2026-04-01T00:00:00Z` in `ghk-cu.evidence.json`) were left alone; whether
  they are also widened month-only metadata is a separate question this pass did not decide.
- **No packet was cleared.** Every affected packet remains `needsReview: true` and review-gated, and this pass
  adds one more open reason to each.
- No source registry, decision batch, seed, census fixture, or public route was touched.

## Verification

- All 78 evidence packets parse as JSON.
- All 78 validate against `backend/src/BioStack.KnowledgeWorker/Schemas/evidence-packet.schema.json`
  (Draft 2020-12, `jsonschema` 4.25.1). Zero errors.
- Edits were applied as targeted text substitutions on the raw file bytes, not a `json.load`/`json.dump`
  round-trip, so per-file formatting (inline versus expanded arrays, literal versus `\uXXXX` non-ASCII,
  presence or absence of a trailing newline) is preserved. `git diff --numstat` shows 2 to 8 added lines per
  file, no reformatting churn.
- The final 3 bytes of every modified file are byte-identical to `HEAD` (trailing-newline preservation checked
  for all 44).
- Object-level diff against `HEAD` for all 44 modified packets confirms that after reverting the three
  permitted mutations, each parsed packet is equal to its `HEAD` counterpart.
- Corpus-wide grep confirms 0 remaining `"publishedAt": "YYYY-01-01T00:00:00Z"` values.
