# Narrow Gate 2 P01 Round 07 Audit Recovery 01 — Ratified 2026-09-13

The human authorized the following exact recovery:

> Narrow Gate 2 P01 Round 07 Audit Recovery 01: I acknowledge the recorded offline-audit stop at goal commit 13b9de2 and authorize one isolated execution of the unchanged P01 runtime and build-closure pip-audit commands. Network access must be restricted by a destination-allowlisting helper to official pypi.org:443 JSON vulnerability-metadata endpoints for only the exact frozen dependency sets under review. No package artifact, wheel, source archive, index, or other destination may be accessed. No source, test, workflow, configuration, timeout, or candidate modification is authorized by this recovery. If both strict audits are green, I authorize resuming the remaining Round 07 verification and correction sequence under the existing ratification; the previously authorized single Debian/docker.io TERM-helper exception remains separate and unused. Any audit failure, unexpected destination, scope drift, dependency-set change, or other ambiguity stops execution. All other prohibitions remain unchanged. P02 remains blocked, and Gate 3A and Gate 3B remain ungranted.

## Execution constraints

- The P01 start candidate remains `59102d3c8170968d2c9d316ec5b07a934042cee5`, with only the five ratified Round-07 files modified and no new commit yet.
- Audit recovery is a single isolated execution of the two unchanged strict audit commands, not permission to alter the candidate or expand package/network scope.
- The helper must enforce both official host/port and exact JSON endpoint paths for the two frozen dependency sets. A blind HTTPS CONNECT proxy alone does not prove endpoint restriction.
- Only two green strict audits reopen the remaining Round-07 sequence. A failure or uncertain boundary is a hard stop.
- The Debian `docker.io` real-Docker TERM allowance is distinct and unused.
