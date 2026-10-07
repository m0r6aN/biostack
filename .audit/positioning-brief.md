# BRIEF — BioStack Positioning, Phase 2 (generative)

You are a member of the BioStack AI Council. **This round is generative, not adversarial.** Round 1
audited an existing page and converged on its flaws. Your job here is the opposite: produce
*divergent, high-quality options* from your assigned angle. Another process will score and
synthesize them. Do not hedge toward a safe middle — a distinctive option that gets rejected is more
useful to this process than a generic one that survives.

You are drafting two artifacts. A third (a data-custody statement) was removed from this round
because the underlying legal policy is unapproved — do not attempt it, and do not reference data
storage, privacy, encryption, or custody anywhere in your output.

---

## 1. What BioStack is (factual, verified against code)

A web platform for people running complex compound protocols — peptides, SARMs, SERMs and similar.
It does five things:

1. **Logs compounds** and what the user is actually taking, with structure instead of a spreadsheet
2. **Does the math** — reconstitution (dissolving lyophilized powder into injectable solution),
   dose volume, unit conversion. Free, no account required
3. **Detects pathway overlap** — flags when two or more compounds in a stack share biological
   pathways, with evidence-confidence levels attached
4. **Daily check-ins** across ~15 tracked signals: weight, sleep, energy, appetite, recovery, focus,
   thought clarity, skin quality, digestion, strength, endurance, joint pain, eyesight, mood, side
   effects
5. **Unified timeline** correlating compound events, protocol phases, and check-ins over time

**Audience, in the product's own words:** "serious self-experimenters who manage complex compound
protocols and need more than a spreadsheet. It is for users who already do the research and want a
system that keeps up with the complexity."

There is a second, smaller audience: **providers** who work with clients, served by a pilot program
for permissioned observational workflows.

**Evidence tiers** are a real product concept: Strong / Moderate / Limited / Mechanistic, used to
classify how well-supported a compound's effects are in the literature.

### Tier facts (from `contracts/product-contract.v1.json`, contractVersion 1.0.0)

| Code | Display name | Current tagline | Price | CTA destination |
|---|---|---|---|---|
| `observer` | Observer | Free | **$0** | `/start` |
| `operator` | Operator | Track & Analyze | **$12/mo** | `/billing?plan=operator` |
| `commander` | Commander | Longitudinal Intelligence | **$29/mo** | `/billing?plan=commander` |

Current per-tier descriptions, verbatim from `frontend/src/lib/marketing.ts`:

- **Observer** — "A simple place to track what you're taking and stop relying on notes, memory, or
  scattered apps." Highlights: free calculators · public compounds and evidence library · up to 8
  active compounds · basic compound tracking · protocol overview and daily schedule · supplement and
  resource records · unknown states and safety warnings · local tool history
- **Operator** — "Everything you need to track compounds, log results, and view reviewed protocol
  relationships without medical-authority copy." Highlights: full protocol analysis ·
  current-stack relationship intelligence · weekly protocol calendar · diet and lifestyle framework ·
  progress and milestone tracking
- **Commander** — "Advanced reviewed intelligence for ambiguity analysis and longitudinal
  observational reports." Highlights: protocol review across run history · pattern memory snapshots ·
  protocol drift snapshots · sequence expectation snapshots · monitoring and lab view ·
  cross-protocol mission control

---

## 2. The problem being solved (verified: this is the current state)

The product currently describes itself **six different ways across six surfaces**:

| Surface | Current self-description |
|---|---|
| `<title>` | Protocol Operations |
| meta description | Your protocol operations system |
| JSON-LD | Tracking, calculator, and stack mapping infrastructure for compound protocols |
| landing footer | Tracking, math, and clarity for complex stacks |
| `/safety` page body | Infrastructure for tracking, math, and clarity |
| FAQ | A personal bio-protocol operating system |

The landing page `<h1>` — "What you're taking. How it's structured. See what it's doing." —
contains **no noun naming the product or its category**. A five-seat review found that a first-time
visitor cannot state what BioStack is after reading the first viewport.

---

## 3. Hard constraints — an option that violates any of these is discarded unread

**C1 · Non-prescriptive doctrine (absolute).** BioStack must never read as prescriptive or as
medical authority. It does not prescribe, diagnose, recommend compounds, suggest dosages, or advise
on switching, tapering, or sourcing. The operative stance, stated by the founder: *"We are not
doctors and we do not provide any information on a prescriptive basis. Instead, we present all
available information, based on our research, which is to the best of our knowledge factual
evidence, as a way for users to make their own choices."* Presenting sourced evidence with
uncertainty markers is permitted. Directing a person's protocol is not.

**C2 · These exact strings are banned and are enforced by a passing unit test.** Your output must
not contain them or close paraphrases: `"What to take. How to use it."` · `"optimize over time"`.

**C3 · Truth constraint.** Claim nothing the product cannot do. Specifically: **do not claim
local-first, on-device, offline, browser-only, or zero-knowledge storage.** Storage is server-side.
Do not mention data storage at all (see §0).

**C4 · No invented proof.** No user counts, customer names, funding, press, certifications,
compliance standards, or clinical validation. None of it exists yet.

**C5 · Legibility to a stranger.** The category sentence must be comprehensible to someone who has
never heard of BioStack and does not know what "reconstitution" or "pathway overlap" means. It may
use the words peptide or compound.

---

## 4. Tonal reference — the best sentence currently on the site

From `/safety`, and the reviewers rated it the most confident line on the public site:

> **"No prescriptions. No guesswork. Just structure."**

and its H1:

> **"BioStack is not a doctor."**

Blunt, short, unhedged, zero marketing throat-clearing. That is the register. Your options should
sound like they came from the same company that wrote those lines. What the site currently lacks is
a sentence with that confidence that says what BioStack **is** rather than what it isn't.

---

## 5. Your task

### Artifact A — the category sentence
One sentence that says what BioStack is and who it's for, to be deployed **verbatim in five
places**: `<title>` context, meta description, JSON-LD description, landing footer, and as a visible
line beneath the H1. It must survive all five placements without rewording.

Produce **three distinct candidates from your assigned angle** — not three phrasings of one idea,
three genuinely different bets. For each, also supply the H1 it would sit beneath (the H1 may be
rewritten; the current one is not protected).

### Artifact B — the tier ladder
Rewrite Observer / Operator / Commander so a stranger understands, in one line each, what the tier
is for and why they'd move up. The names are fixed. The prices are fixed and must appear. The
current taglines ("Free", "Track & Analyze", "Longitudinal Intelligence") are **not** protected —
"Longitudinal Intelligence" in particular tells a newcomer nothing.

The ladder must make the *upgrade logic* obvious: what changes between $0 and $12, and between $12
and $29.

---

## 6. Scoring rubric — your options will be scored on these, weighted equally

1. **Legibility** — a stranger reads it once and can state the category
2. **Truth** — every claim is supported by §1; nothing invented, nothing storage-related
3. **Doctrine safety** — passes C1 and C2 cleanly, with no strained reading required
4. **Distinctiveness** — could not be pasted onto a generic SaaS competitor without becoming false
5. **Register match** — sounds like "No prescriptions. No guesswork. Just structure."
6. **Placement durability** — reads correctly in all five placements, including as a bare `<title>`

---

## 7. Output contract — follow EXACTLY

Plain text. No preamble. Maximum 550 words.

```
ANGLE: <one line restating your assigned angle in your own words>

ARTIFACT A — CATEGORY SENTENCE
A1. SENTENCE: <the sentence>
    H1: <the headline it sits beneath>
    BET: <the strategic bet this option makes, one line>
A2. SENTENCE: ...
    H1: ...
    BET: ...
A3. SENTENCE: ...
    H1: ...
    BET: ...

ARTIFACT B — TIER LADDER
OBSERVER ($0): <tagline> — <one line: what it's for>
OPERATOR ($12/mo): <tagline> — <one line: what it's for, and what triggers the upgrade from Observer>
COMMANDER ($29/mo): <tagline> — <one line: what it's for, and what triggers the upgrade from Operator>

STRONGEST — <which of your three A-options you'd ship, and the single best reason>
RISK — <the most likely objection to your strongest option>
```
