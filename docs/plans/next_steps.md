You are working in the BioStack repo after PR #126 has merged.

Context:
PR #126 removed leaked runtime/user-facing Protocol Intelligence WIP that had been accidentally merged from the salvage vault. Main now builds again. Protocol Intelligence must remain a build-time/offline artifact gate and review workflow only. It must not become a runtime API, runtime service, user-facing narrative response, or frontend intelligence panel.

Your mission:
Resume the preserved Slice 3 reporting-polish work from the existing patch/stash, but only after verifying that the restored architecture remains clean.

Hard boundaries:

* Do not merge, cherry-pick, or modify `preserve/protocol-intelligence-fruition-wip-20260627`.
* Do not restore `ProtocolIntelligenceService`.
* Do not restore `ForbiddenOutputScanner`.
* Do not restore `ProtocolIntelligenceResponse`.
* Do not add `BuildResponse` or similar narrative-building runtime behavior.
* Do not add `GET/POST {id}/intelligence` or `intelligence/preview` runtime endpoints.
* Do not add frontend Protocol Intelligence panels or user-facing PI UI.
* Do not introduce a second forbidden-output scanner, second gate, or inline forbidden phrase list.
* Do not modify billing/feature-flag posture in this PR unless it is directly required for reporting polish. Inert PI flags are a separate follow-up.

Required first step:
Run a contamination scan against current main before applying Slice 3 work. Confirm that no leaked runtime surface remains.

Suggested scan terms:

* `ProtocolIntelligenceService`
* `ForbiddenOutputScanner`
* `ProtocolIntelligenceResponse`
* `BuildResponse`
* `intelligence/preview`
* `protocol-intelligence/contracts`
* `ProtocolIntelligencePanel`
* `HighRiskWarning`
* `PhaseMap`
* `SideEffectAmbiguity`
* `SourceQuality`

Allowed scope:

* Reporting polish for the canonical offline/build-time Protocol Intelligence path.
* Improvements to reviewed artifact reporting, evaluation output readability, diagnostics, receipts, test clarity, or worker output formatting.
* Small refactors only if they directly support the reporting-polish objective.
* Add regression tests or guard tests that prevent runtime/user-facing PI leakage from returning.

Preserve:

* `ProtocolIntelligenceGate`
* `ProtocolIntelligenceContracts`
* `ProtocolIntelligenceArtifactLoader`
* `DoctrineSanitizer`
* `ProtocolIntelligenceEvaluationJob`
* `RunMode`
* `WorkerOptions`
* `ProtocolIntelligenceReview.tsx`
* Lane G/C/H and PR #121/#123 behavior

Validation required:

* `dotnet build backend/BioStack.sln`
* `BioStack.KnowledgeWorker.Tests`
* `BioStack.Application.Tests`
* `BioStack.Api.Tests`
* Focused frontend tests affected by the slice
* Frontend typecheck, while clearly separating any pre-existing unrelated frontend failures

Deliverables:

1. Short summary of what Slice 3 reporting polish changed.
2. Explicit confirmation that no runtime PI endpoint/service/narrative/UI surface was restored.
3. Test/build receipt.
4. Any follow-ups that were intentionally deferred.
