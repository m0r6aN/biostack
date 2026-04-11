
> **PHASE 2 DIRECTIVE APPLIED - STRATEGIC OVERLAY:** 
> BioStack is the definitive **Protocol Intelligence Platform** for human biology. It is NOT a tracker, peptide app, or supplement log. It is an observability engine for compounds and outcomes. 
> *Core Moat:* Synergy/interaction mapping, pathway overlap detection, timeline correlation engine, evidence-tier knowledge base.
> *Aha Moment:* Within 60 seconds of onboarding, users input 1-3 compounds and see pathway overlap, synergies, conflicts, and timeline initialization.
> *Monetization:* Users pay for reduced mistakes, increased clarity, confidence, and better outcomes through understanding.
> *Growth Engine:* Calculators are the primary acquisition surface and lead generation hook.

---
# BioStack Lead Generation & Conversion Funnel Strategy

**Document:** 07 — Lead Gen & Funnel Engine
**Team:** Growth / Marketing
**Date:** April 2026
**Status:** Strategy Draft

---

## Overview

BioStack's lead generation strategy is built on a single, non-negotiable principle: the product creates value before it asks for anything. The Reconstitution Calculator is the wedge — a high-intent, high-utility tool that attracts exactly the users who will become paying subscribers. Every funnel path leads back to this core insight: if someone is working with lyophilized peptides, they need this math done right, and BioStack does it better than any alternative.

This document maps the complete funnel from cold traffic to retained subscriber, including lead magnets, email capture touchpoints, landing page strategy, gating decisions, and retention mechanics.

---

## 1. Lead Magnet Strategy

### Option A — "The Reconstitution & Dosing Reference Card" (PDF)
**What it is:** A one-page, printable reference card covering the most common reconstitution ratios, BAC water volumes, storage guidelines, and a quick-reference dosing math table for the most tracked compound categories. Includes the reconstitution formula, unit conversion shortcuts, and a blank protocol log template.

**Why it attracts the right user:** Anyone ordering lyophilized compounds for the first time faces the same anxiety: "Am I mixing this correctly?" A downloadable reference card is a permanent, offline artifact they'll keep near their workspace. It signals practical expertise, not speculation.

**How it gates toward signup:** The PDF is offered immediately after a user completes a calculation on the free Reconstitution Calculator. Copy: "Want to save this calculation and get the full reference card? Drop your email — takes 5 seconds." The email is required to receive the PDF download link. On delivery, the email contains the PDF plus a single CTA: "Your free BioStack account saves every calculation automatically."

**Estimated conversion quality:** High. The user has already demonstrated intent by running a calculation. The PDF solves a real adjacent problem (offline reference). Email quality will be excellent — these are self-selected, high-intent users who know exactly what they're doing.

---

### Option B — "Protocol Intelligence Starter Guide" (Email Course)
**What it is:** A 5-email sequence delivered over 7 days covering: (1) how to think about evidence tiers when evaluating compounds, (2) how to track check-ins to establish a baseline, (3) how to use pathway overlap checking to spot interaction risks, (4) how to build a named protocol phase, (5) a worked example of a full BioStack protocol from setup to 30-day review.

**Why it attracts the right user:** People who want structure, not just a calculator hit. This attracts the "protocol builder" persona — someone who's serious about tracking outcomes, not just individual doses.

**How it gates toward signup:** Email 3 includes a prompt to create a free account to follow along with the pathway overlap checker. Email 5 includes a direct upgrade CTA to unlock full protocol history and check-in analytics.

**Estimated conversion quality:** Medium-high. Lower volume than the calculator PDF because it requires more upfront commitment, but the users who complete the course will have higher purchase intent.

---

### Option C — "Pathway Overlap Cheat Sheet" (Web Tool Teaser)
**What it is:** A static, read-only preview of BioStack's pathway overlap data for 5–6 common compound combinations. Presented as a formatted table showing shared mechanisms and flagged interactions.

**Why it attracts the right user:** Advanced users who are already stacking multiple compounds and want to understand what they're combining. This is a high-sophistication signal.

**How it gates toward signup:** The table shows partial data — pathway names are visible but interaction severity flags are blurred. "See full interaction analysis — create a free account." Direct, honest gate.

**Estimated conversion quality:** Medium. Smaller audience (requires existing multi-compound knowledge) but very high intent on the users who engage.

---

### Option D — "Weekly Protocol Digest" (Newsletter)
**What it is:** A weekly email covering one compound from the knowledge base — evidence tier summary, common use patterns, key interactions, and a "what to track if you're using this" section. Purely educational, clearly labeled as not medical advice.

**Why it attracts the right user:** Attracts the research-minded user who is in the "learning phase" before starting a protocol. Builds brand authority over time.

**How it gates toward signup:** Newsletter subscribers receive a persistent CTA in every email footer: "Ready to track your own protocol? BioStack is free to start." Lower urgency, longer cycle.

**Estimated conversion quality:** Low-medium on direct conversion, but high on brand authority and long-tail SEO if repurposed as blog content.

---

### Recommendation: Option A — The Reconstitution & Dosing Reference Card

Option A wins because it attaches to the highest-intent moment in the entire funnel (post-calculation), delivers immediate concrete value, and works with zero friction. The user has already done something in the product. They are warm. The PDF is a natural complement to what they just experienced, not an unrelated bribe.

Run Option B as a secondary sequence that triggers after email capture from Option A. After a user downloads the PDF, enroll them in a 5-email "getting more from BioStack" sequence automatically. This layers both strategies without requiring separate acquisition.

---

## 2. Free Tool Funnel Architecture

### Standalone Calculator Page Design (Public, No Account Required)

The `/tools/reconstitution-calculator` page is fully public. No login wall. No friction. The calculation runs immediately. This is non-negotiable — gating the calculator destroys the top-of-funnel value that makes it useful as an acquisition channel.

**Page layout:**
- Header: BioStack wordmark + nav link to "All Tools" and "Sign In"
- Hero: Calculator widget, above the fold, immediately interactive
- Below calculator: Results display with full math shown ("Here's exactly how we calculated this")
- Below results: Lead magnet prompt (conditional — appears after first successful calculation)
- Below prompt: "How this calculator works" explainer for SEO and trust
- Footer: Links to Volume Calculator, Unit Converter, Knowledge Base preview

**Post-calculation conversion prompt:**
After the user sees their result, display a non-blocking banner:

> "Save this calculation to your protocol log.
> BioStack tracks every reconstitution event so you never lose your mixing history.
> Free account — no credit card, takes 30 seconds."
> [Create Free Account] [Download Reference Card →]

Two CTAs: account creation (higher-value conversion) and PDF download (email capture for users not ready to commit). The PDF option keeps the user engaged without losing them.

**When a user hits a usage limit or wants to save results:**

For the standalone public calculator, there is no hard usage limit in the free tier — the math is always available. The conversion prompt is soft and value-framed, not a wall. The gating mechanism is around persistence and history: "You can always run the calculation, but only your account can save and recall it."

If a user tries to save a calculation without an account, the save action triggers the account creation flow inline. They do not lose their inputs. The form prefills with their calculation data. After account creation, the calculation is saved automatically. This is the most respectful possible implementation of a conversion gate.

**Email capture flow from calculator usage:**

1. User runs calculation → sees result
2. After 10–15 seconds (or on scroll past result), slide-in prompt appears: "Get the Reconstitution Reference Card — the offline guide to dosing math. Free download."
3. User enters email → immediate PDF delivery via transactional email
4. Confirmation email contains PDF + account creation CTA
5. 24 hours later: automated email — "Your reference card + one more tool you should know about" (introduces Volume Calculator and check-in tracking)
6. 72 hours later: "The one thing most people miss when starting a new compound" (introduces evidence tiers in the Knowledge Base)
7. 7 days later: Direct invite to create account — "Everything you've been calculating, now tracked automatically."

---

## 3. Email Capture Opportunities

Every touchpoint where email capture makes sense, with specific copy for each:

### 3.1 — Post-Calculation (Reconstitution Calculator)
**Trigger:** User successfully runs a reconstitution calculation.
**Copy:** "Save your calculation history and get the free Reference Card. No spam — just your protocol data and occasionally useful compound research."
**Form:** Single email field. Submit = "Send my Reference Card."

### 3.2 — Post-Calculation (Volume Calculator)
**Trigger:** User calculates injection volume.
**Copy:** "Want to log this dose to a protocol timeline? Create a free account or save your email and we'll remind you when BioStack is ready for your stack."
**Form:** Email field. CTA: "Keep my place in line."

### 3.3 — Knowledge Base Compound Page
**Trigger:** User reads a compound entry and scrolls past the evidence tier summary.
**Copy:** "Tracking [Compound Name]? BioStack logs your compounds, calculates your doses, and surfaces interactions automatically. Start free."
**Form:** Email + "Create Account" inline. No PDF gate here — the CTA is account creation directly.

### 3.4 — Pathway Overlap Checker (partial results)
**Trigger:** User selects compounds and sees partial overlap data.
**Copy:** "Full interaction analysis is one free account away. See every shared pathway and flagged interaction for your specific stack."
**Form:** Account creation gate — email + password. Alternatively, "Email me the full report" captures email without full signup.

### 3.5 — Homepage / Marketing Site Exit Intent
**Trigger:** Exit intent detection on the marketing homepage.
**Copy:** "Before you go — the Reconstitution Reference Card is free. One email, instant download."
**Form:** Single email field.

### 3.6 — Blog / Content Pages (if content strategy is active)
**Trigger:** Bottom of any compound education article.
**Copy:** "BioStack tracks this compound automatically — evidence tier, interactions, dosing history. Free to start."
**Form:** Email field with "Get early access" framing.

### 3.7 — Tools Index Page
**Trigger:** User browses the `/tools` page but hasn't used a calculator yet.
**Copy:** "All tools are free to use. Create an account to save your results and build a full protocol log."
**Form:** Inline account creation widget — email, password, "Start Free."

---

## 4. Calculator Landing Page Strategy

### Primary Page: `/tools/reconstitution-calculator`

**Page structure:**

1. **Title (H1):** "Reconstitution Calculator — Precise Peptide Mixing Math"
2. **Subtitle:** "Enter your lyophilized powder amount and diluent volume. Get exact concentration in mcg/mL instantly."
3. **Calculator widget** (above fold, immediately interactive)
4. **Results section** — shows calculation with full formula displayed
5. **Conversion prompt** — post-calculation account/PDF CTA
6. **"How reconstitution works" section** — 300-400 word explainer for SEO and trust. Covers what lyophilization is, why concentration matters, what BAC water is, common mixing ratios. Educational, not prescriptive.
7. **FAQ section** — "What is BAC water?", "How long does reconstituted peptide last?", "Why does concentration matter?", "What if I use a different diluent volume?" — all SEO-valuable long-tail queries
8. **Related tools** — Volume Calculator, Unit Converter, "Track this compound in BioStack"
9. **Footer** — standard

**Copy angle:** Precision over simplicity. The user working with lyophilized compounds is not a beginner looking for reassurance — they are a competent person who wants accurate math presented clearly. The copy should reflect that intelligence. Avoid condescension, avoid clinical hedging that obscures utility, always include the educational disclaimer naturally: "This calculator is for educational and reference purposes. It does not constitute medical advice."

**SEO value:**
- Primary keyword: "peptide reconstitution calculator"
- Secondary: "lyophilized peptide mixing calculator", "BAC water concentration calculator", "mcg/mL peptide dosing calculator"
- Long-tail: "how to calculate reconstitution concentration", "how much BAC water to add to peptide vial"
- The FAQ section and explainer content capture informational intent queries that funnel into the calculator widget
- This page should rank for the tool itself AND for the educational intent queries upstream of it

**Conversion mechanism from calculator → account:**

The conversion sequence is linear and non-disruptive:
1. User arrives via search or direct
2. Runs calculation immediately (no friction)
3. Sees result with full formula
4. Receives soft CTA: save to protocol log OR download reference card
5. Account creation or email capture
6. Automated sequence begins
7. 7-day email window targeting account creation if only email was captured

The calculator is never gated. The conversion hook is always downstream of the value delivery.

---

### Secondary Pages

**`/tools/volume-calculator`**
Same architecture. Primary keyword: "peptide injection volume calculator", "dose volume calculator mcg/mL."

**`/tools/unit-converter`**
Simpler page. Primary keyword: "mg to mcg converter", "IU conversion calculator peptides." Lower conversion intent but high SEO volume — drives top-of-funnel awareness.

**`/tools/pathway-overlap-checker`** (requires account — teaser landing page)
Landing page shows what the tool does with a worked example (static). CTA is entirely account-creation focused. "This tool requires a free BioStack account to use."

---

## 5. Free vs Gated Asset Framework

### Free — No Account Required
The threshold for free access is: does this tool provide value to a user who will never sign up? If yes, it belongs in the free tier because it earns brand trust and search traffic.

| Asset | Rationale |
|---|---|
| Reconstitution Calculator | Primary acquisition wedge. Always free. |
| Volume Calculator | Supporting tool. High-intent traffic driver. |
| Unit Converter | SEO / awareness. Minimal friction to offer freely. |
| Knowledge Base browsing (compound pages, evidence tiers) | Trust-building content. Partial access acceptable — full compound profiles can require account. |
| Tools landing page | Discovery layer. No gate. |

### Free with Email Capture
The threshold: tools or content that deliver enhanced value and justify a soft ask.

| Asset | Gate Mechanism |
|---|---|
| Reconstitution Reference Card (PDF) | Email required for download |
| Calculation history (session only, no persistence) | Shows "save this" prompt, email capture for reminder |
| Compound search results (limited preview) | Full compound profile requires email or account |

### Free Account Required
The threshold: features that require a persistent user context. No payment required.

| Asset | Rationale |
|---|---|
| Save calculation history | Requires user identity |
| Compound tracking (add to stack) | Requires user context |
| Check-in logging | Requires persistent data |
| Protocol phases | Requires user-owned data |
| Unified timeline | Aggregates user data — needs account |
| Pathway overlap checker (interactive) | Personalized output — needs account |
| Full knowledge base compound profiles | Deeper content justifies light gate |

### Paid Plan Required
The threshold: features with ongoing operational cost, or features that deliver disproportionate value to power users.

| Asset | Rationale |
|---|---|
| Unlimited compound history | Storage cost scales |
| Check-in analytics and trend visualization | Compute-intensive rendering |
| Protocol comparison (phase over phase) | Advanced analytics feature |
| Export (PDF protocol report, CSV data) | High-value power user feature |
| Priority knowledge base updates | Content production cost |
| Advanced interaction flags with source citations | Research curation cost |

---

## 6. Full Conversion Funnel Map

### Stage 1 — Awareness
**Goal:** Get discovered by the right person at the right moment.
**Channel:** Organic search (calculator + compound education pages), Reddit (r/Peptides, r/nootropics, r/biohacking), Twitter/X (protocol enthusiast communities), word-of-mouth from existing users.
**BioStack touchpoints:** `/tools/reconstitution-calculator`, `/tools/unit-converter`, Knowledge Base compound pages, blog content (if active)
**Message/Hook:** "The math, done right. Free to use, no account needed."

### Stage 2 — Interest
**Goal:** User engages with the tool, experiences immediate value, begins to understand what BioStack is.
**Channel:** On-site (calculator interaction, knowledge base browsing)
**BioStack touchpoints:** Calculator widget, results display with formula explanation, "How this works" section, related tools discovery
**Message/Hook:** "This is more than a calculator — this is how serious protocol builders track their work."

### Stage 3 — Consideration
**Goal:** User understands the broader product value. Considers whether BioStack solves their tracking problem.
**Channel:** Post-calculation CTA, email sequence (if PDF downloaded), Knowledge Base deeper browsing
**BioStack touchpoints:** Post-calculation conversion prompt, Pathway Overlap Checker teaser, Check-in tracking explainer on marketing site
**Message/Hook:** "Everything you track in your head, tracked automatically and queryable. Free to start."

### Stage 4 — Intent
**Goal:** User decides to create an account.
**Channel:** Direct CTAs, email sequence day 7, return visit with saved-state prompt
**BioStack touchpoints:** Account creation flow, onboarding screens, first compound add
**Message/Hook:** "Your first compound takes 30 seconds to log. Your protocol timeline starts now."

### Stage 5 — Conversion (Free → Account)
**Goal:** User creates account, logs first compound, runs first saved calculation.
**Channel:** Onboarding sequence, in-app prompts
**BioStack touchpoints:** Compound Protocol Intelligence Platform, Reconstitution Calculator (authenticated), first check-in prompt
**Message/Hook:** "You've logged [Compound]. Now set your first check-in baseline — it takes 2 minutes and tells you everything about how this protocol is working."

### Stage 6 — Conversion (Free Account → Paid)
**Goal:** User hits a meaningful free tier limit or discovers a paid feature they want.
**Channel:** In-app upgrade prompts (contextual), email triggered by engagement milestones
**BioStack touchpoints:** Protocol export (locked), check-in analytics dashboard (locked), compound history limit warning
**Message/Hook:** "You've logged [X] check-ins. Upgrade to see your trends, export your protocol, and get full interaction analysis."

### Stage 7 — Retention
**Goal:** Paid subscriber continues to find daily/weekly value.
**Channel:** Daily check-in habit, weekly digest email, protocol milestone notifications
**BioStack touchpoints:** Check-in logging, unified timeline, protocol phase transitions
**Message/Hook:** "Your protocol is [X] days in. Here's what your check-in data shows." (see Section 8)

---

## 7. "Aha Moment in Under 60 Seconds"

### The Sequence

**0–5 seconds:** User lands on `/tools/reconstitution-calculator` from a search for "peptide reconstitution calculator." The calculator is immediately visible, above the fold. No login prompt. No modal. Just the tool.

**5–20 seconds:** User enters their values. Powder amount: 5 mg. Diluent volume: 2 mL. They hit Calculate.

**20–25 seconds:** Result appears instantly: **2,500 mcg/mL**. Below the number: "Each 0.1 mL withdrawal = 250 mcg." The math is shown in full — not hidden, not magic. Transparent formula builds trust.

**25–40 seconds:** User reads the result. Their anxiety is resolved. They know exactly what concentration they've made. They know exactly what volume to draw for their target dose. This is the aha moment: "This tool just solved the thing I was anxious about. It showed me the work. I trust this."

**40–55 seconds:** The post-calculation prompt appears: "Want to save this? Log it to a protocol and never recalculate from scratch." User sees that this is the tip of a larger iceberg. The calculator is good. The product behind it is what they actually need.

**55–60 seconds:** User either enters email (lower commitment) or clicks "Create Free Account" (higher commitment). Either outcome is a win. They have gone from cold search visitor to engaged lead or new user in under 60 seconds, driven entirely by genuine value delivery.

### Why It Creates Instant Stickiness

The stickiness is not manufactured by a onboarding gambit — it comes from the quality of the first experience. The user experienced:
1. Zero friction to value
2. Transparent, trustworthy math
3. A moment of anxiety resolution
4. An invitation to more — not a demand
5. A clear next step that is proportional to the value received

This sequence is honest. It does not over-promise. It delivers exactly what it says it will. That reliability is the foundation of every retained user.

---

## 8. Retention Hooks Post-Signup

### Habit Loops Built Into the Product

**Daily check-in loop:** The Check-in Protocol Intelligence Platform logs 15+ daily metrics. The value of a single check-in is low. The value of 30 consecutive check-ins is extremely high — it produces a personal dataset that reveals how a specific protocol is performing. The product should communicate this compounding value clearly: "Day 7 of your protocol. You've logged 5 check-ins. 25 more to see your first trend analysis." The loop: Log → See your data accumulate → Want to see the trend → Log again.

**Calculation log as ritual:** Every reconstitution event logged creates a permanent record. Users who reconstitute regularly will return to BioStack for every mixing session. The calculator is not a one-time use — it is a repeated action tied to a real-world behavior.

**Protocol phase transitions:** When a user starts a new protocol phase, they receive an in-app prompt to review the previous phase's check-in data. This creates a natural reflection point — and a reason to log more check-ins during the current phase.

**Knowledge Base as reference:** Users who have compounds in their Protocol Intelligence Platform will return to BioStack to check interaction flags when they consider adding a new compound. The Knowledge Base becomes a reference layer embedded in their decision-making process.

### Re-Engagement Email Triggers

| Trigger | Timing | Subject Line |
|---|---|---|
| No check-in logged | 3 days after last check-in | "Your protocol is running — are you tracking it?" |
| Protocol phase reaching day 14 | Automatic | "Two weeks in. What does your data show?" |
| New compound added, no check-in baseline | 48 hours | "You logged [Compound]. Did you set a baseline?" |
| Account inactive for 7 days | 7 days of no login | "Your protocol timeline is waiting." |
| Approaching free tier limit | At 80% of limit | "You've logged [X] compounds. Here's what Pro unlocks." |
| 30-day protocol milestone | Automatic | "30 days of [Protocol Name]. Here's your timeline." |

### Feature Discovery Sequences

After account creation, deliver a 5-step in-app discovery sequence over the first 10 days:

1. **Day 0 (account creation):** "Add your first compound. It takes 30 seconds." — Compound Protocol Intelligence Platform
2. **Day 1:** "Run your first reconstitution calculation and save it to your log." — Calculator (authenticated)
3. **Day 3:** "Log your first check-in. 15 metrics, 2 minutes, your baseline for everything." — Check-in Protocol Intelligence Platform
4. **Day 5:** "Check for pathway overlaps in your current stack." — Pathway Overlap Checker
5. **Day 10:** "Review your protocol timeline — every event, one view." — Unified Timeline

Each step is a single, concrete action. Each step reveals a new layer of the product. No step requires a paid upgrade — the discovery sequence is entirely free-tier accessible.

### Progress and Streak Mechanics

**Check-in streak counter:** Visible on the dashboard. "You've logged 8 consecutive days." No gamification theater — just a factual counter that creates mild social reinforcement for continued logging.

**Protocol completion milestones:** "Day 14 of your current protocol. [X] check-ins logged." Milestone markers at 7, 14, 30, 60, 90 days. Each milestone is a natural prompt to review data and — if on free tier — consider upgrading to see analytics.

**Data richness indicator:** "Your protocol has [X] data points. At 50, you can generate your first trend report." Creates a clear, motivating threshold that drives continued engagement and, at the threshold, an upgrade conversation.

---

## 9. Referral and Share Loop Ideas

### What Is Shareable About a BioStack Protocol?

The core challenge with sharing is privacy: users tracking personal bio-protocols are handling sensitive health data. Any sharing mechanic must be explicitly opt-in and non-identifying by default.

What is genuinely shareable:
- **Protocol structure** (compound list, phase names, duration) without dosing specifics
- **Calculator results** as a standalone shareable card
- **Evidence tier summaries** for a compound ("Here's what the research actually says about [Compound]")
- **Stack composition** in a stripped-down "read-only" view that omits personal check-in data

### How a User Could Share Their Stack or Timeline

**"Share My Stack" feature (free, opt-in):**
A user can generate a public, read-only link to their current compound stack. The link shows: compound names, evidence tiers, protocol phase duration, pathway overlap status (flags only — no personal dosing data). This is shareable on Reddit, Discord, or Twitter as "here's what I'm running."

The share link is a BioStack-hosted page at `/stack/[public-id]`. Every person who views that link sees BioStack branding and a CTA: "Build your own protocol — free to start."

**Calculator result card:**
After a reconstitution calculation, offer a "Share this result" option that generates an image card: "[X] mg powder + [Y] mL diluent = [Z] mcg/mL. Calculated with BioStack." Shareable as an image on social platforms. Useful for communities where people help each other with mixing math.

**"Stack Check" posts for communities:**
Encourage users to share their stack view links in relevant subreddits and Discord channels where "stack checks" (community review of what someone is running) are a common behavior. The link format is already suited to this use case.

### Community and Comparison Mechanics

**Anonymous protocol benchmarks (future feature, not current):**
If enough users opt in to anonymous data sharing, BioStack can surface aggregate patterns: "Users tracking [Compound] most commonly pair it with [Compound B]." This creates a discovery mechanic that drives Knowledge Base engagement and referral discussion.

**Community-driven evidence tier discussion:**
Users who have strong views on compound evidence can be invited to submit notes or flag outdated research. This creates a contributor relationship that is more durable than pure consumption and generates word-of-mouth from users who feel ownership over the product's quality.

**Referral program (simple, clean):**
"Invite a friend who's serious about their protocol. They sign up, you both get [X months of Pro] free." No complex mechanics. No viral coefficient optimization theater. A straightforward bilateral reward for a genuine referral. Works best after a user has been active for 30+ days and genuinely values the product.

---

## Summary: Priority Actions for Launch

1. Build and deploy `/tools/reconstitution-calculator` as a fully public, SEO-optimized standalone page before launch.
2. Create the Reconstitution Reference Card PDF — this is the lead magnet that anchors the entire email capture flow.
3. Implement post-calculation email capture with the 7-day automated sequence.
4. Set up the free account creation gate for saving calculations and logging compounds — the conversion gate should be frictionless and preserve user input.
5. Define and implement the free/paid feature split before any upgrade messaging goes live — users who encounter an upgrade prompt on a feature they care about will convert; users who encounter a wall before they've found value will leave.
6. Launch the 5-step in-app feature discovery sequence on day one of account creation.
7. Instrument every conversion step for measurement: calculator-to-email, email-to-account, account-to-first-compound, account-to-paid. These four conversion rates are the entire funnel health summary.

The funnel is not complex. It is honest. Value first. Friction only where value has been demonstrated. Upgrade only when the user has already decided the product is worth it.

---

*This document is part of the BioStack commercialization series. Related documents: 01-pricing-and-packaging.md, 02-onboarding-and-compliance-ux.md, 03-landing-page-copy.md*

## Funnel Intensification Flow
Flow: Search / curiosity -> Calculator -> Immediate insight -> Account creation -> Protocol setup -> Aha moment -> Upgrade prompt.
