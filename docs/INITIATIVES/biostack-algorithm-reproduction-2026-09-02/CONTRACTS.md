# Contracts

1. Pin every test to behavior present on refreshed `main` at `339f259b1a467034db4f57cf9d774c292f11b53a` (tree `0f6d0b609ce255aad5cca81698eb3ad0917cfda6`).
2. Add tests and test-local fixtures only. Production source, configuration, migrations, generated artifacts, and snapshots are forbidden.
3. Use synthetic names, content, endpoints, and credentials. Tests must not access a network, cloud resource, production database, or user data.
4. A reproduction test states the desired invariant and is expected to fail on the pinned implementation for the alleged reason.
5. Avoid vacuous failures: the test must reach the named production path, and failure output must identify the violated invariant.
6. Run the narrowest relevant test command and record command, exit status, and failing assertion in the parcel handoff.
7. Do not weaken existing tests or suppress failures. Do not merge reproduction branches into a release branch.
