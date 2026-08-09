# POSITIONING ARTIFACTS — Phase 2 (4-seat generative council)

**Evidence:** `.audit/positioning-brief.md` · four blind seats (category-first, audience-first,
problem-first, contrast-first) · 12 candidate sentences scored against the brief's 6-criterion rubric.
**Status:** proposal awaiting Clint's sign-off. Nothing here is merged.

---

## The convergence

**Three of four seats, working blind from different angles, landed on the same noun phrase:**

| Seat | Its own strongest candidate |
|---|---|
| Category-first | "BioStack is a **tracking app for people who run peptide and compound protocols**" |
| Audience-first | "BioStack is the **tracking system for people running compound protocols**" |
| Problem-first | "…BioStack is a **tracking system for people running peptide and compound protocols**" |

The fourth (contrast-first) produced the same shape with a different adjective — "the structured
system serious self-experimenters use to log complex compound protocols."

**That's settled.** The category noun is **tracking system**; the qualifier is **people running
compound protocols**. Four angles, one answer, no coordination. This is no longer an open question —
the remaining decisions are about what surrounds it.

---

## ARTIFACT A — the category sentence

### Recommended

> **BioStack is a tracking system for people running complex compound protocols — the log, the dose math, and the overlap checks in one place.**

Synthesized from Category-first's durability, Problem-first's concreteness, and Audience-first's
specificity. Scores against the rubric: every claim maps to a shipped feature; three plain nouns and
no adjectives (register match to "No prescriptions. No guesswork. Just structure."); nothing in it
could be pasted onto a generic SaaS competitor without becoming false; and it reads correctly in all
five placements including as a bare `<title>` suffix.

### The graft that resolves the seats' main disagreement

Seats split on whether to name the compound classes. Audience-first named all three
(peptides/SARMs/SERMs) and **flagged its own risk**: in `<title>` and JSON-LD that reads like a
supplement-industry SEO string rather than a confident product line. Category-first named one.
Contrast-first named none.

**Resolution: the compound classes move to the hero eyebrow, which already exists and is already
being rebuilt.** Wave 1 task 1.1 is restyling `LandingHero.tsx:79` — currently the unstyled
`<p>Built for peptides, SARMs, SERMs, and beyond</p>`. Let that element carry the compound-class
specificity as a styled kicker, and the category sentence stays clean and durable everywhere else.

No new component. The fix already in flight absorbs the requirement.

### Hero architecture

```
EYEBROW   Peptides · SARMs · SERMs · and beyond          ← task 1.1, already queued
H1        <see options below>                             ← your call
SUBHEAD   BioStack is a tracking system for people running complex compound
          protocols — the log, the dose math, and the overlap checks in one place.
```

With the category sentence sitting directly beneath the H1, **F2 closes regardless of which H1 you
pick** — the headline no longer has to carry the category alone, which is what broke it.

### H1 options — this one is yours to decide

| | H1 | Origin | Why it works | Watch out |
|---|---|---|---|---|
| **1 · recommended** | **"Six months in, and you still can't say what changed."** | Problem-first A2 | Names a frustration the reader has actually had, and it's the one a spreadsheet genuinely cannot solve — so it earns the timeline, your real differentiator. Doctrine-clean: promises a record, never an outcome. | Opens negative. Needs the subhead immediately beneath it to land. |
| 2 | "Some guy on the internet is not a protocol." | Problem-first A3 | The most distinctive line any seat produced, and it encodes your stated motive — stopping people from following instabro advice blindly — almost verbatim. Perfect register. | **Doctrine risk:** positions BioStack as the *better source of advice*. That brushes C1. I'd want it through the copy review gate, not merged on my say-so. |
| 3 | "BioStack doesn't advise. It structures." | Contrast-first A1 | Flawless doctrine, reads as a direct continuation of "BioStack is not a doctor." | Low information for a stranger. Leans on the subhead entirely. |

I'd ship **1**, hold **2** for the safety copy gate because I think it's the best line here and I'm
not willing to wave it through myself, and keep **3** as the fallback if 1 tests as too negative.

---

## ARTIFACT B — the tier ladder

All four seats converged on the same upgrade *logic*, which is the part that was missing: you move
up when the **unit of interest** changes — from one compound, to a stack, to a history.

| Tier | Price | Recommended tagline | One line |
|---|---|---|---|
| **Observer** | **$0** | *The math, and a place to put it.* | Free calculators with no account, the public compound and evidence library, and structured tracking instead of notes and memory. |
| **Operator** | **$12/mo** | *For stacks, not single compounds.* | Full protocol analysis, reviewed relationships between the compounds you're currently running, a weekly calendar, and daily check-ins logged against what you took. **You upgrade when the items stop being independent.** |
| **Commander** | **$29/mo** | *Compare this run to your last one.* | Reads across every run you've logged: pattern and drift snapshots, sequence expectations, monitoring and lab view, cross-protocol mission control. **You upgrade when one run stops being the interesting unit.** |

Taglines are grafted from Category-first, which produced the clearest set. Note what this replaces:
**"Longitudinal Intelligence" → "Compare this run to your last one."** Same tier, same feature set,
and now a newcomer knows what it's for.

Publishing these three prices on the landing page **closes F5 outright** — the hero's "Operator
required" gating language becomes legible the moment a number sits next to it.

---

## ⚠️ Verification flags — do not ship these without checking

**V1 · The 8-compound cap may not be real, and three seats built the upgrade story on it.**
Category-first, audience-first and problem-first all wrote some version of *"Operator removes the
8-compound cap."* Checked against the source: `contracts/product-contract.v1.json` declares
**no entitlements, limits, or features block on any of the three plans** — all three return
`(none declared)`. "Up to 8 active compounds" exists **only as a marketing string** in
`frontend/src/lib/marketing.ts`. The one `maxCompounds` in the codebase (`frontend/src/lib/api.ts:356`)
is an analyzer request parameter, not a plan entitlement.

So: either the cap is enforced somewhere I haven't found, or it is a claim with nothing behind it.
**Confirm before any tier copy references it.** The recommended Operator line above deliberately
does *not* mention the cap for this reason.

**V2 · Nothing here mentions data storage, and that's intentional.** The custody artifact was pulled
from this round — the policy is unapproved and `/privacy` and `/terms` are both stubs. No seat was
given the option to write custody copy, so none of these candidates carry that risk.

**V3 · Every feature claim above traces to `marketing.ts` or the product contract.** No counts, no
customers, no compliance, no clinical language. C2's banned strings appear nowhere.

---

## What's still blocked

| Item | Blocked on |
|---|---|
| Data-custody statement | Approved Privacy Policy. `/privacy` and `/terms` are noindex stubs that cite the commercialization brief as blocking launch. **Legal, not copywriting.** |
| H1 option 2 | Clinical safety copy review gate |
| Any tier copy referencing the 8-compound cap | V1 verification |
| Wave 3 execution | Your sign-off on the category sentence + H1 choice above |

Once you sign off on the sentence and the H1, Wave 3 tasks 3.1, 3.2 and 3.3 are unblocked and the
coordinator can proceed — F2 and F5 both close, and F3 becomes a legal deliverable rather than an
open design question.
