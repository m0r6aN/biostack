import ReconstitutionCalculatorPage from '@/app/tools/reconstitution-calculator/page';
import UnitConverterPage from '@/app/tools/unit-converter/page';
import VolumeCalculatorPage from '@/app/tools/volume-calculator/page';
import ToolsPage from '@/app/tools/page';
import { metadata as toolsMetadata } from '@/app/tools/page';
import { metadata as reconstitutionMetadata } from '@/app/tools/reconstitution-calculator/page';
import { metadata as unitConverterMetadata } from '@/app/tools/unit-converter/page';
import { metadata as volumeMetadata } from '@/app/tools/volume-calculator/page';
import { LandingHero } from '@/components/marketing/LandingHero';
import * as marketing from '@/lib/marketing';
import { act, fireEvent, render } from '@testing-library/react';
import type { ComponentProps } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

// Doctrine: BioStack never suggests, prefills or computes a dose. The calculators only convert amounts the
// user types (mg to mL to syringe units). This test scans the visible copy of every calculator page, before and
// after a calculation is on screen, plus the landing hero and the FAQ/marketing copy, for dose-direction wording.

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: ComponentProps<'a'>) => <a href={href} {...props}>{children}</a>,
}));
vi.mock('@/components/marketing/MarketingNav', () => ({ MarketingNav: () => <nav>Navigation</nav> }));
vi.mock('@/components/marketing/MarketingFooter', () => ({ MarketingFooter: () => <footer>Footer</footer> }));
vi.mock('@/lib/AuthProvider', () => {
  const value = { user: null, loading: false };
  return { useAuth: () => value, useOptionalAuth: () => value };
});
vi.mock('@/lib/context', () => {
  const value = { currentProfileId: null, profiles: [], setProfiles: vi.fn(), setCurrentProfileId: vi.fn() };
  return { useProfile: () => value };
});
vi.mock('@/lib/api', () => ({
  apiClient: {
    getAllKnowledgeCompounds: vi.fn().mockResolvedValue([]),
    getCompounds: vi.fn().mockResolvedValue([]),
    getProfiles: vi.fn().mockResolvedValue([]),
    createCompound: vi.fn(),
    checkOverlap: vi.fn().mockResolvedValue([]),
  },
}));

// Wording that tells a reader what amount to use, or frames the tool as working out a dose.
const DOSE_DIRECTION = [
  /\bcalculate (your |the |a )?doses?\b/i,
  /\bdose it right\b/i,
  /\bdose (volume )?calculator\b/i,
  /\b(target|desired|recommended|suggested|optimal|ideal|typical|usual|starting|maintenance|safe|right|correct|proper) (dose|dosage|dosing)s?\b/i,
  /\bdosing (plan|schedule|protocol|guide)\b/i,
  /\b(daily|weekly) (total|dose)s?\b/i,
  /\bdoses? per (day|week)\b/i,
  /\bshots?\b/i,
  /\byou (should|can|may|need to|must) (take|inject|dose|use)\b/i,
  /\bhow much (to|should you) (take|inject|use)\b/i,
];

/** Sentences that state what BioStack does NOT do ("does not suggest dosages") are allowed. */
function directionSentences(text: string): string[] {
  const NEGATED = /\b(not|never|no|without|neither|nor|cannot|can't|won't|doesn't|don't)\b/i;
  return text
    .split(/(?<=[.!?])\s+|\n+/)
    .map((s) => s.trim())
    .filter(Boolean)
    .filter((sentence) => !NEGATED.test(sentence))
    .filter((sentence) => DOSE_DIRECTION.some((pattern) => pattern.test(sentence)));
}

function collectStrings(value: unknown, out: string[] = []): string[] {
  if (typeof value === 'string') out.push(value);
  else if (Array.isArray(value)) value.forEach((item) => collectStrings(item, out));
  else if (value && typeof value === 'object') Object.values(value).forEach((item) => collectStrings(item, out));
  return out;
}

/** One line per text node, so adjacent elements are never glued into one false "sentence". */
function textLines(container: HTMLElement): string[] {
  const walker = document.createTreeWalker(container, NodeFilter.SHOW_TEXT);
  const lines: string[] = [];
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    if (node.parentElement?.closest('script,style')) continue;
    const text = (node.textContent ?? '').trim();
    if (text) lines.push(text);
  }
  return lines;
}

function visibleCopy(container: HTMLElement): string {
  const attrs = [...container.querySelectorAll('[aria-label],[placeholder],[title],[alt]')].flatMap((node) =>
    ['aria-label', 'placeholder', 'title', 'alt'].map((name) => node.getAttribute(name) ?? ''),
  );
  const schema = [...container.querySelectorAll('script[type="application/ld+json"]')].map((node) => node.textContent ?? '');
  return [...textLines(container), ...attrs, ...schema].join('\n');
}

function enter(container: HTMLElement) {
  const inputs = [...container.querySelectorAll<HTMLInputElement>('input[type="number"]')];
  const values = ['5', '2', '250'];
  inputs.slice(0, 3).forEach((input, index) => fireEvent.change(input, { target: { value: values[index] } }));
}

const PAGES = [
  ['/tools', ToolsPage],
  ['/tools/reconstitution-calculator', ReconstitutionCalculatorPage],
  ['/tools/volume-calculator', VolumeCalculatorPage],
  ['/tools/unit-converter', UnitConverterPage],
] as const;

describe('calculator and marketing copy contains no dose-direction wording', () => {
  beforeEach(() => window.localStorage.clear());

  it.each(PAGES)('%s: empty state', async (_path, Page) => {
    const { container } = render(<Page />);
    await act(async () => { await Promise.resolve(); });
    expect(directionSentences(visibleCopy(container))).toEqual([]);
  });

  it.each(PAGES.slice(0, 3))('%s: with a calculation on screen', async (_path, Page) => {
    const { container } = render(<Page />);
    await act(async () => { await Promise.resolve(); });
    enter(container);
    expect(container.textContent).toMatch(/Calculated draw: 10 units/);
    expect(directionSentences(visibleCopy(container))).toEqual([]);
  });

  it('page metadata', () => {
    const copy = [toolsMetadata, reconstitutionMetadata, unitConverterMetadata, volumeMetadata]
      .flatMap((metadata) => [String(metadata.title), String(metadata.description)]);
    expect(copy.flatMap(directionSentences)).toEqual([]);
  });

  it('landing hero', () => {
    const { container } = render(<LandingHero />);
    expect(directionSentences(visibleCopy(container))).toEqual([]);
  });

  it('FAQ and marketing copy (every string exported from lib/marketing)', () => {
    const strings = collectStrings({ ...marketing });
    expect(strings.length).toBeGreaterThan(20);
    expect(strings.flatMap(directionSentences)).toEqual([]);
  });

  it('the scanner catches the wording this PR retired', () => {
    for (const retired of [
      'It helps you track what you take, calculate doses accurately, and surface evidence.',
      'Dose it right. Mix correctly. Check compatibility.',
      'Target Dosage',
      'Need to calculate dose volume or reconstitution?',
      'There are exactly 4 shots of 10 units in a 2 mL vial.',
      'Doses per week',
      'Dose Volume Calculator',
    ]) {
      expect(directionSentences(retired), retired).not.toEqual([]);
    }
    expect(directionSentences('BioStack does not recommend compounds, suggest dosages, or provide clinical guidance.')).toEqual([]);
  });

  it('the calculators open with no amount filled in', async () => {
    const { container } = render(<ToolsPage />);
    await act(async () => { await Promise.resolve(); });
    const filled = [...container.querySelectorAll<HTMLInputElement>('input[type="number"]')].filter((input) => input.value !== '');
    expect(filled).toEqual([]);
  });
});
