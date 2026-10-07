# DIRECTIVE — BioStack Landing Page, Adversarial Review Round 1

**Council:** 5 seats — Orchestrator (Opus 5) + Visceral + Mechanics + Consistency + Craft.
**Absent seats:** Grok, Codex, Gemini CLIs. This session ran in the Anthropic cloud sandbox, not on
m0r6an; those CLIs were unreachable. Coverage was replaced with four blind lenses rather than four
vendors. Vendor diversity is a genuine gap in this round — a Round 2 run from the local rig would
add signal this round does not have.

**Evidence:**
- Brief (uncontaminated, shared by all seats): `.audit/landing-brief.md`
- Seat verdicts: `.audit/seats/{orchestrator,visceral,mechanics,consistency,craft}-out.txt`
- Source of truth: repo HEAD `9cecaf7` (2026-08-03), `frontend/src/app/page.tsx` + marketing components
- Live-render confirmation: partial screenshot of biostack.cc on m0r6an (Chrome, read-tier). The
  live nav, hero H1, and cards 3/4 were visually confirmed to match source. A full-page and mobile
  capture was blocked by window masking and is **not** part of this round's evidence.

---

## Verdict

**REVISE AND RE-REVIEW.**

Not one seat approved. Four of five returned REVISE AND RE-REVIEW; one returned REJECT CURRENT FORM.
The craft floor here is high — the dark system, the ambient treatment, the panel, and the
non-prescriptive discipline are genuinely good, and several of them are better than the sites
BioStack will be compared to. The page fails on a different axis: it is a **routing screen for a
product it never names**, and its loudest button is an unpriced gate. Those are comprehension and
trust failures, not taste failures, and they are cheap to fix relative to their cost.

---

## Convergent Findings

Ordered by blast radius. Seat counts are of 5. Every finding below was reached by seats working
blind to each other.

### F1 · BLOCKER · There is no single primary action, and the loudest one is a locked door — **5/5, unanimous**
Exactly two elements on the page use a solid emerald fill: the proof-section `Analyze a stack` and
the mobile sticky bar's `Analyze`. Both resolve to `/tools/analyzer` — which the hero itself labels
`Operator required` and `Operator access`. Meanwhile `Start Free`, the free top-of-funnel entry, is
outline-treated everywhere it appears, and it is **absent entirely** from the mobile sticky bar,
where `Analyze` takes the solid fill instead. A mobile visitor sees two persistent bars arguing for
opposite next steps. The page's dominant conversion path terminates at a paywall whose price is not
printed anywhere on the page.

*Why it matters:* a page with two primary actions has none. This is simultaneously the conversion
defect, the trust defect (bait-and-gate), and the reason no funnel metric from this page will be
interpretable.

### F2 · BLOCKER · The page never says what BioStack is — **5/5, unanimous**
The H1 — "What you're taking. How it's structured. See what it's doing." — contains no noun naming
the product or its category. The only line that names the category is the eyebrow, which is the one
unstyled element on the page (see F7). Four different self-descriptions ship simultaneously:
`<title>` "Protocol Operations" · meta "Your protocol operations system" · JSON-LD "Tracking,
calculator, and stack mapping infrastructure for compound protocols" · footer "Tracking, math, and
clarity for complex stacks" — five if you count the FAQ's "personal bio-protocol operating system."

The Mechanics seat put the consequence best: the first viewport asks the visitor to **self-sort into
four entry paths before it has told them what they'd be entering**. The routing grid fires on a
decision the visitor has no basis to make.

*Why it matters:* fails the 5-second test outright. Everything below the fold is spent on a visitor
who does not know what they are looking at.

### F3 · BLOCKER · No data-custody statement in a category that cannot launch without one — **4/5 flagged, 1 rated BLOCKER; orchestrator concurs on the argument, not the count**
The page asks a user to log peptides, SARMs and SERMs and says nothing about where that data goes.
There is no privacy line, no security statement, no "local-first" claim, no compliance mention, no
team, no counts, no dates. The single privacy signal on the entire page is a 14px footer link
sitting at exactly 4.50:1 contrast.

The Consistency seat's argument is the one that promotes this to blocker, and it is correct:
**local-first is simultaneously BioStack's strongest differentiator and the answer to the trust
question — and the page spends neither.** The copy already exists, written and unrendered, in
`marketing.ts` as `featuredFaqs` → "Where is my data stored?".

*Dissent noted:* three seats rated this MAJOR, not BLOCKER. I am adopting BLOCKER on the strength of
the argument rather than the vote. Challenge this one if you think launch survives it.

### F4 · MAJOR · The four-door router has three doors — **5/5**
Hero cards 1 ("Analyze a protocol") and 3 ("Analyze My Stack") sit adjacent in the same row, framed
as different jobs, and resolve to the identical URL. `/tools/analyzer` is the target of four separate
controls under four different labels page-wide: "Analyze a protocol" / "Analyze My Stack" /
"Analyze a stack" / "Analyze". Fourteen interactive targets in the first desktop viewport resolve to
eight distinct destinations.

*Why it matters:* a choice that returns no new option is pure decision tax, and it teaches the
visitor that labels on this site do not predict destinations.

### F5 · MAJOR · Tier gating is stated before tiers exist — **5/5** (severity spread MINOR→BLOCKER; settled at MAJOR)
`Operator required`, `Operator access`, `with Operator`, `Operator and Commander members`, and
`See Observer, Operator, and Commander` all appear in the hero. Observer, Operator and Commander are
never defined, differentiated, or priced anywhere on the page. Three controls exit to `/pricing` to
find out. The Consistency seat found the sharpest version: `Start Free` in the nav is contradicted
inside its own viewport by "Operator required" one row below it, and the proof section invites
anyone to "Paste a stack" while card 1 restricts pasting to paying members.

### F6 · MAJOR · The middle of the funnel is missing — **4/5**
The page has exactly two content sections. There is no how-it-works, no tier comparison, no feature
substantiation, no FAQ, no differentiator, and no closing CTA — the page ends on a negation
("BioStack is not a doctor") followed by a footer. Six feature strings (`landingFeatures`) and ten
FAQ pairs (`featuredFaqs`) are written, reviewed, doctrine-compliant, and **never rendered**.

*Why it matters:* nothing between "pick a door" and "we're not a doctor" earns the click. The fix is
unusually cheap because the content already exists and has already passed copy review.

### F7 · MAJOR · An unfinished component shipped to production — **4/5**
`LandingHero.tsx:79` is `<p>Built for peptides, SARMs, SERMs, and beyond</p>` with **no className**,
in a file where every other text node carries explicit utility classes. It inherits raw preflight
body styling — 16px, `rgba(255,255,255,0.9)`, no tracking, no margin. It therefore renders *brighter
and heavier than the subhead beneath the H1*, and it is the single most important line on the page
(the only one naming the category, per F2). Companion tell: `lg:col-span-2` is applied to a card grid
whose parent is `flex flex-col` — a dead class.

*Why it matters:* the Visceral seat's read is the operative one — the first thing the eye catches
looks broken rather than deliberate, on a page whose entire job is to look expensive.

### F8 · MAJOR · The proof panel is an unlabeled mock that leaks in-app copy — **2/5 flagged, then verified and strengthened by the orchestrator**
`StackIntelligencePanel` is hardcoded (`BPC-157`, `TB-500`, one overlap candidate), dressed with a
working List/Evidence toggle, a status pill, a stats row and an auto-rotating insight, and presented
under the H2 "See what BioStack catches" with no indication it is illustrative.

**New evidence found during verification** (no seat caught this): every `nextAction` string in
`onboardingIntelligence.ts` is written for a logged-in app user with a list in progress — the panel's
"Suggested next action" block on the public landing page renders **"Save the list or add another
item."** to a visitor who has no list. The mock is not merely unlabeled; it is displaying application
state copy on a marketing surface.

*Why it matters:* this is precisely the "UI element that implies functionality it does not possess"
class of defect. The toggle is an interactive dead end and the panel invites a reading of "output"
that the page cannot support.

### F9 · MAJOR · Accessibility is applied selectively rather than systemically — **3/5** (the two seats that carry the lens both rated it MAJOR)
- `text-white/35` panel stat labels ≈ **3.1:1** — fails AA
- `text-white/42` stage labels ≈ **4.0:1** — fails AA
- Footer text and all 7 footer links ≈ **4.50:1** — passes with literally zero margin
- `focus-visible` styling exists on **4 of ~18** interactive controls (the hero cards only); nav
  links, nav buttons, proof buttons, hero text links, footer links and all four sticky-bar buttons
  have none
- No skip link anywhere in the document
- `role="tablist"` + `role="tab"` + `aria-selected` with **no** `tabpanel`, no `aria-controls`, no id
  wiring, no arrow-key handling. The Craft seat's framing is sharper than mine and I adopt it:
  a half-implemented tab pattern is **worse than no pattern** — a screen-reader user is told
  "tab 1 of 2" and then stranded
- Auto-rotating insight line with no pause/stop/hide control (WCAG 2.2.2). `prefers-reduced-motion`
  is honored, which mitigates but does not satisfy

### F10 · MINOR · Every share of biostack.cc unfurls naked — **1/5, verified**
No `og:image`, no `twitter:image`, and `twitter:card = summary` rather than `summary_large_image`.
Every link posted to Slack, X, LinkedIn, iMessage or Discord renders as a text-only unfurl. For a
product whose growth will be substantially peer-to-peer link sharing, this is the cheapest
credibility point on the board and it is currently zero.

### F11 · MINOR · Wayfinding gaps at both ends — **1/5, verified with a correction**
The footer's 7 links omit `/pricing`, `/start`, and `/tools/analyzer` — the three commercial
destinations. Below `md`, all six primary nav links are hidden with no hamburger or disclosure menu.

*Correction to the Mechanics seat:* it claimed mobile navigation "effectively does not exist." That
overstates — the footer does carry How it works, Tools, Compounds & Evidence, For Providers and
Safety. The accurate finding is that mobile wayfinding is **delegated entirely to the footer and the
sticky bar**, with no disclosure menu, and that Pricing and Start are unreachable from the footer at
any breakpoint.

### F12 · POLISH · Dead class — **2/5**
`lg:col-span-2` on a flex child. Remove.

---

## Disputed / Refuted

**REFUTED — "The proof section breaches the Guidance Content Contract."** The Consistency seat
raised this as its #1 BLOCKER: that "See what BioStack catches" plus a "Suggested next action" block
constitutes the diagnostic/prescriptive behavior §2.4 denies. I checked it against the enforcement
source rather than the rendering. Every `nextAction` string in `onboardingIntelligence.ts` is
navigational — "Type anything you take.", "Add one more item to unlock relationship analysis.",
"Wait for the relationship check.", "Save the list or adjust it." **Class D is never engaged.** The
contract is not breached.

What survives is a **labeling risk, not a doctrine violation**: "catches" implies detection of
something wrong, and "Suggested next action" reads as direction to a lay reader or a regulator who
does not open the source. Downgraded from BLOCKER to MINOR. Worth a wording pass at the clinical
safety copy review gate, not a launch hold. This is a good illustration of why unique claims get
verified before they get executed — the seat's instinct was sound and its conclusion was wrong.

**OVERRULED — "Keep the ASCII `>` icons as a brand signature."** The Visceral seat listed the
literal `>` characters in bordered boxes under PRESERVE, reading them as a distinctive marker rather
than a generic icon-library glyph. The live screenshot settles it: they render as plain typographic
greater-than signs, vertically off-center in their boxes, at a different optical weight than
everything around them. That is a placeholder, not a signature. Replace with a real icon or remove
the boxes entirely.

**SPLIT — footer contrast.** The Craft seat listed footer text under PRESERVE as "meets AA, no
blanket opacity change needed." It measures 4.50:1 against a 4.5:1 threshold — it passes on paper
with no margin at all, and subpixel antialiasing eats that margin in practice. One-token change;
make it.

---

## Execution Plan

Phase 1 is mechanical and needs no debate — no design decisions, no copy review, nothing that
another judge could reasonably contest. Do it first regardless of how the Council rules on the rest.

### Phase 1 — Mechanical (no debate required)
| # | Target | Change | Acceptance |
|---|---|---|---|
| 1.1 | `LandingHero.tsx:79` | Give the eyebrow a real kicker treatment: uppercase, ~11–12px, `tracking-[0.18em]`, emerald-tinted, explicit margin | Eyebrow reads as designed hierarchy above the H1, not as body text |
| 1.2 | `LandingHero.tsx` | Delete `lg:col-span-2` from the card grid | No behavior change; dead class gone |
| 1.3 | `StackIntelligencePanel.tsx` | `text-white/35` → `text-white/56`; `text-white/42` → `text-white/56` | Both ≥4.5:1, verified with a contrast checker |
| 1.4 | `MarketingFooter.tsx` | `text-white/45` → `text-white/60` | ≥5.5:1, real margin |
| 1.5 | All marketing components | Propagate the hero cards' `focus-visible:ring-2` treatment to every nav link, nav button, proof button, hero text link, footer link and sticky-bar button | Tab through the whole page; every stop has a visible ring |
| 1.6 | `layout.tsx` | Add a skip link to `#main` as the first focusable element | First Tab reveals it |
| 1.7 | `site.ts` | Add `og:image` (1200×630) and set `twitter.card = 'summary_large_image'` | Paste the URL into Slack; card renders with the image |
| 1.8 | `StackIntelligencePanel.tsx` | Either complete the tab pattern (`role="tabpanel"`, `aria-controls`, id wiring, arrow keys) or drop the ARIA roles and ship a plain segmented control. **Prefer dropping** — it is a filter, not tabs | Screen reader announces something coherent, or announces a plain button group |
| 1.9 | `StackIntelligencePanel.tsx` | Add a pause/stop control to the rotating insight, or drop the rotation entirely on the marketing surface. **Prefer dropping** | WCAG 2.2.2 satisfied |
| 1.10 | `MarketingFooter.tsx` | Add `Pricing` and `Start Free` to the footer link row | Both reachable from page bottom at every breakpoint |

### Phase 2 — Structural (one owner, decide before building)
| # | Target | Change | Acceptance |
|---|---|---|---|
| 2.1 | `LandingHero.tsx` | **Declare one primary action: `/start`.** Make `Start Free` the only solid-emerald fill on the page. Demote the proof-section `Analyze a stack` to outline | Exactly one solid-fill CTA exists in the rendered DOM |
| 2.2 | `MobileStickyCta.tsx` | Rebuild as two buttons: `Start Free` solid, `Analyze` outline. Drop Evidence/Pricing/Provider (all survive in the footer) | Desktop and mobile privilege the same destination |
| 2.3 | `LandingHero.tsx` | Merge cards 1 and 3 into one Analyzer card. Give the freed slot a distinct destination — `/how-it-works` is the strongest candidate, since it also serves F2 | Four cards, four distinct URLs |
| 2.4 | `LandingHero.tsx` | Strip `Operator required` / `Operator access` from the hero entirely. Gating belongs at the destination or beside a price | Zero tier names appear above the fold without a price |
| 2.5 | `StackIntelligencePanel.tsx` | Add a permanent "Illustrative example — not real user data" caption inside the panel border. Replace app-state `nextAction` strings on the marketing surface with a navigational line | Panel never tells a logged-out visitor to "save the list" |

### Phase 3 — Content (copy review gate applies)
| # | Target | Change | Acceptance |
|---|---|---|---|
| 3.1 | `LandingHero.tsx` | Rewrite eyebrow + H1 so category and audience are legible in five seconds. H1 must name what BioStack **is**, for whom. Current triad demotes to subhead. **Must remain descriptive, never prescriptive** — the existing `HomePageHero.test.tsx` banned-string assertions stay green | A stranger reads the first viewport and can state the category |
| 3.2 | `site.ts` + `page.tsx` | Write **one** category sentence. Use it verbatim in `<title>`, meta description, JSON-LD, footer, and as a visible line under the H1 | Five surfaces, one sentence |
| 3.3 | New section between proof and disclaimer | Render the existing `featuredFaqs` "Where is my data stored?" and "How is BioStack different from a spreadsheet?", plus an Observer/Operator/Commander strip with real prices and one differentiator each, sourced from `pricingTiers` | F3, F5 and F6 close using copy that already passed review |
| 3.4 | `page.tsx` | Move the "BioStack is not a doctor" strip **up**, into or adjacent to the new trust block, and add a closing CTA block repeating the single primary action before the footer | Page ends on an action, not a negation |
| 3.5 | `IntelligenceProofSection.tsx` | Wording pass on "See what BioStack catches" and "Suggested next action" at the clinical safety copy review gate | Non-blocking; ship Round 2 with a recommendation |

---

## Preserve Exactly

Four seats independently defended these. Do not let a revision erode them.

1. **The disclaimer strip copy, verbatim** — "BioStack is not a doctor." plus its four-denial body.
   Blunt, unhedged, non-defensive, and exactly right for the category. **4/5 seats named it.** Only
   its position changes (3.4); not one word of it does.
2. **The dark system** — `#0B0F14`, the three low-opacity ambient blooms, the glass surfaces, the
   gradient-hairline panel border. Restrained, cohesive, and genuinely distinct from generic
   neon-dark SaaS. This is the part that already reads premium.
3. **`prefers-reduced-motion` handling across `globals.css` and the panel** — comprehensive and
   correctly treated as non-negotiable. Named by three seats.
4. **The entry-path card mechanism as a pattern** — eyebrow / title / body / signal / action /
   destination is the right structure for a product with genuinely different entry points. Two seats
   made a point of saying the defect is the duplicate and the count, **not** the pattern. Fix the
   destinations; keep the device.
5. **The sticky header, and the mobile bar's 220px scroll delay** — always one conversion control on
   screen at any scroll depth, with a clean first paint on mobile. Keep both; change only contents.
6. **The H1/subhead pair's doctrinal discipline** — purely observational, zero prescription, banned
   strings correctly absent and unit-tested. The rewrite in 3.1 must clear the same bar.

---

## Rules of Engagement

- **Phase 1 requires no Council ruling.** It is ten mechanical changes with objective acceptance
  criteria. Start it now; it does not block on Round 2.
- **One owner per phase.** Phase 2 in particular must not be split across contributors — F1's fix is
  a single coherent decision about what the page is for, and splitting it reproduces the exact
  inconsistency being fixed.
- **Every merge cites evidence.** Contrast changes ship with a measured ratio. Focus changes ship
  with a tab-through. The og:image ships with an unfurl screenshot.
- **The Guidance Content Contract outranks this directive.** Any copy recommendation here that
  conflicts with Class A–D rules loses. Class D stays prohibited; nothing in Phase 3 may drift toward
  dose selection, switching, tapering, or sourcing.
- **`HomePageHero.test.tsx` stays green.** The banned-prescriptive-string assertions are the
  regression net for 3.1. If a headline rewrite requires changing those assertions, it is the wrong
  headline.
- **Round 2 should be re-run from m0r6an** with the Grok/Codex/Gemini seats live, against the
  post-Phase-2 page, with a full-page and 390px capture in evidence. This round's vendor
  homogeneity is its main methodological weakness and should not repeat.

---

## Council Position

I vote **REVISE AND RE-REVIEW**, and I hold that two findings are individually launch-blocking:
**F1** (no single primary action; the loudest CTA is an unpriced gate) and **F2** (the page never
names its own category).

I additionally vote **F3 as blocking** — a product that asks users to log peptides and SARMs cannot
launch a landing page that is silent on data custody, particularly when local-first is the
differentiator it is declining to spend. Three of five seats rated this MAJOR rather than BLOCKER.
**This is the finding most open to challenge, and I would rather be argued out of it than have it
pass unexamined.**

To the seat that voted REJECT CURRENT FORM: I decline to go that far. Your strongest argument — the
proof/disclaimer contradiction — did not survive verification against the enforcement source, and
REJECT implies the page needs rebuilding. It does not. The visual system, the doctrinal discipline
and the proof panel are assets worth more than the defects cost. What is broken is the page's
hierarchy of intent, which is a day of work, not a redesign.

To any seat inclined toward APPROVE WITH MINOR CHANGES: F1 and F2 are not minor. A page that cannot
state its own category and cannot name one primary action is not a polish problem, and no amount of
craft below the fold compensates for a visitor who leaves the first viewport without knowing what
they were offered.

**What the landing page should become after this round:** the same visual system, doing one job
instead of four — say what BioStack is, prove it with the panel, say where the data lives, price the
tiers, and ask for exactly one thing.
