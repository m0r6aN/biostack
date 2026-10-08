# CLOSURES — Hardening H1 + H2-R2; H4 register

Coordinator closure records, 2026-10-08.

## H1-POSITIVE-CONTROL — DONE

PR #503 (merged `80edabe`). Review `h1_review_1` **PASS**: the discrimination test catches both
blanket-deny and blanket-permit regressions (both directions verified), mocks the user's tier
never the gate, RATIFICATION.md drift fixed, log line visibility-only. **Hardening item H1
CLOSED.**

## H2-R2 (Findings C/D remediation) — DONE

PR #507 (merged). Dual review:
- `h2r2_review_1` **PASS** — Finding C reproduced as fixed against a LIVE Postgres with real
  Npgsql (not test doubles); Finding D narrowed as claimed; AC3 took the honest claims-correction
  path with a disclosed residual; regression green in-scope.
- `h2r2_review_2` **PASS** — C/D genuinely closed, cross-provider tests real.

### H4 register (new, non-blocking residuals from h2r2_review_2)

| ID | Finding | Disposition |
|---|---|---|
| H4a | Connection-string-literal identity drift (watermark identity derivation edge) | hardening queue — bounded follow-up parcel |
| H4b | Symlink/TOCTOU on the hidden governance directory | hardening queue — bounded follow-up parcel |
| H2-AC3-residual | Real mutual binding needs a schema/migration change outside the spine module | recorded; a future trust-path parcel may add it |

None reopen R1/C/D. Production-enablement posture for the spine: H4 pending (documented).
