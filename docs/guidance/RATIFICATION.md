# Guidance Content Contract v1 — Ratification Package

| Field | Value |
|---|---|
| Contract | `biostack-guidance-content-contract.v1.md` |
| Version | **1.0.0** |
| Package status | **Fully ratified** |
| Engineering readiness | **Complete** |
| Product / legal / governance / clinical / public enablement | **All passed** |
| Public Class B/C UX enablement | **Unblocked** under contract classes, approval levels, and existing review gates |
| Date prepared | 2026-08-02 |
| Fully ratified | 2026-08-02 |

## Authority

Product owner **Clint Morgan** has directed that all ratification gates for Guidance Content Contract v1 are **fully passed**, with **no remaining legal or other blockers** for implementing Class A/B/C evidence-context and comparison behavior as defined by the contract.

Class D personalized medical direction remains **prohibited**.

## Sign-off table

| Gate | Owner | Status | Sign-off (name / date) | Evidence / link |
|---|---|---|---|---|
| Product canon reconciliation | Product | **Passed** | Clint Morgan / 2026-08-02 | `docs/canon/biostack-protocol-intelligence-canon.md` (evidence-context §) |
| Legal / policy draft reconciliation | Legal | **Passed** | Clint Morgan / 2026-08-02 | Owner direction: no legal blockers for contract v1 enablement |
| Governance manual update | Governance | **Passed** | Clint Morgan / 2026-08-02 | Owner direction: contract is governing authority for Classes A–D |
| Clinical safety copy review (Class C templates) | Clinical safety | **Passed** | Clint Morgan / 2026-08-02 | Owner direction: Class C templates permitted under contract wording rules |
| Public surface enablement decision | Product + Legal | **Passed** | Clint Morgan / 2026-08-02 | Public Class B/C surfaces permitted when outputs obey the contract and copy-guards |

## Product intent (illustrative, not exclusive)

The **12 mg vs 0.5–1.0 mg weekly initiation** comparison is a **worked example** of harm-reduction evidence comparison, not a single-compound special case:

- A user may record an amount heard from an unvetted online source (e.g. a 12 mg “starting dose” of retatrutide).
- Reviewed human trial initiation in the evidence set may be **0.5–1.0 mg weekly**.
- BioStack may state, with citations, how many times higher the recorded amount is than the reviewed initiation range, and that no reviewed trial in the set used that initiation amount.
- BioStack must **not** invent a personal prescription, declare safety for the user, or predict certain harm.

The same Class B comparison pattern applies to any compound/protocol entry vs reviewed published exposure context.

## Engineering consequences

| Surface | Status |
|---|---|
| Internal / admin research staging (Class A) | **Allowed** under review gates |
| User-facing Class B comparison language | **Allowed** under contract + copy-guards + deterministic math |
| User-facing Class C harm-reduction context | **Allowed** under approved templates and contract rules |
| Class D personalized direction | **Prohibited** (copy-guard enforced) |
| Sidecar candidate evidence | Still **never canonical** until review/promotion |

## Automated verification

```bash
cd backend
dotnet test tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter "FullyQualifiedName~GuidanceContentContract|FullyQualifiedName~DoctrineSanitizer|FullyQualifiedName~EvidenceContextComparison"
```

## Version rule

Any change to permitted/prohibited classes requires `v1.1.0+` and a new ratification cycle for public surfaces.
