# Vocabulary & Explainability Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `HelpTip` component and a `helpTips` definitions module, then wire plain-language one-sentence tooltips for nine technical terms across ten integration surfaces.

**Architecture:** A single `HelpTip` client component wraps any inline term and reveals a tooltip panel on click/tap. All nine definitions live in `lib/helpTips.ts` as a typed `as const` record — no inline strings at call sites. Integration is additive: existing components import `HelpTip` and `helpTips`, wrap one label, and add one import. Nothing is restructured.

**Tech Stack:** Next.js 16, React 18 (`useId`, `useEffect`, `useState`), TypeScript, Tailwind CSS, Vitest + React Testing Library.

---

## File Map

| File | Status | Responsibility |
|---|---|---|
| `frontend/src/lib/helpTips.ts` | **Create** | All 9 definitions + `HelpTipKey` type |
| `frontend/src/components/ui/HelpTip.tsx` | **Create** | Trigger + panel, all interaction logic |
| `frontend/src/components/knowledge/EvidenceTierBadge.tsx` | Modify | Wrap tier label text |
| `frontend/src/components/intel/EvidenceTierBadge.tsx` | Modify | Wrap tier label text (uses EVIDENCE_TIER_TOKENS) |
| `frontend/src/components/knowledge/CompoundRelationshipsSection.tsx` | Modify | Wrap tier label, community signal, review required |
| `frontend/src/components/protocols/InteractionIntelligenceCard.tsx` | Modify | Wrap Synergies/Redundancies/Interferences/Counterfactual headers |
| `frontend/src/components/protocols/StackScoreCard.tsx` | Modify | Add `helpKey` prop to internal `Breakdown`; wire four labels |
| `frontend/src/components/protocol/CounterfactualLab.tsx` | Modify | Wrap "Counterfactual" in heading |
| `frontend/src/components/dashboard/OverlapFlagsBanner.tsx` | Modify | Wrap "Pathway Overlap"; add `'use client'` |
| `frontend/src/components/protocol/StackGraph.tsx` | Modify | Wrap edge labels (InteractionEdge) + tier token (CompoundNode) |
| `frontend/src/components/tools/ProtocolAnalyzerExperience.tsx` | Modify | Add `helpKey` prop to internal `ScoreChip`; wire three labels |

---

## Task 1: helpTips definitions module

**Files:**
- Create: `frontend/src/lib/helpTips.ts`
- Create: `frontend/src/__tests__/lib/helpTips.test.ts`

- [ ] **Step 1: Write the failing tests**

```ts
// frontend/src/__tests__/lib/helpTips.test.ts
import { helpTips } from '@/lib/helpTips';
import type { HelpTipKey } from '@/lib/helpTips';

const REQUIRED_KEYS: HelpTipKey[] = [
  'evidenceTier', 'synergy', 'redundancy', 'interference',
  'communitySignal', 'reviewRequired', 'counterfactual',
  'pathwayOverlap', 'mechanisticEvidence',
];

const BANNED = ['you should', 'dosage', 'diagnosis', 'recommend', ' take '];

describe('helpTips', () => {
  it('exports all nine required keys with non-empty string values', () => {
    for (const key of REQUIRED_KEYS) {
      expect(helpTips).toHaveProperty(key);
      expect(typeof helpTips[key]).toBe('string');
      expect((helpTips[key] as string).length).toBeGreaterThan(0);
    }
  });

  it.each(BANNED)('no definition contains banned phrase "%s"', (phrase) => {
    for (const [key, text] of Object.entries(helpTips)) {
      expect(text.toLowerCase()).not.toContain(phrase);
    }
  });
});
```

- [ ] **Step 2: Run tests — expect failure (module not found)**

```bash
cd frontend && npx vitest run __tests__/lib/helpTips.test.ts
```

Expected: FAIL — `Cannot find module '@/lib/helpTips'`

- [ ] **Step 3: Create `frontend/src/lib/helpTips.ts`**

```ts
export const helpTips = {
  evidenceTier:
    "A research quality rating showing how well a compound's effects are supported by clinical or scientific evidence.",
  synergy:
    'Compounds that may support related outcomes through different biological mechanisms.',
  redundancy:
    'Compounds that may share similar biological mechanisms, which can reduce the value of stacking them together.',
  interference:
    "Compounds whose mechanisms may counteract or complicate each other's intended effects.",
  communitySignal:
    'A pattern reported by users in research communities, not yet verified by clinical trials.',
  reviewRequired:
    'This relationship has been flagged by BioStack for human review before treating it as reliable.',
  counterfactual:
    'A "what if" scenario estimating how your stack score would change if one compound were removed.',
  pathwayOverlap:
    'Two compounds may act on the same biological pathway, which can compound or complicate their combined effects.',
  mechanisticEvidence:
    'The mechanism of action is biologically understood, but direct human trial data is limited or absent.',
} as const;

export type HelpTipKey = keyof typeof helpTips;
```

- [ ] **Step 4: Run tests — expect pass**

```bash
cd frontend && npx vitest run __tests__/lib/helpTips.test.ts
```

Expected: PASS — 10 tests

- [ ] **Step 5: Commit**

```bash
git add frontend/src/lib/helpTips.ts frontend/src/__tests__/lib/helpTips.test.ts
git commit -m "feat(vocab): helpTips definitions module — 9 terms, banned-phrase tested"
```

---

## Task 2: HelpTip component

**Files:**
- Create: `frontend/src/components/ui/HelpTip.tsx`
- Create: `frontend/src/__tests__/components/ui/HelpTip.test.tsx`

- [ ] **Step 1: Write the failing tests**

```tsx
// frontend/src/__tests__/components/ui/HelpTip.test.tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HelpTip } from '@/components/ui/HelpTip';

describe('HelpTip', () => {
  it('renders children and shows tooltip on click, hides on second click', async () => {
    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /synergy/i }));
    const tip = screen.getByRole('tooltip');
    expect(tip).toBeInTheDocument();
    expect(tip).toHaveTextContent('may support related outcomes');

    await userEvent.click(screen.getByRole('button'));
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
  });

  it('dismisses on Escape key press', async () => {
    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    await userEvent.click(screen.getByRole('button'));
    expect(screen.getByRole('tooltip')).toBeInTheDocument();

    await userEvent.keyboard('{Escape}');
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
  });

  it('toggles via Enter and Space keys', async () => {
    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    const trigger = screen.getByRole('button');
    trigger.focus();

    await userEvent.keyboard('{Enter}');
    expect(screen.getByRole('tooltip')).toBeInTheDocument();

    await userEvent.keyboard(' ');
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
  });

  it('links trigger to tooltip via aria-describedby', async () => {
    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    await userEvent.click(screen.getByRole('button'));
    const tooltip = screen.getByRole('tooltip');
    expect(screen.getByRole('button')).toHaveAttribute('aria-describedby', tooltip.id);
  });

  it('sets aria-expanded correctly', async () => {
    render(<HelpTip tipKey="synergy">Synergy</HelpTip>);
    const trigger = screen.getByRole('button');
    expect(trigger).toHaveAttribute('aria-expanded', 'false');

    await userEvent.click(trigger);
    expect(trigger).toHaveAttribute('aria-expanded', 'true');
  });
});
```

- [ ] **Step 2: Run tests — expect failure (module not found)**

```bash
cd frontend && npx vitest run __tests__/components/ui/HelpTip.test.tsx
```

Expected: FAIL — `Cannot find module '@/components/ui/HelpTip'`

- [ ] **Step 3: Create `frontend/src/components/ui/HelpTip.tsx`**

```tsx
'use client';

import { useEffect, useId, useRef, useState } from 'react';
import { helpTips, type HelpTipKey } from '@/lib/helpTips';
import { cn } from '@/lib/utils';

interface HelpTipProps {
  tipKey: HelpTipKey;
  children: React.ReactNode;
  className?: string;
}

const DISPLAY_LABELS: Record<HelpTipKey, string> = {
  evidenceTier:        'Evidence Tier',
  synergy:             'Synergy',
  redundancy:          'Redundancy',
  interference:        'Interference',
  communitySignal:     'Community Signal',
  reviewRequired:      'Review Required',
  counterfactual:      'Counterfactual',
  pathwayOverlap:      'Pathway Overlap',
  mechanisticEvidence: 'Mechanistic Evidence',
};

export function HelpTip({ tipKey, children, className }: HelpTipProps) {
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLSpanElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const rawId = useId();
  const panelId = `helptip-${rawId.replace(/:/g, '')}`;

  useEffect(() => {
    if (!open) return;

    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') setOpen(false);
    }
    function handleMouseDown(e: MouseEvent) {
      if (
        triggerRef.current && !triggerRef.current.contains(e.target as Node) &&
        panelRef.current && !panelRef.current.contains(e.target as Node)
      ) {
        setOpen(false);
      }
    }

    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('mousedown', handleMouseDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('mousedown', handleMouseDown);
    };
  }, [open]);

  return (
    <>
      <style>{`
        .htip:hover > .htip-text,
        .htip:focus-visible > .htip-text {
          text-shadow: 0 0 8px rgba(148,163,184,0.55);
        }
        @media (prefers-reduced-motion: reduce) {
          .htip > .htip-text { text-shadow: none !important; }
        }
      `}</style>
      <span
        ref={triggerRef}
        role="button"
        tabIndex={0}
        aria-expanded={open}
        aria-describedby={open ? panelId : undefined}
        onClick={() => setOpen(v => !v)}
        onKeyDown={e => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            setOpen(v => !v);
          }
        }}
        className={cn('htip relative inline-block cursor-help', className)}
      >
        <span className="htip-text underline decoration-dotted underline-offset-2 decoration-white/25">
          {children}
        </span>

        {open && (
          <div
            ref={panelRef}
            id={panelId}
            role="tooltip"
            className="absolute bottom-[calc(100%+8px)] left-0 z-50 w-56 rounded-lg border border-white/10 bg-[#1e293b] p-3 shadow-[0_8px_24px_rgba(0,0,0,0.4)]"
          >
            <p className="mb-1.5 text-[10px] font-bold uppercase tracking-[0.1em] text-white/40">
              {DISPLAY_LABELS[tipKey]}
            </p>
            <p className="text-xs leading-relaxed text-white/70">
              {helpTips[tipKey]}
            </p>
            {/* CSS caret arrow */}
            <div className="absolute -bottom-[5px] left-3 h-2 w-2 rotate-45 border-b border-r border-white/10 bg-[#1e293b]" />
          </div>
        )}
      </span>
    </>
  );
}
```

- [ ] **Step 4: Run tests — expect pass**

```bash
cd frontend && npx vitest run __tests__/components/ui/HelpTip.test.tsx
```

Expected: PASS — 5 tests

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/ui/HelpTip.tsx frontend/src/__tests__/components/ui/HelpTip.test.tsx
git commit -m "feat(vocab): HelpTip component — click-to-toggle, a11y, prefers-reduced-motion"
```

---

## Task 3: Wire both EvidenceTierBadge components

**Files:**
- Modify: `frontend/src/components/knowledge/EvidenceTierBadge.tsx`
- Modify: `frontend/src/components/intel/EvidenceTierBadge.tsx`

Both are currently server components. They must become client components because they will import `HelpTip` (`'use client'`). Both are consumed only inside existing client components, so this is safe.

- [ ] **Step 1: Update `knowledge/EvidenceTierBadge.tsx`**

The current file renders `{map[lower] ?? tier}` inside a `<span>`. Add `'use client'`, import `HelpTip`, and wrap that text. The `mechanisticEvidence` key applies when `lower === 'mechanistic'` or `lower === 'theoretical'` (closest match); everything else uses `evidenceTier`.

Replace the entire file:

```tsx
'use client';

import { getEvidenceTierColor } from '@/lib/utils';
import { HelpTip } from '@/components/ui/HelpTip';
import type { HelpTipKey } from '@/lib/helpTips';

interface EvidenceTierBadgeProps {
  tier: string;
  variant?: 'default' | 'research';
}

const labels: Record<string, string> = {
  strong: 'Strong Evidence',
  moderate: 'Moderate Evidence',
  limited: 'Limited Evidence',
  theoretical: 'Theoretical',
};

const researchTierLabels: Record<string, string> = {
  strong: 'Strong',
  moderate: 'Moderate',
  limited: 'Limited',
  insufficient: 'Insufficient',
  unknown: 'Unknown',
  anecdotal: 'Anecdotal',
};

function tierHelpKey(lower: string): HelpTipKey {
  return lower === 'mechanistic' || lower === 'theoretical' ? 'mechanisticEvidence' : 'evidenceTier';
}

export function EvidenceTierBadge({ tier, variant = 'default' }: EvidenceTierBadgeProps) {
  const lower = tier.toLowerCase();
  const map = variant === 'research' ? researchTierLabels : labels;
  const label = map[lower] ?? tier;
  return (
    <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${getEvidenceTierColor(lower)}`}>
      <HelpTip tipKey={tierHelpKey(lower)}>{label}</HelpTip>
    </span>
  );
}
```

- [ ] **Step 2: Update `intel/EvidenceTierBadge.tsx`**

The current file renders `{short ? t.short : t.label}` inside a `<span>`. It already uses `title={t.label}` (browser native tooltip) — remove that since `HelpTip` replaces it.

Replace the entire file:

```tsx
'use client';

import { EVIDENCE_TIER_TOKENS } from '@/styles/tokens';
import { cn } from '@/lib/utils';
import { HelpTip } from '@/components/ui/HelpTip';
import type { HelpTipKey } from '@/lib/helpTips';

interface EvidenceTierBadgeProps {
  tier: string;
  short?: boolean;
  className?: string;
}

function tierHelpKey(key: string): HelpTipKey {
  return key === 'mechanistic' ? 'mechanisticEvidence' : 'evidenceTier';
}

export function EvidenceTierBadge({ tier, short = false, className }: EvidenceTierBadgeProps) {
  const key = tier.toLowerCase();
  const t = EVIDENCE_TIER_TOKENS[key] ?? EVIDENCE_TIER_TOKENS.unknown;

  return (
    <span
      className={cn(
        'inline-flex items-center text-[11px] font-medium px-2.5 py-1 rounded-full border',
        t.bg, t.color, t.border,
        className,
      )}
    >
      <HelpTip tipKey={tierHelpKey(key)}>{short ? t.short : t.label}</HelpTip>
    </span>
  );
}
```

- [ ] **Step 3: Run the full test suite to confirm no regressions**

```bash
cd frontend && npx vitest run --reporter verbose
```

Expected: same pass count as before these changes (previously 617 passing). If any badge tests fail, they'll indicate which rendered text changed — the text inside the badge is unchanged (only wrapped), so existing snapshots or text queries should still pass.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/knowledge/EvidenceTierBadge.tsx frontend/src/components/intel/EvidenceTierBadge.tsx
git commit -m "feat(vocab): wire HelpTip into both EvidenceTierBadge components"
```

---

## Task 4: Wire CompoundRelationshipsSection

**Files:**
- Modify: `frontend/src/components/knowledge/CompoundRelationshipsSection.tsx`
- Modify: `frontend/src/__tests__/components/knowledge/CompoundRelationshipsSection.test.tsx`

**What changes:** Three wrapping sites in the rendered edge row — evidence tier label, community signal label, and review required copy. A helper `tierHelpKey` derives the correct key from the label string.

- [ ] **Step 1: Add integration tests to the existing test file**

Open `frontend/src/__tests__/components/knowledge/CompoundRelationshipsSection.test.tsx` and add three tests at the end of the existing `describe` block. You will need to look at how existing tests set up `fetchCompoundGraph` mocks and auth context — follow the same pattern exactly.

Add these tests:

```tsx
it('exposes HelpTip for evidence tier label', async () => {
  // Render with a matched edge that has evidenceTier: 'strong'
  // (reuse the same mock setup as the existing "Renders user-facing labels" test)
  // After the section renders, find the trigger by its role and content:
  const triggers = screen.getAllByRole('button');
  const tierTrigger = triggers.find(t => t.textContent?.includes('Strong evidence'));
  expect(tierTrigger).toBeDefined();
});

it('exposes HelpTip for community signal label when signal is present', async () => {
  // Render with signalStrength: 'recurring'
  const triggers = screen.getAllByRole('button');
  const signalTrigger = triggers.find(t => t.textContent?.includes('Commonly reported'));
  expect(signalTrigger).toBeDefined();
});

it('exposes HelpTip for review required copy when needsReview is true', async () => {
  // Render with needsReview: true
  const triggers = screen.getAllByRole('button');
  const reviewTrigger = triggers.find(t => t.textContent?.includes('Awaiting research review'));
  expect(reviewTrigger).toBeDefined();
});
```

- [ ] **Step 2: Run the new tests — expect failure**

```bash
cd frontend && npx vitest run __tests__/components/knowledge/CompoundRelationshipsSection.test.tsx
```

Expected: the three new tests FAIL (no buttons for these labels yet)

- [ ] **Step 3: Update `CompoundRelationshipsSection.tsx`**

Add two imports at the top:

```tsx
import { HelpTip } from '@/components/ui/HelpTip';
import type { HelpTipKey } from '@/lib/helpTips';
```

Add a helper after the `tierRank` function (around line 43):

```tsx
function tierHelpKey(label: string): HelpTipKey {
  return label.toLowerCase().includes('mechanistic') ? 'mechanisticEvidence' : 'evidenceTier';
}
```

In the rendered row (around lines 158–170), make these three targeted changes:

**Evidence tier label** — change:
```tsx
<span className="text-xs text-white/35">
  {edge.evidenceTierLabel}
</span>
```
to:
```tsx
<span className="text-xs text-white/35">
  <HelpTip tipKey={tierHelpKey(edge.evidenceTierLabel)}>{edge.evidenceTierLabel}</HelpTip>
</span>
```

**Community signal label** — change:
```tsx
<p className="text-xs text-sky-300/70 mt-0.5">
  {edge.communitySignalLabel}
</p>
```
to:
```tsx
<p className="text-xs text-sky-300/70 mt-0.5">
  <HelpTip tipKey="communitySignal">{edge.communitySignalLabel}</HelpTip>
</p>
```

**Review required copy** — change:
```tsx
<p className="text-xs text-amber-300/70 mt-0.5">
  Awaiting research review · Advisory signal only
</p>
```
to:
```tsx
<p className="text-xs text-amber-300/70 mt-0.5">
  <HelpTip tipKey="reviewRequired">Awaiting research review</HelpTip>
  {' · Advisory signal only'}
</p>
```

- [ ] **Step 4: Run all CompoundRelationshipsSection tests — expect all pass**

```bash
cd frontend && npx vitest run __tests__/components/knowledge/CompoundRelationshipsSection.test.tsx
```

Expected: all 18 tests pass (15 existing + 3 new)

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/knowledge/CompoundRelationshipsSection.tsx frontend/src/__tests__/components/knowledge/CompoundRelationshipsSection.test.tsx
git commit -m "feat(vocab): wire HelpTip into CompoundRelationshipsSection — tier, signal, review"
```

---

## Task 5: Wire InteractionIntelligenceCard

**Files:**
- Modify: `frontend/src/components/protocols/InteractionIntelligenceCard.tsx`

The file is already `'use client'`. Four wrapping sites: the three metric header labels (lines 54, 58, 62) and the Counterfactual section heading (line 70).

- [ ] **Step 1: Add imports**

Add at the top of `InteractionIntelligenceCard.tsx`:

```tsx
import { HelpTip } from '@/components/ui/HelpTip';
```

- [ ] **Step 2: Wrap the four labels**

**Synergies header** (line 54) — change:
```tsx
<p className="text-xs uppercase tracking-[0.16em] text-emerald-200/60">Synergies</p>
```
to:
```tsx
<p className="text-xs uppercase tracking-[0.16em] text-emerald-200/60">
  <HelpTip tipKey="synergy">Synergies</HelpTip>
</p>
```

**Redundancies header** (line 58) — change:
```tsx
<p className="text-xs uppercase tracking-[0.16em] text-amber-200/60">Redundancies</p>
```
to:
```tsx
<p className="text-xs uppercase tracking-[0.16em] text-amber-200/60">
  <HelpTip tipKey="redundancy">Redundancies</HelpTip>
</p>
```

**Interferences header** (line 62) — change:
```tsx
<p className="text-xs uppercase tracking-[0.16em] text-rose-200/60">Interferences</p>
```
to:
```tsx
<p className="text-xs uppercase tracking-[0.16em] text-rose-200/60">
  <HelpTip tipKey="interference">Interferences</HelpTip>
</p>
```

**Counterfactual heading** (line 70) — change:
```tsx
<p className="text-xs font-semibold uppercase tracking-[0.16em] text-sky-200/60">Counterfactual</p>
```
to:
```tsx
<p className="text-xs font-semibold uppercase tracking-[0.16em] text-sky-200/60">
  <HelpTip tipKey="counterfactual">Counterfactual</HelpTip>
</p>
```

- [ ] **Step 3: Add integration test**

If `frontend/src/__tests__/components/protocols/InteractionIntelligenceCard.test.tsx` exists, add to it. If not, create it. The test confirms that after rendering with a valid `InteractionIntelligence` object, HelpTip buttons are present for the three score headers.

```tsx
// At minimum, add/create:
import { render, screen } from '@testing-library/react';
import { InteractionIntelligenceCard } from '@/components/protocols/InteractionIntelligenceCard';

const mockIntelligence = {
  compositeScore: 82,
  score: { synergyScore: 14, redundancyPenalty: 3, interferencePenalty: 7 },
  summary: { synergies: 3, redundancies: 1, interferences: 1 },
  topFindings: [],
  counterfactuals: [],
  swaps: [],
};

it('exposes HelpTip buttons for synergy, redundancy, interference, and counterfactual', () => {
  render(<InteractionIntelligenceCard intelligence={mockIntelligence as any} />);
  const buttons = screen.getAllByRole('button');
  expect(buttons.some(b => b.textContent?.includes('Synergies'))).toBe(true);
  expect(buttons.some(b => b.textContent?.includes('Redundancies'))).toBe(true);
  expect(buttons.some(b => b.textContent?.includes('Interferences'))).toBe(true);
});
```

- [ ] **Step 4: Run tests**

```bash
cd frontend && npx vitest run __tests__/components/protocols/InteractionIntelligenceCard.test.tsx
```

Expected: all tests pass

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/protocols/InteractionIntelligenceCard.tsx frontend/src/__tests__/components/protocols/InteractionIntelligenceCard.test.tsx
git commit -m "feat(vocab): wire HelpTip into InteractionIntelligenceCard headers"
```

---

## Task 6: Wire StackScoreCard and OverlapFlagsBanner

**Files:**
- Modify: `frontend/src/components/protocols/StackScoreCard.tsx`
- Modify: `frontend/src/components/dashboard/OverlapFlagsBanner.tsx`

### StackScoreCard

The internal `Breakdown` component (line 39) renders `{label}` in a `<span>`. Add a `helpKey` prop to `Breakdown` and wrap `{label}` when the prop is provided.

- [ ] **Step 1: Update `StackScoreCard.tsx`**

Add import at the top:

```tsx
import { HelpTip } from '@/components/ui/HelpTip';
import type { HelpTipKey } from '@/lib/helpTips';
```

Change the `Breakdown` function signature and body:

```tsx
function Breakdown({ label, value, helpKey }: { label: string; value: number; helpKey?: HelpTipKey }) {
  return (
    <div className="rounded-lg border border-white/[0.06] bg-white/[0.025] p-3">
      <div className="flex items-center justify-between">
        <span className="text-white/45">
          {helpKey ? <HelpTip tipKey={helpKey}>{label}</HelpTip> : label}
        </span>
        <span className="font-semibold text-white">{value}</span>
      </div>
      <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-white/[0.06]">
        <div className="h-full rounded-full bg-emerald-400" style={{ width: `${Math.min(100, value)}%` }} />
      </div>
    </div>
  );
}
```

Update the four call sites in `StackScoreCard`:

```tsx
<Breakdown label="Synergy"    helpKey="synergy"      value={score.breakdown.synergy} />
<Breakdown label="Redundancy" helpKey="redundancy"   value={score.breakdown.redundancy} />
<Breakdown label="Conflicts"  helpKey="interference" value={score.breakdown.conflicts} />
<Breakdown label="Evidence"   helpKey="evidenceTier" value={score.breakdown.evidence} />
```

### OverlapFlagsBanner

`OverlapFlagsBanner` has no `'use client'` directive. Adding `HelpTip` (a client component) requires it.

- [ ] **Step 2: Update `OverlapFlagsBanner.tsx`**

Add `'use client'` at the top and import `HelpTip`:

```tsx
'use client';

import { InteractionFlag } from '@/lib/types';
import { HelpTip } from '@/components/ui/HelpTip';
```

Wrap "Pathway Overlap" in the heading (line 16):

```tsx
<h4 className="font-semibold text-amber-200 text-sm mb-1">
  {flags.length}{' '}
  <HelpTip tipKey="pathwayOverlap">Pathway Overlap</HelpTip>{' '}
  {flags.length === 1 ? 'Detected' : 'Flags Detected'}
</h4>
```

- [ ] **Step 3: Run full test suite**

```bash
cd frontend && npx vitest run --reporter verbose
```

Expected: same pass count as before (no regressions)

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/protocols/StackScoreCard.tsx frontend/src/components/dashboard/OverlapFlagsBanner.tsx
git commit -m "feat(vocab): wire HelpTip into StackScoreCard breakdown labels and OverlapFlagsBanner"
```

---

## Task 7: Wire CounterfactualLab

**Files:**
- Modify: `frontend/src/components/protocol/CounterfactualLab.tsx`

The heading "Counterfactual Lab" appears twice: in the empty-state branch (line 27) and in the populated branch (line 54). Wrap "Counterfactual" in both.

- [ ] **Step 1: Add import**

```tsx
import { HelpTip } from '@/components/ui/HelpTip';
```

- [ ] **Step 2: Wrap in empty-state heading** (line 27):

Change:
```tsx
<p className="text-[10px] font-bold text-white/20 uppercase tracking-widest mb-3">Counterfactual Lab</p>
```
to:
```tsx
<p className="text-[10px] font-bold text-white/20 uppercase tracking-widest mb-3">
  <HelpTip tipKey="counterfactual">Counterfactual</HelpTip> Lab
</p>
```

- [ ] **Step 3: Wrap in populated heading** (line 54):

Change:
```tsx
<p className="text-[10px] font-bold text-white/20 uppercase tracking-widest mb-1">Counterfactual Lab</p>
```
to:
```tsx
<p className="text-[10px] font-bold text-white/20 uppercase tracking-widest mb-1">
  <HelpTip tipKey="counterfactual">Counterfactual</HelpTip> Lab
</p>
```

- [ ] **Step 4: Add integration test**

If `frontend/src/__tests__/components/protocol/CounterfactualLab.test.tsx` exists, add to it; otherwise create it.

```tsx
import { render, screen } from '@testing-library/react';
import { CounterfactualLab } from '@/components/protocol/CounterfactualLab';

it('exposes HelpTip button for counterfactual in empty state', () => {
  render(<CounterfactualLab intelligence={null} />);
  const buttons = screen.getAllByRole('button');
  expect(buttons.some(b => b.textContent?.includes('Counterfactual'))).toBe(true);
});
```

- [ ] **Step 5: Run tests**

```bash
cd frontend && npx vitest run __tests__/components/protocol/CounterfactualLab.test.tsx
```

Expected: all tests pass

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/protocol/CounterfactualLab.tsx frontend/src/__tests__/components/protocol/CounterfactualLab.test.tsx
git commit -m "feat(vocab): wire HelpTip into CounterfactualLab headings"
```

---

## Task 8: Wire StackGraph — InteractionEdge and CompoundNode

**Files:**
- Modify: `frontend/src/components/protocol/StackGraph.tsx`

Both `CompoundNode` and `InteractionEdge` are defined inline in this file (lines 32–96). `StackGraph.tsx` is already `'use client'`.

- [ ] **Step 1: Add imports at the top of `StackGraph.tsx`**

```tsx
import { HelpTip } from '@/components/ui/HelpTip';
import type { HelpTipKey } from '@/lib/helpTips';
```

- [ ] **Step 2: Add edge label lookup helper** after the existing imports, before `CompoundNode`:

```tsx
function edgeLabelHelpKey(label: string): HelpTipKey | null {
  const l = label.toLowerCase();
  if (l.includes('synergi') || l.includes('complement')) return 'synergy';
  if (l.includes('redundant') || l.includes('redundan'))  return 'redundancy';
  if (l.includes('interfer'))                              return 'interference';
  if (l.includes('pathway') || l.includes('overlap'))     return 'pathwayOverlap';
  return null;
}
```

- [ ] **Step 3: Update `CompoundNode` — wrap tier token text** (lines 55–58)

Change:
```tsx
{tierToken && (
  <span className={cn('mt-1.5 inline-block text-[9px] font-medium px-1.5 py-0.5 rounded-full', tierToken.bg, tierToken.color)}>
    {tierToken.short}
  </span>
)}
```
to:
```tsx
{tierToken && (
  <span className={cn('mt-1.5 inline-block text-[9px] font-medium px-1.5 py-0.5 rounded-full', tierToken.bg, tierToken.color)}>
    <HelpTip tipKey={data.evidenceTier === 'mechanistic' ? 'mechanisticEvidence' : 'evidenceTier'}>
      {tierToken.short}
    </HelpTip>
  </span>
)}
```

- [ ] **Step 4: Update `InteractionEdge` — wrap edge label** (lines 87–91)

Before the `return`, add:

```tsx
const labelHelpKey = data?.label ? edgeLabelHelpKey(data.label) : null;
```

Change the label span inside `EdgeLabelRenderer`:
```tsx
{data?.label && (
  <span className="text-[9px] text-white/30 bg-[#0B0F14] px-1.5 py-0.5 rounded-full border border-white/8 font-mono">
    {data.label}
  </span>
)}
```
to:
```tsx
{data?.label && (
  <span className="text-[9px] text-white/30 bg-[#0B0F14] px-1.5 py-0.5 rounded-full border border-white/8 font-mono">
    {labelHelpKey
      ? <HelpTip tipKey={labelHelpKey}>{data.label}</HelpTip>
      : data.label}
  </span>
)}
```

- [ ] **Step 5: Run full test suite**

```bash
cd frontend && npx vitest run --reporter verbose
```

Expected: same pass count as before (StackGraph tests pass if they exist; no regressions)

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/protocol/StackGraph.tsx
git commit -m "feat(vocab): wire HelpTip into StackGraph edge labels and CompoundNode tier token"
```

---

## Task 9: Wire ProtocolAnalyzerExperience

**Files:**
- Modify: `frontend/src/components/tools/ProtocolAnalyzerExperience.tsx`

The file is already `'use client'`. `ScoreChip` is defined at line 1192 — it renders `{label}` at line 1202 inside a `<p>`. Add a `helpKey` prop to `ScoreChip` and wrap `{label}` when provided. Then wire the three call sites (lines 616–618).

- [ ] **Step 1: Add imports** near the top of the file (follow the existing import block style):

```tsx
import { HelpTip } from '@/components/ui/HelpTip';
import type { HelpTipKey } from '@/lib/helpTips';
```

- [ ] **Step 2: Update `ScoreChip`** (line 1192):

Change the function signature:
```tsx
function ScoreChip({ label, value, tone, helpKey }: {
  label: string;
  value: number;
  tone: 'positive' | 'negative' | 'neutral';
  helpKey?: HelpTipKey;
}) {
```

Change the label `<p>` (line 1202):
```tsx
<p className="text-xs font-semibold uppercase tracking-[0.16em] opacity-70">
  {helpKey ? <HelpTip tipKey={helpKey}>{label}</HelpTip> : label}
</p>
```

- [ ] **Step 3: Wire the three ScoreChip call sites** (lines 616–618):

Change:
```tsx
<ScoreChip label="Synergy"      value={result.scoreExplanation.synergy}      tone="positive" />
<ScoreChip label="Redundancy"   value={result.scoreExplanation.redundancy}   tone="negative" />
<ScoreChip label="Interference" value={result.scoreExplanation.interference} tone="negative" />
```
to:
```tsx
<ScoreChip label="Synergy"      helpKey="synergy"      value={result.scoreExplanation.synergy}      tone="positive" />
<ScoreChip label="Redundancy"   helpKey="redundancy"   value={result.scoreExplanation.redundancy}   tone="negative" />
<ScoreChip label="Interference" helpKey="interference" value={result.scoreExplanation.interference} tone="negative" />
```

(`Base` chip at line 615 intentionally receives no `helpKey` — it has no mapped term.)

- [ ] **Step 4: Run the analyzer tests**

```bash
cd frontend && npx vitest run __tests__/components/ProtocolAnalyzerExperience.test.tsx
```

Expected: all existing tests pass (the `ScoreChip` label text is unchanged — only wrapped)

- [ ] **Step 5: Run full suite to confirm zero regressions**

```bash
cd frontend && npx vitest run --reporter verbose
```

Expected: all previously passing tests still pass

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/tools/ProtocolAnalyzerExperience.tsx
git commit -m "feat(vocab): wire HelpTip into ProtocolAnalyzerExperience ScoreChip labels"
```

---

## Self-Review

### Spec coverage check

| Spec requirement | Task covering it |
|---|---|
| `lib/helpTips.ts` — 9 definitions, typed record | Task 1 |
| `HelpTip` component — click-to-toggle, Escape, outside-click, keyboard a11y, aria | Task 2 |
| `HelpTip` — prefers-reduced-motion | Task 2 (style block) |
| Both `EvidenceTierBadge` wired | Task 3 |
| `CompoundRelationshipsSection` — tier, signal, review | Task 4 |
| `InteractionIntelligenceCard` — synergy, redundancy, interference, counterfactual | Task 5 |
| `StackScoreCard` — synergy, redundancy, interference, evidenceTier | Task 6 |
| `OverlapFlagsBanner` — pathwayOverlap | Task 6 |
| `CounterfactualLab` — counterfactual | Task 7 |
| `StackGraph` — edge labels + CompoundNode tier | Task 8 |
| `ProtocolAnalyzerExperience` — synergy, redundancy, interference | Task 9 |
| Tests: all 9 keys exported | Task 1 |
| Tests: banned phrase check | Task 1 |
| Tests: HelpTip click/Escape/keyboard/aria | Task 2 |
| Tests: key component integration tests | Tasks 4, 5, 7 |
| First-occurrence wrapping rule | Applied in Tasks 4–9 — headers and label sites only |
| All definitions from `helpTips.ts`, no local duplicates | Enforced by `HelpTipKey` type at every call site |

### Placeholder scan

None found. Every step contains exact code.

### Type consistency

- `HelpTipKey` defined in Task 1, imported in Tasks 2–9 — consistent.
- `edgeLabelHelpKey` returns `HelpTipKey | null` — checked before use in Task 8.
- `helpKey?: HelpTipKey` prop pattern used identically in Tasks 6 (`Breakdown`) and 9 (`ScoreChip`).
- `tierHelpKey` defined independently in Tasks 3 and 4 — same logic, same return type, no cross-dependency.
