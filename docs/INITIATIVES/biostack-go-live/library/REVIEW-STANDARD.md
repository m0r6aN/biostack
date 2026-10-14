# Library qualifying standard — agent review

Date: 2026-10-14
Owner of judgement: the assigned review agent. Do not leave an item for Clint. He is the sole operator and is not the substance reviewer.

## Source of truth

Edit only the assigned objects inside:

`backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`

Match each assigned `identity.canonicalId`. Do not add a second object with the same `canonicalId` or the same `identity.slug`. Do not delete an assigned object. Do not edit objects outside the assignment.

The file is one JSON array. Preserve valid JSON. Do not reorder the array. Change field values inside the assigned objects only.

## What counts

An entry counts only when all of the following are true after your edit:

- Distinct `canonicalId` and display name. Aliases are not separate entries.
- Classification is a real class, not `Other` and not empty.
- Purpose: at least one indication with a non-empty statement of what people look this compound up for. Phrase it as studied, labeled, or commonly discussed use. Do not phrase it as a personal recommendation.
- Evidence quality: `evidence.overallTier` is one of Strong, Moderate, Limited, Insufficient, Unknown. Use Unknown or Limited when the support is thin. Do not upgrade a thin record to Strong.
- Known risks: at least one contraindication, warning, or adverse effect. If specific risks are not established, say that the risk profile is not established and name the practical harm (uncertain identity, injection risk, prescription-only status, or lack of human data).
- Interactions: at least one interaction, or an explicit statement that no specific interaction is established and a named class risk still applies.
- Uncertainty: at least one evidence gap or controversy. Do not delete uncertainty to make the record look finished.
- Sources: at least one `provenance.sourceRecords` item with a real title, publisher, and URL. Prefer the FDA label, DailyMed, PubMed, NIH ODS, or a named systematic review. Do not invent PMIDs, URLs, quotes, or approval facts.
- Review date: `provenance.lastReviewedAt` is `2026-10-14T00:00:00Z` when you accept the record.
- Regulatory status: `regulatory.regulatoryStatus` and `regulatory.jurisdiction` are filled. Use `not-established` when you cannot verify an approval. Do not invent an FDA approval.
- `provenance.reviewStatus` is `reviewed`.
- `ops.isActive` is true.
- `ops.needsReview` is false.
- `ops.completeness` is `substantial`.
- `ops.lastChangeType` is `agent-review-2026-10-14`.
- Remove any curation note that says the record is an intentionally conservative launch seed or that a human must review it before publication.

## Judgement rules

- You may correct classification, purpose, risks, uncertainty, sources, and regulatory wording when the assigned record or a public primary source supports the change.
- If you cannot verify a fact, narrow the claim or mark it unknown. Unknown is a valid published state.
- Never add a dose, schedule, reconstitution step, sourcing instruction, or "safe for you" statement.
- Never say BioStack guarantees safety or eliminates risk.
- Prescription, investigational, gray-market, and illegal-sourcing status must stay visible. Do not help a reader obtain a controlled substance or evade a rule.
- `acute` emergency content, if present, must point to urgent professional care and must not become ordinary use guidance.
- Do not refuse an entry only because evidence is limited. Limited evidence with an honest risk statement qualifies. A placeholder that still says "draft" or "human review required" does not.

## Output

Write a batch report to the path named in your assignment. One row per canonical ID: `accepted` or `narrowed`, the evidence tier you set, the regulatory status you set, and the source title you relied on. If you could not make an object valid JSON, stop and say so. Do not claim a count you did not edit.
