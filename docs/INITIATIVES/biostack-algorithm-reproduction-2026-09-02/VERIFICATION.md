# Verification

Verification is parcel-local and intentionally expects the new reproduction assertions to fail on the pinned base. Compilation, collection, fixture, or environmental errors do not count as reproductions.

After all parcels finish, reconcile:

- every changed file against Allowed Files;
- every failing assertion against its named audit claim;
- network isolation and synthetic-data use;
- branch and commit identity;
- reproduced, not reproduced, and inconclusive claims.

## Final reconciliation

- All five branch heads have refreshed `main` commit `339f259b1a467034db4f57cf9d774c292f11b53a` as an ancestor.
- Parcel diffs contain only contract-authorized test files.
- 17 new assertions fail for the intended current-behavior reasons: parser 7, interaction 4, evidence/provenance 2, sidecar 2, and outbound 2.
- Adjacent baselines recorded by parcels pass: interaction 4, evidence/staging 36, and sidecar 19. Parser and outbound commands compiled and reached only their intended assertions.
- No production source, cloud environment, external provider, secret, or protected data was used.
- Inconclusive: broad endpoint gate coverage, direct regex runtime exploitability, and Collective outbound behavior under the current Allowed Files.

## Coordinator closure check (2026-09-02)

- Rechecked all five parcel branch heads and their diffs from the contract commit. Each worktree is clean; each branch descends from the contract commit and has execution base `339f259b1a467034db4f57cf9d774c292f11b53a` as an ancestor.
- Re-ran the parser, interaction, evidence/provenance, OCR outbound, and sidecar commands recorded in `SESSION-HANDOFFS.md`; each reached only its intended assertion failures.
- The frontend outbound reproduction could not be independently collected in this environment because the outbound worktree lacks local npm dependencies. This is an environment blocker, not a reproduction result; the prior dependency-complete assertion-level result remains recorded as historical evidence.
- Gate 1 is pending explicit developer ratification in this coordinator run. Gate 3 remains ungranted; no failing parcel branch may be merged.
