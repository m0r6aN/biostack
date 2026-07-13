# BioStack Production Readiness

Durable coordination state for initiative `biostack-production-readiness`.

**Status:** active, release blocked. **Recommendation:** **NO-GO / HOLD**.

The hosted evidence baseline remains `main@a37726a4df9b73378e46232b849f409db67d12df`. The latest local remediation candidate is `565805a` on `codex/security-integration`; its application/dependency state was validated at `fb0ed84`, and the later commit adds only the offline-verification fetch correction. It has not been pushed, deployed, or verified live. Markdown is the approved persistence surface for this initiative; no runtime coordinator database is introduced.

## State index

- [Discovery](DISCOVERY.md), [Charter](CHARTER.md), [Tracks](TRACKS.md)
- [Integration surfaces](INTEGRATION-SURFACES.md), [Contracts](CONTRACTS.md), [Scenarios](SCENARIOS.md)
- [Dispatch](DISPATCH.md), [Parcels](PARCELS.md), [PR-DOC-001](parcels/PR-DOC-001.md)
- [Verification](VERIFICATION.md), [Security gates](SECURITY-GATES.md), [Release gates](RELEASE-GATES.md)
- [Decisions](DECISIONS.md), [Risks](RISKS.md), [Evidence](EVIDENCE.md)
- [Session handoff](SESSION-HANDOFF.md), [Final handoff](FINAL-HANDOFF.md)

Only the release owner may change the recommendation after all blocking gates have environment-specific evidence.
