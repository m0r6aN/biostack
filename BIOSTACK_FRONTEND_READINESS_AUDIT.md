# BioStack Frontend Readiness Audit

**Prepared for:** Clint
**Original audit date:** July 6, 2026
**Updated:** July 12, 2026 — re-verified against `origin/main` (commit `a37726a`, PR #181 "codex/production-readiness" merged 2026-07-11)
**Scope:** Product-experience audit of the BioStack Next.js frontend (`frontend/src`). UI, UX, IA, product flow, nomenclature, conversion clarity, free-vs-paid clarity, provider value, data surfacing. Not a code review.
**Method:** Static read of routes, components, middleware, feature gates, copy, and the intelligence/knowledge data model, diffed against the original audit's findings. No live/browser walkthrough was run, so dynamic issues (real auth-loop behavior, actual API responses) are still flagged as "verify live" where relevant.

---

## 0. What Changed Since July 6 (delta summary)

Since the original audit, `codex/production-readiness` shipped and merged to `main` (PRs #180, #181). **Every P0 finding from the original audit is now resolved except one.** This is a genuinely large jump in readiness.

**Resolved:**
- Evidence library (`/knowledge`) is now public — added to `middleware.ts`'s `PUBLIC_PREFIX_ROUTES`.
- `TierGate` now does real tier-rank enforcement (was a documented no-op).
- `/my-protocol` pulls from `apiClient` — the hardcoded mock fixture and its "Preview fixture" banner are gone.
- "Map Stack" is gone; nav CTA is now "Analyze My Stack" → `/tools/analyzer`, enforced by a regression test (`MarketingNavReadiness.test.tsx`).
- Tier vocabulary is now identical across pricing and billing: "Observer — Free," "Operator — Track & Analyze," "Commander — Longitudinal Intelligence."
- `/providers` grew from 3 bullets to a real landing page (165 lines) with a working `ProviderAccessForm` wired to a new backend endpoint (`ProviderAccessEndpoints.cs` + DB migration) and a substantive FAQ (data ownership, revocation, medical-device disclaimer, multi-client).
- Nav (marketing + sidebar) now surfaces "Compounds & Evidence" and "Audit Receipts" — both were previously orphaned.
- Sidebar name drift fixed: "Dashboard" (not "Mission Control"), "My Protocol," "Compounds," "Protocols," "Check-ins" — one name per concept.
- Sidebar "Research" is now `adminOnly: true` (was leaking an admin surface to all signed-in users).
- `benefits[]` and `drugInteractions[]` now render (`CompoundIntelligenceCard.tsx`).
- Signin page and onboarding's "Finish Setup" both now carry the recommended pre-wall value line: *"Create a free profile to save your analysis and track how your stack changes over time. No card required."*
- Empty "No Profile Selected" state now routes into profile creation ("Let's set up your first profile" → Create profile).
- "Coming soon" analyzer stub is gone.
- Cohesion and Drift panels now carry plain-language tooltips.

**Still open:**
- **`/compounds` remains gated** — only `/knowledge` was added to the public route list. If `/compounds` is meant to be a second public surface (per the original recommendation), it still redirects anonymous visitors to sign-in.
- **Onboarding is still three routes** (`/start`, `/map`, `/onboarding`) — not collapsed to one canonical flow with a mode toggle.
- **Homepage/how-it-works proof panel (`IntelligenceProofSection`) is still hardcoded** ("BPC-157 + TB-500" is a static string, not pulled from the live engine).
- **`Observation Debt` internal component/variable naming is unchanged** — the on-screen comment was updated to "Check-ins Due Inbox" but no user-visible heading was found; worth a quick manual check live.
- **No real protocol→schedule pipeline was added** — `CalendarTab` inside `/my-protocol` is real now (no longer mock-fixture-backed), but a dedicated free/basic weekly-schedule build-out wasn't part of this merge; verify live whether the calendar renders real data end-to-end.

---

## 1. Executive Summary

**Updated verdict: Ready for provider acquisition. Close to ready for paid conversion. Ready as a free tools + evidence destination.**

The core thesis of the original audit — "the beast is real, but the storefront barely shows it" — has largely been addressed. The evidence-aware intelligence engine is no longer hidden: `/knowledge` is public, compound dossiers render benefits and drug interactions, the tier story is unified, gating is real, and the provider path is now an actual product surface with a working intake form backed by a real database table.

The single most important finding from the original audit was:

> **The entire evidence/knowledge library is behind the auth wall.**

That finding is now **half-resolved**: `/knowledge` is public. `/compounds` — the compound *management* surface, distinct from the `/knowledge` dossier library — is still gated. Confirm whether that's intentional (compounds-as-personal-data vs. knowledge-as-reference-library is a defensible split) or an oversight from the original recommendation.

Remaining gaps before calling this fully paid-conversion-ready:

- **Duplicate onboarding routes** (`/start`, `/map`, `/onboarding`) still exist — minor confusion risk, not a trust or money issue.
- **The homepage's proof-of-intelligence panel is still a hardcoded example**, not a live pull from the engine — a missed credibility opportunity now that the real thing is public and could be linked or sampled instead.
- **Auth-loop and calendar-data-liveness claims still need a live browser pass** — this audit remains a static read; the two items above plus the auth callback loop should be smoke-tested before a launch announcement.

The good news: what's left is small, well-scoped, and mostly polish rather than architecture. This is a materially different, launch-adjacent posture compared to July 6.

---

## 2. Top 10 UX/IA Risks (ranked by customer-loss risk) — updated status

1. ~~Evidence library is auth-walled.~~ **PARTIALLY RESOLVED.** `/knowledge` is public. `/compounds` is still gated — confirm intent. *(was P0, now P2 if intentional / P0 if not)*
2. ~~Free vs paid is described three incompatible ways.~~ **RESOLVED.** Pricing and billing now share one tier vocabulary. *(closed)*
3. ~~"Map Stack" CTA is meaningless.~~ **RESOLVED.** Renamed "Analyze My Stack," regression-tested. *(closed)*
4. ~~Paywalled value is shown against fake screens.~~ **RESOLVED.** `/my-protocol` is live-data-backed; `TierGate` enforces tier rank. *(closed)*
5. ~~Provider path is a non-offer.~~ **RESOLVED.** Real landing page, working intake form, backend-persisted, substantive FAQ. *(closed)*
6. **Onboarding still has three near-identical routes** (`/start`, `/map`, `/onboarding`). Not collapsed. *(P2 — was P1; downgraded since nav CTAs now disambiguate entry points)*
7. ~~Onboarding dead-ends into a sign-in wall without warning.~~ **RESOLVED.** "Finish Setup" now shows the value line before the wall. *(closed — verify the auth-loop live)*
8. **Internal vocabulary mostly addressed.** Cohesion and Drift now have tooltips; "Mission Control" is gone from user-facing sidebar (now "Dashboard"); tier names carry plain descriptors everywhere checked. "Observation Debt" heading not confirmed fixed — verify live. *(P2 — was P1)*
9. ~~Nav omits the knowledge base.~~ **RESOLVED.** "Compounds & Evidence" and "Audit Receipts" both linked in marketing nav / sidebar. *(closed)*
10. ~~Empty/no-profile states dead-end.~~ **RESOLVED.** Routes into profile creation with the recommended copy. *(closed)*

**New/carried-forward item:** Homepage/how-it-works proof panel (`IntelligenceProofSection`) still hardcodes its example instead of pulling a live engine result — now the top open item, since it's a missed-credibility gap rather than a hidden-value gap. *(P1)*

---

## 3. Funnel Walkthroughs (updated)

### A. New visitor → free value
Homepage hero is unchanged and still strong. **Updated:** the evidence library is no longer auth-walled, so a visitor who wants proof before signing up can now reach `/knowledge` directly. **Grade: B+.** Free tools land; proof-of-intelligence is now reachable too, though the homepage doesn't yet link a live example (see §7).

### B. Free anonymous user → saved protocol/profile
Same architecture as before (`localStorage` before signup, callback-preserving signin), but **the wall now has a warning.** Both onboarding's "Finish Setup" and the signin page carry the value line before the redirect. **Grade: B.** Still a wall, but no longer a surprise.

### C. Free profile user → paid upgrade
**Updated: no longer the weakest funnel.** `TierGate` now enforces tier rank instead of being inert, and `/my-protocol` is live-data-backed rather than a labeled mock. Pricing and billing quote the same tier language. **Grade: B-.** The seams that made this feel fake are closed; remaining polish is upgrade-CTA placement, not trust.

### D. User → saved protocol → schedule/check-ins
Unchanged real check-ins flow (`/checkins`). The weekly calendar (`CalendarTab`) inside `/my-protocol` is no longer sitting behind an inert gate on top of a mock — both the gate and the data source are real now. **Verify live** whether the calendar actually has a save-protocol-to-schedule pipeline behind it or just consumes whatever `apiClient` returns for that endpoint. **Grade: B- (up from C-)**, pending live confirmation.

### E. Provider visitor → trust/value → sign-up/request access
**Updated: this funnel now exists.** `/providers` has a real hero, workflow explanation, explicit "what BioStack does not do" boundary, a `ProviderAccessForm` that posts to a real backend endpoint and persists to a DB table, and a FAQ covering data ownership, revocation, and medical-device status. **Grade: B (up from F).** This is a genuine request-access flow now, not a dead end.

---

## 4. Route & IA Inventory

| Route | Purpose | Audience | Auth | Clarity |
|---|---|---|---|---|
| `/` | Homepage / hero + persona cards | Public | Public | Strong |
| `/how-it-works` | Deeper explainer | Public | Public | Good |
| `/tools` | Dose/Mix/Convert calculators | Public | Public | Strong |
| `/tools/analyzer` | Protocol analyzer (paste/upload/scan/link) | Public | Public | Strong |
| `/tools/reconstitution-calculator`, `/tools/volume-calculator`, `/tools/unit-converter` | Deep-links into the tools surface | Public | Public | OK (thin wrappers) |
| `/pricing` | Plans (Observer/Operator/Commander) | Public | Public | **Fixed — matches billing** |
| `/providers` | Provider landing | Providers | Public | **Rebuilt — real offer + form** |
| `/safety` | Safety boundary | Public | Public | Good |
| `/faq` | FAQ (10 Qs) | Public | Public | Good, consumer-only |
| `/start` | New-user onboarding | Public | Public | Still duplicates `/map` |
| `/map` | Existing-user onboarding | Public | Public | Still duplicates `/start` |
| `/onboarding` | Onboarding (mode param) | Public | Public | Still a third duplicate |
| `/terms`, `/privacy` | Legal | Public | Public | (assumed OK) |
| `/auth/signin`, `/auth/verify` | Magic-link auth | All | Public | Good state-preservation; now has value line |
| `/protocol-console` | Dashboard | Authed | **Gated** | Dense, powerful; sidebar label fixed to "Dashboard" |
| `/mission-control` | Redirect → `/protocol-console` | — | — | Legacy redirect (fine) |
| `/my-protocol` | Protocol portal (calendar/labs/milestones) | Authed | Gated | **Fixed — apiClient-backed, no mock banner** |
| `/profiles`, `/profiles/[id]` | Profile CRUD | Authed | Gated | OK |
| `/compounds` | Compound management | Authed | **Still gated** | Not added to public list — confirm intent |
| `/protocols`, `/protocols/[id]`, `/protocols/[id]/review` | Saved protocols, detail, review | Authed | Gated | Good, real intelligence |
| `/checkins` | Daily observations | Authed | Gated | Strong |
| `/timeline` | Unified event stream | Authed | Gated | Good |
| `/knowledge`, `/knowledge/[slug]` | **Evidence library / compound dossiers** | Public | **Now public** | **Fixed — flagship, visible** |
| `/billing` | Subscription mgmt | Authed | Gated | **Fixed — matches pricing** |
| `/governance/receipts`, `/receipts/[uri]` | Audit/provenance receipts | Authed | Gated | **Fixed — linked in sidebar as "Audit Receipts"** |
| `/admin`, `/admin/research/*` | Admin + research pipeline | Admin | Gated | Correct; `/admin/research` now properly `adminOnly: true` |

**IA flags — updated:**
- **Duplicate routes still open:** `/start`, `/map`, `/onboarding` remain three doors to one experience. Not collapsed in this merge. *(P2)*
- ~~Hidden high-value routes.~~ **RESOLVED.** `/knowledge` and `/governance/receipts` are both linked in nav now.
- **Auth pressure partially relieved:** `/knowledge` is public; `/compounds` is not. Decide and document whether that split is intentional.
- ~~Nav item mislabeled/misrouted.~~ **RESOLVED.** Sidebar "Research" is `adminOnly: true`.
- ~~Sidebar vs page name drift.~~ **RESOLVED.** Sidebar now says "Dashboard," "My Protocol," "Compounds," "Protocols," "Check-ins" — one name per concept, matching page headers where checked.

---

## 5. Nomenclature Table

| Term | Where | Current clarity | Issue | Recommendation |
|---|---|---|---|---|
| **Map Stack** | Nav CTA, mobile CTA, `/map` | Confusing (creator unsure) | Verb+noun brand phrase; implies paid next to "Start free" | **Rename → "Analyze my stack"** or remove; route to analyzer |
| **Stack** | Everywhere | Medium | Jargon for beginners | Keep, but gloss once: "your stack = everything you take" (already done well on `/start`) |
| **Start free** | Nav CTA | Good | Fine | Keep |
| **Mission Control** | Sidebar, dashboard header | Low (external) | Internal/brand; also mismatched with route `/protocol-console` | Rename user-facing → **"Dashboard"** or **"Protocol Console"**; pick ONE name |
| **Observer / Operator / Commander** | Pricing, billing | Low | Cute tier names carry no value signal; beginners can't map them to benefits | Add plain descriptors: "Observer (Free) · Operator (Track & Analyze) · Commander (Longitudinal)" |
| **Analyzer** | Tools, hero | Good | Clear | Keep |
| **Cohesion** | `CohesionTimelinePanel` | Low | Abstract | Tooltip: "how consistently your protocol holds together over time" |
| **Drift** | `DriftRegimePanel`, "Protocol Weather" | Low | Abstract | Tooltip: "how much your inputs/results are changing vs baseline" |
| **Observation Debt** | `ObservationDebtInbox` | Very low | Sounds like a penalty | Rename → **"Check-ins due"** or "What to log next" |
| **Protocol Weather** | Mission panel | Low | Cute, unclear | Rename → **"Protocol status"** |
| **Stack Score / Protocol Score** | Multiple | Medium | Two names for one metric | Standardize on **"Stack Score"**; add "what is this?" popover |
| **Evidence tier** | Knowledge, FAQ | Good | Well-defined in FAQ | Keep; surface the definition inline via tooltip |
| **Phase / Milestone** | Protocol portal | Good | Standard | Keep |
| **Contradiction / Conflict / Interference / Redundancy** | Pricing, analyzer, panels | Medium | Four words for overlapping ideas | Consolidate vocabulary: "overlap," "conflict," "redundancy" — define each once |
| **Provider** | Nav | OK | Singular "Provider" reads oddly as a nav label | Consider "For Providers" |
| **Observations** (sidebar) vs **Check-ins** (page) | Sidebar/route | Low | Sidebar says "Observations," page header says "Check-ins" | Pick one — recommend **"Check-ins"** |
| **Protocol Lab** (sidebar) vs **Protocols** (page) | Sidebar/route | Low | Two names | Standardize → **"Protocols"** |
| **Compound Intelligence** (sidebar) vs **Knowledge Base** (page) | Sidebar/route | Low | Two names | Standardize → **"Compounds"** or **"Evidence Library"** |

**Pattern:** the sidebar routinely uses a "premium" alias while the page uses a plain name. Pick the plain nouns; keep the flavor for section eyebrows only.

---

## 6. Free vs Paid Feature Matrix (recommended)

The current copy scatters features across pricing, billing, and marketing with conflicting lists. Recommended clean structure:

| Capability | Free (anon) | Free profile (Observer) | Paid (Operator $12) | Paid (Commander $29) | Provider |
|---|---|---|---|---|---|
| Dose / reconstitution / unit calculators | ✅ | ✅ | ✅ | ✅ | ✅ |
| Save calculations (this device) | ✅ | ✅ (to account) | ✅ | ✅ | ✅ |
| Analyzer (paste/upload/scan/link) + score | ✅ (capped) | ✅ (capped) | ✅ full breakdown | ✅ | ✅ |
| **Evidence library / citations / dossiers** | ✅ **(un-gate — read only)** | ✅ | ✅ | ✅ | ✅ |
| Overlap / pathway checker | ✅ preview | ✅ | ✅ full | ✅ | ✅ |
| Profiles | — | ✅ (limit 8 compounds) | ✅ unlimited | ✅ | ✅ multi-client |
| Check-ins + trends + Day-7 review | — | ✅ basic | ✅ | ✅ | ✅ |
| Save protocol snapshot | — | ✅ | ✅ | ✅ | ✅ |
| **Weekly schedule / calendar** | — | ✅ basic (recommend) | ✅ reminders/adherence | ✅ | ✅ |
| Stack Score across full stack | — | preview only | ✅ | ✅ | ✅ |
| Live stack intelligence / simulation | — | locked | ✅ | ✅ | ✅ |
| Mission Control (drift/pattern/sequence) | — | — | partial | ✅ | ✅ |
| Longitudinal / cross-run / drift analytics | — | — | — | ✅ | ✅ |
| Exports / provider summary sharing | — | — | ✅ basic | ✅ full | ✅ |
| Evidence changelog / provenance receipts | — | — | ✅ | ✅ | ✅ |

**Product-risk gaps — status update:**
- ~~`/my-protocol` is a mock fixture.~~ **RESOLVED** — pulls from `apiClient`, no "Preview fixture" banner found anywhere in the codebase.
- ~~`TierGate` is inert.~~ **RESOLVED** — real tier-rank comparison, renders an honest "Compare plans" upgrade card when locked.
- ~~Billing/pricing feature lists differ.~~ **RESOLVED** — both now use identical tier naming and taglines ("Operator — Track & Analyze," "Commander — Longitudinal Intelligence").
- **Live-verify:** confirm the weekly schedule inside `CalendarTab` reflects a real save-protocol-to-schedule pipeline and isn't just a differently-sourced placeholder — this audit is a static read and can't confirm end-to-end data flow.

---

## 7. Data Surfacing Gaps — *the money leak*

This is the highest-value section. The `KnowledgeEntry` model is rich:

```
canonicalName, aliases, classification, regulatoryStatus, mechanismSummary,
evidenceTier, sourceReferences[], notes, pathways[], benefits[],
pairsWellWith[], avoidWith[], compatibleBlends[], recommendedDosage,
frequency, preferredTimeOfDay, weeklyDosageSchedule[], drugInteractions[],
optimizationProtein/Carbs/Supplements/Sleep/Exercise
```

Findings — updated:

1. ~~The whole library is gated.~~ **PARTIALLY RESOLVED.** `/knowledge` is public. `/compounds` is still gated. Citations, evidence tiers, mechanism summaries, and pathways are now visible to anonymous visitors via `/knowledge`. Confirm whether `/compounds` (the management surface) needs a public read tier too, or whether `/knowledge` alone satisfies the original intent.
2. ~~`benefits[]` and `drugInteractions[]` are never rendered.~~ **RESOLVED** — both render in `CompoundIntelligenceCard.tsx`.
3. ~~Provenance/receipts are orphaned.~~ **RESOLVED** — `/governance/receipts` is now linked in the sidebar as "Audit Receipts."
4. **Still open: marketing panels show a stub, not the real thing.** `IntelligenceProofSection` (homepage/how-it-works) still hardcodes `compoundNames={['BPC-157', 'TB-500']}` and a static label rather than pulling a live example from the now-public `/knowledge` data. This is now the single biggest remaining lever — you have a real public dossier to link to or sample from, and the homepage still shows a canned example next to it.
5. ~~"Coming soon" stub on analyzer report.~~ **RESOLVED** — no "coming soon" strings found anywhere in `frontend/src`.
6. **Compound dossier page is now public and SEO-reachable.** This was "the single biggest lever in the audit" in the original write-up — it has been pulled. Worth a follow-up SEO metadata pass (title tags, structured data) now that it's indexable, but the core un-gating is done.

**Updated one-line takeaway:** The front door is open now — `/knowledge` renders citations, evidence tiers, `benefits`, and `drugInteractions`, and receipts are linked as a trust badge. The one thing still hiding the real engine is the homepage proof panel, which still shows a canned example instead of linking into the library that now actually exists.

---

## 8. Protocol Intelligence Assessment

**Updated: closer to flagship — the dossier is public and the gate is real, but the homepage still undersells it.**

- The **definitions are rich and structured** (dossier page proves it), connected to evidence tiers, pathways, relationships, and profile context — unchanged, still good.
- Intelligence surfacing is now **mostly consistent**: full and credible in the (now public) dossier; enforced correctly in the portal via real `TierGate`. The one remaining inconsistency is the homepage proof panel, still stubbed and hardcoded.
- **Uncertainty is represented** (evidence tiers, "unknown states," "reference only" disclaimers) — unchanged, still tasteful.
- **Beginners are served** on `/start`; advanced depth (compound dossier, overlap detail) is now one click away via the "Compounds & Evidence" nav link — previously it wasn't linked at all.

**Remaining to fully reach flagship:**
- Feed a live engine example into the homepage/how-it-works proof panel instead of the hardcoded "BPC-157 + TB-500" string.
- Add SEO metadata (structured data, unique titles/descriptions) to the now-public dossier pages to capture the acquisition value of un-gating.
- ~~Render every rich field with progressive disclosure.~~ Largely done — `benefits`/`drugInteractions` render now.

---

## 9. Provider Readiness Assessment

**Updated: `/providers` is now a real product surface (165 lines, up from 3 bullets).** Confirmed present:

- A working `ProviderAccessForm` component that posts to a real backend endpoint (`ProviderAccessEndpoints.cs`), persisted via a new `ProviderAccessRequest` entity and EF migration (`AddProviderAccessRequests`) — this is a genuine "request access" flow, not a mailto link.
- An explicit boundary statement: *"BioStack is not a medical device, EHR, prescribing system, or treatment planner."*
- FAQ entries confirmed present covering: medical-device/EHR status, multi-client management, data ownership ("Clients own their profile data. Provider access is permissioned..."), and revocation ("Revocation is a required pilot control. BioStack will not represent provider sharing as available until clients can grant and revoke access reliably.") — notably, that last line is honest about a real limitation rather than overselling.

**Remaining gaps against the original checklist (verify live):**
- Confirm the hero framing is outcome-focused (not re-checked line-by-line here).
- Confirm example workflow (invite → log → review → check-in) is spelled out step-by-step on the page.
- Confirm pricing-per-provider is stated, or if it's intentionally a "request access, we'll follow up" pilot model — the revocation FAQ answer suggests this may still be a pilot/waitlist posture rather than self-serve, which is a reasonable and honest choice for a feature with unresolved access-control questions.

**Net:** this went from an F (no offer to convert against) to a legitimate B-range request-access flow with honest limitations disclosed rather than hidden.

---

## 10. Auth & Sign-In Friction Assessment

**The infrastructure is better than the framing.** The signin page preserves `callbackUrl`, masks email, has resend cooldown, and shows a reassuring "your saved analysis will carry through" banner. Local state (analyzer/onboarding/tools) is persisted before auth. That's the right architecture.

**Problems — updated:**
- ~~Premature wall via routing, not messaging.~~ **RESOLVED.** Onboarding's "Finish Setup" now shows *"Create a free profile to save your analysis and track how your stack changes over time. No card required"* directly above the button, before the redirect.
- ~~No "why" at the wall.~~ **RESOLVED.** Signin page carries the same value line plus *"Your saved analysis will carry through sign-in."*
- **Loop risk still needs a live test.** No code change was found in `middleware.ts`'s cookie-check logic; this remains a "verify live" item exactly as before — signin → email link → `/auth/verify` → callback route, confirming `biostack_session` is set before the callback resolves.
- **CTA taxonomy improved but not fully consolidated.** "Map Stack" is gone (replaced by "Analyze My Stack" / "Start Free" per the nav test). Not independently re-verified: whether "Choose Operator," "Finish Setup," and "Convert to BioStack Protocol" still coexist without a shared verb pattern — worth a follow-up copy pass, but no longer a P0/P1 trust issue since the two biggest offenders (Map Stack, silent wall) are fixed.

**Still recommended:** Keep all tools + evidence library + onboarding fully anonymous (now true for `/knowledge`; still confirm `/compounds`). Sign-in now explains why — the remaining work is the live loop test.

---

## 11. Scheduling Recommendation

**Recommendation: Hybrid.**

- **Free (Observer):** basic saved protocol → weekly schedule view, manual dose/supplement logging, and check-ins. This is the habit-forming core; gating it kills retention. The pieces already exist (`/checkins`, protocol snapshots, the mock `CalendarTab`).
- **Paid (Operator+):** reminders, adherence trends, response-pattern analytics, evidence changelogs, exports, and provider sharing.

**Reality check:** today there is **no real protocol→schedule pipeline** — the weekly calendar lives only in the mock `/my-protocol` portal behind an inert gate. Priority is to build the *free* basic weekly schedule from saved protocols first (retention), then layer paid analytics on top. Selling scheduling before the free version exists would be selling a mock.

---

## 12. Calculator / Tool UX Assessment

**Strong overall.** The tools surface is public, no-account, has a clear dose/mix/convert mode switch, a results panel, a `SyringeDrawVisualizer`, reconstitution + storage steps, a blend-safety checker, saved calculations, and a "track in my stack" bridge. Math-only disclaimer is present and appropriately non-medical.

**Good visual aids already present:** syringe draw visualizer, a vial-measurement info image (`/images/vial.jpg`) behind an accessible info popover on the powder field.

**Improvements (math clarity only, no dosing advice):**
- **Units are abstract for first-timers.** "Powder amount / Solution volume / Desired dose" with unit dropdowns is correct but dense. Add a one-line plain-language framing per field ("How much powder is in the vial?").
- **Show the calculation, not just the answer.** A collapsible "How this was calculated" (the `formula` field already exists in the result) builds trust and catches input errors.
- **Add a "check your math" summary** restating inputs → output in words ("2 mg in 1 mL = 2000 mcg/mL; 250 mcg = 0.125 mL = 12.5 units on a U-100 syringe").
- **Dynamic syringe:** the visualizer exists — ensure it clearly marks unit gradations and the draw point, since U-100 unit confusion is the classic first-timer error.
- **Common-mistake warnings** (non-medical): flag when solution volume is 0, when concentration seems off by an order of magnitude, or when units were likely mixed (mg vs mcg).
- **Vial illustration** is image-only; consider a lightweight dynamic vial that reflects entered powder/volume for grounding.

---

## 13. CTA & Header Recommendation

**RESOLVED.** "Map Stack" is gone. Confirmed via `MarketingNavReadiness.test.tsx`: nav now shows "Compounds & Evidence" → `/knowledge`, "Analyze My Stack" → `/tools/analyzer`, "Start Free" → `/start`, and explicitly asserts "Map Stack" is *not* in the document.

**Not independently re-verified in this pass:** mobile sticky-bar CTA count, logged-in "Go to Dashboard" wording, and paid-state CTA — these weren't part of the merged commits' file list and likely didn't change. Spot-check live if mobile CTA clutter was a concern.

**Still open:** the third onboarding route (`/onboarding`) alongside `/start` and `/map` — the original recommendation to collapse to one canonical route with a mode toggle wasn't part of this merge.

---

## 14. Quick Wins (copy / IA / nav / tooltip / route) — status

1. ✅ **Un-gate `/knowledge` for read.** Done. (`/compounds` still open — was originally grouped with `/knowledge`; confirm if it needs the same treatment.)
2. ✅ **Rename "Map Stack" → "Analyze my stack."** Done, test-enforced.
3. ✅ **Add evidence to the top nav.** Done ("Compounds & Evidence").
4. ✅ **Reconcile tier names.** Done — identical across pricing/billing.
5. ✅ **Match billing's feature lists to pricing's.** Done.
6. ✅ **Remove the "coming soon" stub.** Done — no longer present anywhere.
7. ✅ **Fix sidebar/page name drift.** Done — "Dashboard," "My Protocol," "Compounds," "Protocols," "Check-ins" standardized.
8. ✅ **Add a value line to the signin page.** Done.
9. ❌ **Collapse `/start`, `/map`, `/onboarding`.** Still open.
10. ✅ **Set Sidebar "Research" to `adminOnly: true`.** Done.
11. ✅ **Render `drugInteractions` and `benefits`.** Done.
12. ✅ **Link `/governance/receipts`.** Done — "Audit Receipts" in sidebar.

**11 of 12 quick wins from the original audit are done.** The one remaining (#9) is low-risk — it's a route-consolidation cleanup, not a trust or conversion blocker.

---

## 15. Strategic Fixes — status

1. ✅ **Public, SEO-indexed evidence library.** `/knowledge` is public; SEO metadata pass not separately confirmed — worth a follow-up check.
2. ✅ **Honest paid gating.** `TierGate` enforces; `/my-protocol` is live-data-backed.
3. ✅ **Unify the tier story.** Done across pricing/billing (marketing homepage copy not independently re-checked).
4. ✅ **Build a real provider product surface.** Done — page, form, backend, FAQ.
5. ⚠️ **Build the free basic scheduler.** Not confirmed as a distinct deliverable in this merge — `/my-protocol`'s `CalendarTab` is real-data-backed now, but whether there's a dedicated free/basic tier scheduler vs. paid analytics split wasn't verifiable statically. Verify live.
6. ❌ **Feed real engine output into marketing panels.** Still hardcoded. Now the top strategic item remaining.
7. ✅ **Glossary + progressive-disclosure tooltips.** Cohesion and Drift confirmed tooltipped; "Observation Debt" heading not confirmed — verify live.

---

## 16. Suggested Copy Improvements

*(Note: the nav CTA, signin value line, "Finish Setup" pre-wall copy, empty-profile state, and pricing tier labels below have all shipped verbatim or near-verbatim on `main`. Left in place below for reference/traceability.)*

**Nav CTA (replace "Map Stack"):**
> **Analyze my stack** — free, no account

**Signin value line (add above the email field):**
> Create a free profile to save your analysis and track how your stack changes over time. No card required.

**Onboarding "Finish Setup" (set expectation before the wall):**
> Save this to a free profile → *(button)* Create free profile & save

**Empty "No Profile Selected" state (route them, don't dead-end):**
> **Let's set up your first profile.** Your profile personalizes overlap checks and keeps your protocol in one place. *(button)* Create profile

**Analyzer "coming soon" stub (remove or ship):**
> *(Delete the stub, or:)* Share a read-only score card — no private details exposed. *(button)* Create shareable card

**Paid upgrade prompt (concrete, honest):**
> **Operator** adds your full Stack Score, synergy/conflict detail, and live stack intelligence across every compound — and removes the 8-compound cap. $12/mo.

**Provider hero:**
> **A clearer picture of what your clients are taking.** Organize multi-client protocols, reduce intake friction, and keep an evidence-aware shared reference — without crossing into medical advice. BioStack never prescribes, doses, or diagnoses; that stays with you.

**Pricing tier labels:**
> Observer — Free · Operator — Track & Analyze · Commander — Longitudinal Intelligence

---

## 17. Prioritized Backlog — updated (July 12, 2026)

**P0 — customer-losing / confidence-breaking**
- ~~Un-gate the evidence library.~~ ✅ Done for `/knowledge`. **Remaining: decide + implement `/compounds` public read tier if intended.**
- ~~Reconcile free-vs-paid into one consistent story.~~ ✅ Done.
- ~~Rename/remove "Map Stack"; fix CTA taxonomy.~~ ✅ Done.
- ~~Stop selling paid value against mock/inert screens.~~ ✅ Done.
- **Live-test the auth callback loop end to end.** Still open — no code evidence either way; needs a live browser pass.

**P1 — conversion / readiness blocker**
- ~~Build the real provider page + FAQ + request-access.~~ ✅ Done.
- **Collapse the three onboarding routes** (`/start`, `/map`, `/onboarding`). Still open, downgraded to P1 since the silent-wall problem is fixed.
- ~~Add "Evidence" to nav; render `drugInteractions`/`benefits`; surface receipts as trust.~~ ✅ Done.
- **Feed a real engine example into the homepage proof panel.** Still open — now the top open item overall.

**P2 — important polish**
- ~~Fix all sidebar/page name drift.~~ ✅ Done.
- **Glossary + tooltips for Cohesion/Drift/Observation Debt/Stack Score.** Cohesion and Drift done; Observation Debt heading unconfirmed — verify live.
- ~~Route empty/no-profile states into profile creation.~~ ✅ Done.
- Calculator "how this was calculated" + "check your math" + mistake warnings. Not part of this merge — still open.

**P3 — later optimization**
- Dynamic vial illustration. Still open.
- **Verify whether a free basic scheduler exists distinct from paid adherence analytics** — `/my-protocol` calendar is real now but the free/paid split wasn't independently confirmed.
- Provider multi-client demo/workflow. Not independently re-verified — the intake form and FAQ exist; a live multi-client demo wasn't found.
- SEO metadata pass on newly public dossiers. Still open — worth doing now that `/knowledge` is actually public.

---

## Category Scores (1–5) — July 12, 2026 (July 6 score in parens)

| Category | Score | Note |
|---|---|---|
| Homepage clarity | 4 (4) | Unchanged; still strong, still no live-engine link |
| Navigation / IA | 4 (2) | Knowledge base and receipts now linked, name drift fixed; only the 3 onboarding routes remain |
| CTA clarity | 4 (2) | "Map Stack" gone, test-enforced; onboarding verb consolidation still partial |
| Free feature discoverability | 4 (3) | Evidence library now public and linked from nav |
| Free-to-paid path | 4 (2) | Unified tiers, real `TierGate`, live-data `/my-protocol` |
| Profile onboarding | 4 (3) | Pre-wall value line added; architecture unchanged (still good) |
| Scheduling flow | 3 (2) | Mock removed, but free/paid scheduler split unconfirmed — verify live |
| Calculator / tool usability | 4 (4) | Unchanged; math-transparency polish still recommended |
| Protocol intelligence surfacing | 3 (2) | Public + enforced now; homepage proof panel still stubbed |
| Evidence / provenance visibility | 4 (2) | Dossier public, receipts linked; `/compounds` still gated |
| Provider path | 4 (1) | Real page, working form, backend-persisted, honest FAQ |
| Auth / sign-in friction | 4 (3) | Wall now explained; callback loop still needs a live test |
| Mobile UX | 3 (3) | Not independently re-verified this pass |
| Empty / error states | 4 (3) | No-profile state now routes to creation |
| Safety / trust posture | 5 (5) | Unchanged — still excellent |
| **Overall product readiness** | **3.8 (2.5)** | **Ready for provider acquisition; close to ready for paid conversion; two open items (`/compounds` gating decision, homepage proof panel) and one live-test (auth loop) stand between here and launch-ready** |

---

## Analytics & Instrumentation Recommendations (for later — do not implement)

Note: the analyzer already fires a solid event set (`analyzer_viewed`, `analyzer_analysis_started`, `analyzer_result_viewed`, `analyzer_save_clicked`, `analyzer_convert_clicked`, `analyzer_unlock_clicked`, etc.). Extend the same discipline to the funnel:

Homepage CTA clicks (per CTA) · free tool starts/completions · calculator completions · analyzer start/complete (have it) · save-protocol attempts · **sign-in prompt impressions** (critical given loop history) · sign-in completions · **lost-return-path events** · profile-creation start/complete · schedule-creation attempts · upgrade-CTA clicks (per placement) · provider-CTA clicks · FAQ engagement · **evidence/citation expansion clicks** (to prove the un-gating thesis) · knowledge-dossier views (anon vs authed).

---

*Prepared as a product-experience audit. No source files were modified. Recommendations are copy/IA/flow-level and preserve BioStack's observational, educational, non-prescriptive boundary throughout.*

*Updated July 12, 2026: re-verified against `origin/main` at commit `a37726a` (PR #181 merged). Method was a static code/route diff against the original findings — grep and file reads confirming or refuting each item, not a live browser walkthrough. Three items remain unverified without a live pass: the auth callback loop, whether `CalendarTab` has a real free-vs-paid scheduler split, and the "Observation Debt" panel's on-screen heading text.*
