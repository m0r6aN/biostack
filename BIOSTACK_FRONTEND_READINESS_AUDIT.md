# BioStack Frontend Readiness Audit

**Prepared for:** Clint
**Date:** July 6, 2026
**Scope:** Product-experience audit of the BioStack Next.js frontend (`frontend/src`). UI, UX, IA, product flow, nomenclature, conversion clarity, free-vs-paid clarity, provider value, data surfacing. Not a code review.
**Method:** Static read of routes, components, middleware, feature gates, copy, and the intelligence/knowledge data model. No live/browser walkthrough was run, so dynamic issues (real auth-loop behavior, actual API responses) are flagged as "verify live" where relevant.

---

## 1. Executive Summary

**Verdict: Not ready for paid conversion. Not ready for provider acquisition. Mostly ready as a free tools destination.**

BioStack is two products wearing one coat. There is a genuinely premium, evidence-aware intelligence engine underneath (rich `KnowledgeEntry` model with evidence tiers, pathways, citations, drug interactions, optimization guidelines; overlap detection; drift/pattern/sequence panels; cohesion timelines). And there is a marketing/onboarding surface that mostly speaks in **vague stack language and internal vocabulary** ("Map Stack," "Mission Control," "Operator/Commander," "Cohesion," "Drift," "Observation Debt"). The gap between the two is the core problem: **the beast is real, but the storefront barely shows it.**

The single most important finding, and the one you flagged as the likely money leak, is confirmed:

> **The entire evidence/knowledge library is behind the auth wall.** `/knowledge` and `/compounds` are NOT in the middleware public list. An anonymous visitor — the exact person deciding whether to trust you — can never see a citation, an evidence tier, a mechanism summary, or a pathway map. Your most credibility-building asset is invisible to the people who most need to see it. This is backend brilliance trapped behind frontend fog, literally gated by a redirect.

Second-order problems that block paid conversion:

- **Free vs paid is genuinely unclear.** Three different vocabularies describe the tiers (homepage "free vs Operator," pricing "Observer/Operator/Commander," billing "Operator/Commander" with different feature lists than pricing). A visitor cannot answer "what do I actually get for $12?"
- **Paid gating is largely inert or faked.** `TierGate` is a documented no-op. `/my-protocol` is a hardcoded mock fixture with a visible "Preview fixture" banner. Selling "Operator unlocks this" against screens that are demonstrably fake is a product-risk gap.
- **The primary nav CTA "Map Stack" is undefined even to you.** It sits beside "Start free," implying it costs money, and routes to a near-duplicate of "Start free."
- **Provider path is a stub.** Three bullet points, no FAQ, no workflow, no business case, no pricing. Providers have no reason to sign up.

The good news: most of the highest-impact fixes are copy, IA, and un-gating decisions — not new engineering. Un-gate the knowledge base, unify the tier vocabulary, rename two CTAs, and build one honest provider page, and readiness jumps materially.

---

## 2. Top 10 UX/IA Risks (ranked by customer-loss risk)

1. **Evidence library is auth-walled.** Anonymous visitors cannot browse `/knowledge` or `/compounds`. The trust engine is invisible pre-signup. *(P0)*
2. **Free vs paid is described three incompatible ways** across homepage, pricing, and billing. Buyers can't form a purchase decision. *(P0)*
3. **"Map Stack" CTA is meaningless and implies paid.** Confuses the primary nav; duplicates "Start free." *(P0)*
4. **Paywalled value is shown against fake screens.** `/my-protocol` is a mock with a "Preview fixture" banner; `TierGate` doesn't gate. Erodes trust at the exact moment of upgrade consideration. *(P0)*
5. **Provider path is a non-offer.** No FAQ, no workflow, no value case, no pricing. Zero provider conversion. *(P1)*
6. **Two parallel onboarding flows (`/start`, `/map`) that are nearly identical** and a third (`/onboarding`) that duplicates them. "Now what?" and choice paralysis. *(P1)*
7. **Onboarding dead-ends into a sign-in wall with "Finish Setup" → `/profiles`,** which is auth-gated by middleware. Value is built, then the door slams. *(P1)*
8. **Internal vocabulary everywhere** ("Cohesion," "Drift," "Observation Debt," "Mission Control," "Operator/Commander") with no glossary or tooltips. Beginners bounce. *(P1)*
9. **Nav omits the knowledge base and pricing-critical proof.** Logged-out nav has no "Compounds/Evidence" entry; the flagship asset isn't even linkable. *(P1)*
10. **Empty/no-profile states dead-end** ("No Profile Selected") instead of routing users into profile creation. Authenticated users can get stuck. *(P2)*

---

## 3. Funnel Walkthroughs

### A. New visitor → free value
Homepage hero is strong and offers four persona cards (Analyzer / Starter / Experienced / Provider) plus a tools link. **This works.** A visitor can reach `/tools` (public, no account) and get a real dose/mix/convert result. The break: nothing routes them to the evidence library (auth-walled), and the hero's secondary link "See free vs Operator" introduces a tier name ("Operator") with no prior context. **Grade: B-.** Free tools land; proof-of-intelligence doesn't.

### B. Free anonymous user → saved protocol/profile
Analyzer and onboarding both save to `localStorage` (good — value before signup). But "Finish Setup" and "Convert to BioStack Protocol" both route to `/profiles` or `/protocol-console`, which middleware redirects to `/auth/signin`. The sign-in page *does* preserve callback + local state (nicely handled, see `resolveRedirectPath` and the "your saved analysis will carry through" banner). **The mechanics are decent; the framing is a surprise wall.** User builds a list, gets told "See what is known," clicks Finish, hits email-link auth. **Grade: C+.** Works, but the wall arrives without warning.

### C. Free profile user → paid upgrade
This is the weakest funnel. Upgrade prompts exist (`LockedTierCard`, `UpgradeNotice`) and are placed at reasonable moments (live stack intelligence, simulation, mission control). But: (a) the value proposition differs between `/pricing` and `/billing`; (b) the marquee paid surface `/my-protocol` is a labeled mock; (c) `TierGate` is inert so "locked" sections may actually render for free, undercutting scarcity. **Grade: D.** The seams are visible.

### D. User → saved protocol → schedule/check-ins
There is a real check-ins flow (`/checkins`) with trends, a Day-7 review, and overlap-aware suggestions — this is good. There is a weekly calendar view, but only inside the **mock** `/my-protocol` portal (`CalendarTab` behind an inert `TierGate`). **There is no real save-protocol-to-schedule path.** A user can save a protocol snapshot (`/protocols`) and log check-ins, but cannot turn a protocol into a live schedule with a weekly view. **Grade: C-.** Pieces exist; the connective scheduling tissue is mock-only.

### E. Provider visitor → trust/value → sign-up/request access
`/providers` is three bullets and two CTAs that dump the provider back into the *consumer* onboarding (`/start`, `/map`). No provider FAQ, no workflow, no multi-client demo, no pricing, no data-ownership language, no "request access." **Grade: F.** There is no provider offer to convert against.

---

## 4. Route & IA Inventory

| Route | Purpose | Audience | Auth | Clarity |
|---|---|---|---|---|
| `/` | Homepage / hero + persona cards | Public | Public | Strong |
| `/how-it-works` | Deeper explainer | Public | Public | Good |
| `/tools` | Dose/Mix/Convert calculators | Public | Public | Strong |
| `/tools/analyzer` | Protocol analyzer (paste/upload/scan/link) | Public | Public | Strong |
| `/tools/reconstitution-calculator`, `/tools/volume-calculator`, `/tools/unit-converter` | Deep-links into the tools surface | Public | Public | OK (thin wrappers) |
| `/pricing` | Plans (Observer/Operator/Commander) | Public | Public | **Conflicting w/ billing** |
| `/providers` | Provider landing | Providers | Public | **Stub** |
| `/safety` | Safety boundary | Public | Public | Good |
| `/faq` | FAQ (10 Qs) | Public | Public | Good, consumer-only |
| `/start` | New-user onboarding | Public | Public | Duplicates `/map` |
| `/map` | Existing-user onboarding | Public | Public | Duplicates `/start` |
| `/onboarding` | Onboarding (mode param) | Public | Public | **Third duplicate** |
| `/terms`, `/privacy` | Legal | Public | Public | (assumed OK) |
| `/auth/signin`, `/auth/verify` | Magic-link auth | All | Public | Good state-preservation |
| `/protocol-console` | "Mission Control" dashboard | Authed | **Gated** | Dense, powerful |
| `/mission-control` | Redirect → `/protocol-console` | — | — | Legacy redirect (fine) |
| `/my-protocol` | Protocol portal (calendar/labs/milestones) | Authed | Gated | **Mock fixture** |
| `/profiles`, `/profiles/[id]` | Profile CRUD | Authed | Gated | OK |
| `/compounds` | Compound management | Authed | **Gated** | Should be partly public |
| `/protocols`, `/protocols/[id]`, `/protocols/[id]/review` | Saved protocols, detail, review | Authed | Gated | Good, real intelligence |
| `/checkins` | Daily observations | Authed | Gated | Strong |
| `/timeline` | Unified event stream | Authed | Gated | Good |
| `/knowledge`, `/knowledge/[slug]` | **Evidence library / compound dossiers** | Should be public | **Gated** | **Flagship, hidden** |
| `/billing` | Subscription mgmt | Authed | Gated | Conflicts w/ pricing |
| `/governance/receipts`, `/receipts/[uri]` | Audit/provenance receipts | Authed | Gated | Orphaned (not in nav) |
| `/admin`, `/admin/research/*` | Admin + research pipeline | Admin | Gated | Correct |
| `/checkins`, `/timeline`, etc. | — | — | — | — |

**IA flags:**
- **Duplicate routes:** `/start`, `/map`, `/onboarding` are three doors to one experience (`OnboardingExperience` with a `mode` prop). Collapse to one canonical route.
- **Hidden high-value routes:** `/knowledge` (evidence library) and `/governance/receipts` (provenance/trust) exist and are invisible in nav. The receipts route is a *trust asset* nobody can find.
- **Auth over-pressure:** `/knowledge` and `/compounds` should have public read tiers.
- **Nav item mislabeled/misrouted:** Sidebar "Research" (`/admin/research`) has `adminOnly: false`, exposing an admin surface in the nav to every signed-in user. Verify intended; likely should be `adminOnly: true`.
- **Sidebar vs page name drift:** Sidebar labels the dashboard "Mission Control," routes to `/protocol-console`, whose `<Header>` says "Mission Control," but the file/route is `protocol-console`. Three names for one thing.

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

**Product-risk gaps (UI promises not clearly supported):**
- `/my-protocol` (calendar, labs, milestones) is a **mock fixture** — do not sell it as live until wired.
- `TierGate` is **inert** — "locked" sections may render regardless of tier. Either enforce or stop implying gating.
- Billing lists Operator features ("counterfactual scenarios," "no compound cap") that differ from the pricing page's Operator list. Reconcile to one source of truth.

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

Findings:

1. **The whole library is gated.** `/knowledge` + `/compounds` are not public. Citations (`sourceReferences`), evidence tiers, mechanism summaries, and pathways — your entire trust case — are invisible to anonymous visitors. **This alone likely suppresses conversion and SEO.** Un-gate read access.
2. **`benefits[]` and `drugInteractions[]` are never rendered anywhere** in the user-facing components (confirmed by grep). Drug interactions in particular are a high-trust, high-engagement field sitting completely dark.
3. **Provenance/receipts (`/governance/receipts`, `/receipts/[uri]`) are orphaned** — not linked from any nav. Audit/verification is a premium trust signal that no user can find.
4. **Marketing panels show *stubs of* intelligence, not the real thing.** `StackIntelligencePanel` on the homepage/how-it-works uses hardcoded "BPC-157 + TB-500 tissue-repair overlap" copy and an "included in Operator" evidence teaser rather than pulling a live example from the actual engine. The proof section proves less than the engine can.
5. **Analyzer results are strong but the report's richest layers hide behind an inert paywall** and a "Shareable summary — coming soon" stub. The "coming soon" card on a conversion surface reads as unfinished.
6. **Compound dossier page (`/knowledge/[slug]`) is genuinely good** (citations, pathways, notes, relationships) — it's the best data-surfacing surface you have, and it's the one nobody without an account can reach. Promote it to a public, SEO-indexed, linkable asset. This is the single biggest lever in the audit.

**One-line takeaway:** You built a research-grade evidence layer and then hid the front door. Open `/knowledge` to the public, render `benefits` + `drugInteractions`, surface receipts as a trust badge, and feed one real engine example into the homepage panel.

---

## 8. Protocol Intelligence Assessment

**Does it feel like a flagship? Not yet — because it's mostly behind auth and mocks, and it speaks in code names.**

- The **definitions are rich and structured** (dossier page proves it), connected to evidence tiers, pathways, relationships, and profile context (demographics only, safely non-prescriptive — good).
- But intelligence is **surfaced inconsistently**: full and credible in the authed dossier; stubbed and hardcoded in the public proof panel; locked (via inert gate) or mocked in the portal.
- **Uncertainty is represented** (evidence tiers, "unknown states," "reference only" disclaimers) — this is done tastefully and on-brand for the safety posture.
- **Beginners are served** on `/start` ("a protocol is just the list of things you take") but **advanced depth is buried**; the compound dossier and overlap detail should be one click from the marketing surface.

**To make it flagship:**
- Public, indexed compound dossiers with visible citations and evidence tiers.
- A live "See what BioStack catches" panel driven by the real engine, not hardcoded strings.
- Render every rich field (benefits, drug interactions, pathways) with progressive disclosure ("What does this mean?").
- Name it something a human recognizes ("Evidence & Interactions") instead of "Compound Intelligence."

---

## 9. Provider Readiness Assessment

Current `/providers` = 3 bullets + 2 consumer CTAs. **This cannot convert a provider.**

**Recommended provider page structure:**
1. Hero: outcome-focused ("Give clients a clearer picture of what they're taking — without giving medical advice").
2. What providers can do: multi-client protocol organization, structured intake, shared observational summaries, check-in visibility.
3. Business value: reduced intake friction, fewer "what am I on again?" conversations, an evidence-aware shared reference.
4. Explicit boundary: what BioStack does **not** do (no prescribing, dosing, diagnosis, or clinical decisioning; provider stays the clinician).
5. Example workflow: invite client → client logs stack → provider reviews summary → check-ins over time.
6. Data ownership & privacy language.
7. Provider pricing / "Request access" CTA (distinct from consumer "Start free").
8. Provider FAQ.

**Missing provider FAQ questions to add:**
- Is BioStack a medical device or EHR? (No — observational/organizational.)
- Can I manage multiple clients from one account?
- Who owns client data, and can clients revoke access?
- What can clients see vs. what can I see?
- Does BioStack give dosing or treatment recommendations? (No.)
- How do clients get invited / onboarded?
- Can I export or share an observational summary?
- Is there a BAA / what's the compliance posture?
- What does it cost per provider / per client?
- What happens to client data if a client stops using BioStack?

---

## 10. Auth & Sign-In Friction Assessment

**The infrastructure is better than the framing.** The signin page preserves `callbackUrl`, masks email, has resend cooldown, and shows a reassuring "your saved analysis will carry through" banner. Local state (analyzer/onboarding/tools) is persisted before auth. That's the right architecture.

**Problems:**
- **Premature wall via routing, not messaging.** Onboarding's "Finish Setup" → `/profiles` and analyzer's "Convert" → `/protocol-console` both hit the middleware redirect with **no prior signal** that an account is required. The user experiences it as a surprise wall after doing work.
- **No "why" at the wall.** The signin page explains *how* (email link) but not *what you unlock*. Add a one-line value reason at the point of auth.
- **Loop risk (verify live):** middleware redirects any non-public route to `/auth/signin?callbackUrl=…`. If a magic-link lands the user back on a gated route before the session cookie is readable, you can bounce. The previous "infinite loop" history means this needs a live test: signin → email link → `/auth/verify` → callback route, confirming the `biostack_session` cookie is set before the callback resolves.
- **CTA taxonomy is muddy.** "Start free," "Sign in," "Map Stack," "Build My Protocol," "Choose Operator," "Finish Setup," "Convert to BioStack Protocol" — users can't tell which create accounts, which cost money, and which are free tools. Consolidate to a small, consistent verb set.

**Recommendation:** Keep all tools + evidence library + onboarding fully anonymous. Only prompt sign-in at the *save-to-account* moment, and when you do, say why ("Create a free profile to save this and track it over time").

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

**"Map Stack" should be renamed or removed.** It communicates nothing (you confirmed this), and its placement beside "Start free" implies it is *not* free — while it actually routes to a near-duplicate of "Start free."

**Recommended header CTA structure:**

- **Logged-out desktop:** Primary = **"Analyze my stack"** (→ `/tools/analyzer`, the strongest free proof). Secondary/text = **"Start free"** (→ onboarding). Nav links: How it works · Tools · Evidence *(new, → public knowledge)* · Pricing · For Providers · Safety.
- **Logged-out mobile:** single primary **"Analyze my stack"**; move "Start free" into the sticky bar. Drop the 4-up sticky CTA (Start/Map/Pricing/Provider) down to 2 clear actions.
- **Logged-in free:** Primary = **"Go to Dashboard"**; secondary = **"Upgrade"** only near value moments, not globally.
- **Paid:** Primary = **"Dashboard"**; no upgrade nag.
- **Providers:** distinct **"Request provider access."**

Kill the third onboarding route; make "Start free" and "Analyze my stack" the only two entry verbs.

---

## 14. Quick Wins (copy / IA / nav / tooltip / route)

1. **Un-gate `/knowledge` and `/knowledge/[slug]` for read** — add to middleware public prefixes. Highest ROI change in the audit.
2. **Rename "Map Stack" → "Analyze my stack"** (nav + mobile sticky) and point it at `/tools/analyzer`.
3. **Add "Evidence" to the top nav** linking to the (now public) knowledge base.
4. **Reconcile tier names** — put "(Free)" next to Observer and one-word descriptors on Operator/Commander everywhere they appear.
5. **Make billing's Operator/Commander feature lists identical to pricing's.**
6. **Remove or finish the "Shareable summary — coming soon" stub** on the analyzer report.
7. **Fix sidebar/page name drift** (Observations↔Check-ins, Protocol Lab↔Protocols, Compound Intelligence↔Knowledge, Mission Control↔Protocol Console).
8. **Add a value line to the signin page** ("Create a free profile to save this and track it over time").
9. **Collapse `/start`, `/map`, `/onboarding`** to one canonical route with a mode toggle.
10. **Set Sidebar "Research" to `adminOnly: true`** (verify intent).
11. **Render `drugInteractions` and `benefits`** in the compound dossier (data already loaded).
12. **Link `/governance/receipts`** somewhere as a "provenance / audit" trust badge.

---

## 15. Strategic Fixes

1. **Public, SEO-indexed evidence library** with per-compound dossiers as the top-of-funnel trust + acquisition engine.
2. **Honest paid gating:** replace inert `TierGate` with real enforcement, and replace the `/my-protocol` mock with live data (or clearly label it "Preview" outside the paywall and stop implying it's what you buy).
3. **Unify the tier story** into one source of truth consumed by pricing, billing, and marketing.
4. **Build a real provider product surface** (page + FAQ + multi-client workflow + request-access + pricing).
5. **Build the free basic scheduler** (saved protocol → weekly view → manual logs) to anchor retention, then layer paid analytics.
6. **Feed real engine output into marketing panels** instead of hardcoded examples.
7. **Glossary + progressive-disclosure tooltips** across the intelligence vocabulary so beginners aren't gatekept by code names.

---

## 16. Suggested Copy Improvements

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

## 17. Prioritized Backlog

**P0 — customer-losing / confidence-breaking**
- Un-gate the evidence library (`/knowledge`, `/compounds` read) — public + indexed.
- Reconcile free-vs-paid into one consistent story (pricing = billing = marketing).
- Rename/remove "Map Stack"; fix CTA taxonomy.
- Stop selling paid value against mock (`/my-protocol`) and inert (`TierGate`) screens — enforce or relabel.
- Live-test the auth callback loop end to end.

**P1 — conversion / readiness blocker**
- Build the real provider page + FAQ + request-access.
- Collapse the three onboarding routes; warn before the sign-in wall.
- Add "Evidence" to nav; render `drugInteractions`/`benefits`; surface receipts as trust.
- Feed a real engine example into the homepage proof panel.

**P2 — important polish**
- Fix all sidebar/page name drift; standardize on plain nouns.
- Glossary + tooltips for Cohesion/Drift/Observation Debt/Stack Score.
- Route empty/no-profile states into profile creation.
- Calculator "how this was calculated" + "check your math" + mistake warnings.

**P3 — later optimization**
- Dynamic vial illustration.
- Build free basic scheduler, then paid adherence analytics.
- Provider multi-client demo/workflow.
- SEO metadata pass on newly public dossiers.

---

## Category Scores (1–5)

| Category | Score | Note |
|---|---|---|
| Homepage clarity | 4 | Strong hero + persona cards; tier name leaks in early |
| Navigation / IA | 2 | Duplicate onboarding routes, hidden knowledge base, name drift |
| CTA clarity | 2 | "Map Stack" undefined; too many competing verbs |
| Free feature discoverability | 3 | Tools shine; evidence library hidden drags this down |
| Free-to-paid path | 2 | Conflicting tiers, gating against mock/inert screens |
| Profile onboarding | 3 | Good local-save architecture; surprise wall at the end |
| Scheduling flow | 2 | Real check-ins; weekly schedule is mock-only |
| Calculator / tool usability | 4 | Genuinely good; needs math-transparency polish |
| Protocol intelligence surfacing | 2 | Rich engine, mostly hidden or stubbed |
| Evidence / provenance visibility | 2 | Best-in-app dossier is auth-walled; receipts orphaned |
| Provider path | 1 | Stub; no offer to convert |
| Auth / sign-in friction | 3 | Solid mechanics, surprise-wall framing, loop needs live test |
| Mobile UX | 3 | Responsive; 4-up sticky CTA is busy |
| Empty / error states | 3 | Consistent components; some dead-end "no profile" states |
| Safety / trust posture | 5 | Disciplined, non-prescriptive, disclaimers everywhere — excellent |
| **Overall product readiness** | **2.5** | **Mostly ready as free tools; not ready for paid or provider conversion** |

---

## Analytics & Instrumentation Recommendations (for later — do not implement)

Note: the analyzer already fires a solid event set (`analyzer_viewed`, `analyzer_analysis_started`, `analyzer_result_viewed`, `analyzer_save_clicked`, `analyzer_convert_clicked`, `analyzer_unlock_clicked`, etc.). Extend the same discipline to the funnel:

Homepage CTA clicks (per CTA) · free tool starts/completions · calculator completions · analyzer start/complete (have it) · save-protocol attempts · **sign-in prompt impressions** (critical given loop history) · sign-in completions · **lost-return-path events** · profile-creation start/complete · schedule-creation attempts · upgrade-CTA clicks (per placement) · provider-CTA clicks · FAQ engagement · **evidence/citation expansion clicks** (to prove the un-gating thesis) · knowledge-dossier views (anon vs authed).

---

*Prepared as a product-experience audit. No source files were modified. Recommendations are copy/IA/flow-level and preserve BioStack's observational, educational, non-prescriptive boundary throughout.*
