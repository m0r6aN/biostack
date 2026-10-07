# BIO-ANALYZER-004 Gate 2 Dispatch Record

Coordinator-owned; frozen at dispatch. Authority: developer authorization 2026-10-04 ("Yes, authorized"), recorded in `CHARTER.md`. Gate 3 (merge/push/PR) is withheld.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-ANALYZER-004",
  "specPath": "docs/specs/active/BIO-ANALYZER-004-spreadsheet-row-reconstruction.md",
  "specRevision": 6,
  "specSha256": "3EC9C94BC116DA48EDA5753A4E49462EB12A4A392579AC0C70F48B0421451D3C",
  "specReview": {
    "reviewers": ["Spec004ReviewerCorrectness", "Spec004ReviewerBlastRadius"],
    "rev2": "both FAIL (5 + 5 blocking-class, triaged in spec)",
    "rev3": "BlastRadius PASS; Correctness FAIL (1 blocking)",
    "rev5": "Correctness PASS; BlastRadius FAIL (1 blocking: 002 golden literal interaction)",
    "rev6": "BlastRadius PASS (0 blocking)"
  },
  "comparisonBase": "goal/protocol-upload-known-gaps tip containing BIO-ANALYZER-001, -002 (96b8f40d) and -003 (ce113a90), merged locally at f9c08d73; the builder branch starts at the commit that adds this record",
  "builderId": "BioAnalyzer004Builder",
  "branch": "fix/analyzer-spreadsheet-row-reconstruction",
  "worktree": "D:/Repos/BioStack/.worktrees/bio-analyzer-004-20261004",
  "permissionEnvelope": "local-only:no-push:no-network-mutation",
  "evidenceFile": "docs/goals/protocol-upload-graceful-failure/EVIDENCE-BIO-ANALYZER-004.md",
  "implementationReviewers": [
    "BioAnalyzer004CodeReviewerA (fresh reviewer session, read-only; lens: correctness / hostile input)",
    "BioAnalyzer004CodeReviewerB (fresh reviewer session, read-only; lens: blast radius / tests / contracts)"
  ],
  "verificationContract": "spec Verification section 1-5, run from backend/ in the named worktree",
  "allowedBuilderSurfaces": [
    "backend/src/BioStack.Application/Services/ProtocolIngestionService.cs",
    "backend/src/BioStack.Application/Services/ProtocolFingerprintService.cs",
    "backend/tests/BioStack.Application.Tests/Services/SpreadsheetRowReconstructionTests.cs",
    "backend/tests/BioStack.Application.Tests/Services/ProtocolIngestionServiceTests.cs",
    "backend/tests/BioStack.Application.Tests/Services/SpreadsheetProtocolExtractorPackageTests.cs",
    "docs/goals/protocol-upload-graceful-failure/EVIDENCE-BIO-ANALYZER-004.md"
  ],
  "frozenSurfaces": [
    "docs/specs/active/BIO-ANALYZER-004-spreadsheet-row-reconstruction.md",
    "docs/goals/protocol-upload-known-gaps/CHARTER.md",
    "docs/goals/protocol-upload-known-gaps/GATE2-BIO-ANALYZER-004.md",
    "docs/goals/protocol-upload-known-gaps/loop-directive.md",
    "docs/specs/INDEX.md"
  ]
}
```
