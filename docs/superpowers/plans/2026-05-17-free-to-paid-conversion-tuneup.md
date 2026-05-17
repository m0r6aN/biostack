# Free-to-Paid Conversion Tune-Up Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Raise the free-tier active-compound limit from 5 to 8, rewrite upgrade copy to lead with user outcomes, add a tracking CTA after interaction intelligence output, and improve the Analyzer save section—all without introducing medical, dosing, or recommendation language.

**Architecture:** Centralise the compound limit in two constants (one backend C#, one frontend TS), update all scattered references, then apply copy rewrites and a new tracking CTA to the protocol and analyzer surfaces.

**Tech Stack:** C# / ASP.NET Core (backend), Next.js 16 / TypeScript / Tailwind CSS / Vitest + React Testing Library (frontend)

---

## Before / After Copy Reference

| Surface | Before | After |
|---|---|---|
| LockedTierCard #1 title | "Current stack intelligence is gated" | "See how your protocol fits together" |
| LockedTierCard #1 detail | "Upgrade to Operator to unlock live stack scoring and interaction intelligence." | "Score your active stack, surface synergies and conflicts, and run counterfactual scenarios — all included in Operator." |
| LockedTierCard #2 title | "Simulation stays locked on Observer" | "Protocol simulation unlocks with Operator" |
| LockedTierCard #2 detail | "Observer can still save and manage protocols, but the deeper stack intelligence surfaces unlock with Operator." | "Model compound timing across phases, visualize your protocol's structure, and see how your stack is projected to play out over time." |
| UpgradeBanner title | "Commander keeps the historical intelligence layer unlocked" | "Pattern memory, drift analysis, and sequence intelligence unlock with Commander" |
| Billing — Observer status detail | "Observer includes up to 5 active compounds. Existing data stays available if a paid plan ends." | "Observer includes up to 8 active compounds. Existing data stays available if a paid plan ends." |
| Billing — Operator card title | "Stack intelligence" | "Stack intelligence" (unchanged) |
| Billing — Operator card detail | "Unlock current stack scoring, interaction intelligence, and remove the Observer active compound cap." | "See how your compounds interact — score your protocol, identify synergies and conflicts, and model what changes with counterfactual scenarios. Removes the active compound limit." |
| Billing — Operator card bullets | (none) | Stack score · Synergy & conflict surface · Counterfactual scenarios · No compound cap |
| Billing — Commander card title | "Historical intelligence" | "Pattern intelligence" |
| Billing — Commander card detail | "Unlock protocol review, pattern memory, drift, sequence expectation, and mission control." | "Track how your protocols evolve — detect trends and drift, predict what comes next, and get structured reviews across all your protocol runs." |
| Billing — Commander card bullets | (none) | Trend & drift detection · Sequence expectation · Protocol reviews · Cross-run comparison |
| Billing — warning card condition | `activeLimit === 5` | `activeLimit != null` |
| Billing — warning card text | "Observer is capped at 5 active compounds." | "Observer is capped at {activeLimit} active compounds." |
| marketing.ts Observer highlight | "Track up to 5 active compounds" | "Track up to 8 active compounds" |
| Analyzer section heading | "Turn this into a BioStack protocol" | "Track whether these patterns hold" |
| Analyzer section copy | "Save the analysis, convert it into a protocol, and track it in Mission Control." | "Save this stack as a protocol and check in over time to see whether the synergies and conflicts playing out now actually hold." |

---

## File Map

| File | Change type |
|---|---|
| `backend/src/BioStack.Application/Services/FeatureGate.cs` | Modify — change `ObserverActiveCompoundLimit = 5` to `8` |
| `frontend/src/lib/tiers.ts` | Create — `FREE_TIER_COMPOUND_LIMIT = 8` |
| `frontend/src/components/tools/ProtocolAnalyzerExperience.tsx` | Modify — `maxCompounds` + section heading/copy |
| `frontend/src/components/dashboard/ActiveCompoundsCard.tsx` | Modify — `.slice(0, 5)` → constant |
| `frontend/src/lib/marketing.ts` | Modify — Observer highlight number |
| `frontend/src/app/billing/page.tsx` | Modify — Observer status text, warning card, Operator/Commander card copy + bullets |
| `frontend/src/app/protocols/page.tsx` | Modify — two LockedTierCard call sites |
| `frontend/src/app/protocols/[id]/page.tsx` | Modify — UpgradeBanner title |
| `frontend/src/components/protocols/InteractionIntelligenceCard.tsx` | Modify — add `showTrackingCta` prop + CTA block |
| `frontend/src/__tests__/lib/tiers.test.ts` | Create — constant value test |
| `frontend/src/__tests__/lib/marketing.test.ts` | Modify — update Observer highlight assertion |
| `frontend/src/__tests__/components/protocols/InteractionIntelligenceCard.test.tsx` | Modify — add CTA test |
| `frontend/src/__tests__/conversion/conversionCopy.test.ts` | Create — banned-phrase check across all new copy |

---

## Task 1: Backend — Raise ObserverActiveCompoundLimit to 8

**Files:**
- Modify: `backend/src/BioStack.Application/Services/FeatureGate.cs:16`

The existing backend test at `BillingAndFeatureGateTests.cs:37` uses `FeatureGate.ObserverActiveCompoundLimit` by reference so it stays correct automatically.

- [ ] **Step 1: Change the constant**

Open `backend/src/BioStack.Application/Services/FeatureGate.cs`. On line 16 change:

```csharp
public const int ObserverActiveCompoundLimit = 5;
```
to:
```csharp
public const int ObserverActiveCompoundLimit = 8;
```

- [ ] **Step 2: Run backend tests**

```
cd backend
dotnet test --no-restore --logger "console;verbosity=minimal"
```

Expected: all tests pass. The `FeatureGate_ReturnsExpectedLimitsAndPaidFeaturesByTier` test asserts `== FeatureGate.ObserverActiveCompoundLimit` (not a hardcoded `5`), so it remains correct.

- [ ] **Step 3: Commit**

```bash
git add backend/src/BioStack.Application/Services/FeatureGate.cs
git commit -m "feat(tiers): raise Observer active compound limit to 8"
```

---

## Task 2: Frontend — Constant + Behavioral References

**Files:**
- Create: `frontend/src/lib/tiers.ts`
- Modify: `frontend/src/components/tools/ProtocolAnalyzerExperience.tsx`
- Modify: `frontend/src/components/dashboard/ActiveCompoundsCard.tsx`

- [ ] **Step 1: Write failing test for constant**

Create `frontend/src/__tests__/lib/tiers.test.ts`:

```ts
import { FREE_TIER_COMPOUND_LIMIT } from '@/lib/tiers';
import { describe, expect, it } from 'vitest';

describe('tiers', () => {
  it('FREE_TIER_COMPOUND_LIMIT matches backend ObserverActiveCompoundLimit', () => {
    expect(FREE_TIER_COMPOUND_LIMIT).toBe(8);
  });
});
```

Run: `cd frontend && pnpm vitest run src/__tests__/lib/tiers.test.ts`
Expected: FAIL with "Cannot find module '@/lib/tiers'"

- [ ] **Step 2: Create lib/tiers.ts**

Create `frontend/src/lib/tiers.ts`:

```ts
export const FREE_TIER_COMPOUND_LIMIT = 8;
```

Run: `pnpm vitest run src/__tests__/lib/tiers.test.ts`
Expected: PASS

- [ ] **Step 3: Update ProtocolAnalyzerExperience.tsx — maxCompounds**

In `frontend/src/components/tools/ProtocolAnalyzerExperience.tsx`, add the import after existing imports:

```ts
import { FREE_TIER_COMPOUND_LIMIT } from '@/lib/tiers';
```

Then find all three occurrences of `maxCompounds: 5 as const` (lines ~208, ~210, ~216) and replace each with:

```ts
maxCompounds: FREE_TIER_COMPOUND_LIMIT
```

(Remove `as const` — the API field is typed as `number`, not a literal.)

- [ ] **Step 4: Update ActiveCompoundsCard.tsx — slice limit**

In `frontend/src/components/dashboard/ActiveCompoundsCard.tsx`, add the import:

```ts
import { FREE_TIER_COMPOUND_LIMIT } from '@/lib/tiers';
```

Change line 18:
```ts
{activeCompounds.slice(0, 5).map((compound) => (
```
to:
```ts
{activeCompounds.slice(0, FREE_TIER_COMPOUND_LIMIT).map((compound) => (
```

- [ ] **Step 5: Run frontend tests**

```bash
pnpm vitest run
```

Expected: all tests pass (pre-existing failures ≤ 13, unrelated to this change).

- [ ] **Step 6: Commit**

```bash
git add frontend/src/lib/tiers.ts \
  frontend/src/components/tools/ProtocolAnalyzerExperience.tsx \
  frontend/src/components/dashboard/ActiveCompoundsCard.tsx \
  frontend/src/__tests__/lib/tiers.test.ts
git commit -m "feat(tiers): add FREE_TIER_COMPOUND_LIMIT constant and wire into analyzer + dashboard"
```

---

## Task 3: Limit Number — marketing.ts and billing page

**Files:**
- Modify: `frontend/src/lib/marketing.ts:84`
- Modify: `frontend/src/app/billing/page.tsx:21,131-135`

- [ ] **Step 1: Write failing test for Observer highlight**

In `frontend/src/__tests__/lib/marketing.test.ts`, update the `'keeps the featured FAQ set populated'` test by adding at the end of the file:

```ts
it('Observer tier highlight reflects the current free-tier compound limit', () => {
  const observer = pricingTiers.find((t) => t.name === 'Observer')!;
  expect(observer.highlights.some((h) => h.includes('8'))).toBe(true);
  expect(observer.highlights.every((h) => !h.includes(' 5 '))).toBe(true);
});
```

Run: `pnpm vitest run src/__tests__/lib/marketing.test.ts`
Expected: FAIL (Observer highlight still says "5")

- [ ] **Step 2: Update marketing.ts Observer highlight**

In `frontend/src/lib/marketing.ts` line 84, change:

```ts
'Track up to 5 active compounds',
```
to:
```ts
'Track up to 8 active compounds',
```

Run: `pnpm vitest run src/__tests__/lib/marketing.test.ts`
Expected: PASS

- [ ] **Step 3: Update billing page Observer status detail**

In `frontend/src/app/billing/page.tsx` line 21, change:

```ts
detail: 'Observer includes up to 5 active compounds. Existing data stays available if a paid plan ends.',
```
to:
```ts
detail: 'Observer includes up to 8 active compounds. Existing data stays available if a paid plan ends.',
```

- [ ] **Step 4: Update billing page warning card — condition and dynamic text**

In `frontend/src/app/billing/page.tsx`, change lines 131-137:

**Before:**
```tsx
{subscription.tier === 'Observer' && activeLimit === 5 && (
  <section className="rounded-lg border border-amber-300/15 bg-amber-400/[0.06] p-5">
    <h3 className="text-base font-semibold text-amber-100">Observer active compound limit</h3>
    <p className="mt-2 text-sm leading-6 text-amber-50/70">
      Observer is capped at 5 active compounds. If a paid plan ends while more are active, your data remains saved, but adding or reactivating active compounds is blocked until enough records are paused or completed.
    </p>
  </section>
)}
```

**After:**
```tsx
{subscription.tier === 'Observer' && activeLimit != null && (
  <section className="rounded-lg border border-amber-300/15 bg-amber-400/[0.06] p-5">
    <h3 className="text-base font-semibold text-amber-100">Observer active compound limit</h3>
    <p className="mt-2 text-sm leading-6 text-amber-50/70">
      Observer is capped at {activeLimit} active compounds. If a paid plan ends while more are active, your data remains saved, but adding or reactivating active compounds is blocked until enough records are paused or completed.
    </p>
  </section>
)}
```

- [ ] **Step 5: Run tests**

```bash
pnpm vitest run src/__tests__/lib/marketing.test.ts
```

Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add frontend/src/lib/marketing.ts \
  frontend/src/app/billing/page.tsx \
  frontend/src/__tests__/lib/marketing.test.ts
git commit -m "feat(conversion): raise Observer limit copy to 8, make billing warning dynamic"
```

---

## Task 4: LockedTierCard — Outcome-Focused Copy

**Files:**
- Modify: `frontend/src/app/protocols/page.tsx:141-157`

No new tests needed — copy-only change. The banned-phrase check in Task 9 covers it.

- [ ] **Step 1: Update LockedTierCard call site #1 (left column, stack intelligence)**

In `frontend/src/app/protocols/page.tsx`, find lines 141-145:

```tsx
<LockedTierCard
  eyebrow="Operator"
  title="Current stack intelligence is gated"
  detail={stackLockedMessage ?? 'Upgrade to Operator to unlock live stack scoring and interaction intelligence.'}
/>
```

Replace with:

```tsx
<LockedTierCard
  eyebrow="Operator"
  title="See how your protocol fits together"
  detail={stackLockedMessage ?? 'Score your active stack, surface synergies and conflicts, and run counterfactual scenarios — all included in Operator.'}
/>
```

- [ ] **Step 2: Update LockedTierCard call site #2 (right column, simulation)**

Find lines 152-157:

```tsx
<LockedTierCard
  eyebrow="Operator"
  title="Simulation stays locked on Observer"
  detail="Observer can still save and manage protocols, but the deeper stack intelligence surfaces unlock with Operator."
  large
/>
```

Replace with:

```tsx
<LockedTierCard
  eyebrow="Operator"
  title="Protocol simulation unlocks with Operator"
  detail="Model compound timing across phases, visualize your protocol's structure, and see how your stack is projected to play out over time."
  large
/>
```

- [ ] **Step 3: TypeScript check**

```bash
cd frontend && pnpm tsc --noEmit
```

Expected: 0 new errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/protocols/page.tsx
git commit -m "feat(conversion): rewrite LockedTierCard copy to lead with outcomes"
```

---

## Task 5: UpgradeBanner — Outcome Copy

**Files:**
- Modify: `frontend/src/app/protocols/[id]/page.tsx:248`

- [ ] **Step 1: Update UpgradeBanner call site**

In `frontend/src/app/protocols/[id]/page.tsx`, find lines 246-251:

```tsx
{commanderLockedMessage && (
  <UpgradeBanner
    title="Commander keeps the historical intelligence layer unlocked"
    detail={commanderLockedMessage}
  />
)}
```

Replace with:

```tsx
{commanderLockedMessage && (
  <UpgradeBanner
    title="Pattern memory, drift analysis, and sequence intelligence unlock with Commander"
    detail={commanderLockedMessage}
  />
)}
```

- [ ] **Step 2: TypeScript check**

```bash
pnpm tsc --noEmit
```

Expected: 0 new errors.

- [ ] **Step 3: Commit**

```bash
git add "frontend/src/app/protocols/[id]/page.tsx"
git commit -m "feat(conversion): rewrite UpgradeBanner title to outcome language"
```

---

## Task 6: Billing Page — Card Copy + "See What Unlocks" Preview Bullets

**Files:**
- Modify: `frontend/src/app/billing/page.tsx:140-166`

- [ ] **Step 1: Rewrite Operator card**

In `frontend/src/app/billing/page.tsx`, replace the entire Operator card div (lines 141-152):

**Before:**
```tsx
<div className="rounded-lg border border-white/[0.08] bg-white/[0.025] p-5">
  <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/35">Operator</p>
  <h3 className="mt-2 text-xl font-semibold text-white">Stack intelligence</h3>
  <p className="mt-2 text-sm leading-6 text-white/55">Unlock current stack scoring, interaction intelligence, and remove the Observer active compound cap.</p>
  <button
    onClick={() => startCheckout('operator')}
    disabled={busy !== null}
    className="mt-5 rounded-lg border border-emerald-300/25 px-4 py-2 text-sm font-semibold text-emerald-100 transition-colors hover:bg-emerald-400/10 disabled:opacity-50"
  >
    {busy === 'operator' ? 'Opening...' : 'Upgrade to Operator'}
  </button>
</div>
```

**After:**
```tsx
<div className="rounded-lg border border-white/[0.08] bg-white/[0.025] p-5">
  <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/35">Operator</p>
  <h3 className="mt-2 text-xl font-semibold text-white">Stack intelligence</h3>
  <p className="mt-2 text-sm leading-6 text-white/55">See how your compounds interact — score your protocol, identify synergies and conflicts, and model what changes with counterfactual scenarios. Removes the active compound limit.</p>
  <ul className="mt-3 space-y-1.5">
    {['Stack score across all compounds', 'Synergy and conflict surface', 'Counterfactual scenarios', 'No compound cap'].map((item) => (
      <li key={item} className="flex items-center gap-2 text-xs text-white/50">
        <span className="h-1 w-1 flex-none rounded-full bg-emerald-400/60" />
        {item}
      </li>
    ))}
  </ul>
  <button
    onClick={() => startCheckout('operator')}
    disabled={busy !== null}
    className="mt-5 rounded-lg border border-emerald-300/25 px-4 py-2 text-sm font-semibold text-emerald-100 transition-colors hover:bg-emerald-400/10 disabled:opacity-50"
  >
    {busy === 'operator' ? 'Opening...' : 'Upgrade to Operator'}
  </button>
</div>
```

- [ ] **Step 2: Rewrite Commander card**

Replace the Commander card div (lines 154-165):

**Before:**
```tsx
<div className="rounded-lg border border-white/[0.08] bg-white/[0.025] p-5">
  <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/35">Commander</p>
  <h3 className="mt-2 text-xl font-semibold text-white">Historical intelligence</h3>
  <p className="mt-2 text-sm leading-6 text-white/55">Unlock protocol review, pattern memory, drift, sequence expectation, and mission control.</p>
  <button
    onClick={() => startCheckout('commander')}
    disabled={busy !== null}
    className="mt-5 rounded-lg bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300 disabled:opacity-50"
  >
    {busy === 'commander' ? 'Opening...' : 'Upgrade to Commander'}
  </button>
</div>
```

**After:**
```tsx
<div className="rounded-lg border border-white/[0.08] bg-white/[0.025] p-5">
  <p className="text-xs font-semibold uppercase tracking-[0.16em] text-white/35">Commander</p>
  <h3 className="mt-2 text-xl font-semibold text-white">Pattern intelligence</h3>
  <p className="mt-2 text-sm leading-6 text-white/55">Track how your protocols evolve — detect trends and drift, predict what comes next, and get structured reviews across all your protocol runs.</p>
  <ul className="mt-3 space-y-1.5">
    {['Trend and drift detection', 'Sequence expectation modeling', 'Structured protocol reviews', 'Cross-run comparison'].map((item) => (
      <li key={item} className="flex items-center gap-2 text-xs text-white/50">
        <span className="h-1 w-1 flex-none rounded-full bg-emerald-400/60" />
        {item}
      </li>
    ))}
  </ul>
  <button
    onClick={() => startCheckout('commander')}
    disabled={busy !== null}
    className="mt-5 rounded-lg bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300 disabled:opacity-50"
  >
    {busy === 'commander' ? 'Opening...' : 'Upgrade to Commander'}
  </button>
</div>
```

- [ ] **Step 3: TypeScript check**

```bash
pnpm tsc --noEmit
```

Expected: 0 new errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/billing/page.tsx
git commit -m "feat(conversion): rewrite billing card copy and add feature preview bullets"
```

---

## Task 7: Analyzer Section — Tracking Copy

**Files:**
- Modify: `frontend/src/components/tools/ProtocolAnalyzerExperience.tsx:659-689`

- [ ] **Step 1: Update section heading and copy**

In `frontend/src/components/tools/ProtocolAnalyzerExperience.tsx`, find lines 659-663:

```tsx
<section className="rounded-lg border border-white/[0.08] bg-[#121923]/95 p-4">
  <h2 className="text-lg font-semibold text-white">Turn this into a BioStack protocol</h2>
  <p className="mt-3 text-sm leading-6 text-white/58">
    Save the analysis, convert it into a protocol, and track it in Mission Control.
  </p>
```

Replace with:

```tsx
<section className="rounded-lg border border-white/[0.08] bg-[#121923]/95 p-4">
  <h2 className="text-lg font-semibold text-white">Track whether these patterns hold</h2>
  <p className="mt-3 text-sm leading-6 text-white/58">
    Save this stack as a protocol and check in over time to see whether the synergies and conflicts playing out now actually hold.
  </p>
```

- [ ] **Step 2: Run Analyzer tests**

```bash
pnpm vitest run src/__tests__/components/ProtocolAnalyzerExperience.test.tsx
```

Expected: PASS — no test checks the heading text.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/tools/ProtocolAnalyzerExperience.tsx
git commit -m "feat(conversion): update analyzer save section copy to tracking-forward language"
```

---

## Task 8: InteractionIntelligenceCard — Tracking CTA

**Files:**
- Modify: `frontend/src/components/protocols/InteractionIntelligenceCard.tsx`
- Modify: `frontend/src/app/protocols/page.tsx`
- Modify: `frontend/src/__tests__/components/protocols/InteractionIntelligenceCard.test.tsx`

The CTA only appears when the component is showing a **not-yet-saved** stack (the `currentStack` view on `/protocols`). On the saved-protocol detail page (`/protocols/[id]`), the stack is already being tracked so no CTA is needed.

- [ ] **Step 1: Write failing test**

Open `frontend/src/__tests__/components/protocols/InteractionIntelligenceCard.test.tsx`. The existing test file has at least one integration test. Add:

```tsx
import Link from 'next/link';

// At the bottom of the describe block:
it('renders a tracking CTA when showTrackingCta is true', () => {
  render(
    <InteractionIntelligenceCard
      intelligence={mockIntelligence}
      showTrackingCta
    />
  );
  const link = screen.getByRole('link', { name: /start tracking/i });
  expect(link).toBeInTheDocument();
  expect(link).toHaveAttribute('href', '/protocols');
});

it('does not render a tracking CTA when showTrackingCta is false or omitted', () => {
  render(<InteractionIntelligenceCard intelligence={mockIntelligence} />);
  expect(screen.queryByRole('link', { name: /start tracking/i })).not.toBeInTheDocument();
});
```

(The file already imports `render`, `screen`, and a `mockIntelligence` fixture or similar — check and use what already exists.)

Run: `pnpm vitest run src/__tests__/components/protocols/InteractionIntelligenceCard.test.tsx`
Expected: FAIL — `showTrackingCta` prop does not exist yet.

- [ ] **Step 2: Add prop and CTA to InteractionIntelligenceCard**

In `frontend/src/components/protocols/InteractionIntelligenceCard.tsx`:

Add `Link` import at the top:
```tsx
import Link from 'next/link';
```

Update the props interface (line 6-9):
```tsx
interface InteractionIntelligenceCardProps {
  intelligence: InteractionIntelligence;
  title?: string;
  showTrackingCta?: boolean;
}
```

Update function signature:
```tsx
export function InteractionIntelligenceCard({
  intelligence,
  title = 'Interaction Intelligence',
  showTrackingCta = false,
}: InteractionIntelligenceCardProps) {
```

Add the CTA block at the very end of the component, just before the closing `</div>` on line 128:

```tsx
      {showTrackingCta && (
        <div className="mt-4 rounded-lg border border-white/[0.06] bg-white/[0.02] p-4">
          <p className="text-sm text-white/60">
            Tracking this protocol over time will show whether these interaction patterns hold.
          </p>
          <Link
            href="/protocols"
            className="mt-3 inline-block rounded-lg border border-emerald-300/25 px-4 py-2 text-sm font-semibold text-emerald-100 hover:bg-emerald-400/10"
          >
            Start tracking
          </Link>
        </div>
      )}
```

Run: `pnpm vitest run src/__tests__/components/protocols/InteractionIntelligenceCard.test.tsx`
Expected: PASS

- [ ] **Step 3: Wire showTrackingCta in protocols/page.tsx**

In `frontend/src/app/protocols/page.tsx`, find line 138:

```tsx
<InteractionIntelligenceCard intelligence={currentStack.interactionIntelligence} title="Current Stack Intelligence" />
```

Replace with:

```tsx
<InteractionIntelligenceCard intelligence={currentStack.interactionIntelligence} title="Current Stack Intelligence" showTrackingCta />
```

(Note: `protocols/[id]/page.tsx` line 303 also uses `InteractionIntelligenceCard` but without `showTrackingCta` — leave that unchanged, that's a saved, already-tracked protocol.)

- [ ] **Step 4: Run all tests**

```bash
pnpm vitest run
```

Expected: all tests pass (pre-existing failures ≤ 13, unrelated).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/protocols/InteractionIntelligenceCard.tsx \
  frontend/src/app/protocols/page.tsx \
  frontend/src/__tests__/components/protocols/InteractionIntelligenceCard.test.tsx
git commit -m "feat(conversion): add tracking CTA to InteractionIntelligenceCard for unsaved stack view"
```

---

## Task 9: Tests — Banned-Phrase Check for All New Copy

**Files:**
- Create: `frontend/src/__tests__/conversion/conversionCopy.test.ts`

All new and rewritten copy strings must not contain medical advice, dosing guidance, diagnosis, or recommendation language.

- [ ] **Step 1: Write the test**

Create `frontend/src/__tests__/conversion/conversionCopy.test.ts`:

```ts
import { describe, expect, it } from 'vitest';

const ALL_NEW_COPY = [
  // LockedTierCard
  'See how your protocol fits together',
  'Score your active stack, surface synergies and conflicts, and run counterfactual scenarios — all included in Operator.',
  'Protocol simulation unlocks with Operator',
  'Model compound timing across phases, visualize your protocol\'s structure, and see how your stack is projected to play out over time.',
  // UpgradeBanner
  'Pattern memory, drift analysis, and sequence intelligence unlock with Commander',
  // Billing — Observer
  'Observer includes up to 8 active compounds. Existing data stays available if a paid plan ends.',
  // Billing — Operator card
  'See how your compounds interact — score your protocol, identify synergies and conflicts, and model what changes with counterfactual scenarios. Removes the active compound limit.',
  'Stack score across all compounds',
  'Synergy and conflict surface',
  'Counterfactual scenarios',
  'No compound cap',
  // Billing — Commander card
  'Track how your protocols evolve — detect trends and drift, predict what comes next, and get structured reviews across all your protocol runs.',
  'Trend and drift detection',
  'Sequence expectation modeling',
  'Structured protocol reviews',
  'Cross-run comparison',
  'Pattern intelligence',
  // Analyzer
  'Track whether these patterns hold',
  'Save this stack as a protocol and check in over time to see whether the synergies and conflicts playing out now actually hold.',
  // InteractionIntelligenceCard CTA
  'Tracking this protocol over time will show whether these interaction patterns hold.',
  'Start tracking',
];

const BANNED = [
  'you should',
  'dosage',
  'diagnosis',
  'recommend',
  ' take ',
  'medical advice',
  'clinical guidance',
  'dose',
];

describe('conversion copy safety', () => {
  it('contains no banned medical or recommendation language', () => {
    for (const line of ALL_NEW_COPY) {
      for (const phrase of BANNED) {
        expect(line.toLowerCase()).not.toContain(phrase.toLowerCase());
      }
    }
  });

  it('free-tier compound limit references 8 not 5', () => {
    const limitLines = ALL_NEW_COPY.filter((l) => l.includes('compound'));
    for (const line of limitLines) {
      expect(line).not.toContain(' 5 ');
    }
  });
});
```

- [ ] **Step 2: Run the test**

```bash
pnpm vitest run src/__tests__/conversion/conversionCopy.test.ts
```

Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add frontend/src/__tests__/conversion/conversionCopy.test.ts
git commit -m "test(conversion): banned-phrase safety check for all PR 4 copy strings"
```

---

## Task 10: Full Test Run + TypeScript Check

- [ ] **Step 1: Run all frontend tests**

```bash
cd frontend && pnpm vitest run
```

Expected: all tests pass (pre-existing failures ≤ 13).

- [ ] **Step 2: TypeScript check**

```bash
pnpm tsc --noEmit
```

Expected: 0 errors.

- [ ] **Step 3: Run backend tests**

```bash
cd backend && dotnet test --no-restore --logger "console;verbosity=minimal"
```

Expected: all tests pass.

- [ ] **Step 4: Report results**

Report:
- Frontend tests: N passed, M pre-existing failures (list them)
- TypeScript: 0 errors
- Backend tests: N passed

---

## Explicit Safety Confirmation Required

After all tasks complete, the implementer must confirm:

> "No new copy string introduced in this PR contains medical advice, dosing guidance, diagnosis language, recommendation language, or any form of 'you should take/combine/avoid' phrasing. The conversionCopy.test.ts banned-phrase check passes and covers all new strings."
