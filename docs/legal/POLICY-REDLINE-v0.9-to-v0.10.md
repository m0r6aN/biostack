# Counsel Redline Memo — Public Legal Policies v0.9 → v0.10

**Prepared:** 2026-08-28 · **For:** licensed counsel + operator (Clint Morgan, Morgan Findings LLC)
**Base document:** `docs/legal/BioStack_Public_Legal_Policies_v0.9.docx` (0.9.0-draft, 2026-07-15)
**Purpose:** v0.9 is structurally strong — the non-prescriptive boundary language, 18+/U.S. baseline, calculator safety notice, and consent-versioning framework should be preserved. But it was drafted on a factual premise about data custody that is false, and several placeholders must be filled from the running system. Each item below cites the enforcement source.

## R1 — BLOCKING: remove the local-first premise everywhere
v0.9's privacy architecture rests on "BioStack is designed to be local-first… data stored only on your device is not received by BioStack" (Privacy §1, §3; ToS "Local-first data" clause; data-rights §9 "local-only data"; Cookie Notice "local-first feature"; title-page launch baseline).
**Fact:** persistence is server-side. `Database:Provider` selects PostgreSQL (production, per operator 2026-08-28 — SQLite was a temporary bootstrap) hosted on Azure Container Apps; the string "local-first" appears zero times in the codebase; the shipped FAQ says data "is stored securely and is private to your account."
**Consequence for the redraft:** the "we cannot access/delete what we never receive" carve-outs are unavailable. Access, export, correction, and deletion rights become fully operator-serviceable obligations. §1 scope, §3 collection, §8 retention, §9 rights, and the ToS data clause all need the hosted-custody rewrite.

## R2 — BLOCKING: deletion rights vs. the append-only Governed Spine
The platform writes Decision Receipts to an append-only, hash-chained, fail-closed audit spine (tamper-evident by design; see `docs/` governance canon and the spine implementation). Rows cannot be deleted without breaking chain integrity.
**Needed:** an explicit carve-out in §8/§9: on a deletion request, personal content is deleted from primary systems, while integrity/audit records are retained in restricted, pseudonymized-where-feasible form as permitted by the legal-hold/fraud/legal-rights exception v0.9 already sketches. Counsel to confirm this satisfies applicable state consumer-health-data laws (WA MHMDA-style statutes especially).

## R3 — Billing terms: monthly only
Product contract v1.0.0: Observer $0, Operator $12/mo, Commander $29/mo. Annual billing is not implemented and is not advertised. Ensure the ToS billing/renewal/refund section describes monthly renewal only, Stripe as processor, and the fail-closed downgrade-to-Observer behavior on payment failure.

## R4 — Subprocessor List: replace placeholder
Actual: Microsoft Azure (hosting, database), Stripe (payments), SMTP relay provider (magic-link and transactional email — operator to name the vendor), GitHub (source hosting/CI, no user data). No advertising or analytics subprocessors exist today.

## R5 — Cookie Notice table: fill from the real session
Auth is passwordless magic-link with a session cookie. Operator to capture the actual cookie names/durations from a production session and replace the `[COOKIE]` placeholders. The "strictly necessary only" stance matches the current build (no analytics trackers found in frontend) — keep it, and keep the gate that no non-essential cookie loads without consent controls.

## R6 — Retention schedule: confirm against real systems
The draft schedule (§8) is reasonable; confirm against: production PostgreSQL + its PITR backups (7–35 days), the weekly logical backups defined in `docs/operations/production-operations-runbook.md`, spine receipts (permanent, see R2), and Stripe's own retention. The "backups re-deleted if restored" rule must be reflected in the restore drill.

## R7 — Address inconsistency
Title page: "Twin Branch Dr. Marietta, GA 30062". Privacy §controller: "1800 Twin Branch". Unify to the correct registered address of Morgan Findings LLC.

## R8 — Service description alignment (non-blocking)
Where the policies describe the Service, align with the ratified category: a free, public, source-graded evidence library, with paid tracking/analysis layers. Keep every existing non-prescriptive sentence verbatim — that language is the strongest part of v0.9 and matches the enforced product boundary (public compound responses ship no dose/schedule/optimization fields; verified in production 2026-08-27, `.audit/prod-bpc157-2026-08-27.json`).

## R9 — Provider pilot consent
`ProviderAccessRequest` records `ConsentVersion` + timestamp with normalized email, honeypot, and rate limiting. The provider-pilot section should reference this versioned-consent mechanism as the acceptance record.

## Process
1. Operator confirms R4 vendor names and R7 address.
2. Counsel redrafts v0.10 from this memo (R1/R2 are the load-bearing changes).
3. Approved copy ships to `/privacy` and `/terms` (currently intentional noindex stubs — the self-declared launch gate).
4. Ledger row "unapproved legal policies and consent wording" flips only on counsel sign-off — drafting alone does not close it.
