# Parcel: KEO-74-FDA-CANDIDATE-ENVELOPE-PARITY-016

Status: implementation and verification complete; independent review and
publish pending.

## Goal

Bring the existing FDA openFDA drug-label adapter into candidate-envelope
parity without widening source retrieval, runtime activation, storage, or
canonical-ingest authority.

## Scope

The adapter now:

- derives nonempty authorized field uses only from fields actually emitted;
- records substantive FDA label identity and effective-time provenance;
- attaches reviewed FDA and openFDA rights attributions whose covered fields
  exactly partition the emitted field set;
- records the validated source effective date as the document update date while
  leaving the unsupported publication date blank;
- requires a non-endorsement reuse boundary that excludes restricted or GMDN
  data, media and third-party content, copyrighted full text, unallowlisted
  source text, and individualized guidance;
- rejects a missing, blank, malformed, or invalid-calendar `effective_time`
  instead of inventing a publication date;
- emits the exact transformation version and no manual-capture audit; and
- reruns the shared required-provenance guard before emitting each candidate.

All candidate content remains observational and review-required. The change
does not add fields, source text, medical guidance, prescribing logic, or
promotion authority.

## Adversarial coverage

Tests prove fail-closed behavior for:

- missing, blank, incorrectly formatted, and invalid-calendar effective dates;
- missing or unreviewed rights attribution;
- missing source-specific provenance;
- rights coverage that omits an emitted field;
- an authorized use not represented by emitted fields; and
- a missing transformation version; and
- a fabricated publication date copied from `effective_time`.

The runtime-compatible envelope test also reruns the shared required-provenance
guard and the FDA-specific envelope validator against the emitted candidate.

## Allowed files

- `backend/src/BioStack.KnowledgeWorker/Pipeline/FdaOpenFdaDrugLabelAcquisitionAdapter.cs`
- `backend/tests/BioStack.KnowledgeWorker.Tests/FdaOpenFdaDrugLabelAcquisitionAdapterTests.cs`
- this parcel record

## Forbidden

- No runtime mode, dependency-injection, scheduling, configuration, database,
  canonical-ingest, promotion, frontend, deployment, or secret change.
- No live FDA or openFDA request.
- No raw response persistence or source-text expansion.
- No commit, push, pull request, merge, or deployment is authorized by this
  parcel.

## Verification

```powershell
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~FdaOpenFdaDrugLabelAcquisitionAdapterTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --filter FullyQualifiedName~SourceAcquisitionPrimitivesTests --disable-build-servers
rtk test dotnet test backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --disable-build-servers
rtk proxy dotnet format backend/tests/BioStack.KnowledgeWorker.Tests/BioStack.KnowledgeWorker.Tests.csproj --no-restore --verify-no-changes --include backend/src/BioStack.KnowledgeWorker/Pipeline/FdaOpenFdaDrugLabelAcquisitionAdapter.cs backend/tests/BioStack.KnowledgeWorker.Tests/FdaOpenFdaDrugLabelAcquisitionAdapterTests.cs --verbosity minimal
rtk git diff --check
rtk git status --short
```

## Verification results

- FDA adapter suite: 33 passed, 0 failed, 0 skipped.
- Shared acquisition-primitives suite: 55 passed, 0 failed, 0 skipped.
- Full KnowledgeWorker suite: 772 passed, 0 failed, 0 skipped.
- Targeted formatting verification and `git diff --check`: passed.
- Existing `System.Security.Cryptography.Xml` 10.0.9 `NU1903` advisories and
  the existing FDA test nullable warning remain unchanged.
