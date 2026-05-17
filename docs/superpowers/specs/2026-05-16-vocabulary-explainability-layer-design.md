# Vocabulary & Explainability Layer — Design Spec
**Date:** 2026-05-16
**PR:** 3 — Guided vocabulary and explainability layer
**Status:** Approved

---

## Problem

BioStack surfaces dense technical terms — Synergy, Redundancy, Interference, Evidence tier, Community signal, Mechanistic evidence, Pathway overlap, Counterfactual, Review required — with no inline explanation. Experienced users tolerate this; new users are blocked or misread the terms. The gap between admin vocabulary and plain language is widest on the highest-traffic surfaces: the relationship cards, the analyzer, and the interaction intelligence card.

---

## Goal

Add a lightweight `HelpTip` component that wraps any technical term and reveals a one-sentence plain-language definition on click/tap. Wire it into the nine key terms across twelve integration surfaces. All tooltip definitions live in a single typed module to prevent drift.

---

## Non-goals / constraints

- No backend changes.
- No new routes or full glossary page.
- No medical advice, dosing guidance, diagnosis, or recommendation language in any tooltip definition.
- Do not redesign pages or restructure components beyond what wrapping requires.
- Do not wrap every repeated row value — label-level and header-level wrapping only.

---

## Architecture

Two new files; twelve files modified.

```
lib/helpTips.ts                      — all 9 definitions, typed record, single source of truth
components/ui/HelpTip.tsx            — trigger + tooltip panel, self-contained
```

Every integration site imports `helpTips[key]` from `lib/helpTips.ts`. No local duplicate definitions anywhere.

---

## Shared definitions — `lib/helpTips.ts`

```ts
export const helpTips = {
  evidenceTier:        "A research quality rating showing how well a compound's effects are supported by clinical or scientific evidence.",
  synergy:             "Compounds that may support related outcomes through different biological mechanisms.",
  redundancy:          "Compounds that may share similar biological mechanisms, which can reduce the value of stacking them together.",
  interference:        "Compounds whose mechanisms may counteract or complicate each other's intended effects.",
  communitySignal:     "A pattern reported by users in research communities, not yet verified by clinical trials.",
  reviewRequired:      "This relationship has been flagged by BioStack for human review before treating it as reliable.",
  counterfactual:      "A \"what if\" scenario estimating how your stack score would change if one compound were removed.",
  pathwayOverlap:      "Two compounds may act on the same biological pathway, which can compound or complicate their combined effects.",
  mechanisticEvidence: "The mechanism of action is biologically understood, but direct human trial data is limited or absent.",
} as const;

export type HelpTipKey = keyof typeof helpTips;
```

---

## Component — `components/ui/HelpTip.tsx`

**Props:**
```ts
interface HelpTipProps {
  tipKey: HelpTipKey;
  children: React.ReactNode;
  className?: string;
}
```

**Behaviour:**

1. Renders `children` in a `<span>` with dotted underline styling and `cursor-help`.
2. Applies a subtle focus/hover affordance (e.g. soft underline brightness or text-shadow on `:hover` and `:focus-visible`). The affordance must be declared with `@media (prefers-reduced-motion: reduce)` fallback that disables any animation.
3. Click/tap toggles the tooltip panel open and closed (`useState(false)`).
4. Tooltip dismisses on: second click on trigger, `Escape` key, or outside click. Both the Escape handler and outside-click handler attach via `useEffect` and clean up on unmount.
5. Tooltip panel is absolutely positioned (`bottom: calc(100% + 8px)`, `left: 0`, `z-50`, `w-56`). Wrapper `<span>` is `position: relative`.
6. Keyboard: trigger `<span>` has `tabIndex={0}`, `role="button"`, `aria-expanded={open}`, and `aria-describedby` pointing to the tooltip panel `id`. Panel has `role="tooltip"`.
7. Tooltip panel contains:
   - Small-caps term label at top (derived from `tipKey`, formatted for display)
   - One-sentence body from `helpTips[tipKey]`
   - CSS caret arrow at the bottom-left

**Wrapping rule:** Wrap the first visible occurrence within a section, table header, badge label, or compact metric label. Do not wrap every repeated row value in a dense list.

**`'use client'` directive required.** All existing integration targets are already client components.

---

## Integration points

| File | Terms wired | Wrapping site |
|---|---|---|
| `components/knowledge/CompoundRelationshipsSection.tsx` | `evidenceTier`, `communitySignal`, `reviewRequired` | Tier label string, signal label string, review copy |
| `components/protocols/InteractionIntelligenceCard.tsx` | `synergy`, `redundancy`, `interference` | Column/row header labels |
| `components/protocols/StackScoreCard.tsx` | `synergy`, `redundancy`, `interference` | Score breakdown labels |
| `components/knowledge/EvidenceTierBadge.tsx` | `evidenceTier`, `mechanisticEvidence` | Tier text inside the badge; `mechanisticEvidence` key used when tier resolves to `mechanistic` |
| `components/intel/EvidenceTierBadge.tsx` | `evidenceTier`, `mechanisticEvidence` | Same as above |
| `components/protocol/CounterfactualLab.tsx` | `counterfactual` | Section heading or term label |
| `components/dashboard/OverlapFlagsBanner.tsx` | `pathwayOverlap` | Flag type label |
| `components/protocol/StackGraph.tsx` | `synergy`, `redundancy`, `interference`, `pathwayOverlap` | Edge labels inside the custom ReactFlow `InteractionEdge` component only |
| `components/protocol/CompoundNode.tsx` | `evidenceTier` | Tier token text |
| `components/tools/ProtocolAnalyzerExperience.tsx` | `synergy`, `redundancy`, `interference`, `counterfactual` | `ScoreChip` labels; counterfactual section heading |

**Not wired (confirmed absent or skip):**

- `components/tools/StackReviewBoard.tsx` — no user-visible instances of these terms
- `app/knowledge/page.tsx` — no evidence tier badges in list view
- `components/knowledge/CompoundIntelligenceCard.tsx` — EvidenceTierBadge is rendered here but the tip is wired inside the badge component itself; no additional wrapping needed at card level

---

## Files changed

| File | Change type |
|---|---|
| `frontend/src/lib/helpTips.ts` | New |
| `frontend/src/components/ui/HelpTip.tsx` | New |
| `frontend/src/components/knowledge/CompoundRelationshipsSection.tsx` | Modified |
| `frontend/src/components/protocols/InteractionIntelligenceCard.tsx` | Modified |
| `frontend/src/components/protocols/StackScoreCard.tsx` | Modified |
| `frontend/src/components/knowledge/EvidenceTierBadge.tsx` | Modified |
| `frontend/src/components/intel/EvidenceTierBadge.tsx` | Modified |
| `frontend/src/components/protocol/CounterfactualLab.tsx` | Modified |
| `frontend/src/components/dashboard/OverlapFlagsBanner.tsx` | Modified |
| `frontend/src/components/protocol/StackGraph.tsx` | Modified |
| `frontend/src/components/protocol/CompoundNode.tsx` | Modified |
| `frontend/src/components/tools/ProtocolAnalyzerExperience.tsx` | Modified |

---

## Test plan

Tests added or updated across:

- `__tests__/lib/helpTips.test.ts` (new)
- `__tests__/components/ui/HelpTip.test.tsx` (new)
- Existing component test files for relationship cards, intelligence cards, evidence badges, and counterfactual UI

| # | Test |
|---|---|
| 1 | `helpTips` exports all nine required keys |
| 2 | `HelpTip` renders tooltip text on click; hides on second click |
| 3 | `HelpTip` dismisses on `Escape` key press when open |
| 4 | `HelpTip` is keyboard accessible: `Enter` and `Space` toggle the tooltip |
| 5 | `HelpTip` sets `aria-describedby` linking trigger to tooltip panel; panel has `role="tooltip"` |
| 6 | `CompoundRelationshipsSection` exposes help for evidence tier, community signal, and review required |
| 7 | `InteractionIntelligenceCard` exposes help for synergy, redundancy, and interference |
| 8 | `EvidenceTierBadge` exposes evidence tier help; exposes mechanistic evidence help when tier is `mechanistic` |
| 9 | `CounterfactualLab` exposes counterfactual help |
| 10 | No tooltip text in `helpTips` contains "you should", "dosage", "diagnosis", "recommend", or "take" |

---

## Spec self-review

- **Placeholders:** None. All file paths confirmed against live codebase. All term definitions finalized.
- **Internal consistency:** `HelpTipKey` type enforces that only defined keys can be passed. `mechanisticEvidence` key used when `tier === 'mechanistic'` — consistent across both badge components.
- **Scope:** Two new files, twelve modified. No new routes, no redesign, no backend changes.
- **Ambiguity:** Wrapping rule ("first visible occurrence in a section/header/badge") is explicit. `CounterfactualLab` and `ProtocolAnalyzerExperience` both use `helpTips.counterfactual` — no local duplicates.
- **Safety:** All ten definitions reviewed against the banned-language constraint. `recommend` in the banned-word test applies to `helpTips` definitions only, which contain no such form.
