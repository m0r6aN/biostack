# W3-P0 — Prerequisite reconciliation (evidence only)

**Date:** 2026-09-24 · **Base inspected:** `origin/main` = `e5b75e0` (fetched; local `main` identical) · No product files touched.
Local working tree has unrelated uncommitted edits (`dosingCalculator*`, `ToolsDecisionSurface.tsx`); all evidence below is read from `origin/main` via `git show` / `git grep`, not the working tree.

## Verdict

**D3 prerequisite NOT met. Wave 1 is merged; Wave 2 is not.** W3-P1..P4 stay blocked. Separately, the charter's "Verified starting state" is stale: part of the signed Wave 3 copy has already landed outside this goal.

## Wave 1 — MERGED (PR #248, `codex/landing-r1`, merged 2026-08-08, merge `1d944b2` is an ancestor of `origin/main`)

| Task | Evidence on `origin/main` |
|---|---|
| 1.1 eyebrow kicker | `LandingHero.tsx:84` `uppercase tracking-[0.18em] text-emerald-200/78` |
| 1.4 footer contrast | `MarketingFooter.tsx:6` `text-white/60` |
| 1.10 footer Pricing/Start Free | `MarketingFooter.tsx:25,28` `/pricing`, `/start` |
| 1.9 rotation removed | no `setInterval` in `components/marketing` |
| 1.8 tablist | `StackIntelligencePanel.tsx:245-259` keeps `role="tablist"/"tab"`; PR #248 body says it "completes its keyboard tab behavior" (packet-permitted alternative). Not re-verified behaviorally. |
| Caveat | PR #248 body: `npm run lint` timed out, `npm run build` not completed, tab-through not done — Wave 1 DoD evidence was incomplete at merge. |

## Wave 2 — NOT MERGED (no Wave 2 PR found in last 100 PRs; no branch)

| Task | Evidence on `origin/main` |
|---|---|
| 2.4 strip hero gate strings | **FAIL** — `LandingHero.tsx:31` `signal: 'Operator required'` |
| 2.3 merge duplicate cards | **FAIL** — hero still has `Analyze a protocol` → `/tools/analyzer` (4 cards; dedupe with card 3 not evidenced) |
| 2.2 two-button sticky CTA | **FAIL** — `MobileStickyCta.tsx:56-71` still links Evidence / Analyze / Pricing (+ console/start) |
| 2.5 illustrative caption + nextAction leak | **FAIL** — no "Illustrative example" caption; `onboardingIntelligence.ts:274,308` still `'Save the list or add another item.'` |
| 2.1 single solid CTA | Not verified (needs rendered-DOM query) |

## Charter drift (charter written against `e83bb09`, 2026-08-09; main has advanced)

Commit `74b4a7a` (#253, 2026-08-28) already shipped part of the Wave 3 copy, with **deviations from the signed packet**:

| Placement | Signed packet | `origin/main` |
|---|---|---|
| Hero H1 | No prescriptions. No guesswork. Just what's known. | **Matches** (`LandingHero.tsx:87-89`) |
| `/safety` echo | Just what's known. | **Matches** (`safety/page.tsx:45`) |
| Eyebrow | `Peptides · SARMs · SERMs · and beyond` | `Evidence-graded research on peptides, SARMs, SERMs, and beyond` |
| Subhead | category sentence verbatim (em dash) | comma variant + appended `Tracking and analysis come after.` |
| P1 `SITE_TITLE` | `BioStack \| Peptide and Compound Evidence Library` | `BioStack \| Evidence-Graded Research on Peptides and Similar Compounds` |
| Footer | category placement | `BioStack. What the research says, graded by evidence strength.` |
| Commander tagline | `Every run, side by side.` | still `Longitudinal Intelligence` |

Retired-string sweep (exit criterion 2, "zero non-`.audit/` occurrences") is **not achievable as written**:
- `Longitudinal Intelligence` is in **three** contract copies (`contracts/`, `frontend/src/contracts/`, `backend/src/BioStack.Application/ProductContract/`) plus `marketing.ts:105`, `pricing/page.tsx:28`, `billing/page.tsx:42`, `TierGate.tsx:19`, `ProtocolConsole.tsx:415,484`, and a pinning test `launchSafetyCopy.test.ts:102`. Charter W3-P1 names only `contracts/product-contract.v1.json`.
- `Protocol Operations` is a legitimate backend/offline-kit product term (CI workflow, CLI, tests, docs) and `ProtocolConsole.tsx` subtitles — a repo-wide zero is out of scope.
- `Tracking, math, and clarity` and `Just structure.`: already zero in source.

`contractVersion` still `1.0.0` (D4 assumption holds).

## Governance state

- Charter status: **Draft — Gate 1 NOT ratified.** No ownership block, no `loop-directive.md`, no `plan-review-findings.md` exist. No live coordinator conflict found.

## Required human decisions before any dispatch

1. Gate 1 ratification of D1–D5, with charter corrections: re-baseline starting state to `e5b75e0`; decide whether the #253 deviations (eyebrow, subhead, `SITE_TITLE`, footer) are accepted or must be reverted to the signed text; widen W3-P1 file list to all contract copies + tier-tagline consumers + `launchSafetyCopy.test.ts`; scope exit criterion 2 to the marketing/tier surfaces.
2. Wave 2: schedule/dispatch it (single owner, per handoff §4) or explicitly amend D3. Until then Wave 3 is blocked.
3. Optional: close Wave 1's missing lint/build/tab-through evidence.
