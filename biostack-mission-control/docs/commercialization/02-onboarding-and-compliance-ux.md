
> **PHASE 2 DIRECTIVE APPLIED - STRATEGIC OVERLAY:** 
> BioStack is the definitive **Protocol Intelligence Platform** for human biology. It is NOT a tracker, peptide app, or supplement log. It is an observability engine for compounds and outcomes. 
> *Core Moat:* Synergy/interaction mapping, pathway overlap detection, timeline correlation engine, evidence-tier knowledge base.
> *Aha Moment:* Within 60 seconds of onboarding, users input 1-3 compounds and see pathway overlap, synergies, conflicts, and timeline initialization.
> *Monetization:* Users pay for reduced mistakes, increased clarity, confidence, and better outcomes through understanding.
> *Growth Engine:* Calculators are the primary acquisition surface and lead generation hook.

---
# BioStack Mission Control — Onboarding, Trust & Compliance UX

**Document Type:** UX Design Specification  
**Team:** Onboarding, Trust & Compliance UX  
**Version:** 1.0  
**Date:** 2026-04-04  
**Status:** Approved for Implementation

---

## Overview

This document defines the complete first-run onboarding experience for BioStack Mission Control. Every screen, every line of copy, and every interaction pattern is specified here.

The guiding principle: legal protection and user trust are not in tension. Handled correctly, the boundaries we set become a signal of quality — evidence that BioStack is a serious, intelligent tool built by people who care about doing this right.

This is not a harm reduction disclaimer. This is an intelligent scaffolding system.

---

## Onboarding Flow Map

```
[First Visit]
     │
     ▼
[Screen 1: Welcome]
     │  CTA: "Enter Mission Control"
     ▼
[Screen 2: Boundary Setting + Consent]
     │  CTA: "I understand — let's go"
     │  Two checkboxes required to unlock CTA
     ▼
[Screen 3: Profile Creation]
     │  CTA: "Set My Baseline"
     ▼
[Screen 4: Goal Selection]
     │  CTA: "Continue"
     ▼
[Screen 5: Protocol Initialization (Required)]
     │  User inputs 1-3 compounds
     │  CTA: "Initialize Protocol →"
     ▼
[Screen 6: The Aha Moment Reveal]
     │  System immediately maps pathway overlap, synergies, and conflicts.
     │
     ▼
[Persistent App State: Soft Disclaimers Active]
```

Each screen is a full-page modal layered over the dark dashboard background, which is visible but blurred behind. This creates the sense that Mission Control already exists — the user is being walked to their seat, not waiting in a lobby.

---

## Screen 1 — Welcome

**Visual:** Full-screen. BioStack logo centered. Animated emerald pulse ring expands slowly from center. Background: #0B0F14 with faint constellation grid overlay.

---

**Headline:**
> Your protocol. Observed.

**Subtext:**
> BioStack Mission Control is your personal bio-intelligence system — purpose-built to track compounds, compute doses, and surface evidence-grade insights across your active stack.
>
> This is where precision meets accountability.

**CTA Button:**
> Enter Mission Control →

**Footer Microcopy (small, muted):**
> Built for researchers, athletes, and advanced self-experimenters. Not a medical service.

---

## Screen 2 — Boundary Setting + Consent

**Visual:** Clean modal card, glass-morphism style. Emerald rule line at top. No red. No skull-and-crossbones. No aggressive warning iconography. A calm, intelligent handshake between the product and the user.

---

**Headline:**
> One important thing before we begin.

**Subtext:**
> BioStack is a tracking and intelligence platform. We're exceptionally good at the math, the evidence, and the observability. What we're not — and what we'll never claim to be — is a doctor, a pharmacist, or a diagnostic service.
>
> Everything you log here is yours. Every calculation we run is a math utility, not a prescription. Every piece of compound intelligence we surface is evidence-referenced research, not clinical guidance.
>
> We believe the most responsible thing we can do is be completely clear about that.

**Boundary Box (styled card within the modal, slightly inset, subtle border):**

> **What BioStack does:**
> - Tracks your compounds, doses, and protocols
> - Computes reconstitution concentrations and injection volumes
> - Surfaces evidence-tiered compound intelligence
> - Identifies pathway overlaps between active substances
> - Correlates your check-in data with your stack over time
>
> **What BioStack does not do:**
> - Prescribe or recommend compounds for you to use
> - Diagnose conditions or interpret symptoms clinically
> - Replace consultation with a licensed medical professional
> - Constitute medical advice of any kind

**Consent Checkboxes (both required to unlock CTA):**

- [ ] I understand that BioStack is a tracking and research tool, not a medical service. I'm using it to observe and manage my own protocols, and I take full responsibility for my choices.

- [ ] I confirm that I am 18 years of age or older and that I have read and agree to BioStack's [Terms of Service] and [Privacy Policy].

**CTA Button (disabled until both checked):**
> I understand — let's go

**Footer Microcopy:**
> This acknowledgment is stored with your account. You can review our full Terms of Service and Privacy Policy at any time from Settings.

---

## Screen 3 — Profile Creation

**Visual:** Form screen with clean single-column layout. Progress indicator: step 1 of 3 (small, unobtrusive). Fields animate in sequentially on load.

---

**Headline:**
> Set your baseline.

**Subtext:**
> Your profile anchors your tracking data. We use these basics to give your check-ins and calculations context — nothing is shared externally.

**Form Fields:**

| Field | Label | Placeholder / Helper |
|---|---|---|
| Display Name | What should we call you? | "Your name or handle" |
| Biological Sex | — | Male / Female / Prefer not to say |
| Date of Birth | — | DD / MM / YYYY (used to surface age-relevant notes only) |
| Height | — | cm or ft/in toggle |
| Weight | — | kg or lbs toggle |
| Activity Level | How would you describe your training? | Sedentary / Recreational / Trained / Athletic / Elite |

**Privacy Microcopy (below form):**
> Your data stays yours. BioStack does not sell, share, or monetize your personal health information.

**CTA Button:**
> Set My Baseline →

**Skip Option (text link, low visual priority):**
> Skip for now — I'll complete my profile later

---

## Screen 4 — Goal Selection

**Visual:** Icon-grid selection screen. 6–8 tiles arranged in a responsive grid. Tiles highlight with emerald border on selection. Multi-select allowed.

---

**Headline:**
> What are you optimizing for?

**Subtext:**
> Select everything that applies. This shapes how the knowledge base surfaces information and how your check-ins are framed — it's not a commitment, and you can change it anytime.

**Goal Tiles:**

| Icon | Label |
|---|---|
| ⬡ | Muscle & Strength |
| ⬡ | Fat Loss & Body Composition |
| ⬡ | Recovery & Injury Repair |
| ⬡ | Cognitive Performance |
| ⬡ | Longevity & Healthspan |
| ⬡ | Hormonal Optimization |
| ⬡ | Sleep & Stress Resilience |
| ⬡ | Research & Tracking Only |

**Selection Microcopy (appears after first selection):**
> Good. Your stack intelligence will weight evidence accordingly.

**CTA Button:**
> Continue →

**Footer Microcopy:**
> You can update your goals anytime from your Profile settings.

---

## Screen 5 — Protocol Initialization (Required)

**Visual:** Two-panel layout. Left: auto-completing search for 1-3 compounds. Right: A dynamic visual matrix building real-time connections between the chosen compounds.

---

**Headline:**
> Initialize your protocol.

**Subtext:**
> Input 1-3 compounds you are currently researching or using. BioStack will immediately map them against our intelligence engine to detect pathway overlaps, synergies, and potential interactions.

**Form Fields:**

| Field | Label |
|---|---|
| Compounds (Required) | Search or type 1-3 compounds |

**Smart Microcopy (appears when compound is recognized):**
> We found intelligence on [Compound]. Evidence tier: [Strong / Moderate / Limited]. 
> *Pathway overlap mapping initiated...*

**CTA Button (primary) — [Activates the AHA Moment]:**
> Initialize Protocol & Generate Map →

---

## Screen 6 — The Aha Moment / Dashboard Reveal

**Visual:** The onboarding modal shatters or elegantly fades out. The system dynamically renders a **Protocol Intelligence Map** in real-time. Nodes connect. Overlaps are highlighted. Synergies glow. Conflicts blink amber. This is the "Holy Shit" moment.

---

**Welcome Banner (top of dashboard, dismissible):**
> Protocol Initialized. Pathway overlaps and intelligence data loaded. Welcome to clarity.

**Dashboard Empty State Copy (if no compound added yet):**

**Headline:**
> Your stack is empty — for now.

**Subtext:**
> Add your first compound to start generating intelligence. The more you track, the sharper your signal.

**CTA:**
> + Add Compound

**Secondary CTA (text link):**
> Explore the Knowledge Base →

---

**Dashboard Empty State Copy (if compound was added):**

**Headline:**
> [Compound Name] is live.

**Subtext:**
> Your first compound is active. Start a check-in to begin correlating your stack with your response data.

**CTA:**
> + Log Check-In

**Secondary CTA (text link):**
> View Compound Intelligence →

---

## Trust Microcopy Library

Short phrases that appear throughout the product at key moments. Each one reinforces accuracy, evidence quality, and user ownership — not recklessness or clinical authority.

### On Calculation Outputs

> Math verified. This calculation is based on the values you entered — always double-check against your source material.

> Reconstitution result is a mathematical output. Verify against your vial label before use.

> Volume calculated. Accuracy depends on the concentration values you've entered.

### On Knowledge Base Intelligence

> Evidence tier: **Strong** — Multiple peer-reviewed studies with consistent outcomes.

> Evidence tier: **Moderate** — Promising data with some limitations in study quality or sample size.

> Evidence tier: **Limited** — Early-stage or mechanistic evidence only. Interpret with caution.

> Evidence tier: **Mechanistic** — Theoretical or preclinical basis. No robust human trial data.

> This information is research-referenced. It is not clinical guidance.

### On Pathway Overlap / Interaction Flags

> Pathway overlap detected between [A] and [B]. This is an informational flag — review the detail and consult appropriate resources.

> Interaction flag: these compounds share [pathway name]. This does not mean they cannot be co-administered — it means you should be aware of the overlap.

### On Check-Ins

> Your check-in data is yours. We never interpret symptoms clinically — we surface patterns so you can.

> Self-reported data is the foundation of good observability. The more consistently you log, the more meaningful your timeline becomes.

### On Protocol Phases

> Protocol phase saved. Use phases to compare your response data across different periods of your stack.

### On Compound Source

> BioStack does not verify, endorse, or validate compound sources. Source tracking is for your own records.

### General Trust Badges (sidebar / footer / settings)

> **Precision math.** Every calculation is deterministic and verifiable.

> **Evidence-tiered intelligence.** We cite our sources. You see the tier before you read the claim.

> **Your data, your control.** Export, delete, or modify your records at any time.

> **No prescribing. Ever.** BioStack doesn't tell you what to take. It helps you track what you've decided.

---

## Persistent Disclaimer Placement

Disclaimers in BioStack are not footnotes. They are woven into the interface at the exact moment context matters. Here is where they live and what form they take.

### 1. Reconstitution / Volume Calculator

**Placement:** Directly below the calculation output, above the copy/save buttons.

**Form:** Single line, muted text, no icon.

> Calculated result. Verify against your source material before use. BioStack calculations are mathematical utilities, not clinical guidance.

### 2. Knowledge Base Compound Pages

**Placement:** Sticky banner at the top of every compound knowledge page.

**Form:** Thin bar, glass-morphism style, emerald-left-border.

> The following information is evidence-referenced research. It is not medical advice and does not constitute a recommendation to use this compound.

### 3. Drug Interaction / Pathway Overlap Alerts

**Placement:** Inline within the overlap checker result, before the detail list.

**Form:** Alert card, amber-left-border (not red — amber signals attention, not danger).

> The following flags are informational only. A pathway overlap or interaction flag does not mean co-administration is harmful — it means you should investigate and take appropriate precautions.

### 4. Settings / Account Page

**Placement:** Footer of the Settings page.

**Form:** Legal-style paragraph, small font, muted color.

> BioStack Mission Control is a personal tracking and research tool. It is not a medical device, does not provide medical advice, and is not a substitute for professional medical consultation. Use of this platform does not establish any clinical relationship. You are solely responsible for all decisions related to your health and supplementation protocols.

### 5. First Check-In (only — one-time contextual note)

**Placement:** Above the check-in form on first use.

**Form:** Dismissible callout card.

> Check-ins log subjective self-reported data. BioStack tracks trends — it does not diagnose, interpret, or assess your health clinically. If you're experiencing concerning symptoms, consult a qualified healthcare professional.

### 6. Email / Notification Footer

**Placement:** Footer of all outbound emails and push notifications.

**Form:** Small legal text.

> BioStack is a personal tracking platform. Nothing communicated via this service constitutes medical advice. Always consult a qualified professional before making health decisions.

---

## "Soft Legal" Language Guide

This guide governs how legal boundaries are expressed throughout BioStack. The goal is plain language that communicates real limits without sounding defensive, litigious, or condescending.

### Core Principles

**1. Describe what the tool does — not what you're afraid of.**

Instead of: "This is not medical advice and should not be construed as..."
Write: "This is a mathematical calculation. It tells you the concentration of your solution."

**2. Use the word "you" to reinforce ownership.**

Instead of: "Users are advised to consult medical professionals."
Write: "You're tracking your own protocol. We surface the data. You make the calls."

**3. Pair every limit with a capability.**

Instead of: "BioStack does not diagnose or treat conditions."
Write: "BioStack doesn't diagnose — but it will surface every pattern in your data so you can see exactly what's happening."

**4. Use evidence tiers as confidence scaffolding, not hedges.**

Instead of: "This information may not be accurate."
Write: "Evidence tier: Moderate. Promising data — interpret with appropriate context."

**5. Avoid the word "warning" where possible. Prefer "note," "flag," or "heads up."**

Instead of: "WARNING: drug interaction detected."
Write: "Interaction flag. [Compound A] and [Compound B] share [pathway]. Review details."

**6. Never use "at your own risk" — it reads as abandonment.**

Instead of: "Use at your own risk."
Write: "You're in control of your protocol. We're here to make sure you have the best possible information."

### Approved Soft Legal Phrases

| Hard Legal Version | Soft Legal BioStack Version |
|---|---|
| "This does not constitute medical advice." | "This is research intelligence, not clinical guidance." |
| "Consult a physician before use." | "For anything beyond tracking, loop in a professional you trust." |
| "We are not responsible for outcomes." | "Your protocol, your decisions. We give you the data." |
| "Drug interactions may be harmful." | "Pathway overlap flagged. Worth reviewing before you proceed." |
| "Results may vary." | "Self-reported data is subjective — trends matter more than single data points." |
| "Not intended to diagnose, treat, cure, or prevent any disease." | "BioStack tracks and informs. It doesn't diagnose or prescribe — that's not what it's for." |
| "Use only under medical supervision." | "For controlled substances or clinical protocols, always involve a qualified professional." |

---

## Onboarding Success Metrics

These are the outcomes this onboarding flow is designed to produce. Implementation teams should instrument accordingly.

| Metric | Target |
|---|---|
| Welcome → Consent completion rate | > 85% |
| Consent → Profile completion rate | > 75% |
| Profile → Goal selection completion rate | > 90% |
| Goal selection → First compound add rate | > 50% |
| Overall onboarding funnel completion | > 65% |
| Day-7 retention (completed onboarding) | > 40% |
| Consent screen drop-off | < 15% |

**Key signal:** If the consent screen drop-off exceeds 15%, the language is too aggressive. Revisit framing — not legal substance.

---

## Implementation Notes

### State Management
- Onboarding completion state must be persisted to the user's account, not just local storage. If a user completes onboarding on mobile and opens on desktop, they should not see it again.
- Consent acceptance must be timestamped and stored server-side. Store: `consent_accepted_at`, `consent_version`, `tos_version`.

### Screen Accessibility
- All onboarding screens must meet WCAG 2.1 AA contrast requirements even within the dark theme.
- Checkbox labels must be fully readable at 16px minimum.
- CTA buttons must have sufficient tap target size (min 44x44px) for mobile.

### Skip Behaviors
- Profile fields: skippable (except display name)
- Goal selection: skippable
- First compound: skippable
- Consent screen: NOT skippable. Both checkboxes required.

### Returning User Flow
- Users who have completed onboarding land directly on the dashboard.
- If consent version is updated (ToS changes), a single-screen re-acknowledgment modal appears at next login — not the full onboarding flow.

### Mobile Considerations
- Screens 1–6 must be fully responsive. Priority layout is mobile-first.
- The compound preview panel on Screen 5 collapses on mobile (single column, preview hidden).
- Progress indicator on Screen 3 is text-only on mobile ("Step 1 of 3").

---

*End of Document — BioStack Onboarding, Trust & Compliance UX v1.0*
