# Track D — Independent QA and Security Evidence

**Date:** 2026-07-28  
**Scope:** KEO-84 / KEO-85 independent assessment lane  
**Mode:** assessment-only; no product-code changes; no GO decision

## Bottom line

**Current verdict: HOLD / NO-GO.** Deterministic local controls and a limited public browser smoke pass. Authenticated production journeys, Stripe lifecycle behavior, webhook replay/failure, live tenant isolation, full accessibility acceptance, backup/restore, monitoring, and rollback remain unverified or blocked. This lane does not close KEO-84 or KEO-85.

## Starting state

- KEO-84 is **In Progress**, blocked by KEO-68, KEO-69, and KEO-64.
- KEO-85 is **Todo**, blocked by KEO-193, KEO-84, KEO-83, KEO-82, KEO-81, KEO-69, KEO-80, KEO-68, KEO-70, KEO-72, KEO-64, and KEO-65.
- BioStack repository: `main`, local HEAD `7b10831608fe1136c6b114523fb8558fc452d381`.
- Pre-existing dirty state was preserved: deleted `package-lock.json` and untracked `.codex-remote-attachments/`. No product source, deployment config, or existing readiness ledger was changed.
- The canonical launch ledger remains **NO-GO** and records missing live billing/auth/operations/accessibility/rollback evidence plus known dependency findings.

## Environment evidence

| Environment | Evidence | Result | Release meaning |
|---|---|---|---|
| Local frontend | Vitest, 8 focused files with `--pool=threads --maxWorkers=1` | **40 passed** | Deterministic UI seams pass locally only |
| Local backend application | `BioStack.Application.Tests` | **536 passed, 5 skipped** | Service/unit controls pass locally only |
| Local backend API | Auth config 6, billing tier 3, consent 17, ownership 1, auth endpoints 18 | **45 passed** | Selected API controls pass locally only |
| Local backend full API project | Full `BioStack.Api.Tests` run | **Timed out after 244s** | Full API qualification is unverified |
| Public `https://biostack.cc` | Browser at 375×812, 768×1024, 1440×900 | **No horizontal overflow; no unlabeled controls observed** | Limited browser smoke only; not WCAG acceptance |
| Public `https://biostack.cc` | `/`, `/start`, `/pricing`, `/providers`, `/terms`, `/privacy`, `/tools/analyzer` | **HTTP 200** | Public route availability observed |
| Public no-cookie path | `/billing?plan=operator`, `/profiles`, `/checkins`, `/timeline`, `/admin`, `/protocol-console`, `/mission-control` | **Redirects to sign-in with callback path** | Unauthenticated boundary observed; authenticated path unverified |
| Public no-cookie API | `/api/v1/auth/session` | `200 {"authenticated":false,"user":null}` | Session absence observed; delivery/expiry/replay unverified |
| Public SEO surface | `/robots.txt`, `/sitemap.xml` | **HTTP 200; current `biostack.cc` host** | Current host is correct in this observation; deploy revision was not pinned |

The public analyzer browser pass recorded zero console errors and zero unlabeled controls at the three viewport sizes. A transient aborted session request appeared during navigation, followed by a successful `200` session response; no browser console error was recorded.

## Qualification matrix

| ID | Scenario | Environment | Evidence | Status | Blocking condition |
|---|---|---|---|---|---|
| D-01 | Anonymous user cannot access paid analyzer/billing/protected routes | Public | Browser/curl redirects to sign-in with relative callback paths | **Observed pass** | Does not prove authenticated authorization or entitlement correctness |
| D-02 | Magic-link delivery, expiry, replay, newest-link-wins, callback, logout | Production/staging | No test identity or delivery evidence available | **Blocked** | Run the KEO-69 authorized browser scenarios against one pinned deployment |
| D-03 | Consent accept/refuse/current-version gate | Local + production | Local ConsentPage, consent-gate, and auth tests pass; legal/version approval and deployed run absent | **Partial / blocked** | Attach approved text/version and deployed accept/refuse evidence |
| D-04 | Paid entitlement is granted only after verified billing state | Local + Stripe test/live-authorized | Local billing-tier and frontend billing tests pass; no authorized transaction | **Partial / blocked** | Prove checkout, verified webhook, portal, renewal, cancellation, downgrade, refund |
| D-05 | Replayed, invalid-signature, oversized, unknown-price webhook | Stripe test | No replay fixture or deployed receipt/state evidence run in this lane | **Blocked** | Execute KEO-68 replay/quarantine matrix with redacted event IDs and persisted-state proof |
| D-06 | Failed payment removes paid access without deleting retained data | Stripe test/live-authorized | No payment-failure event or post-failure entitlement proof | **Blocked** | Produce `invoice.payment_failed` and recovery evidence on the candidate |
| D-07 | Cross-user/tenant profile and protocol access is denied | Local + staging | Ownership isolation API test passes locally | **Partial / blocked** | Repeat against deployed staging/production identities and tenant boundaries |
| D-08 | 375px/tablet/desktop critical funnel has no overflow and usable controls | Public | Analyzer smoke: 375, 768, 1440; no overflow/unlabeled controls | **Partial** | Run keyboard/focus/dialog, screen-reader, contrast, zoom, reduced-motion and critical-funnel tests |
| D-09 | Public claims, consent, Terms, Privacy, analytics, support | Public + approval records | Routes serve; no dated legal approval, analytics payload review, or support/SLA proof in this lane | **Blocked** | Attach exact approvals, privacy-safe analytics evidence, and staffed support contract |
| D-10 | Backup/restore, health/readiness, monitoring/alerts, rollback | Staging/production | Existing ledger marks these failed or blocked; no new drill run | **Blocked** | Run timed restore and rollback rehearsal with named RPO/RTO, probes, alerts, and owner |

## Security and release interpretation

Passing local tests are necessary evidence, not release evidence. A merged PR, a `200` homepage, or an unauthenticated redirect does not establish production readiness. No secrets, PII, raw customer evidence, payment details, or unsafe payloads were collected or written here.

### CONDITIONAL GO conditions

A release owner may consider **CONDITIONAL GO** only after all of the following are true for a named target environment and one exact commit/configuration set:

1. KEO-84 critical browser and accessibility scenarios pass, including keyboard/focus, responsive critical funnels, contrast/zoom/reduced motion, console/network review, and privacy-safe analytics.
2. KEO-69 authenticated magic-link, consent accept/refuse, expiry/replay, callback, logout, and cross-user denial scenarios pass with dedicated test identities.
3. KEO-68 checkout, verified webhook, replay/idempotency, unknown-price quarantine, portal, renewal, failed-payment downgrade/recovery, cancel/expiry, and refund/cancellation scenarios pass.
4. Tenant/ownership isolation, database readiness, monitoring/alerts, backup restore, and rollback are evidenced in the target environment.
5. No unresolved Critical/High security findings remain; any Medium waiver names the owner, rationale, date, and reassessment date.
6. Exact legal/consent approvals, support ownership/SLA, deployment protection, and claims-to-evidence mapping are attached.
7. The release owner records explicit constraints, residual risks, rollback owner, and the CONDITIONAL GO decision.

### HOLD conditions

The release remains **HOLD** if any required scenario is unknown, blocked, or failed; if authenticated billing/auth paths have not been run; if rollback/restore evidence is absent; if tenant isolation is only local; if public claims lack evidence; or if a security finding is not resolved or formally waived. The current evidence meets multiple HOLD conditions.

## Session handoff

**Files changed:**

- Added this evidence file only.
- Generated browser snapshots remain under the Playwright session directory; no application or test source was modified.

**Commands/tests run:**

- Frontend Vitest focused matrix with single worker: 8 files, 40 passed.
- Backend Application.Tests: 536 passed, 5 skipped.
- Backend API focused classes: 45 passed across auth configuration, auth endpoints, consent, billing tier, and ownership isolation.
- Backend full API test project: timed out after 244 seconds; no final pass claim.
- Playwright CLI public browser smoke at 375×812, 768×1024, and 1440×900; route snapshots, console inspection, redirects, and no-overflow/control-label checks.
- Read-only public HTTP checks for auth session, protected routes, robots, sitemap, policy routes, and billing entry.

**Blockers:** live identity/email delivery, approved consent/legal artifacts, Stripe test/live authorization and lifecycle evidence, staging tenant proof, full accessibility report, full API completion, dependency/security closure, monitoring/alerts, backup/restore, and rollback rehearsal.

**Decisions needed:** release owner must name the target qualification environment, exact candidate SHA/config, authorized Stripe account/test-clock procedure, test identities, legal/consent artifact versions, RPO/RTO, rollback owner, and support/on-call owner.

**Next safe action:** coordinator dispatches bounded remediation/evidence owners for the blocked KEO-68/69/80/81/82/83/84/85 gates, then reruns only the affected scenarios in the named target environment. Track D remains independent and does not mark GO.
