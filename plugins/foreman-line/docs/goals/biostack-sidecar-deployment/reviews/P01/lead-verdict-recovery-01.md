# P01 Lead Verdict — Recovery 01

Candidate: `59102d3c8170968d2c9d316ec5b07a934042cee5`

Verdict before independent reviewer output: **NO RELEASE-BLOCKING P01 DEFECT DEMONSTRATED; PROCEED TO FRESH REVIEW**

## Independent lead findings

1. The candidate is frozen and clean, its full base diff stays inside the unchanged P01 allowlist, and current local-main drift does not overlap the parcel surface.
2. The exact runtime package census is fail-closed: canonical duplicates, invalid/nameless metadata, every missing required distribution, and every unexpected distribution are rejected. The explicit provider/build-tool denylist remains defense in depth.
3. The Dockerfile copy graph is an exact source/metadata allowlist rather than a build-context denylist; the real credential-shaped context probe produced the same image and could not find the fixture at runtime.
4. Interrupted or delayed creates retain their ownership claim through bounded outer recovery. Cleanup targets only an exact full ID whose random ownership label is revalidated. The documented hard-late-create and uncatchable-interruption boundaries are honest residuals, not hidden guarantees.
5. The checked-in CI TERM step operates on one exact verifier PID and one exact random-labeled container, requires verifier exit 1 inside the 20-second grace, checks for name/label remnants, and constrains trap recovery to a proven full ID plus revalidated owner marker.
6. Narrow Gate 2 Recovery 01 supplied a clean, single-use execution receipt for the previously missing native-Linux proof. The candidate did not change, the literal fixture passed once, and independent postchecks showed zero remnants.
7. Exact runtime versions, immutable image identity, no-pull execution, warning-level dark configuration, auth/privacy/kill-switch tests, installer/build-tool absence, read-only ownership posture, secret scanning, and documented downstream custody remain aligned with P01's contract.

## Coverage boundary

This verdict covers local P01/SC-01 through SC-04 evidence only. It does not claim hosted-runner execution, registry custody, Azure topology, live internal ingress, managed identity, live log inspection, deployment, rollback, API enablement, or provider behavior. Those remain P02–P04 and Gate 3 concerns.

## Next action

Obtain fresh independent adversarial and security verdicts on the exact candidate. P01 acceptance requires convergence and coordinator confirmation; P02 remains blocked until then.
