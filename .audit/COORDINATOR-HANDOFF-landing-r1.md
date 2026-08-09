# COORDINATOR HANDOFF — BioStack Landing Page, Round 1 Remediation

**You are the implementation coordinator.** You do not write the code yourself. You partition the
work, delegate it, verify each return against its acceptance criteria, and report status. This
document is self-contained — you should not need to rediscover context, and you should not re-run
the review that produced it.

---

## 0. Mission

Execute the remediation for the BioStack landing page (`/`) following the Round 1 AI Council
directive. Five blind reviewers returned **REVISE AND RE-REVIEW** with three findings held as
launch-blocking. Your job is to land Waves 1 and 2 cleanly and to stage Wave 3 for human decision.

**You are NOT authorized to:** re-litigate severity rankings, redesign the visual system, expand
scope to other pages, or change copy that touches dosing/evidence language without routing it
through §7 escalation.

### Source of truth

| Item | Location |
|---|---|
| Repo | `D:\Repos\BioStack` (branch at review time: HEAD `9cecaf7`, 2026-08-03) |
| Working dir for all tasks below | `D:\Repos\BioStack\frontend` |
| Full directive | `.audit/DIRECTIVE-landing-round1.md` |
| Shared brief the reviewers read | `.audit/landing-brief.md` |
| Five raw seat verdicts | `.audit/seats/*-out.txt` |
| Governing doctrine | `docs/guidance/biostack-guidance-content-contract.v1.md` |

Read the directive's **Convergent Findings** section before delegating. Every task ID below maps to
a finding ID (F1–F12) there; agents who ask "why" should be pointed at the finding, not at you.

### Definition of done — whole engagement

1. Wave 1 (10 mechanical tasks) merged, with every acceptance criterion evidenced.
2. Wave 2 (5 structural tasks) merged behind a single owner.
3. Wave 3 staged as a written proposal awaiting Clint's sign-off — **not** merged.
4. `npm test`, `npm run lint`, `npm run build` all green in `frontend/`.
5. `.audit/ROUND1-COMPLETION.md` written, mapping each task ID to its evidence artifact.
6. Round 2 council re-review requested, with the conditions in §8 satisfied.

---

## 1. Environment and commands

All commands run from `D:\Repos\BioStack\frontend`. Package manager is **npm** (`package-lock.json`;
no pnpm/yarn — do not switch).

```
npm test          # vitest run — the regression net. MUST stay green.
npm run lint      # eslint
npm run build     # next build
npm run dev       # next dev on port 3043 (note: 3043, not 3000)
```

**Execution environment trap — read this before assigning anything.** There is no .NET SDK and no
Python runtime in the Cowork sandbox, and the sandbox is proxy-blocked from `biostack.cc`. For any
task requiring actual execution, use **Desktop Commander's `start_process`** against
`D:\Repos\BioStack` — its filesystem tools are scoped elsewhere but terminal commands work. Reach
for it early; do not let an agent claim a task complete on the strength of reading code.

**Device bridge limits.** `device_bash` cannot delete or rename files or directories in the working
tree — any task needing a delete or rename escalates to Clint (§7). Long `git status` / `git diff`
on this repo routinely exceed the 45s timeout; scope every git command to a pathspec.

---

## 2. Guardrails — non-negotiable

**G1 · The Guidance Content Contract outranks this directive.** Class D (personalized medical
direction) is prohibited: no dose selection, switching, tapering, or sourcing language may enter any
copy. If a task's fix conflicts with the contract, the contract wins and the task escalates.

**G2 · These test files are the regression net. They must stay green, and they must not be edited
to accommodate a change.** If a change requires weakening an assertion in these, the change is
wrong:

```
src/__tests__/components/HomePageHero.test.tsx          # banned prescriptive strings in the hero
src/__tests__/components/MarketingNavReadiness.test.tsx
src/__tests__/components/StackIntelligencePanel.test.tsx
src/__tests__/lib/marketing.test.ts
src/__tests__/conversion/launchSafetyCopy.test.ts       # doctrine copy net
src/__tests__/conversion/pr2PublicCopyPolish.test.ts    # doctrine copy net
```

`HomePageHero.test.tsx` explicitly asserts that `"What to take. How to use it."` and
`"optimize over time"` do **not** appear. Those assertions are the guard on Wave 3's headline
rewrite. An agent that "fixes" a failing test by relaxing it has failed the task.

**G3 · Preserve list — do not touch, do not "improve".** Four reviewers independently defended
these:
- The `"BioStack is not a doctor."` strip and its four-denial body — **verbatim**. Wave 2 moves its
  position; not one word changes.
- The `#0B0F14` system, the three ambient blooms in `layout.tsx`, glass and gradient-hairline
  surfaces.
- `prefers-reduced-motion` handling throughout `globals.css` and the panel.
- The entry-path card *pattern* (eyebrow / title / body / signal / action). The defect is duplicate
  destinations and count — not the device.
- Sticky header, and the mobile bar's 220px scroll trigger.

**G4 · One file, one owner, per wave.** Task lanes below are partitioned by **file ownership**, not
by topic, specifically to prevent merge conflicts. Do not reassign a file across lanes mid-wave.

**G5 · Evidence per merge.** Contrast changes ship with a measured ratio. Focus changes ship with a
tab-through recording or screenshot series. The og:image ships with an unfurl screenshot. "Looks
right" is not evidence.

---

## 3. WAVE 1 — Mechanical (start immediately, no Council ruling required)

Ten changes with objective acceptance criteria and zero design decisions. **Four lanes run in
parallel.** Each lane owns its files exclusively.

### Lane A — `StackIntelligencePanel.tsx` (sole owner)
| Task | Change | Acceptance |
|---|---|---|
| **1.3** (F9) | `text-white/35` → `text-white/56`; `text-white/42` → `text-white/56` | Both ≥4.5:1 measured with a contrast checker against the composited local background; paste both ratios into the task report |
| **1.8** (F9) | The `role="tablist"` / `role="tab"` / `aria-selected` toggle is half-built — no `tabpanel`, no `aria-controls`, no id wiring, no arrow keys. **Prefer removing the ARIA roles** and shipping a plain segmented button group; it is a filter, not a tab set. Completing the pattern is acceptable but higher cost | Screen reader announces something coherent. Currently it announces "tab 1 of 2" and strands the user — that state is worse than no pattern and must not survive |
| **1.9** (F9) | Rotating insight line has no pause/stop/hide (WCAG 2.2.2). **Prefer dropping the rotation entirely on the marketing surface**; add a pause control if the rotation is defended | No auto-updating content without user control on `/` |
| **1.5a** (F9) | Add `focus-visible` rings to this file's interactive controls, matching the hero cards' tone-matched treatment | Every control in this component has a visible ring on Tab |

### Lane B — `LandingHero.tsx` (sole owner)
| Task | Change | Acceptance |
|---|---|---|
| **1.1** (F7) | Line 79 is `<p>Built for peptides, SARMs, SERMs, and beyond</p>` with **no className** — it inherits raw preflight body styling and renders brighter and heavier than the subhead below the H1. Give it a real kicker treatment: uppercase, ~11–12px, `tracking-[0.18em]`, emerald-tinted, explicit margin | Eyebrow reads as designed hierarchy above the H1, not as body text. Side-by-side before/after screenshot in the report |
| **1.2** (F12) | Delete `lg:col-span-2` from the card grid — its parent is `flex flex-col`, so the class is dead | No visual change; class gone |

### Lane C — `MarketingFooter.tsx`, `MarketingNav.tsx`, `IntelligenceProofSection.tsx`, `LandingPathCard.tsx`
| Task | Change | Acceptance |
|---|---|---|
| **1.4** (F9) | Footer `text-white/45` → `text-white/60`. It currently measures **exactly 4.50:1** against a 4.5:1 threshold — passes on paper with zero margin, and antialiasing eats that in practice | ≥5.5:1 measured |
| **1.5b** (F9) | Propagate the hero cards' `focus-visible:ring-2` treatment to every nav link, both nav buttons, both proof-section buttons, both hero text links, and all 7 footer links. `LandingPathCard.tsx` already has the correct treatment — **use it as the template, do not redesign it** | Tab through the entire page; every stop has a visible ring. Currently 4 of ~18 controls do |
| **1.10** (F11) | Add `Pricing` and `Start Free` to the footer link row. The footer currently omits `/pricing`, `/start` and `/tools/analyzer` — the three commercial destinations | Both reachable from page bottom at every breakpoint |

### Lane D — `layout.tsx`, `site.ts`, `public/`
| Task | Change | Acceptance |
|---|---|---|
| **1.6** (F9) | Add a skip link to `#main` as the first focusable element. There is currently none | First Tab on page load reveals it; activating it moves focus to main |
| **1.7** (F10) | Add `og:image` (1200×630) and set `twitter.card = 'summary_large_image'`. `openGraph` currently has **no `images` key at all** and `public/` contains no OG asset — every share of biostack.cc unfurls as text-only | Paste the URL into Slack; card renders with image. Screenshot in report |
| **1.5c** (F9) | Add the skip link's own focus-visible treatment | Visible when focused |

### Wave 1 definition of done
All four lanes merged; `npm test` / `npm run lint` / `npm run build` green; a single tab-through
artifact covering the whole page; measured ratios recorded for 1.3 and 1.4; unfurl screenshot for
1.7. **Wave 1 does not block on Wave 2 or on any Council ruling — do not hold it.**

---

## 4. WAVE 2 — Structural (single owner; starts only after Wave 1 merges)

**Assign all five tasks to ONE agent.** These are not five changes; they are one coherent decision
about what the page is for, expressed in five files. Splitting them across contributors reproduces
the exact inconsistency being fixed. This is the most important orchestration instruction in this
document.

Wave 2 touches `LandingHero.tsx`, `MobileStickyCta.tsx`, `IntelligenceProofSection.tsx` and
`StackIntelligencePanel.tsx` — all of which Wave 1 also touches. **Serialize; do not overlap.**

| Task | Finding | Change | Acceptance |
|---|---|---|---|
| **2.1** | F1 · BLOCKER | Declare `/start` the single primary action. Make `Start Free` the only solid-emerald fill on the page; demote the proof-section `Analyze a stack` to outline | Exactly one solid-fill CTA exists in the rendered DOM. Verify by querying the built page, not by eye |
| **2.2** | F1 · BLOCKER | Rebuild `MobileStickyCta` as two buttons — `Start Free` solid, `Analyze` outline. Drop Evidence / Pricing / Provider (all survive in the footer after 1.10) | Desktop and mobile privilege the same destination. Currently they privilege opposite ones |
| **2.3** | F4 | Merge hero cards 1 ("Analyze a protocol") and 3 ("Analyze My Stack") — they are adjacent in the same row and resolve to the **identical URL**. Give the freed slot a distinct destination; `/how-it-works` is the strongest candidate since it also serves F2 | Four cards, four distinct URLs. Normalize the four different labels for `/tools/analyzer` to one |
| **2.4** | F5 | Strip `Operator required` / `Operator access` / `Operator and Commander members` from the hero entirely. Gating belongs at the destination or beside a price | Zero tier names appear above the fold without an accompanying price |
| **2.5** | F8 | Add a permanent "Illustrative example — not real user data" caption inside the panel border. **Then fix the leak:** every `nextAction` string in `src/lib/onboardingIntelligence.ts` is written for a logged-in user with a list in progress — the public landing page currently tells a logged-out stranger *"Save the list or add another item."* Replace with a navigational line on the marketing surface | Panel never instructs a logged-out visitor to act on state they do not have. Verify by loading `/` in a clean incognito session |

**2.5 is the one no reviewer caught** — it surfaced during verification. Do not let it get dropped as
a footnote; it is the clearest instance of the page implying functionality it does not possess.

### Wave 2 definition of done
Single owner, single PR. Full test suite green. A before/after capture at **1440px and 390px**.
Incognito load confirming 2.5.

---

## 5. WAVE 3 — Content (STAGE ONLY — do not merge)

Wave 3 requires decisions that are Clint's to make and copy that must clear the clinical safety
review gate. **Produce a written proposal; do not land it.**

| Task | Finding | What to propose |
|---|---|---|
| **3.1** | F2 · BLOCKER | Rewrite eyebrow + H1 so category and audience are legible in five seconds. The current H1 contains **no noun naming the product or category**. Demote the existing three-clause triad to subhead. Must remain descriptive, never prescriptive — `HomePageHero.test.tsx` assertions stay green (G2). Deliver **3 headline options**, not one |
| **3.2** | F2 · BLOCKER | One category sentence, used verbatim in `<title>`, meta description, JSON-LD, footer, and as a visible line under the H1. Four different self-descriptions currently ship simultaneously |
| **3.3** | F3 · BLOCKER + F5 + F6 | New section between proof and disclaimer rendering the **already-written, already-reviewed** `featuredFaqs` entries "Where is my data stored?" and "How is BioStack different from a spreadsheet?", plus an Observer/Operator/Commander strip with real prices from `pricingTiers` and one differentiator each. All source copy is in `src/lib/marketing.ts` and is currently never imported by the landing page |
| **3.4** | F6 | Move the "BioStack is not a doctor" strip up into/adjacent to the new trust block (copy verbatim per G3), and add a closing CTA block repeating the single primary action before the footer. Page currently ends on a negation |
| **3.5** | F-minor | Wording pass on "See what BioStack catches" and "Suggested next action" for the clinical safety copy review gate. **Context you must carry forward:** one reviewer called this a contract breach; that claim was checked against `src/lib/onboardingIntelligence.ts` and **refuted** — every `nextAction` string is navigational and Class D is never engaged. This is a perception/labeling risk only. Do not let it be re-escalated to a blocker |

**Wave 3 blocks on Clint for:** the category sentence (3.2), which headline ships (3.1), and whether
prices go on the landing page at all (3.3).

---

## 6. Sequencing

```
Wave 0  branch from HEAD; record baseline (test + lint + build all green) ─┐
                                                                           │
Wave 1  ┌─ Lane A  StackIntelligencePanel.tsx        1.3, 1.8, 1.9, 1.5a ─┤
        ├─ Lane B  LandingHero.tsx                   1.1, 1.2            ─┤  parallel
        ├─ Lane C  Footer/Nav/Proof/PathCard         1.4, 1.5b, 1.10     ─┤
        └─ Lane D  layout.tsx, site.ts, public/      1.6, 1.7, 1.5c      ─┘
                                                                           │
                          ── merge + full suite green ──                   │
                                                                           ▼
Wave 2  SINGLE OWNER, serialized                     2.1 → 2.2 → 2.3 → 2.4 → 2.5
                                                                           │
                          ── merge + 1440/390 capture ──                   │
                                                                           ▼
Wave 3  proposal document only — BLOCKED on Clint          3.1 … 3.5
                                                                           ▼
        Round 2 council re-review (see §8)
```

Wave 1 lanes have **zero file overlap** and can be delegated to four agents simultaneously.
Wave 2 has hard file conflicts with Lanes A and B — it must not start until Wave 1 is merged.

---

## 7. Escalation triggers

Stop and escalate rather than deciding, when:

| Trigger | Route to |
|---|---|
| A fix requires editing any test in G2 | **Clint** — the fix is wrong, not the test |
| Copy touches dosing, evidence tiers, switching, tapering, or sourcing | **Clinical safety copy review gate** — do not merge |
| A file or directory needs deleting or renaming | **Clint** — `device_bash` cannot do it |
| A task appears to require redesigning anything on the G3 preserve list | **Council** — that's a Round 2 question |
| Prices need to be published on the landing page | **Clint** — commercial decision, not an implementation one |
| An agent argues a finding's severity is wrong | **Do not adjudicate.** Log it for Round 2 and proceed |

**One finding is explicitly open to challenge:** F3 (no data-custody statement) was promoted to
BLOCKER by the orchestrator over a 3-of-5 MAJOR vote. If Clint or a Round 2 seat wants to argue it
down to MAJOR, that is a legitimate reversal — log it, don't defend it reflexively.

---

## 8. Reporting contract

Per task, the executing agent returns exactly:

```
TASK: <id>   FINDING: <Fn>   FILES: <paths touched>
CHANGE: <one sentence, what actually changed>
ACCEPTANCE: PASS | FAIL | BLOCKED — <the evidence, or what blocked it>
EVIDENCE: <path to screenshot / measured ratio / command output>
SUITE: test <pass|fail> · lint <pass|fail> · build <pass|fail>
```

Per wave, you return a rollup: task IDs by status, evidence artifact index, any escalations opened,
and remaining blockers.

At completion, write `.audit/ROUND1-COMPLETION.md` mapping every task ID to its evidence, then
request **Round 2**. Round 2 must satisfy conditions Round 1 could not:

- Run from **m0r6an**, not the cloud sandbox — Round 1's Grok, Codex and Gemini CLI seats were
  unreachable and `biostack.cc` was proxy-blocked from both sandboxes and from WebFetch.
- Include **full-page and 390px live captures** in evidence. Round 1 had only a partial desktop
  screenshot; a window kept masking Chrome.
- Vendor homogeneity (four Claude-model lenses) is Round 1's honest methodological weakness. It
  should not repeat.

---

## 9. Two-line summary for whoever you delegate to

> The landing page is a routing screen for a product it never names, and its loudest button is an
> unpriced gate. The visual system is good and stays; the hierarchy of intent is what's broken.
