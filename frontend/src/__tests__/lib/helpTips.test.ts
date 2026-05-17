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
