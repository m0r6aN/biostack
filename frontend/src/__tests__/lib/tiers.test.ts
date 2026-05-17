import { FREE_TIER_COMPOUND_LIMIT } from '@/lib/tiers';
import { describe, expect, it } from 'vitest';

describe('tiers', () => {
  it('FREE_TIER_COMPOUND_LIMIT matches backend ObserverActiveCompoundLimit', () => {
    expect(FREE_TIER_COMPOUND_LIMIT).toBe(8);
  });
});
