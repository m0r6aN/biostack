# Parcel: KEO-74-NIH-NCCIH-MANUAL-REVIEW-011

Status: implementation, verification, and independent review complete; publish and merge pending.

## Goal

Add one fail-closed, manual-review-only NCCIH candidate workflow for the
initial allowlisted melatonin page. The workflow accepts only operator-supplied
capture data with an independent approved audit. It performs no retrieval and
does not promote captured context into canonical claims.

## Branch and worktree

- Branch: `codex/keo74-nih-nccih-manual-20260725`
- Worktree: `D:\Repos\BioStack-keo74-nih-nccih-manual-011-20260725`
- Base: shared source-acquisition primitives

## Scope

Allowed files:

- `backend/src/BioStack.KnowledgeWorker/Pipeline/NccihManualReviewCandidateWorkflow.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/NccihManualReviewCandidateWorkflowTests.cs`
- this parcel record

No HTTP client, browser, scraper, live NCCIH content, source fixture, runtime
registration, scheduler, persistence, database, canonical ingest, deployment,
frontend, commit, publish, or merge is included.

## Source contract

- Source: `nih-nccih`
- Planning adapter: `nih-nccih-planning-v1`
- Candidate method: `manual-review`
- Transformation: `nccih-manual-review-v1`
- Initial page URL:
  `https://www.nccih.nih.gov/health/melatonin-what-you-need-to-know`
- Exact page title: `Melatonin: What You Need To Know`
- Exact source-item slug: `melatonin-what-you-need-to-know`

The only mapped target is canonical `Melatonin`. Its planning terms must
contain `Melatonin` and may contain only the registered chemical alias
`N-acetyl-5-methoxytryptamine`. A valid governed intent for any other target
returns `NoMatch` before the workflow inspects or accepts a capture.

## Capture boundary

The operator must supply the exact allowlisted URL, title, and slug, plus an
exact `yyyy-MM-dd` page update date and a substantive page section. The only
capture keys are:

| Capture key | Authorized field use |
|---|---|
| `identity_context` | `identity` |
| `mechanism_context` | `mechanism` |
| `efficacy_context` | `efficacy-claims` |
| `interaction_context` | `interactions` |

Each key must intersect the intent's authorized field uses. Captured values
are bounded, nonempty, unique plain-text strings. Markup, links, URLs,
control characters, unknown keys, unauthorized keys, empty collections, and
oversized collections fail closed. The workflow does not draft, infer, fetch,
or summarize source text.

## Manual audit

Every completed candidate carries `SourceManualCaptureAudit`. The workflow
calls `ValidateApprovedManualCaptureAudit` and also bounds the supplied actor
identifiers and review notes. Completion requires:

- distinct substantive operator and reviewer identifiers;
- UTC capture and review timestamps in strict chronological order;
- review completed before candidate retrieval time;
- decision `approved`;
- substantive bounded review notes; and
- all seven source, rights, acknowledgement, non-endorsement,
  non-prescriptive, non-regulatory, and no-safety-conclusion attestations.

Any mismatch fails closed. `QueryUrl` is always null.

## Reviewer-owner transfer

Evidence-review responsibility is assigned to Clint Morgan, Entra object id
`461a4112-8e91-41cb-afef-6889b8f48ff0`, by the receipt-time binding in
`research/source-authorization/keo-74-reviewer-owner-transfer-receipt.v1.json`.
Clint Morgan is also the assigned NCCIH operator, so he is not an independent
reviewer of a capture he performs. The existing distinct-identifier guard is
unchanged, and operator-created NCCIH candidates remain blocked until a
distinct authorized reviewer performs the reviewer action.

## Provenance, rights, and reuse

Each candidate requires the exact page title, source-item slug, section, page
update date, canonical URL, registry binding, UTC retrieval time, reviewed
rights state, transformation version, and `review-required` status.

Rights metadata:

- acknowledges NIH National Center for Complementary and Integrative Health;
- limits the covered content to manually captured NCCIH-authored
  public-domain text;
- records BioStack's grouping transformation;
- requires non-endorsement language; and
- excludes photographs, illustrations, videos, logos, trademarks, external
  linked resources, separately copyrighted third-party material,
  individualized advice, dosing direction, regulatory claims, and
  safety-critical conclusions.

The workflow emits evidence context only. Claim-level evidence review remains
required before canonical promotion.

## Synthetic verification

Tests use only constructed synthetic capture strings and audit metadata. They
cover:

- exact allowlisted page identity and exact melatonin target correlation;
- `NoMatch` before capture inspection for unmapped targets;
- ready intent, planning adapter, manual method, registry hash, provenance,
  and UTC requirements;
- exact page update date and section validation;
- all four strict capture-key to field-use mappings;
- unsupported, mis-cased, unauthorized, empty, duplicate, HTML/Markdown
  markup, scheme URL, bare-domain link, control-character, and oversized
  capture rejection;
- exact candidate provenance, null query URL, rights, acknowledgement,
  reuse exclusions, non-endorsement, and review-required status;
- independent operator and reviewer identities, timestamp ordering,
  approval, bounded notes, and retrieval-after-review; and
- each of the seven required manual-capture attestations.

No live source request, page content, or source fixture is used.

## Verification commands

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~NccihManualReviewCandidateWorkflowTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~SourceAcquisitionPrimitivesTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~FdaOpenFdaDrugLabelAcquisitionAdapterTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --disable-build-servers
rtk git diff --check
rtk git status --short
```

## Stop gates

- No automated NCCIH retrieval is authorized.
- No target or page expansion is authorized.
- No live or copied NCCIH page content is authorized in tests.
- No capture may complete without an independent approved audit.
- No evidence promotion, runtime wiring, persistence, commit, publish, or
  merge is authorized in this parcel.

## Handoff

- NCCIH workflow tests: 71 passed, 0 failed, 0 skipped.
- Shared acquisition-primitives tests: 55 passed, 0 failed, 0 skipped.
- Existing FDA adapter tests: 21 passed, 0 failed, 0 skipped.
- Full KnowledgeWorker tests: 462 passed, 0 failed, 0 skipped.
- Diff whitespace validation: passed.
- Warnings: pre-existing `System.Security.Cryptography.Xml` 10.0.9
  `NU1903` advisories only.
- No commit, publish, or merge was performed.
