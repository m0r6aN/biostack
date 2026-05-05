import { toSlug, buildSlugMap } from '@/lib/research/slugs';
import type { ResearchSummaryCompound } from '@/lib/research/types';

describe('toSlug', () => {
  it('lowercases and replaces spaces with hyphens', () => {
    expect(toSlug('Testosterone cypionate')).toBe('testosterone-cypionate');
  });
  it('handles hyphens already present', () => {
    expect(toSlug('GHK-Cu')).toBe('ghk-cu');
  });
  it('handles numbers', () => {
    expect(toSlug('BPC-157')).toBe('bpc-157');
  });
  it('strips parentheses and punctuation', () => {
    expect(toSlug('Vitamin D3 (cholecalciferol)')).toBe('vitamin-d3-cholecalciferol');
  });
  it('collapses consecutive hyphens', () => {
    expect(toSlug('A -- B')).toBe('a-b');
  });
  it('strips leading/trailing whitespace', () => {
    expect(toSlug(' Creatine ')).toBe('creatine');
  });
});

describe('buildSlugMap', () => {
  const compounds: Pick<ResearchSummaryCompound, 'name'>[] = [
    { name: 'BPC-157' },
    { name: 'Testosterone cypionate' },
    { name: 'Creatine' },
  ];

  it('maps slug to canonical name', () => {
    const map = buildSlugMap(compounds);
    expect(map.get('bpc-157')).toBe('BPC-157');
    expect(map.get('testosterone-cypionate')).toBe('Testosterone cypionate');
    expect(map.get('creatine')).toBe('Creatine');
  });

  it('returns undefined for unknown slug', () => {
    expect(buildSlugMap(compounds).get('unknown-xyz')).toBeUndefined();
  });
});
