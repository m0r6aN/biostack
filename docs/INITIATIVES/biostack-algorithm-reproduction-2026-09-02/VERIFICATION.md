# Verification

Verification is parcel-local and intentionally expects the new reproduction assertions to fail on the pinned base. Compilation, collection, fixture, or environmental errors do not count as reproductions.

After all parcels finish, reconcile:

- every changed file against Allowed Files;
- every failing assertion against its named audit claim;
- network isolation and synthetic-data use;
- branch and commit identity;
- reproduced, not reproduced, and inconclusive claims.
