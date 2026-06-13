import { describe, expect, it } from 'vitest';
import {
  formatDelta,
  formatDose,
  getScoreBand,
  unique,
} from '@/components/tools/analyzer/analyzerView';
import type { ProtocolAnalyzerResult } from '@/lib/types';

// ── getScoreBand ──────────────────────────────────────────────────────────────

describe('getScoreBand', () => {
  it('returns excellent_fit at 90', () => {
    expect(getScoreBand(90)).toBe('excellent_fit');
  });

  it('returns strong_fit at 70', () => {
    expect(getScoreBand(70)).toBe('strong_fit');
  });

  it('returns mixed_fit at 60', () => {
    expect(getScoreBand(60)).toBe('mixed_fit');
  });

  it('returns inefficient at 40', () => {
    expect(getScoreBand(40)).toBe('inefficient');
  });

  it('returns high_concern at 30', () => {
    expect(getScoreBand(30)).toBe('high_concern');
  });

  it('returns unknown when undefined', () => {
    expect(getScoreBand(undefined)).toBe('unknown');
  });

  // Boundary: exactly 85 → excellent_fit
  it('returns excellent_fit at exactly 85', () => {
    expect(getScoreBand(85)).toBe('excellent_fit');
  });

  // Boundary: 84 → strong_fit
  it('returns strong_fit at 84', () => {
    expect(getScoreBand(84)).toBe('strong_fit');
  });
});

// ── formatDelta ───────────────────────────────────────────────────────────────

describe('formatDelta', () => {
  it('prefixes positive values with +', () => {
    expect(formatDelta(5)).toBe('+5');
  });

  it('does not double-prefix negative values', () => {
    expect(formatDelta(-3)).toBe('-3');
  });

  it('returns "0" for zero', () => {
    expect(formatDelta(0)).toBe('0');
  });

  it('rounds fractional values', () => {
    expect(formatDelta(4.7)).toBe('+5');
    expect(formatDelta(-2.3)).toBe('-2');
  });
});

// ── unique ────────────────────────────────────────────────────────────────────

describe('unique', () => {
  it('removes duplicates', () => {
    expect(unique(['a', 'a', 'b'])).toEqual(['a', 'b']);
  });

  it('trims whitespace before deduplication', () => {
    expect(unique([' a', 'a ', 'b'])).toEqual(['a', 'b']);
  });

  it('removes empty strings', () => {
    expect(unique(['a', '', 'b', '  '])).toEqual(['a', 'b']);
  });

  it('returns empty array for empty input', () => {
    expect(unique([])).toEqual([]);
  });
});

// ── formatDose ────────────────────────────────────────────────────────────────

type Entry = ProtocolAnalyzerResult['protocol'][number];

function makeEntry(overrides: Partial<Entry> = {}): Entry {
  return {
    compoundName: 'BPC-157',
    dose: 500,
    unit: 'mcg',
    frequency: 'daily',
    duration: '',
    ...overrides,
  };
}

describe('formatDose', () => {
  it('returns empty string when dose is 0', () => {
    expect(formatDose(makeEntry({ dose: 0 }))).toBe('');
  });

  it('returns empty string when dose is negative', () => {
    expect(formatDose(makeEntry({ dose: -1 }))).toBe('');
  });

  it('returns dose and unit for a positive dose', () => {
    expect(formatDose(makeEntry({ dose: 500, unit: 'mcg' }))).toBe('500 mcg');
  });

  it('trims the result (no leading/trailing spaces)', () => {
    const result = formatDose(makeEntry({ dose: 100, unit: 'mg' }));
    expect(result).toBe('100 mg');
    expect(result.trim()).toBe(result);
  });
});
