# Parcel: KEO-74-SEVEN-SOURCE-ACTIVATION-004

Status: implementation complete; verification and publish pending.

## Goal

Record Johnathan Harper's affirmative legal-rights decision for the seven recommended official sources and activate only the acquisition boundaries already documented in the reviewed decision packet.

## Human decision

Clint Morgan conveyed the following source-specific decisions from legal/rights approver Johnathan Harper on 2026-07-25:

- `fda`: approved
- `pubchem`: approved
- `pubmed`: approved
- `clinicaltrials`: approved
- `dailymed`: approved
- `nih-ods`: approved
- `nih-nccih`: approved

The governed record timestamp is `2026-07-25T15:24:14Z`, the time the decisions were recorded in this parcel. It is not represented as an independently supplied decision timestamp.

## Approval interpretation

Each approval applies only to the corresponding existing entry in `recommended-seven-source-decisions.v1.json`:

- the existing proposed uses and data boundary;
- the existing first-party terms URL and documented limitations;
- the existing candidate acquisition method;
- the existing no-private-data, no-copyrighted-full-text-storage, and no-training-use controls; and
- claim-level human review before canonical promotion.

No broader license, content class, redistribution right, automated scraping permission, or claim authority is inferred.

Six sources use their reviewed public API candidate method. `nih-nccih` remains `manual-review`; no automated NCCIH retrieval is authorized.

## Changes

- Activates the seven approved entries in the governed source registry while leaving the other six entries disabled and pending human legal review.
- Records Johnathan Harper as the legal-rights reviewer and preserves source-specific rights and redistribution controls.
- Enables manual refresh and conservative source-specific request policies.
- Updates the registry SHA-256 binding in the decision packet and its schema.
- Replaces the registry schema's mixed-state `if`/`then` validation with an equivalent per-source `anyOf`, allowing approved and disabled entries to coexist while keeping active entries fail-closed.
- Updates real-artifact and planning tests to require 490 of 490 acquisition intents to be ready.

## Boundaries

- No HTTP request is made.
- No raw source content is stored.
- No database, worker scheduling, intake, promotion, or deployment path is changed.
- Evidence review by Ellison Nemoy remains required before canonical claim promotion.
- Security/data review by Pradic Patel remains conditional on a declared trigger.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~ResearchArtifactValidatorTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~SourceAcquisitionPlanningTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --disable-build-servers
rtk proxy certutil -hashfile research/input/sources/pilot-source-registry.json SHA256
rtk git diff --check
```

Expected registry SHA-256:

`3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28`
