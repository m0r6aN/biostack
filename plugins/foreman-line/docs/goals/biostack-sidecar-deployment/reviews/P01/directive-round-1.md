# P01 Review Directive — Round 1

Candidate: `56063a4d238a0e8ddabb2ebfa56c64f1de47d027`

This directive is the lead synthesis after the lead verdict and all three independent seat reports were fixed in evidence.

## Convergence triage

| Topic | Lead | Adv A | Adv B | Security | Disposition |
| --- | --- | --- | --- | --- | --- |
| Mutable/unhashed CI inputs | yes | yes | yes | yes | **FIX** |
| Runtime installer/write access | yes | — | yes | yes | **FIX** |
| Cleanup after malformed create | — | yes | — | yes | **FIX** |
| Checkout-only gitleaks limitation | yes | — | — | — | **ACCEPT-DEFERRED to P03**; keep claims exact |
| Protected-check enforcement | — | — | yes | — | **ACCEPT-DEFERRED to Gate 3 evidence** |

## Verified unique findings

- **FIX:** thread the complete sensitive set through every Docker subprocess and sanitize all terminal control sequences.
- **FIX:** randomize and track every submitted request marker used by leakage assertions.
- **FIX:** fail closed on dockerignore negations that re-include protected paths.
- **FIX:** require exact workflow allowlist equality.
- **FIX:** require a nonempty expected runtime package census and canonicalize `-`, `_`, and `.` separators.
- **FIX:** add orchestration regression coverage for malformed create output, timeout, cleanup, and primary-plus-cleanup failure.
- **FIX:** bind the runtime image to the local immutable image ID in verifier execution and evidence output where practical within the parcel.
- **ACCEPT-RESIDUAL:** the synthetic local test token may appear in same-host Docker argv. It is randomly generated, has no production authority, never crosses the loopback-only test boundary, and remains prohibited from diagnostics.
- **ACCEPT-RESIDUAL pending independent confirmation:** the project lock does not govern isolated build-system dependency resolution. P01 must either make that path deterministic within its allowed files or state and test the build-mode constraint honestly.

## Rework constraints

1. Run a fresh P01 Step 0 against this exact candidate and the unchanged nine-file allowlist.
2. Modify only the smallest subset of P01 Allowed Files needed for the FIX dispositions.
3. No external provider call, secret use, public ingress, production configuration, push, PR, merge, or Azure/GitHub administrative mutation.
4. Re-run the complete deterministic P01 verification chain.
5. Submit the new exact candidate commit to fresh two-seat adversarial review plus security review. P01 is not accepted until that review chain is green.
