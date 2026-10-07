# P01 Round-Six Network-Boundary Incident

Date: 2026-09-09

Candidate: `59102d3c8170968d2c9d316ec5b07a934042cee5`

## Event

The isolated builder executed the literal checked-in CI TERM fixture in a cached digest-pinned Node 22 container. To provide a Linux Docker CLI inside that helper, it ran:

```text
apt-get update -qq
apt-get install -y -qq --no-install-recommends docker.io
```

The helper then used the local Docker Desktop socket and local candidate image. The functional TERM assertion passed, but the package-manager acquisition used external network access not authorized by the Gate 1 no-external-call boundary.

## Scope and impact

- No source, model, scientific, provider, Azure, production, or protected-data endpoint was contacted.
- No credential or secret was supplied or created.
- No repository remote mutation occurred.
- The helper was run with `--rm`; no helper or P01-owned test container remains.
- Candidate source and image identity were not changed.
- Process impact: round-six deterministic verification is invalid and cannot support P01 acceptance.

## Required control improvement

Future parcel dispatches must treat any package-manager metadata or artifact acquisition as an external call unless the human expressly authorizes it. Step 0 and handoff reports must name the provenance of every required helper binary and state whether it is already cached. A command that would resolve or download missing tooling must stop before execution.
