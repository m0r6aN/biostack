# BIO-ANALYZER-001 Gate 2 Dispatch Record

Coordinator-owned; frozen at dispatch. Authority: charter standing authorization (Gate 2 delegated for this parcel under D1–D5, 2026-10-03). Gate 3 (merge/push/PR) is withheld.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-ANALYZER-001",
  "specPath": "docs/specs/active/BIO-ANALYZER-001-upload-table-label-leak.md",
  "specRevision": 3,
  "specSha256": "4A866B6669CDFFACB4F1211CEFD0BD10CBAEDD2F6B1E7F7486AADE9A5E889586",
  "specReview": {
    "reviewer": "SpecReviewerA",
    "rev1": "FAIL (3 blocking-class, triaged in spec)",
    "rev2": "PASS @ 70dc5d47 (0 blocking); rev 3 = clarification-only amendments"
  },
  "comparisonBase": "1c8a16e5e950e3b75f559e9c8c0744f28db2f6a5",
  "builderId": "BioAnalyzer001Builder",
  "branch": "fix/protocol-upload-graceful-failure",
  "worktree": "D:/Repos/BioStack/.worktrees/protocol-upload-graceful-failure-20261003",
  "permissionEnvelope": "local-only:no-push:no-network-mutation",
  "evidenceFile": "docs/goals/protocol-upload-graceful-failure/EVIDENCE.md",
  "implementationReviewers": ["BioAnalyzer001CodeReviewer (fresh reviewer session, read-only)"],
  "verificationContract": "spec Verification section 1-4, run from backend/ in the named worktree",
  "allowedBuilderSurfaces": [
    "backend/src/BioStack.Application/Services/ProtocolParser.cs",
    "backend/src/BioStack.Application/Services/ProtocolFingerprintService.cs",
    "backend/tests/BioStack.Application.Tests/Services/ProtocolUploadGracefulFailureTests.cs",
    "backend/tests/BioStack.Api.Tests/Integration/AnalyzeEndpointsIntegrationTests.cs",
    "docs/goals/protocol-upload-graceful-failure/EVIDENCE.md"
  ],
  "frozenSurfaces": [
    "docs/specs/active/BIO-ANALYZER-001-upload-table-label-leak.md",
    "docs/goals/protocol-upload-graceful-failure/CHARTER.md",
    "docs/goals/protocol-upload-graceful-failure/GATE2.md",
    "docs/goals/protocol-upload-graceful-failure/loop-directive.md",
    "docs/specs/INDEX.md"
  ]
}
```
