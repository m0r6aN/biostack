# Security Gates — biostack-local-readiness (A-5, local only)

Method: no separate hosted audit. Each `high` surface gets hostile-input probing inside its parcel's adversarial review (fresh session, frontier, zero builder context) + deterministic negative tests. Findings map per severity→gate table below. No waiver exists at assessment time.

| Gate | Surface/threat (local) | Required evidence (parcel) | Status | Blocking |
|---|---|---|---|---|
| SG-L1 | L3 auth/session/ownership bypass (magic-link replay/tamper, session fixation, direct-ID access, consent bypass) | LS4–LS6 negative tests + review probe report | unverified | yes |
| SG-L2 | L2/L8 reasoning-leak + Class D (B3/B4/B5 reduced-shape bypass; prescriptive/dosing/injection language; math-vs-advice confusion) | LS3+LS9 fixtures + backstop suite + probe report | unverified | yes |
| SG-L3 | L4 data loss/cross-user access + hash round-trip (SQLite `DateTimeKind`, owner scoping) | LS5+LS7 isolation/round-trip output | unverified | yes |
| SG-L4 | L2 provider-intake abuse (enumeration, oversize/abusive input, PII retention) | LS5 intake limits + non-enumeration proof | unverified | yes |
| SG-L5 | L1/L7 supply-chain + config (secret defaults fail-closed locally; no dev secret in prod path; lockfile integrity for touched parcels) | config assertions + `git diff --check` + parcel-scoped audit output | unverified | yes |
| SG-L6 | Logging/telemetry leakage (health payload, identity, raw prompt/source dump in logs or artifacts) | structured-log/payload review note + redaction attestation | unverified | yes |
| SG-L7 | Public safety/legal copy (no outcome promise, no data-custody claim, no withheld-evidence claim, stubs stay stubs) | copy-guard review vs `.audit/POSITIONING-ARTIFACTS-v2.md` | unverified | yes |
| SG-L8 | Seed claim integrity (no invented claims/indications/safety statements in new seed records; unknowns stay unknown-honest; collisions never auto-merged; records stay draft/inactive) | LS12–LS13 validator output + per-claim source trace + dual-review probe | unverified | yes |

## Severity → gate mapping (binding)

Critical/High → `failing`, release **blocked**. Medium → `failing`, blocked by default; waivable only with finding ID + rationale + named owner + date + re-assessment timeline. Low/Informational → `advisory`, non-blocking, carried as deferred work. Critical/High waivers without a named owner are invalid.

## Parcel security-gate linkage

003 → SG-L1, SG-L3 (partial), SG-L4. 002+005 → SG-L2, SG-L7. 004 → SG-L3 (partial), SG-L5 (partial), SG-L6. 001 → SG-L5 (partial), SG-L6 (partial). 006 → SG-L5 (partial, entitlement honesty). 007 → SG-L8 (partial: allocation contains only traceable IDs, no claim content). 008/009/010 → SG-L8 (primary: claim-trace + unknown-honesty + collision discipline). 011 → SG-L8 (partial: serving layer exposes draft status honestly, no published-reading of unreviewed records). A parcel with a security-gate requirement is not mergeable until its gate evidence is recorded and triaged.
