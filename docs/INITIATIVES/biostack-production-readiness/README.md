# BioStack Production Readiness

> **2026-10-25 owner rulings (supersede this file where they conflict):** Clinton Morgan is the sole owner, operator and decision maker; his sign-off is final. No human-gated approval: the two-reviewer AI council (`docs/operations/ai-council-review.md`) is the content/approval gate, and disagreement escalates to Clint with options. No legal review: legal items are known risks, not blockers. Current gates: `docs/launch-readiness-ledger.md`.

Durable coordination state for initiative `biostack-production-readiness`.

**Status:** active, release blocked. **Recommendation:** **NO-GO / HOLD**.

The original hosted evidence baseline is `main@a37726a4df9b73378e46232b849f409db67d12df`. Candidate `c96bc3b` on `codex/security-integration` now passes hosted validation, offline verification, and current-tree Gitleaks. It has not been merged or deployed, and historical credential/proxy/live-environment evidence remains open. Markdown is the approved persistence surface for this initiative; no runtime coordinator database is introduced.

## State index

- [Discovery](DISCOVERY.md), [Charter](CHARTER.md), [Tracks](TRACKS.md)
- [Integration surfaces](INTEGRATION-SURFACES.md), [Contracts](CONTRACTS.md), [Scenarios](SCENARIOS.md)
- [Dispatch](DISPATCH.md), [Parcels](PARCELS.md), [PR-DOC-001](parcels/PR-DOC-001.md)
- [Verification](VERIFICATION.md), [Security gates](SECURITY-GATES.md), [Release gates](RELEASE-GATES.md)
- [Decisions](DECISIONS.md), [Risks](RISKS.md), [Evidence](EVIDENCE.md)
- [Session handoff](SESSION-HANDOFF.md), [Final handoff](FINAL-HANDOFF.md)

Only Clinton Morgan changes the recommendation. The ranked remaining gates are in `docs/launch-readiness-ledger.md`.
