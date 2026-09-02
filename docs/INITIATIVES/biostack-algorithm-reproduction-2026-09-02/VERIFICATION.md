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
