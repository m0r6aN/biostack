# WAVE 3 EXECUTION PACKET — ratified copy, exact placements

**Status: SIGNED OFF by Clint, 2026-08-08.** The category sentence and hero line below are final.
This packet unblocks Wave 3 tasks 3.1, 3.2, 3.3 and 3.4 in `COORDINATOR-HANDOFF-landing-r1.md`.

**Prerequisite:** Waves 1 and 2 must be merged first. This packet edits `LandingHero.tsx`, which
both prior waves also touch.

---

## The ratified copy

**Category sentence** — deployed verbatim in five placements:

> BioStack is a free, public library of what the research says about peptides and similar compounds — graded by evidence strength, for anyone deciding for themselves.

**Hero H1:**

> No prescriptions. No guesswork. Just what's known.

**Tier ladder:**

| Tier | Price | Tagline |
|---|---|---|
| Observer | $0 | The evidence, open. |
| Operator | $12/mo | Your protocol, not compounds in general. |
| Commander | $29/mo | Every run, side by side. |

---

## Hero architecture (`LandingHero.tsx`)

```
EYEBROW   Peptides · SARMs · SERMs · and beyond
H1        No prescriptions. No guesswork. Just what's known.
SUBHEAD   BioStack is a free, public library of what the research says about
          peptides and similar compounds — graded by evidence strength, for
          anyone deciding for themselves.
```

The eyebrow is the element Wave 1 task 1.1 restyles (currently the unstyled `<p>` at line 79).
Wave 3 changes only its text; 1.1 already supplies the kicker treatment.

**Retirement:** the current H1 — "What you're taking. How it's structured. See what it's doing." —
does not survive. It names no category, which is finding F2. It is doctrinally clean and reviewers
liked it, so it is available for reuse as a lower section heading if wanted. **Default: retire it.**

**Also retired:** the current subhead, "Start with clarity. Then track, compare, and observe changes
over time." The category sentence replaces it.

---

## The five placements — exact before/after

### P1 · `frontend/src/lib/site.ts` — `SITE_TITLE`
```
- 'BioStack | Protocol Operations'
+ 'BioStack | Peptide and Compound Evidence Library'
```
Alternate if you prefer the research framing: `'BioStack | What the Research Says'` (shorter, less
literal). Do not exceed ~60 characters.

### P2 · `frontend/src/lib/site.ts` — `SITE_DESCRIPTION`
```
- 'Your protocol operations system. Track compounds, surface overlap, and turn daily signal into continuity.'
+ 'BioStack is a free, public library of what the research says about peptides and similar compounds — graded by evidence strength, for anyone deciding for themselves.'
```
**Length note:** 161 characters, slightly past Google's ~155 truncation. Clint accepted front-loaded
truncation — the first clause carries alone. If strict compliance is preferred later, the approved
trim is: *"BioStack is a free, public library of what the research says about peptides and similar
compounds — graded by evidence strength."* (~139). **Do not invent a different trim.**

### P3 · `frontend/src/app/page.tsx` — JSON-LD `softwareSchema.description`
```
- 'Tracking, calculator, and stack mapping infrastructure for compound protocols.'
+ 'BioStack is a free, public library of what the research says about peptides and similar compounds — graded by evidence strength, for anyone deciding for themselves.'
```
While in this file: `applicationCategory: 'HealthApplication'` is unchanged and correct.

### P4 · `frontend/src/components/marketing/MarketingFooter.tsx` — the left-hand line
```
- 'BioStack. Tracking, math, and clarity for complex stacks.'
+ 'BioStack is a free, public library of what the research says about peptides and similar compounds — graded by evidence strength, for anyone deciding for themselves.'
```
Footer has horizontal room at `md:flex-row`; verify it does not crowd the 9-link row (7 links plus
Pricing and Start Free from Wave 1 task 1.10). If it does, the P2 trim is the approved fallback here.

### P5 · `frontend/src/components/marketing/LandingHero.tsx` — visible subhead beneath the H1
Full sentence, no trim. This is the placement the sentence was written for.

---

## Tier ladder — ⚠️ the taglines are NOT in `marketing.ts`

**Trap.** `pricingTiers` in `marketing.ts` composes `tagline` from `getProductPlan(code).tagline` —
which reads `contracts/product-contract.v1.json`, **not** the marketing file. Editing `marketing.ts`
will silently fail to change the taglines.

**T1 · `contracts/product-contract.v1.json`** — `billing.plans[].tagline`
```
observer:  'Free'                      → 'The evidence, open.'
operator:  'Track & Analyze'           → 'Your protocol, not compounds in general.'
commander: 'Longitudinal Intelligence' → 'Every run, side by side.'
```
Verified: no test pins these strings (`productContract.test.ts` and `marketing.test.ts` were checked
and contain no tagline assertions). Prices and codes are untouched — **do not alter
`monthlyPriceCents` or `marketingCtaPath`.** Confirm whether this edit requires a `contractVersion`
bump from 1.0.0 before merging; that convention is not documented in the file.

**T2 · `frontend/src/lib/marketing.ts`** — `pricingContent[].detail`, rewritten so the free tier
reads as complete rather than limited:

- **observer** → "The full sourced compound library with evidence grades, source types and mechanism summaries, plus the reconstitution, dose-volume and conversion calculators. No account. Nothing held back."
- **operator** → "Daily check-ins across ~15 signals, your own timeline, and a flag with supporting evidence when two compounds you're taking share a biological pathway. Upgrade when you stop reading about compounds and start running specific ones."
- **commander** → "Past protocols and check-ins on one timeline so you can read across them. Upgrade when you have more than one run worth comparing."

**⚠️ V1 STANDS.** The `observer` highlights array contains `'Up to 8 active compounds'`. The product
contract declares **no entitlements on any plan**. Leave that string exactly as it is and **do not
add any copy anywhere claiming a paid tier lifts a cap** until the limit is confirmed enforced in
the .NET entitlement code. Escalate if anyone proposes otherwise.

**T3 · `frontend/src/app/pricing/page.tsx:9`** — metadata description still reads "…observational
protocol tracking and longitudinal intelligence." Update to match the new ladder language.

---

## `/safety` echo decision

`frontend/src/app/safety/page.tsx` closes with **"No prescriptions. No guesswork. Just structure."**
Once this becomes the landing H1 with a different third beat, the two disagree.

**Decision: make them match.** Change `/safety`'s closing line to *"No prescriptions. No guesswork.
Just what's known."* This mirrors the existing deliberate echo — `/safety`'s H1 and the landing
disclaimer strip already share "BioStack is not a doctor." — and reinforces rather than duplicates.

Leave the rest of `/safety` untouched. Reviewers rated it the strongest-written page on the site.

---

## Test updates — read this before touching `HomePageHero.test.tsx`

Guardrail G2 says do not edit tests to accommodate a change. That rule needs a precise reading here,
because this file contains **two different kinds of assertion** and they get opposite treatment.

**SACRED — never edit, never weaken.** These are the doctrine regression net:
```
queryByText(/What to take\. How to use it\./)      not.toBeInTheDocument()
queryByText(/optimize over time/)                   not.toBeInTheDocument()
queryByText('Stop guessing what to take—or what your stack is actually doing.')
queryByText('Multi-client') / 'Protocol Surface' / 'Learn more' / 'Live'
queryByText(/No inputs detected/)
```
If a copy change makes one of these fail, **the copy is wrong.** Escalate; do not touch the test.

**UPDATE IN LOCKSTEP — these describe the old copy, not the doctrine:**
```
getByRole('heading', { name: /What you're taking\. How it's structured\.\s*See what it's doing\./ })
  → new H1: /No prescriptions\. No guesswork\. Just what's known\./

getByText('Start with clarity. Then track, compare, and observe changes over time.')
  → the category sentence
```

**Also affected by Wave 2, not by this packet:** the card assertions (`'Analyzer'`,
`'Existing stack'`, their body strings, and the `/tools/analyzer` link expectations) change when
task 2.3 merges cards 1 and 3. If Wave 2 landed first, those are already updated — verify rather
than assume, and do not update them twice.

---

## Acceptance criteria

1. All five placements carry the identical sentence (P2 may carry the approved trim; no other variant exists anywhere).
2. Grep the repo: **zero** occurrences of "Protocol Operations", "protocol operations system", "Tracking, math, and clarity", or "Longitudinal Intelligence" outside `.audit/`.
3. `/` and `/pricing` render the three prices $0 / $12 / $29 with the new taglines.
4. `/safety` and the landing H1 carry the identical third beat.
5. `npm test` green with the sacred assertions **unmodified** — diff the test file and confirm only the two lockstep assertions changed.
6. `npm run lint` and `npm run build` green.
7. A stranger shown only the first viewport can state what BioStack is. Test on one person who has not seen the site.

---

## What this closes

| Finding | Status after this packet |
|---|---|
| **F2** — page never states its category | **CLOSED.** One sentence, five placements, plus a visible line under the H1. |
| **F5** — tier gating before tiers are defined | **CLOSED** once prices publish and Wave 2 task 2.4 strips the hero gate strings. |
| **F6** — middle of funnel absent | **PARTIAL.** Ladder and category land; the trust block still needs the custody copy. |
| **F3** — no data-custody statement | **STILL OPEN — legal, not copy.** `/privacy` and `/terms` are noindex stubs citing the commercialization brief as blocking launch. Nothing in this packet touches it, by design. |

---

## Provenance

Category sentence and tier ladder synthesized from a 4-seat blind generative council
(`.audit/positioning-brief-v2.md`, four angles: enemy / librarian / on-ramp / skeptic) — all four
converged on a free, source-graded evidence library, and on the upgrade logic that the question
changes from *"what is this compound"* → *"what about mine"* → *"what about across my runs."*
*"Every run, side by side"* was produced verbatim by two independent seats. Hero third beat selected
by Clint from four candidates. Full reasoning and the honest note on partial anchoring of the noun
"library": `.audit/POSITIONING-ARTIFACTS-v2.md`.
