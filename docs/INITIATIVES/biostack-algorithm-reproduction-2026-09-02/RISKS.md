# Risks

- Test harness friction can masquerade as a reproduction; only assertion-level failures count.
- Static architecture tests may overstate runtime reachability; distinguish latent/config-dependent exposure.
- Timing tests can be flaky; use synchronization primitives and bounded deterministic waits.
- A single example does not establish prevalence or exploitability.
- Concurrent parcel branches must not be merged merely to aggregate intentionally failing tests.
