# Integration Surfaces — biostack-local-readiness (A-2, local only)

| ID | Producer → Consumer | Contract | Auth / data | Security relevance | Parcel |
|---|---|---|---|---|---|
| L1 | Compose → API (`:5000`) + UI (`:3043`) | `docker-compose.dev.yml` (healthchecks, `biostack-dev-data` volume, `NEXT_PUBLIC_API_URL=http://localhost:5000`) | none (loopback dev) | low (local-only blast radius) | BIO-LOCAL-001 |
| L2 | Browser → Frontend → API knowledge/tools | contract `routes.publicPrefixes` + `canonical`/`aliases`; `GET /api/v1/knowledge/compounds[/{name}]`, `POST /api/v1/knowledge/overlap-check`, `POST /api/v1/knowledge/interaction-check`, `/tools/*` | anonymous allowed; reduced shapes only | high — B4/B5 reasoning-leak + honesty boundary | BIO-LOCAL-002, 005 |
| L3 | Browser → Auth → protected routes | magic-link (in-memory inbox, Development) + passkey/OAuth posture; session cookie; return-path; owner-scoped profiles/protocols; versioned consent gate | identity + customer health/profile data | high — session/ownership/consent bypass | BIO-LOCAL-003 |
| L4 | API → SQLite (local) | `Database:Provider` + hand-written migrations; `Data Source=/app/data/biostack.db` | owner-scoped rows; no prod data | medium — cross-user access, hash round-trip (`DateTimeKind`) | BIO-LOCAL-003, 004 |
| L5 | Runtime → Spine → receipt views | `SpineEntry`/`SpineChain` + `SpineCheckpointService` + cadence worker; Keon adapter (`IKeonRuntimeClient`, `ReceiptClass`); stubbed runtime in Development; `/governance/receipts`, `/receipts/[uri]` authenticated | receipt/provenance data | high — audit-integrity, fail-closed posture, receipt-authz | BIO-LOCAL-004 |
| L6 | Cognition → envelope → effect path | Stack Review Board + deliberation translator in separate assemblies; envelope for downstream handling, never direct user text | deliberation content | medium — self-execution / confused-deputy analogue | BIO-LOCAL-004 |
| L7 | Product contract → FeatureGate → UI/API | `contracts/product-contract.v1.json` v1.0.0 → `FeatureGate.cs` + generated mirrors (`sync-product-contract.mjs --check`) | tier/entitlement mapping | medium — paywall honesty, grace-zero, alias correctness | BIO-LOCAL-006 |
| L8 | Guidance contract → gates → rendered copy | Guidance Content Contract v1 Classes A–D + `RATIFICATION.md` (B3/B4/B5, severity-null correction) → `DoctrineSanitizer`/`PolicyGate`/`UserFacingIntelligenceGate`/`HighRiskCategoryGate` | evidence/comparison/harm-reduction copy | high — Class D prohibition, math-only calculators | BIO-LOCAL-005 |

Surfaces L2, L3, L5, L8 are flagged `security_relevance: high` → Phase A-5 review in-parcel (adversarial reviewer with hostile-input probing), not a separate hosted audit. No staging/production surface is evaluated in this goal.
