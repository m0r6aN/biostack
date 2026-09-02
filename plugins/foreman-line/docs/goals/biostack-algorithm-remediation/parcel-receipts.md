# Parcel Receipts — BioStack Algorithm Remediation

Status: **live coordinator record; no merge or release authority**

## P01 — Parser semantics

- Base: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Candidate: `751090eabf3ee6f066c17ee83861ae04711fd4a0`
- Changed paths: exactly AF-P01 (`ProtocolParser.cs` and retained `ProtocolParserReproductionTests.cs`)
- Diagnostic test blob: `ab4bd922f2f17c05b37e05115245e52ef265ee98`, byte-identical
- Targeted: 6 passed
- Adjacent parser/ingestion: 28 passed
- Application: 615 passed, 5 skipped, 620 total
- Full solution: Domain 7; Application 615 passed/5 skipped; verifier 158; API 377; KnowledgeWorker 866; zero failures
- Independent adversarial review: ACCEPT, no P0-P3 findings
- Custody: clean local branch `codex/biostack-remediation-p01`; not pushed, merged, deployed, or released

## P02 — Protocol ingestion boundaries

- Base: `339f259b1a467034db4f57cf9d774c292f11b53a`
- Candidate: `1b5742051ee2b54a80dac9bbdf68c68e5a6b5992`
- Changed paths: exactly AF-P02 (`ProtocolIngestionService.cs` plus retained PDF and OCR reproduction tests)
- Targeted: 4 passed; denial/gate-failure/authorized fake sends `0/0/1`
- Adjacent ingestion/security: 19 passed
- Real current-user/consent integration: 17 passed
- Full solution: 2,021 passed, 5 pre-existing live-test skips, zero failures
- Adversarial review A: ACCEPT, no findings
- Adversarial review B: ACCEPT, no Critical/High/Medium/Low findings
- Defensive security review: ACCEPT; SG-SCOPE PASS; SG-OUTBOUND PASS; no security findings
- Custody: clean local branch `codex/biostack-remediation-p02`; not pushed, merged, deployed, or released

## Q04 — Frontend dependency readiness

- Base/head: `339f259b1a467034db4f57cf9d774c292f11b53a`; no repository changes
- First run: `npm ci --offline` succeeded and the main-based route test passed 2/2, but npm 11.6.2 consumed the required worker flags
- Fresh review: REJECT; the first `READY` token is void and does not unblock P07
- Procedural repair: goal commit `f5af63ea313d4c5dcb4cebd221d9df1677f29d28` uses the installed local Vitest entrypoint through `node`, emits exact argv, rejects unknown-option/config warnings, and requires a preserved external transcript
- Current status: amended Step 0 confirmed; corrected offline rerun pending

## Gate custody

P01 and P02 are review-complete candidate commits, not approved merge commits. Q04 is not yet accepted. Amendment 01 remains pending for D6-D9. Gate 3 remains ungranted, so no push, PR, merge, deployment, publication, or release is authorized.
