# Go-Live Assessment Scope — BioStack Local-Only

Initiative: `biostack-local-readiness`
Target Environment: local only (`docker-compose.dev.yml` — API `http://localhost:5000`, frontend `http://localhost:3043`, SQLite in Docker volume `biostack-dev-data`)
Mode: assessment (no-build during assessment; remediation via bounded foreman-line parcels only)
Known Tracks: discover from workspace (single repo `m0r6aN/biostack`, `main@e5b75e0`)
Known Security-Sensitive Surfaces: to be discovered; candidates — magic-link/passkey auth + ownership, provider PII intake, analyzer/knowledge egress gating (B3/B4/B5), Spine/receipt chain, guidance Class D boundary
Assessment Depth: standard (full gate + scenario sweep on local; no staging/prod promotion, no live billing/email/Azure)
PDD Remediation Authorized: yes — bounded, one parcel per gap, local-only, via foreman-line plugin at `D:\Repos\agent-skills\plugins\foreman-line`
Deployments: explicitly OUT OF SCOPE — no `docker-compose.yml` prod shape, no Azure, no Stripe live, no email delivery, no data restores against production

## Foreman-line execution

- Plugin root: `D:\Repos\agent-skills\plugins\foreman-line`
- Conventions: `docs/SPEC-CONVENTION.md` (spec schema v0.2, `Allowed Files` authority), `docs/COORDINATOR-PATTERN.md` (Gates 1/2/3, loop), `skills/parcel-driven-development/SKILL.md` (parcel mechanics)
- Charter: `docs/INITIATIVES/biostack-local-readiness/GOAL-CHARTER.md` (foreman-style, D1–Dn + waves/parcels + exit + standing auth)
- Parcels: `docs/specs/active/BIO-LOCAL-*.md` (SPEC-CONVENTION compliant, `status: active`, dispatchable after Gate 1)
- Gate 1 (charter ratification): NOT auto-granted — requires explicit human ratification. Gate 2 (dispatch): standing authorization requested in charter, effective per-parcel after Gate 1. Gate 3 (merge): human-owned unless repo branch rules prove otherwise at merge time.

## Prior state (not reused as proof)

- `docs/INITIATIVES/biostack-production-readiness/` verdict is **NO-GO / HOLD** (production-gated). That assessment is environment-specific and does not transfer to local.
- `BIOSTACK_FRONTEND_READINESS_AUDIT.md` (2026-07-12, re-verified at `a37726a`) is product-UX signal, not release evidence for `main@e5b75e0`.
- Untracked local `docs/specs/` (pairwise lane KEO-TBD P0–P5, `status: draft`) is out of scope for this go-live; it is research-lane work, not launch-blocking.
