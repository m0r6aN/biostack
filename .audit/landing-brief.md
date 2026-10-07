# BRIEF — BioStack Landing Page (biostack.cc `/`) — Adversarial Review, Round 1

You are a voting member of the BioStack AI Council. This brief contains the complete objective
inventory of the page under review plus tool-verified measurements. It contains NO opinions and
NO conclusions. Form your own.

---

## 1. Context

**Product.** BioStack is a local-first biometrics and protocol observability platform. Users log
compounds (peptides, SARMs, SERMs and similar), run reconstitution/dose math, get pathway-overlap
detection across their active stack, do daily check-ins across ~15 biomarkers, and correlate all of
it on a unified timeline.

**Audience (per the product's own FAQ copy):** "serious self-experimenters who manage complex
compound protocols and need more than a spreadsheet."

**Plans.** Three tiers: **Observer** (free), **Operator** (paid, "Track & Analyze"),
**Commander** (paid, "Longitudinal Intelligence"). Prices are not printed on the landing page.

**Non-negotiable doctrine (the "Guidance Content Contract").** BioStack must never read as
prescriptive or as medical authority. Unit tests actively assert that banned prescriptive strings
do NOT appear in the hero — e.g. `"What to take. How to use it."` and `"optimize over time"` are
explicitly asserted absent in `HomePageHero.test.tsx`. The product does not prescribe, diagnose,
recommend compounds, or suggest dosages. **Any recommendation you make must survive this
constraint.** Copy that solves a marketing problem by implying medical guidance is invalid.

**Scope.** The landing page (`/`) ONLY. Not secondary pages, not the app, not the backend.

---

## 2. Inventory — what is on the page, in document order

Source of truth: `frontend/src/app/page.tsx` and its imported components, at repo HEAD `9cecaf7`
(2026-08-03). Page background is `#0B0F14` (near-black); the whole site is dark-theme only. A fixed
ambient layer sits behind everything: three large blurred radial blooms (emerald at 5.5% opacity,
blue at 4%, violet at 3.5%).

The page renders exactly **five** blocks: nav, hero, proof section, disclaimer strip, footer.

### 2.1 Sticky header (`MarketingNav.tsx`)
Sticky, `bg-[#0B0F14]/75`, `backdrop-blur-xl`, bottom border `white/8`.

- Left: BioStack logo (horizontal, dark theme, `animated` and `hoverable` props on)
- Center nav links (hidden below `md`), `text-sm text-white/55`, in this order:
  `How it works` · `Tools` · `Compounds & Evidence` · `Pricing` · `For Providers` · `Safety`
- Right, two buttons:
  - `Analyze My Stack` → `/tools/analyzer` — outlined pill, `border-white/12`, `text-white/75`, hidden below `sm`
  - `Start Free` → `/start` — emerald-tinted pill, `border-emerald-300/30 bg-emerald-400/12 text-emerald-100`, always visible

### 2.2 Hero (`LandingHero.tsx`)
Container: `min-h-[calc(90svh-61px)]` mobile / `calc(90svh-73px)` sm / `calc(88svh-73px)` lg —
i.e. the hero occupies ~88–90% of the viewport height on its own.

**Eyebrow line, verbatim, with its complete source markup:**
```jsx
<p>Built for peptides, SARMs, SERMs, and beyond</p>
```

**H1**, `text-[2.45rem]/sm:text-6xl/lg:text-7xl`, `font-semibold`, `leading-[0.96]`, hard `<br />`:
```
What you're taking. How it's structured.
See what it's doing.
```

**Subhead**, `text-base sm:text-lg text-white/64`:
```
Start with clarity. Then track, compare, and observe changes over time.
```

**Four entry-path cards** (grid: 1 col mobile → 2 cols `sm` → 4 cols `lg`; each
`min-h-[132px]` → `154px` at `lg`). Each card = eyebrow label, title, body, a colored "signal"
string bottom-left, an action word + `>` bottom-right, and a bordered box on the right containing
the literal ASCII character `>` as its icon (`sm` and up). Left edge has a 4px glowing colored rail.

| # | Label | Title | Body | Signal | Action | Destination | Tone |
|---|---|---|---|---|---|---|---|
| 1 | Analyzer | Analyze a protocol | Operator and Commander members can review a pasted, uploaded, scanned, or linked stack. | Operator required | Analyze | `/tools/analyzer` | cyan |
| 2 | Starter | I am getting started | Set up compound tracking without rebuilding a spreadsheet. | Guided | Start | `/start` | emerald |
| 3 | Existing stack | Analyze My Stack | Review active compounds, overlap signals, and timeline context with Operator. | Operator access | Analyze | `/tools/analyzer` | sky |
| 4 | Provider | I work with clients | Request access to the provider pilot for permissioned observational workflows. | Pilot request | Request | `/providers` | gold |

**Two text links below the cards:**
- `Need to calculate dose volume or reconstitution? → Start here` → `/tools` (`text-white/62`)
- `See Observer, Operator, and Commander` → `/pricing` (`text-emerald-100/78`)

### 2.3 Proof section (`IntelligenceProofSection.tsx`)
Two columns at `lg` (`0.82fr / 1.18fr`), stacked below. Background `bg-black/15`.

- **H2**: `See what BioStack catches`
- Body: `Paste a stack and BioStack turns raw compound names into structured context: parsed items, relationship checks, evidence-aware previews, and a clear upgrade path when deeper analysis is useful.`
- Two buttons:
  - `Analyze a stack` → `/tools/analyzer` — **solid emerald fill**, `bg-emerald-400 text-slate-950`
  - `See what Operator unlocks` → `/pricing` — outlined, `border-white/12`
- Right column: `StackIntelligencePanel` — a **hardcoded static mock**, not live product output.
  Props passed: `compoundNames={['BPC-157','TB-500']}`, one relationship candidate of type
  `overlap`, label `BPC-157 + TB-500`, detail `tissue-repair overlap: educational reference only,
  with full evidence detail in Operator.`

  Panel anatomy (top to bottom): gradient hairline border wrapper; eyebrow (uppercase, 11px,
  `text-emerald-300/72`); a status pill; a subtext line; **a two-option toggle** labelled
  `List` / `Evidence` with an animated sliding pill (`role="tablist"`, buttons `role="tab"`,
  `aria-selected`); a stage-label strip (`Compounds added / Relationships mapped / Next step ready`)
  at `text-white/42`; a 3-up stats row with labels at `text-white/35`; relationship cards; an
  "insight" block; a rotating insight line; and a `Suggested next action` block.

  **Motion:** the insight line auto-rotates on a `setInterval` — `LOOP_DURATION` is 7.2s divided by
  the number of insights. There is no pause/stop/hide control. `useReducedMotion()` is honored and
  disables the rotation and the transitions.

### 2.4 Disclaimer strip (inline in `page.tsx`)
A single bordered row, `border-white/8 bg-white/[0.025]`:
- Bold, `text-white` `sm:text-lg`: `BioStack is not a doctor.`
- Body, `text-sm text-white/56`: `BioStack organizes tracking, math, overlap context, and evidence references. It does not prescribe, diagnose, recommend compounds, or replace qualified medical care.`

### 2.5 Footer (`MarketingFooter.tsx`)
`border-t border-white/8 bg-black/20`, all text `text-sm text-white/45`.
- Left: `BioStack. Tracking, math, and clarity for complex stacks.`
- Right, one flat row of 7 links: `How it works` · `Tools` · `Compounds & Evidence` ·
  `For Providers` · `Safety` · `Terms` · `Privacy`

### 2.6 Mobile-only sticky bottom bar (`MobileStickyCta.tsx`)
`md:hidden`. Hidden until `window.scrollY > 220`, then slides up. `aria-label="Primary actions"`.
Fixed to the bottom, `grid-cols-4`, four equal-width buttons, each `min-h-12`:

| Button | Style | Destination |
|---|---|---|
| `Analyze` | **solid emerald fill**, `text-slate-950` | `/tools/analyzer` |
| `Evidence` | outlined cyan tint | `/knowledge` |
| `Pricing` | outlined emerald tint | `/pricing` |
| `Provider` | outlined amber tint | `/providers` |

Note: `/start` is not present in this bar. The page body carries `pb-24 md:pb-0` to reserve space.

### 2.7 Metadata (`site.ts`, `page.tsx`)
- `<title>`: `BioStack | Protocol Operations`
- `description`: `Your protocol operations system. Track compounds, surface overlap, and turn daily signal into continuity.`
- OpenGraph: type/locale/url/siteName/title/description — **no `images` key of any kind**
- Twitter: `card: 'summary'` (not `summary_large_image`), no image
- `<html lang="en">`; JSON-LD `SoftwareApplication` present, its `description` field reads
  `Tracking, calculator, and stack mapping infrastructure for compound protocols.`
- `robots.ts` allows `/`; `sitemap.ts` lists 16 URLs

### 2.8 Defined but NOT rendered on this page
`frontend/src/lib/marketing.ts` exports `landingFeatures` (6 feature strings) and `featuredFaqs`
(10 Q&A pairs, including "Where is my data stored?" and "How is BioStack different from a
spreadsheet?"). Neither is imported by `page.tsx` or by any component the landing page renders.

---

## 3. Verified observations (tool-established facts, not interpretations)

**O1 — Eyebrow markup.** `LandingHero.tsx:79` is `<p>Built for peptides, SARMs, SERMs, and beyond</p>`
with no `className` attribute. Every other text node in that file carries explicit utility classes.
Under Tailwind v4 preflight this element inherits body styling: 16px, `rgba(255,255,255,0.9)`,
no margin, no letter-spacing, no uppercase treatment.

**O2 — Destination collision.** `/tools/analyzer` is the target of **4 distinct controls** on
desktop (nav `Analyze My Stack`; hero card 1 `Analyze a protocol`; hero card 3 `Analyze My Stack`;
proof-section `Analyze a stack`) and **4 on mobile** (hero cards 1 and 3; proof-section button;
sticky-bar `Analyze`). Hero cards 1 and 3 are adjacent in the same 4-card row and resolve to the
same URL. `/pricing` is the target of 3 controls (nav, hero text link, proof-section button;
plus the mobile sticky bar).

**O3 — Interactive control count above/near the fold.** Desktop first viewport: 6 nav links +
2 nav buttons + 4 cards + 2 text links = **14 interactive targets** before any scroll.
Mobile after 220px of scroll, 4 more appear in the sticky bar.

**O4 — The only solid-filled buttons on the page.** Exactly two elements use a solid emerald fill
(`bg-emerald-400`, dark text): the proof-section `Analyze a stack`, and the mobile sticky bar's
`Analyze`. Both resolve to `/tools/analyzer`. Every other CTA on the page — including
`Start Free` — is an outline or tinted-translucent treatment.

**O5 — Gating language precedes any pricing information.** The strings `Operator required`,
`Operator access`, `with Operator`, `Operator and Commander members`, and
`See Observer, Operator, and Commander` all appear in the hero. The words Observer / Operator /
Commander are never defined, differentiated, or priced anywhere on the landing page.

**O6 — Contrast measurements** (computed against the composited local background, WCAG 2.1
normal-text threshold 4.5:1; large-text threshold 3.0:1):

| Element | Class | Size | Ratio | AA normal text |
|---|---|---|---|---|
| Panel stat labels | `text-white/35` | 10px uppercase | **≈3.1:1** | FAIL |
| Panel stage labels | `text-white/42` | 11px | **≈4.0:1** | FAIL |
| Card eyebrow labels | `text-white/46` | 11px uppercase | ≈4.6:1 | pass (margin ≈0.1) |
| Footer text + all 7 footer links | `text-white/45` | 14px | **≈4.50:1** | pass (exactly at threshold) |
| Nav links | `text-white/55` | 14px | ≈6.2:1 | pass |
| Hero subhead | `text-white/64` | 16–18px | ≈8.0:1 | pass |

**O7 — Focus treatment is inconsistent.** The four hero cards declare
`focus-visible:outline-none focus-visible:ring-2` with a tone-matched ring color. The 6 nav links,
both nav buttons, both proof-section buttons, both hero text links, all 7 footer links, and all 4
sticky-bar buttons declare **no** focus-visible styling and no `:focus` styling; they fall back to
the UA default ring. There is no skip link anywhere in the document.

**O8 — Incomplete ARIA tab pattern.** The panel's toggle uses `role="tablist"` and two
`role="tab"` buttons with `aria-selected`, but there is no element with `role="tabpanel"`, no
`aria-controls`, no `id` wiring, and no arrow-key handler. Under the WAI-ARIA tabs pattern a
screen-reader user is told "tab 1 of 2" with no panel to move to, and arrow keys do nothing.

**O9 — Auto-updating content with no user control.** The panel's insight line replaces itself on a
timer with no pause, stop, or hide affordance. `prefers-reduced-motion` disables it.

**O10 — Share-preview payload is empty.** No `og:image`, no `twitter:image`, and
`twitter:card = summary`. Any link to biostack.cc posted to Slack, X, LinkedIn, iMessage or Discord
renders as a text-only or small-icon unfurl.

**O11 — Dead class.** `LandingHero.tsx` applies `lg:col-span-2` to the card grid, but that grid's
parent is `flex flex-col`, not a grid container. The class has no effect.

**O12 — Zero third-party or quantitative trust signals.** The rendered page contains no customer
logos, no testimonials, no user/protocol counts, no security or privacy badge, no compliance
mention, no team or company information, no "as seen in", and no dates. The FAQ answer covering
data storage exists in `marketing.ts` but is not rendered here.

**O13 — Four different self-descriptions ship simultaneously.** `<title>`: "Protocol Operations".
Meta description: "Your protocol operations system." JSON-LD: "Tracking, calculator, and stack
mapping infrastructure for compound protocols." Footer: "Tracking, math, and clarity for complex
stacks." Product FAQ (elsewhere): "a personal bio-protocol operating system." The H1 itself names
no category.

**O14 — Primary action inverts across breakpoints.** On desktop the persistently visible,
color-privileged CTA is `Start Free` → `/start`. On mobile below `md`, the sticky bar's
color-privileged CTA is `Analyze` → `/tools/analyzer`, and `/start` has no representation in that
bar at all.

**O15 — Page length.** Five blocks total, with the hero alone consuming ~88–90svh. Excluding nav
and footer, the page has exactly two content sections.

---

## 4. Your task

Review the BioStack landing page as an adversarial reviewer. Assume it is **not** ready for launch
until evidence proves otherwise. Judge it against the standard of exceptional modern technology
companies, not against average websites.

Evaluate it as a first-time visitor's journey, not as a list of components: what do I think this is
in 5 seconds; who is it for; what problem does it solve; why is it different; is it credible; does
each section earn the next; is the primary action obvious; do secondary actions compete with it; by
the end, do I know what to do next.

Every criticism must connect to a defensible principle — usability, hierarchy, comprehension, trust,
consistency, redundancy, accessibility, conversion, or positioning. Personal aesthetic preference is
not a finding. Do not recommend change merely to demonstrate activity: if something is already
excellent, say so and say it should be preserved.

Severity vocabulary: **BLOCKER** (should prevent launch) / **MAJOR** / **MINOR** / **POLISH**.
Do not inflate. Reserve BLOCKER for issues that genuinely justify withholding launch approval.

---

## 5. Output contract — follow EXACTLY

Plain text. No preamble, no closing remarks, no markdown headers beyond the three section labels
below. Maximum 600 words total.

```
VERDICT: <one of: APPROVE | APPROVE WITH MINOR CHANGES | REVISE AND RE-REVIEW | REJECT CURRENT FORM>

TOP 6 BRUTAL FINDINGS
1. [SEVERITY] <finding> — <the specific evidence> — <why it matters, one clause>
2. ...
(exactly 6, ordered most-damaging first, one line each)

TOP 5 MOVES
1. <implementation-ready change, naming the target element and the outcome>
2. ...
(exactly 5, ordered by impact)

PRESERVE
- <element that should survive revision unchanged, and why> (2-4 bullets)
```
