# Contracts

1. Pin every test to behavior present at base `27cdb5c17e4b0a302567dbd854ed29277156a966`.
2. Add tests and test-local fixtures only. Production source, configuration, migrations, generated artifacts, and snapshots are forbidden.
3. Use synthetic names, content, endpoints, and credentials. Tests must not access a network, cloud resource, production database, or user data.
4. A reproduction test states the desired invariant and is expected to fail on the pinned implementation for the alleged reason.
5. Avoid vacuous failures: the test must reach the named production path, and failure output must identify the violated invariant.
6. Run the narrowest relevant test command and record command, exit status, and failing assertion in the parcel handoff.
7. Do not weaken existing tests or suppress failures. Do not merge reproduction branches into a release branch.
