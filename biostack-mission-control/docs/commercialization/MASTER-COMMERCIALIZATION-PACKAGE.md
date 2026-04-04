# BioStack Mission Control — Master Commercialization Package

**Version:** 1.0  
**Date:** April 2026  
**Status:** Canonical — Supersedes all individual workstream documents where contradictions exist  
**Synthesized from:** Documents 01–08 (pricing, onboarding, landing page, FAQ, glossary, SEO, lead gen, implementation)

---

> **How to use this document:**  
> Founders — read Section A first.  
> Engineers — go directly to Section I.  
> Designers — Sections C and D are your brief.  
> Content writers — Sections E, F, G are your queue.  
> Legal — Section J requires your review before launch.

---

## Section A — Executive Summary

### What BioStack Is

BioStack Mission Control is a personal protocol intelligence platform built for serious self-experimenters who manage complex compound stacks — peptides, pharmaceuticals, coenzymes, and supplements. It is not a supplement reminder app, a generic health tracker, or a symptom journal. It is purpose-built infrastructure for people who treat their protocols like systems: precision dose math, pathway overlap detection, evidence-tiered compound intelligence, and a unified timeline that turns scattered data into observable patterns.

### Core Positioning (3 sentences)

BioStack occupies the gap between a spreadsheet and a clinical system — sophisticated enough to handle the complexity of a real compound stack, precise enough to be trusted for dose math, and evidence-aware enough to function as a research companion. The target user already knows what they are doing; they simply lack a tool that keeps up with them. BioStack is that tool: intelligent, non-prescriptive, and built on the principle that clarity is safer than guessing.

### Top 5 Strategic Decisions Across All Workstreams

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | **Free calculators are the acquisition wedge.** The Reconstitution Calculator is public, no account required, and the single highest-intent entry point in the funnel. Never gate it. | All 7 workstreams converge on this. The calculator is both a product feature and a lead magnet. |
| 2 | **Three tiers: Observer (Free), Operator ($12/$96), Commander ($29/$228).** Operator is the primary revenue target. Commander is the intelligence upsell. | Naming is final and must be consistent everywhere. Tier names confirmed canonical across all 8 documents. |
| 3 | **Hard paywall on in-app calculators; soft prompts everywhere else.** The moment a user needs reconstitution math inside the app and lacks Operator access is the highest-converting upgrade moment. | Conversion logic from doc 01, validated by funnel design in doc 07. |
| 4 | **Compliance is a product feature, not legal boilerplate.** The non-prescriptive, evidence-tiered framing is BioStack’s trust moat — and it is also the signal AI answer engines weight heavily when selecting citation sources. | Docs 02, 04, 05, 06 all align on this. |
| 5 | **Content is compounding infrastructure.** Calculator pages, glossary terms, and compound reference pages are both SEO assets and in-product features. Build them with AEO structure from day one; retrofitting is expensive. | Docs 05, 06, 07 align on this architecture. |

### The Single Most Important Thing to Do First

**Ship the auth layer and the public Reconstitution Calculator page simultaneously.**

Without auth, there is no revenue path. Without the public calculator, there is no top-of-funnel. These are the two P0 items that unblock everything else. They can be built in parallel. Neither should wait for the other.

---

## Section B — Final Pricing & Packaging

> **Full detail:** `01-pricing-and-packaging.md`

### Recommended Tier Model

| Tier | Price (Monthly) | Price (Annual) | Effective Monthly | Target User |
|------|-----------------|----------------|-------------------|-------------|
| **Observer** (Free) | $0 | $0 | $0 | New users, explorers, casual trackers |
| **Operator** (Pro) | $12/mo | $96/yr | $8.00 | Serious self-experimenters — the primary revenue tier |
| **Commander** (Elite) | $29/mo | $228/yr | $19.00 | Power users, advanced researchers, data-obsessed protocol builders |

Annual is the default-selected billing option on the pricing page. The $8/month effective rate for Operator annual is the primary price anchor.

### Feature Gating Summary

| Feature | Observer | Operator | Commander |
|---------|----------|----------|-----------|
| Compound tracking | 5 active (hard cap) | Unlimited | Unlimited |
| Reconstitution Calculator (in-app) | No — hard paywall | Yes | Yes |
| Volume Calculator (in-app) | No — hard paywall | Yes | Yes |
| Unit Conversion | Yes (always free) | Yes | Yes |
| Check-in fields | 3 of 16 | All 16 | All 16 |
| Protocol Phases | No | Yes | Yes |
| Pathway Overlap Checker | Read-only summary | Full analysis | Full analysis |
| Knowledge Base | Read-only browse | Full access | Full access |
| Timeline | Last 30 days | Unlimited history | Unlimited history |
| Data export (CSV/JSON) | No | Yes | Yes |
| AI Protocol Analysis | No | No | Yes |
| Cross-session trend correlation | No | No | Yes |
| Side effect pattern detection | No | No | Yes |
| Priority support | No | No | Yes (24hr SLA) |

**Note on public calculators:** The Reconstitution, Volume, and Unit Conversion calculators are also available as fully public, no-account-required pages at `/tools/`. The in-app calculator within the authenticated dashboard is what is gated. This distinction is intentional and critical — the public tool drives acquisition; the in-app tool drives conversion.

### The Key Conversion Logic

**Observer to Operator** — three natural paywall moments, in order of conversion probability:
1. User opens the in-app Reconstitution or Volume Calculator — immediate hard gate with upgrade prompt
2. User attempts to add compound #6 — blocked, upgrade prompt
3. User has logged 5–10 check-ins on the limited field set — soft prompt to unlock all 16 fields

**Operator to Commander** — three natural upsell moments:
1. After 30 days of check-ins — AI Analysis banner appears in Timeline view
2. Recurring side effect logged 3x in 7 days — pattern detection prompt
3. After a completed protocol phase — PDF report generation prompt

### Stripe Implementation Summary

**Products:** `BioStack Operator` and `BioStack Commander` (Observer requires no Stripe product)

**Prices:**

| Label | Amount | Interval | Trial |
|-------|--------|----------|-------|
| Operator Monthly | $12.00 | monthly | 7 days (optional — decision required, see Section J) |
| Operator Annual | $96.00 | yearly | 7 days (optional) |
| Commander Monthly | $29.00 | monthly | None |
| Commander Annual | $228.00 | yearly | None |

**Required webhooks at launch:** `customer.subscription.created`, `customer.subscription.updated`, `customer.subscription.deleted`, `invoice.payment_failed`, `invoice.payment_succeeded`

**Entitlement map:**
- `active` / `trialing` / `past_due` (7-day grace) — full tier access
- `canceled` / `unpaid` — revert to Observer

**Proration:** Enable for upgrades; disable for downgrades (effective at period end).

**B2B / Clinic tier:** Do not build until 1,000+ paying Operator users exist. Full outline in `01-pricing-and-packaging.md` Section 5.

---

## Section C — Final Onboarding Flow

> **Full detail:** `02-onboarding-and-compliance-ux.md`

### Recommended Flow

6-screen wizard, full-screen modal overlay on blurred dashboard background. Onboarding state persisted server-side — completing on mobile means not seeing it again on desktop.

```
[Screen 1: Welcome]  →  [Screen 2: Consent]  →  [Screen 3: Profile]
     →  [Screen 4: Goals]  →  [Screen 5: First Compound]  →  [Screen 6: Dashboard Reveal]
```

### Screen-by-Screen Specification

**Screen 1 — Welcome**
- Headline: `Your protocol. Observed.`
- Subtext: `BioStack Mission Control is your personal bio-intelligence system — purpose-built to track compounds, compute doses, and surface evidence-grade insights across your active stack. This is where precision meets accountability.`
- CTA: `Enter Mission Control →`
- Footer microcopy: `Built for researchers, athletes, and advanced self-experimenters. Not a medical service.`

**Screen 2 — Boundary Setting + Consent** (NOT skippable — both checkboxes required)
- Headline: `One important thing before we begin.`
- Body: Explains BioStack’s role clearly — tracking tool, not prescribing service. Includes a “What BioStack does / does not do” two-column card.
- Checkbox 1: `I understand that BioStack is a tracking and research tool, not a medical service. I’m using it to observe and manage my own protocols, and I take full responsibility for my choices.`
- Checkbox 2: `I confirm that I am 18 years of age or older and that I have read and agree to BioStack’s Terms of Service and Privacy Policy.`
- CTA (disabled until both checked): `I understand — let’s go`
- Server action: Store `consent_accepted_at`, `consent_version`, `tos_version` on user record.

**Screen 3 — Profile Creation** (skippable except display name)
- Headline: `Set your baseline.`
- Fields: Display name, Biological sex, Date of birth, Height (cm/ft toggle), Weight (kg/lbs toggle), Activity level
- Privacy note: `Your data stays yours. BioStack does not sell, share, or monetize your personal health information.`
- CTA: `Set My Baseline →`

**Screen 4 — Goal Selection** (skippable)
- Headline: `What are you optimizing for?`
- Multi-select tile grid: Muscle & Strength / Fat Loss & Body Composition / Recovery & Injury Repair / Cognitive Performance / Longevity & Healthspan / Hormonal Optimization / Sleep & Stress Resilience / Research & Tracking Only
- CTA: `Continue →`

**Screen 5 — First Compound** (skippable)
- Headline: `What’s in your stack right now?`
- Fields: Compound name (search), Category, Status, Started on, Source (optional), Goal (optional)
- Skip link: `Skip for now — I’ll add compounds from the dashboard →`

**Screen 6 — Dashboard Reveal**
- Animation: Modal fades out, dashboard fades in with a brief emerald pulse
- Welcome banner: `Welcome to Mission Control. Your baseline is set. Everything from here is signal.`
- Empty state (no compound added): CTA `+ Add Compound` and secondary `Explore the Knowledge Base →`
- Empty state (compound added): CTA `+ Log Check-In` and secondary `View Compound Intelligence →`

### Disclaimer Approach and Consent Wording

Disclaimers are embedded at the moment of context — not relegated to footers. Key placements:

| Location | Form | Content |
|----------|------|---------|
| Calculator output | Single muted line below result | `Calculated result. Verify against your source material before use. BioStack calculations are mathematical utilities, not clinical guidance.` |
| Knowledge Base pages | Sticky top banner, emerald-left-border | `The following information is evidence-referenced research. It is not medical advice and does not constitute a recommendation to use this compound.` |
| Pathway Overlap results | Inline alert card, amber-left-border | `The following flags are informational only. Pathway overlap does not mean co-administration is harmful — it means you should investigate and take appropriate precautions.` |
| First check-in only | Dismissible callout | `Check-ins log subjective self-reported data. BioStack tracks trends — it does not diagnose, interpret, or assess your health clinically.` |
| Settings footer | Legal paragraph | `BioStack Mission Control is a personal tracking and research tool. It is not a medical device, does not provide medical advice, and is not a substitute for professional medical consultation.` |

**Soft Legal Language Approach — replace hard legal phrases with plain-English equivalents:**

| Avoid | Use instead |
|-------|-------------|
| “This does not constitute medical advice.” | “This is research intelligence, not clinical guidance.” |
| “Use at your own risk.” | “Your protocol, your decisions. We give you the data.” |
| “Not intended to diagnose, treat, cure, or prevent any disease.” | “BioStack tracks and informs. It doesn’t diagnose or prescribe — that’s not what it’s for.” |
| “Drug interactions may be harmful.” | “Pathway overlap flagged. Worth reviewing before you proceed.” |
| “Results may vary.” | “Self-reported data is subjective — trends matter more than single data points.” |

### Implementation Notes

- Onboarding completion state: persisted to user account (not localStorage)
- Consent: timestamped server-side with version tracking (`consent_accepted_at`, `consent_version`, `tos_version`)
- Re-consent: If ToS version changes, show a single-screen re-acknowledgment modal on next login — not the full flow
- WCAG 2.1 AA compliance required on all screens even within dark theme
- Mobile-first; Screen 5 compound preview panel collapses on mobile; minimum tap target 44x44px

**Target Metrics:**

| Metric | Target |
|--------|--------|
| Welcome to Consent completion | >85% |
| Overall funnel completion | >65% |
| Consent screen drop-off | <15% |
| Day-7 retention (completed onboarding) | >40% |

If consent screen drop-off exceeds 15%, revisit framing — not legal substance.

---

## Section D — Final Landing Page Copy

> **Full detail:** `03-landing-page-copy.md`

**Resolved contradiction:** The pricing teaser section of doc 03 (Section 8) describes “no feature-gating by tier” and “no upsells mid-protocol.” This directly conflicts with the three-tier model. The corrected pricing teaser is included below. All other sections are approved as written.

### Full Page in Section Order

---

**SECTION 1: HERO**

Headline: Protocol Intelligence for People Who Take This Seriously.

Subheadline: Track compounds with precision. Detect pathway overlaps. Spot patterns in your data. BioStack is the operating system your protocol has been missing.

Primary CTA: Enter Mission Control  
Secondary CTA: Try the Calculators Free →

Supporting line: Built for serious self-experimenters who manage complex stacks and need more than a notes app.

---

**SECTION 2: PROBLEM** — Label: THE CURRENT STATE

Headline: Your protocol is more complex than your system for tracking it.

You’ve done the research. You know the compounds. You’ve read the papers. But somewhere between managing your stack, logging your check-ins, and trying to understand what’s actually working — the system breaks down.

You’re keeping notes in a spreadsheet that doesn’t calculate doses. You’re trying to remember when you started a compound three cycles ago. You’re stacking three protocols that may or may not overlap — and you have no clean way to check. You’re looking at a week of data that means nothing because it’s scattered across four apps and a voice memo.

This isn’t a knowledge problem. It’s a tooling problem.

**The people who get the most out of complex protocols are the ones who treat them like systems.** BioStack was built to close that gap.

---

**SECTION 3: SOLUTION** — Label: WHAT BIOSTACK PROVIDES

Headline: One system. Every layer of your protocol.

**Precision math** for dose calculation and unit conversion. **Pathway intelligence** that detects overlaps and interaction flags across your active stack. **A unified timeline** that makes every event — every compound start, stop, phase change, and check-in — visible and navigable in one place. Finally, a system that keeps up with your stack.

---

**SECTION 4: FEATURE BLOCKS** (8 features — full copy in `03-landing-page-copy.md` Section 4)

1. Compound Tracking — Every compound in your stack. Fully documented.
2. Pathway Overlap Intelligence — See the interactions your stack is creating — before they become a problem.
3. Reconstitution & Volume Calculators — Math you can trust. Every injection, every time.
4. Bio-Intelligence Knowledge Base — Evidence-aware data on hundreds of compounds.
5. Daily Check-In Tracking — Log 15+ biomarkers daily. Turn subjective experience into analyzable data.
6. Unified Protocol Timeline — Every event. One chronological stream. Total pattern visibility.
7. Protocol Phases — Structure your protocol like the experiment it is.
8. Unit Conversion Engine — Precision conversions across every unit your protocol uses.

---

**SECTION 5: CALCULATOR SPOTLIGHT** — Label: PRECISION TOOLS

Headline: The calculators that should have existed years ago.

- Reconstitution Calculator: mg powder + mL diluent → exact mcg/mL concentration + injection guide
- Volume Calculator: target dose in mcg + concentration → exact mL to draw
- Unit Conversion: mg, mcg, IU, g — precise, immediate, always available

These tools are available free. Try them before you commit to anything else.

CTA: → Try the Calculators Free

---

**SECTION 6: TRUST & SAFETY** — Label: BUILT ON CLARITY

Headline: BioStack is not a doctor. It’s something more useful for what you’re actually doing.

BioStack doesn’t prescribe, diagnose, or recommend. The knowledge base cites evidence tiers. The overlap engine surfaces flags, not verdicts. The calculators give you math, not interpretation. You bring the research. BioStack gives you the infrastructure to use it seriously.

*BioStack is a personal tracking and intelligence tool. It is not a medical device, diagnostic tool, or source of clinical advice. Always work with qualified professionals for medical decisions.*

---

**SECTION 7: SOCIAL PROOF** — Label: WHO USES BIOSTACK

Headline: Built for the people who already know what they’re doing — and need the tools to match.

[3 testimonial blocks — populate with real user quotes before launch. Do not publish with placeholder text.]

[Stats bar — populate when data is available: compounds tracked, protocols active, check-ins logged, evidence tiers on database entries]

---

**SECTION 8: PRICING TEASER** — Label: PLANS (CORRECTED)

Headline: One platform. Built for serious use.

Free to start with Observer — includes unit conversion, basic tracking, and knowledge base preview. Upgrade to Operator ($12/month, or $8/month on the annual plan) for the full protocol workflow: unlimited compounds, precision calculators, all 16 check-in fields, pathway overlap analysis, and protocol phases. Commander ($29/month, or $19/month annually) adds AI-assisted protocol analysis and advanced pattern detection.

CTA: → See full pricing

---

**SECTION 9: FAQ TEASER**

5 questions inline — sourced from the Featured FAQ Set in Section E of this document.

---

**SECTION 10: LEAD MAGNET CTA** — Label: START HERE

Headline: Not ready to commit to a full protocol setup? Start with the calculators.

No account required. No commitment. Just the math, done right.

CTA: → Open the Calculators

If the precision matters to you, the rest of BioStack will too.

---

**SECTION 11: FINAL CTA**

Headline: Your protocol is already this complex. Your system should be too.

Precision math. Pathway intelligence. Daily observability. A unified timeline that turns protocol data into pattern recognition. Every compound documented. Every check-in logged. Every event visible. This is what protocol intelligence looks like.

CTA: → Enter Mission Control

*Free to start. No credit card required. Full platform access on paid plans.*

Footer trust line: BioStack Mission Control — Protocol intelligence for serious self-experimenters. Not medical advice. Not a diagnostic tool. Not a prescribing system.

---

## Section E — FAQ System

> **Full FAQ page content (all 10 categories, 40+ questions, full answers):** `04-faq-system.md`

### Featured FAQ Set (Homepage / Landing Page — 10 Q&As)

**Q: Is BioStack medical advice?**  
No. BioStack is an organizational and observational tool. It helps you track what you take, calculate doses accurately, and surface evidence from scientific literature. BioStack does not recommend compounds, suggest dosages, or provide clinical guidance. Always consult a qualified healthcare professional before starting or modifying any compound protocol.

**Q: What does BioStack actually do?**  
BioStack is a personal bio-protocol operating system. It lets you log compounds, calculate injection math, detect pathway overlaps between stacked compounds, track daily check-ins across 15+ biomarkers, and correlate patterns over time in a unified timeline. It organizes everything your protocol generates into one intelligent system.

**Q: Who is BioStack designed for?**  
BioStack is built for serious self-experimenters who manage complex compound protocols — peptides, coenzymes, pharmaceuticals, supplements — and need more than a spreadsheet. If you’ve ever lost track of when you started a compound or tried to calculate a reconstitution dose in your head, BioStack is for you.

**Q: What is the Reconstitution Calculator?**  
The Reconstitution Calculator handles the math for dissolving lyophilized (freeze-dried) powders into injectable solutions. Enter the compound amount in milligrams and your diluent volume in milliliters — BioStack instantly outputs concentration in mcg/mL plus injection volume for your target dose. No manual math, no calculation errors.

**Q: What are evidence tiers?**  
Evidence tiers classify how well-supported a compound’s effects are in scientific literature. BioStack uses four levels: Strong, Moderate, Limited, and Mechanistic. Each tier tells you how much human trial evidence exists, so you can read Knowledge Base entries with the right expectations built in.

**Q: Is there a free plan?**  
Yes. The Observer plan is free forever and includes compound tracking (up to 5 active), basic Knowledge Base browsing, check-in logging, and access to the Unit Conversion Calculator. No credit card required. Upgrade to Operator for the full protocol workflow including the in-app precision calculators.

**Q: What is Pathway Overlap detection?**  
Pathway Overlap detection analyzes your compound stack and identifies when two or more compounds share the same biological pathways. When overlaps are found, BioStack surfaces interaction flags and evidence confidence levels — giving you a clearer picture of what may be compounding or competing in your stack.

**Q: Where is my data stored?**  
Your BioStack data is stored securely and is private to your account. BioStack does not sell personal data, does not share protocol information with third parties, and treats your compound log and check-in history as strictly confidential. Full data export and deletion are supported.

**Q: How is BioStack different from a spreadsheet?**  
Spreadsheets don’t calculate reconstitution math, don’t detect pathway overlaps, don’t have a compound Knowledge Base, and don’t surface patterns in your check-in data. BioStack does all of that in one place, structured for protocols — not general data entry. The difference is protocol intelligence versus passive data storage.

**Q: What daily metrics can I track?**  
BioStack’s daily check-in covers 15+ metrics: weight, sleep quality, energy levels, appetite, recovery, focus, thought clarity, skin quality, digestion, strength, endurance, joint pain, eyesight, mood, and side effects. All data appears in your unified timeline, aligned with compound start and stop dates for direct pattern correlation.

### FAQ Category Map

| # | Category | Primary Intent |
|---|----------|----------------|
| 1 | Trust & Safety | Establish legitimacy, handle legal and medical concerns |
| 2 | What BioStack Is | Orient new visitors, differentiate from alternatives |
| 3 | Getting Started | Reduce onboarding friction, drive activation |
| 4 | Calculators | Explain core math tools, unlock utility-driven users |
| 5 | Knowledge Base | Surface compound intelligence, support research-phase users |
| 6 | Check-ins & Tracking | Explain observability features, drive daily habit formation |
| 7 | Protocol Design | Explain stack and phase logic, support advanced users |
| 8 | Pricing & Account | Remove purchase friction, support conversion |
| 9 | Data & Privacy | Handle data trust concerns |
| 10 | Advanced Features | Reward power users, differentiate technically |

---

## Section F — Glossary & Content Moat

> **Full glossary term list (50+ terms), page templates, definition standards, and topic cluster maps:** `05-glossary-architecture.md`

### Top 15 Money Pages to Build First

| Priority | Term | Search Vol | Commercial Value | Rationale |
|----------|------|-----------|-----------------|-----------|
| P1 | Reconstitution | High | High | Calculator flagship term — highest intent |
| P1 | mcg vs mg | High | High | Enormous FAQ search, calculator bridge |
| P1 | Peptide | High | High | Top-of-funnel anchor term |
| P1 | BPC-157 | High | High | #1 searched peptide globally |
| P1 | GLP-1 | High | Medium | Mainstream crossover, massive search volume |
| P1 | NAD+ | High | Medium | Mainstream, mitochondrial crossover |
| P2 | Half-life | High | High | Dosing logic, schedule builder bridge |
| P2 | Lyophilized powder | High | High | Pairs with reconstitution |
| P2 | Bacteriostatic water | High | High | Common prep query, calculator bridge |
| P2 | Protocol | High | High | Product identity term |
| P2 | TB-500 | High | High | #2 most searched peptide |
| P2 | Bioavailability | High | Medium | Foundational pharmacokinetics term |
| P2 | Loading phase | Medium | High | Maps directly to Protocol Phase product feature |
| P2 | Wash-out period | Medium | High | Protocol design intelligence concept |
| P2 | Compound stack | Medium | High | BioStack identity term |

### Glossary Page Template Summary

Every glossary page follows four content layers, in order:
1. **Schema layer** (under 25 words) — AEO extraction target; must stand alone as a complete definition
2. **Callout layer** (1–2 sentences) — bolded quote at the top of the page
3. **Body layer** (2–4 paragraphs) — full educational treatment with context; no clinical framing; “researchers working with” not “you should”
4. **FAQ layer** — 3 self-contained Q&As; each answer complete without referencing the question

Every page must include a “How BioStack Handles [Term]” section with a direct product feature link. Full template specification in `05-glossary-architecture.md` Section 4.

### Internal Linking Strategy Summary

Four-tier content graph with defined directional flow:

- **Calculator pages** (Tier 1): receive links from everything; link out to glossary terms, compound pages, and signup. Protect conversion path — do not link promiscuously.
- **Compound pages** (Tier 2): entity hubs; link to glossary terms, related compounds, calculators, and pathway pages
- **Glossary pages** (Tier 3): connective tissue; each links to 3–5 related terms, the most relevant calculator, and at least one compound page
- **Educational articles** (Tier 4): authority layer; link to glossary (every first technical term use), compound pages, calculators, and product features

Five linking chains to maintain:
1. Reconstitution chain: reconstitution → lyophilized powder → bacteriostatic water → diluent → concentration → mcg vs mg
2. Protocol chain: protocol → protocol phase → loading phase → maintenance dose → wash-out period → tolerance
3. Pharmacokinetics chain: half-life → bioavailability → dosage → tolerance → receptor upregulation
4. Evidence chain: evidence tier → evidence-based → mechanistic evidence → clinical evidence → biomarker
5. Stack Intelligence chain: compound stack → pathway overlap → synergy → interaction flag → timeline correlation

---

## Section G — SEO / AEO / GEO Plan

> **Full strategy including keyword clusters per pillar, metadata framework, AEO content standards, and structured data specs:** `06-seo-aeo-geo-strategy.md`

### 7 Content Pillars Summary

| # | Pillar | Topic Focus | Product Alignment |
|---|--------|-------------|-------------------|
| 1 | Compound Intelligence | Per-compound reference: mechanism, evidence, pharmacokinetics, interactions | Compound tracking, Knowledge Base, Evidence Tiers |
| 2 | Bio-Mathematics & Protocol Calculations | Reconstitution, volume, unit conversion math and how-to content | All three calculators |
| 3 | Protocol Architecture & Phase Management | How to design, structure, and manage protocols over time | Protocol Phase Management, Timeline |
| 4 | Pathway Science & Biological Systems | mTOR, GH/IGF-1, collagen synthesis, mitochondrial biogenesis | Pathway Overlap Detection, Knowledge Base |
| 5 | Observability & Self-Experimentation Methodology | How to track, measure, and interpret biometric and subjective data | Daily Check-In, Unified Timeline |
| 6 | Evidence Literacy & Research Interpretation | How to read compound research; evidence tiers explained | Evidence Tiers system, Knowledge Base citations |
| 7 | Tools, Calculators & Reference Utilities | Standalone calculator and utility pages with SEO/AEO architecture | All three calculators and future tools |

### 30/60/90-Day Publishing Roadmap

**Day 1–30: Foundation Layer (must exist at launch)**
- All 3 calculator pages with full AEO structure — FAQPage schema, HowTo schema, inline glossary links
- Core glossary: 20 essential terms with DefinedTerm schema
- Top 10 compound pages: BPC-157, TB-500, Semaglutide, Tirzepatide, Sermorelin, Ipamorelin, MK-677, NAD+, Epithalon, PT-141
- About page and Evidence Tiers explainer page
- Submit sitemap; verify Google Search Console

**Day 31–60: Authority Building Layer**
- Compound pages: expand to top 30 (add CJC-1295, GHRP-2, GHRP-6, Selank, Semax, AOD-9604, GHK-Cu, and others based on early search signals)
- All 7 pillar hub pages
- 3 FAQ mega-pages: peptide reconstitution, biohacking protocol tracking, understanding compound evidence
- 5–8 pathway science explainers: GH/IGF-1 axis, mTOR signaling, collagen synthesis, melanocortin system, GLP-1 pathway, NAD+ metabolism
- 2–3 comparison pages: BioStack vs. spreadsheet, BioStack vs. generic health app

**Day 61–90: Scale and Compounding Layer**
- Compound pages: expand to 60+
- 12+ educational articles (1–2/week), answer-first format, targeting how-to and explainer queries in the 7 pillars
- Structured data audit across all pages; validate in Google Rich Results Test
- AEO optimization pass on top 30 compound pages: rewrite first paragraphs to BLUF format, add AI-queryable H2s
- Community seeding: 10–15 high-traffic forums with calculator and compound page links as canonical reference answers

### Top 10 Highest-Leverage First Moves

1. Publish all three calculator pages with full AEO structure and JSON-LD schema
2. Implement FAQPage and HowTo JSON-LD on all calculator pages at launch — not as a retrofit
3. Publish the 20-term core glossary with DefinedTerm schema
4. Publish top 10 compound reference pages with consistent section templates
5. Publish an Evidence Tiers explainer page and link it from every compound page
6. Submit sitemap and verify Search Console on Day 1
7. Implement BreadcrumbList schema site-wide as part of initial technical setup
8. Write BLUF (Bottom Line Up Front) first paragraphs for every compound and calculator page — answer first, context second
9. Build the “BioStack vs. spreadsheet” comparison page
10. Seed calculator and compound pages in top 5 relevant online communities (Reddit r/Peptides, r/nootropics, r/biohacking, relevant Discord servers)

### Structured Data Priority List

| Schema Type | Pages | Priority |
|-------------|-------|----------|
| FAQPage | All compound pages, all calculator pages, FAQ page | High — implement at launch |
| HowTo | All calculator how-to content, reconstitution and protocol guides | High — implement at launch |
| DefinedTerm | Every glossary page | High — implement at launch |
| BreadcrumbList | Every page except homepage | High — implement at launch |
| SoftwareApplication | Main product page, calculator tool pages | Medium — implement at launch |
| Article | All blog posts, educational deep-dives | Medium — implement at content launch |
| MedicalWebPage | All compound knowledge base pages | Medium — implement at KB launch; important for E-E-A-T scoring |

---

## Section H — Lead Gen Funnel

> **Full funnel detail including all email capture touchpoints, 7-day email sequence, referral mechanics, and full retention sequences:** `07-lead-gen-funnel.md`

### Recommended Lead Magnet

**”The Reconstitution & Dosing Reference Card” (PDF)**

A one-page, printable reference card covering common reconstitution ratios, BAC water volumes, storage guidelines, quick-reference dosing math, the reconstitution formula, unit conversion shortcuts, and a blank protocol log template.

**Trigger:** Post-calculation on the public Reconstitution Calculator page. After the user sees their result, display: “Save this calculation and get the full reference card. Drop your email — takes 5 seconds.”

**Email delivery:** PDF link + single CTA: “Your free BioStack account saves every calculation automatically.”

**Secondary sequence:** After PDF download, auto-enroll in a 5-email “Protocol Intelligence Starter” sequence over 7 days: (1) evidence tiers, (2) check-in baseline tracking, (3) pathway overlap checker, (4) protocol phases, (5) worked 30-day example + account creation CTA.

### Funnel Map: First Visit to Paid

| Stage | Goal | Primary Touchpoint | Hook |
|-------|------|--------------------|------|
| Awareness | Get discovered | Organic search to calculator or compound pages | “The math, done right. Free to use.” |
| Interest | Tool engagement | Calculator interaction + results display | “This is more than a calculator — this is how serious protocol builders work.” |
| Consideration | Understand broader value | Post-calculation CTA + email sequence | “Everything you track in your head, tracked automatically. Free to start.” |
| Intent | Create account | Direct CTAs, email day 7, return visit | “Your first compound takes 30 seconds to log. Your protocol timeline starts now.” |
| Conversion (Free) | Log first compound + first saved calculation | Onboarding sequence + in-app prompts | “You’ve logged [Compound]. Now set your first check-in baseline.” |
| Conversion (Paid) | Hit a free tier limit or discover a paid feature | In-app upgrade prompts at natural paywall moments | “You’ve logged [X] check-ins. Upgrade to see your trends and export your protocol.” |
| Retention | Daily/weekly value habit | Check-in loop, calculation ritual, protocol milestone emails | “Your protocol is [X] days in. Here’s what your check-in data shows.” |

The calculator is never gated. The conversion hook is always downstream of value delivery.

### Aha Moment Design

```
0–5 sec   Land on /tools/reconstitution-calculator from search. Calculator visible. No login. No modal.
5–20 sec  Enter values: 5mg powder, 2mL diluent. Hit Calculate.
20–25 sec Result: 2,500 mcg/mL. Below: “Each 0.1mL withdrawal = 250mcg.” Full formula shown.
25–40 sec User reads result. Anxiety resolved. Trust built by transparent math.
40–55 sec Post-calc prompt: “Want to save this? Log it to a protocol and never recalculate from scratch.”
55–60 sec Email capture or “Create Free Account.” Cold visitor to engaged lead in under 60 seconds.
```

Stickiness is earned by zero friction to value, transparent math, an anxiety-resolution moment, and an invitation (not a demand) to go deeper.

### Top 3 Retention Hooks

1. **Daily check-in streak + compounding value signal.** A single check-in is low value. Thirty consecutive check-ins produce a personal dataset that reveals actual protocol performance. Surface this explicitly: “Day 7. 5 check-ins logged. 25 more to see your first trend analysis.” Loop: log → see data accumulate → want to see the trend → log again.

2. **Calculation ritual.** Every reconstitution event logged creates a permanent record. Users who reconstitute regularly return to BioStack for every mixing session. The calculator is not a one-time tool — it is a repeated behavior anchored to a real-world action.

3. **Knowledge Base as embedded decision-support.** Users with active compounds return to BioStack to check interaction flags when considering new additions. The Knowledge Base becomes part of their research process — not a feature visited once.

**Re-engagement email triggers:**

| Trigger | Timing | Subject |
|---------|--------|---------|
| No check-in logged | 3 days after last | “Your protocol is running — are you tracking it?” |
| Protocol phase reaching day 14 | Automatic | “Two weeks in. What does your data show?” |
| New compound added, no baseline | 48 hours | “You logged [Compound]. Did you set a baseline?” |
| Account inactive | 7 days | “Your protocol timeline is waiting.” |
| Approaching free tier limit | At 80% | “You’ve logged [X] compounds. Here’s what Operator unlocks.” |
| 30-day protocol milestone | Automatic | “30 days of [Protocol Name]. Here’s your timeline.” |

---

## Section I — Implementation Plan

> **Full engineering tickets, acceptance criteria, API routes, and design/copy/SEO handoff checklists:** `08-implementation-handoff.md`

### Priority Matrix P0/P1/P2 Summary Table

**P0 — Must exist before any user can transact:**

| Item | Epic |
|------|------|
| User registration + login | Auth |
| JWT auth middleware | Auth |
| Data isolation (user_id on all records) | Auth |
| First-run detection + onboarding routing | Onboarding |
| Onboarding wizard (6 steps) | Onboarding |
| Disclaimer acceptance (server-side, timestamped) | Onboarding |
| Landing page (`/`) | Marketing |
| Pricing page (`/pricing`) | Marketing |
| Stripe Checkout integration | Stripe |
| Stripe webhook handler | Stripe |
| Subscription status on user record | Stripe |
| Feature gating middleware (`FeatureGate` component + server-side check) | Stripe |
| `robots.txt` | SEO |

**P1 — Must exist before public launch:**

| Item | Epic |
|------|------|
| Password reset flow | Auth |
| `sitemap.xml` (dynamic) | SEO |
| FAQ page (`/faq`) | Marketing |
| Terms of Service (`/terms`) | Legal |
| Privacy Policy (`/privacy`) | Legal |
| Public Reconstitution Calculator (`/tools/reconstitution-calculator`) | Tools |
| Public Volume Calculator (`/tools/volume-calculator`) | Tools |
| Email capture on public tools (`POST /api/leads/capture`) | Tools |
| JSON-LD structured data (FAQPage, HowTo, Product, WebSite) | SEO |
| Open Graph + Twitter card metadata | SEO |
| Stripe Customer Portal link | Stripe |

**P2 — Post-launch organic growth:**

| Item | Epic |
|------|------|
| Public Unit Converter (`/tools/unit-converter`) | Tools |
| Email verification | Auth |
| Glossary index + term pages (MDX-powered) | Content |
| Blog / educational content MDX pipeline | Content |

### Critical Path: Fastest Route to Revenue

```
Phase 1 (Auth) → Phase 2 (Marketing Pages) → Phase 3 (Stripe) → Phase 4 (Onboarding) → GO-LIVE ELIGIBLE
                                                                                                ↑
Phases 5–7 (Tools, SEO Infrastructure, Content) run in parallel after Phase 1 completes
```

### Phase 1–4 Build Order

**Phase 1 — Foundation** (unblocks everything)
1. DB migrations: Users table + `user_id` FK on all existing tables (`Profiles`, `Compounds`, `CheckIns`, `ProtocolPhases`, `KnowledgeEntries`)
2. Auth endpoints: `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout`, `POST /api/auth/refresh`
3. JWT middleware: validate token, attach user context to all protected routes via `ICurrentUserService`
4. Scope all existing API endpoints to authenticated user
5. Frontend auth state: `AuthContext`, login/register pages, `middleware.ts` route guards

**Phase 2 — Public Marketing Layer** (unblocks conversion)
1. Next.js `(marketing)` route group with public nav layout
2. Landing page (`app/(marketing)/page.tsx`) — copy from doc 03 with pricing section corrected per Section D
3. Pricing page — Observer/Operator/Commander cards, monthly/annual toggle, annual default-selected, full feature comparison table
4. Terms of Service and Privacy Policy pages (legal copy needed — see Section J)

**Phase 3 — Stripe Integration** (unblocks revenue)
1. Create products and prices in Stripe Dashboard; store 4 price IDs in environment variables
2. `POST /api/billing/checkout-session` — lookup/create Stripe Customer, return checkout URL
3. `POST /api/billing/webhook` — handle subscription lifecycle; verify signature before processing
4. Add `subscription_tier` + `stripe_customer_id` to Users table
5. `useSubscription` hook + `FeatureGate` component + `featureFlags.ts` tier map
6. `GET /api/billing/portal-session` — Stripe Customer Portal link

**Phase 4 — Onboarding** (unblocks trust + retention)
1. First-run detection: `GET /api/users/me` returns `onboarding_complete: boolean`
2. Onboarding wizard component (6 screens, modal overlay, blurred dashboard background)
3. `POST /api/users/onboarding` — validates `disclaimerAccepted: true`, stores profile, stores first compound, sets `onboarding_complete`
4. Post-onboarding redirect to dashboard

**Phase 5 — Public Tools** (parallel with Phases 2–4)
1. `(tools)` route group with minimal public layout
2. `/tools/reconstitution-calculator` — no auth, post-calc email capture prompt, `HowTo` JSON-LD
3. `/tools/volume-calculator` — same pattern
4. `POST /api/leads/capture` — stores email + source tag; idempotent on duplicate

**Phase 6 — SEO Infrastructure** (parallel)
1. `robots.txt`, `sitemap.xml` (includes all public marketing and tool routes; excludes app routes)
2. Base metadata layout (OG, Twitter cards, canonical URL, `noindex` on app routes)
3. JSON-LD components: `FaqSchema`, `HowToSchema`, `DefinedTermSchema`, `ProductSchema`, `WebSiteSchema`
4. FAQ page with `FAQPage` JSON-LD

**Phase 7 — Content Layer** (post-launch)
1. Glossary index and term pages (MDX-powered via `content/glossary/[term].mdx`)
2. MDX pipeline for blog and educational content

### Launch Checklist (Top 20 Go-Live Gates)

**Authentication & Security**
- [ ] User registration and login functional in production
- [ ] All API endpoints return 401 for unauthenticated requests
- [ ] All API endpoints return only the authenticated user’s data (no cross-contamination)
- [ ] HTTPS enforced; CORS locked to production frontend origin only
- [ ] Passwords hashed with BCrypt (cost factor ≥ 12) — verified via database inspection

**Legal & Compliance**
- [ ] Terms of Service live at `/terms` with approved legal copy
- [ ] Privacy Policy live at `/privacy` with approved legal copy
- [ ] Disclaimer acceptance stored server-side (timestamped) for every onboarded user
- [ ] “Not medical advice” messaging present on dashboard, onboarding, and all public tool pages

**Payments**
- [ ] Stripe in live mode (not test mode); all 4 price IDs in production environment variables
- [ ] Stripe Checkout functional for both tiers, both billing intervals
- [ ] Webhook handler receiving live events; full subscription lifecycle tested (subscribe, cancel, payment fail)
- [ ] Customer Portal link functional from Settings page

**Onboarding**
- [ ] New user after registration is routed to onboarding wizard
- [ ] Both disclaimer checkboxes block CTA progression if unchecked
- [ ] Completed onboarding is not shown again on subsequent logins

**Marketing & Public Tools**
- [ ] Landing page, Pricing page, and FAQ page live and fully mobile-responsive
- [ ] `/tools/reconstitution-calculator` accessible without auth; email capture stores leads in database
- [ ] `robots.txt` correct; `sitemap.xml` valid XML with all public pages listed; Google Search Console verified and sitemap submitted

**End-to-End Test**
- [ ] Full flow passed: register → onboard → add compound → log check-in → hit upgrade wall → subscribe → view gated feature

*(Complete launch checklist with all gates across Authentication, Legal, Payments, Onboarding, Marketing, Public Tools, SEO, Infrastructure, and QA: `08-implementation-handoff.md` Section 7)*

---

## Section J — Risks & Red Flags

### Legal / Compliance Risks

| Risk | Severity | Required Action |
|------|----------|----------------|
| Terms of Service and Privacy Policy do not exist | Critical | Legal must draft and approve both before Stripe goes live. Stripe requires these URLs in account settings. |
| Onboarding disclaimer checkbox wording | High | Legal must review the exact checkbox copy in doc 02 Screen 2. Current draft is strong but has not been reviewed by counsel. |
| Cookie consent / GDPR | Medium | Not addressed in any of the 8 documents. **Human decision required:** are EU users in the initial target market? If yes, a GDPR-compliant consent banner is a P0 item. |
| Evidence tier labeling accuracy | High | The Strong / Moderate / Limited / Mechanistic tier system must be applied accurately. Mislabeling a compound as “Strong” when only animal or mechanistic evidence exists is a material compliance risk. A content review process must be defined before the Knowledge Base goes live. |
| Age verification | Medium | Screen 2 requires users to confirm they are 18+ via checkbox. This is assertion-based, not verified identity. Acceptable for launch — document the limitation. |
| B2B / Clinic tier and HIPAA | Low (not yet built) | Any early practitioner pilot must NOT use HIPAA-covered patient data without a BAA in place. This constraint must be communicated to anyone involved in B2B pilot outreach. |

### Technical Risks

| Risk | Severity | Required Action |
|------|----------|----------------|
| Data isolation gap before auth | Critical | Currently the product has no multi-tenancy. All existing data in development is not user-scoped. This must be fully resolved before any beta user accesses the system. |
| Stripe webhook idempotency | High | Webhooks can fire multiple times. The handler must be idempotent — processing the same event twice must not double-provision or double-downgrade a user. Implement idempotency key checks. |
| Test mode to live mode cutover | High | Subscriptions tested in Stripe test mode do not carry over to live mode. Full lifecycle must be tested in live mode with a real card before public launch. |
| Public calculator math parity | Medium | The public `/tools/` calculator pages must produce identical results to the authenticated in-app version. Any discrepancy erodes trust at the highest-intent funnel moment. Parity test is a required acceptance criteria for Ticket 6.1. |
| MDX pipeline for content team | Low | If the MDX pipeline (Phase 7) is not ready, the content team cannot publish glossary and blog content without engineering involvement. Define a workflow that unblocks content before the pipeline is production-ready. |

### Messaging Risks

| Risk | Severity | Required Action |
|------|----------|----------------|
| Pricing teaser copy in doc 03 (contradiction) | High | **Resolved in this document.** The corrected pricing teaser is in Section D. The original doc 03 Section 8 must not be used — it describes “no feature gating by tier” which contradicts the final pricing model. |
| “Side effect pattern detection” (Commander feature) | High | This feature name implies clinical signal detection. All copy must make clear this is surfacing patterns in user-logged subjective data — not detecting or diagnosing adverse effects. Legal must review Commander feature descriptions before the tier launches. |
| Testimonial placeholders on landing page | Medium | The landing page social proof section contains placeholder testimonials. Do not launch with placeholder or fabricated quotes. Populate with real verified user quotes from beta, or remove the section until they exist. |
| “Bio-Intelligence” terminology | Medium | Strong positioning but could attract regulatory scrutiny if read as a medical intelligence system. Every use must be paired with clear non-prescriptive framing in close proximity. |
| Community seeding approach | Low | Doc 07 recommends seeding content in Reddit and Discord communities. Posts that appear promotional may be removed or banned. Community engagement must answer real questions with content as the genuine answer — not as an advertisement for BioStack. |

### Human Decisions Required Before Proceeding

The following items have no resolution in the existing documents. Each requires a decision from leadership before the affected work begins:

1. **GDPR / cookie consent scope.** Are EU users in the initial target market? Yes = cookie consent is P0. No = document and set a threshold for revisiting.

2. **7-day Operator trial implementation.** Doc 01 calls this a “soft recommendation.” Decision needed: build the trial flow at launch, or launch at standard pricing without a trial? Affects Stripe configuration, onboarding email sequence, and landing page copy.

3. **Early access / beta pricing.** Doc 01 proposes offering the first 500 users 3 months of Operator at $6/month. Decision needed: implement this, or launch at standard pricing? Requires a custom Stripe coupon and a code distribution mechanism.

4. **Email marketing provider.** The full lead gen funnel (doc 07) depends on an automated email sequence. The lead capture API in doc 08 defers to “configured provider.” A provider must be chosen and integrated before the email sequences can ship.

5. **Content review process for the Knowledge Base.** Who reviews compound evidence tier assignments before publishing? What is the review cadence? This is a compliance requirement that is not defined in any document.

6. **Commander launch timing.** Doc 01 recommends launching Commander 60–90 days post-launch once AI features are ready. Engineering leadership must confirm: will AI Protocol Analysis be ready in that window, or should Commander launch with a waitlist?

---

## Contradiction Resolution Log

| Contradiction | Source | Resolution |
|---------------|--------|------------|
| Landing page pricing teaser (doc 03 Section 8) says “no feature-gating by tier” and “no upsells mid-protocol” | Doc 03 vs. Doc 01 | **Doc 01 wins.** Three-tier model with defined feature gates is the canonical pricing structure. Corrected landing page pricing section is in Section D of this document. |
| “Free calculator” scope: doc 07 says calculators are free; doc 01 gates in-app calculators at Operator | Doc 07 vs. Doc 01 | **Both are correct in context.** Public `/tools/` pages are free with no account required. In-app authenticated calculators are gated at Operator. This distinction must be explicit in all copy and engineering specs. |
| Tier naming consistency across all documents | All 8 docs | No contradiction. Observer / Operator / Commander is consistent across all 8 documents. Confirmed canonical. |

---

*This document was synthesized from eight specialist workstream outputs dated April 2026. It supersedes individual documents where contradictions exist. Individual source documents remain authoritative for their full detail content, as referenced in each section above. Review cadence: update this master document whenever a source document is materially revised.*
