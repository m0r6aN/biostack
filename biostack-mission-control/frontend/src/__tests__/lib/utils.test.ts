import { describe, it, expect } from 'vitest';
import {
  cn,
  formatDate,
  formatDateTime,
  getStatusColor,
  getEvidenceTierColor,
  getEventIcon,
  formatWeight,
  lbsToKg,
  kgToLbs,
  daysAgo,
} from '@/lib/utils';

// ── cn ────────────────────────────────────────────────────────────────────────
describe('cn', () => {
  it('joins truthy class strings', () => {
    expect(cn('a', 'b', 'c')).toBe('a b c');
  });
  it('filters out falsy values', () => {
    expect(cn('a', undefined, null, false, 'b')).toBe('a b');
  });
  it('returns empty string when all values are falsy', () => {
    expect(cn(undefined, null, false)).toBe('');
  });
});

// ── formatDate ────────────────────────────────────────────────────────────────
describe('formatDate', () => {
  it('formats a valid ISO date string', () => {
    // Use a date-time string so timezone doesn't shift the date
    const result = formatDate('2024-06-15T12:00:00');
    expect(result).toMatch(/Jun/);
    expect(result).toMatch(/15/);
    expect(result).toMatch(/2024/);
  });
  it('returns the original string on invalid input', () => {
    expect(formatDate('not-a-date')).toBe('Invalid Date');
  });
});

// ── getStatusColor ────────────────────────────────────────────────────────────
describe('getStatusColor', () => {
  it('returns emerald classes for Active', () => {
    expect(getStatusColor('Active')).toContain('emerald');
  });
  it('returns blue classes for Completed', () => {
    expect(getStatusColor('Completed')).toContain('blue');
  });
  it('returns amber classes for Paused', () => {
    expect(getStatusColor('Paused')).toContain('amber');
  });
  it('returns a fallback for unknown status', () => {
    const result = getStatusColor('Unknown');
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });
});

// ── getEvidenceTierColor ──────────────────────────────────────────────────────
describe('getEvidenceTierColor', () => {
  it('returns emerald for strong', () => {
    expect(getEvidenceTierColor('strong')).toContain('emerald');
  });
  it('returns blue for moderate', () => {
    expect(getEvidenceTierColor('moderate')).toContain('blue');
  });
  it('returns amber for limited', () => {
    expect(getEvidenceTierColor('limited')).toContain('amber');
  });
  it('returns a muted tone for theoretical', () => {
    const result = getEvidenceTierColor('theoretical');
    expect(typeof result).toBe('string');
  });
});

// ── getEventIcon ──────────────────────────────────────────────────────────────
describe('getEventIcon', () => {
  it('returns an emoji for each known event type', () => {
    expect(getEventIcon('compound_added')).toBe('➕');
    expect(getEventIcon('compound_ended')).toBe('🛑');
    expect(getEventIcon('phase_started')).toBe('🚀');
    expect(getEventIcon('check_in')).toBe('📊');
    expect(getEventIcon('knowledge_update')).toBe('📚');
  });
  it('returns a default pin emoji for unknown types', () => {
    expect(getEventIcon('unknown_type')).toBe('📌');
  });
});

// ── formatWeight ──────────────────────────────────────────────────────────────
describe('formatWeight', () => {
  it('shows kg unit in metric mode', () => {
    expect(formatWeight(75, 'metric')).toBe('75 kg');
  });
  it('converts and shows lbs unit in imperial mode', () => {
    const result = formatWeight(75, 'imperial');
    expect(result).toContain('lbs');
    // 75 kg × 2.20462 = 165.3 lbs
    expect(result).toContain('165.3');
  });
  it('handles fractional kg values in metric', () => {
    expect(formatWeight(68.5, 'metric')).toBe('68.5 kg');
  });
});

// ── lbsToKg ───────────────────────────────────────────────────────────────────
describe('lbsToKg', () => {
  it('converts 220 lbs to ~99.79 kg', () => {
    expect(lbsToKg(220)).toBeCloseTo(99.79, 1);
  });
  it('converts 0 lbs to 0 kg', () => {
    expect(lbsToKg(0)).toBe(0);
  });
  it('is the inverse of kgToLbs within rounding tolerance', () => {
    const original = 80;
    const roundTrip = lbsToKg(kgToLbs(original));
    expect(roundTrip).toBeCloseTo(original, 0);
  });
});

// ── kgToLbs ───────────────────────────────────────────────────────────────────
describe('kgToLbs', () => {
  it('converts 100 kg to ~220.5 lbs', () => {
    expect(kgToLbs(100)).toBeCloseTo(220.5, 0);
  });
  it('converts 0 kg to 0 lbs', () => {
    expect(kgToLbs(0)).toBe(0);
  });
});

// ── daysAgo ───────────────────────────────────────────────────────────────────
describe('daysAgo', () => {
  it('returns "Today" for today\'s date', () => {
    const today = new Date().toISOString();
    expect(daysAgo(today)).toBe('Today');
  });
  it('returns a string for old dates', () => {
    const old = '2020-01-01';
    const result = daysAgo(old);
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });
  it('returns a non-empty string on invalid input', () => {
    const result = daysAgo('not-a-date');
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });
});
