import { featuredFaqs, landingFeatures, pricingTiers } from '@/lib/marketing';
import { describe, expect, it } from 'vitest';

describe('marketing content', () => {
  it('keeps the featured FAQ set populated for the landing page teaser', () => {
    expect(featuredFaqs).toHaveLength(10);
    for (const item of featuredFaqs) {
      expect(item.question.length).toBeGreaterThan(10);
      expect(item.answer.length).toBeGreaterThan(30);
    }
  });

  it('defines all three pricing tiers with a single featured Operator plan', () => {
    expect(pricingTiers.map((tier) => tier.name)).toEqual([
      'Observer',
      'Operator',
      'Commander',
    ]);

    const featured = pricingTiers.filter((tier) => tier.featured);
    expect(featured).toHaveLength(1);
    expect(featured[0].name).toBe('Operator');
    expect(featured[0].monthly).toBe('$12/mo');
    expect(pricingTiers.every((tier) => !('annual' in tier))).toBe(true);
    expect(pricingTiers[0].href).toBe('/start');
  });

  it('keeps each pricing tier actionable and benefit-oriented', () => {
    for (const tier of pricingTiers) {
      expect(tier.ctaLabel.length).toBeGreaterThan(3);
      expect(tier.href.startsWith('/')).toBe(true);
      expect(tier.detail.length).toBeGreaterThan(30);
      expect(tier.highlights.length).toBeGreaterThanOrEqual(4);
    }
  });

  it('includes the core landing-page feature themes', () => {
    expect(landingFeatures).toHaveLength(6);
    expect(landingFeatures.some((feature) => feature.includes('Pathway overlap'))).toBe(true);
    expect(landingFeatures.some((feature) => feature.includes('Unified timeline'))).toBe(true);
  });

  it('Observer tier does not advertise the paid analyzer', () => {
    const observer = pricingTiers.find((t) => t.name === 'Observer')!;
    expect(observer.highlights).toContain('Free calculators');
    expect(observer.highlights).toContain('Up to 8 active compounds');
    expect(observer.highlights).toContain('Local tool history');
    expect(observer.highlights.join(' ').toLowerCase()).not.toContain('analyzer');
    expect(observer.highlights.every((h) => !h.includes(' 5 '))).toBe(true);
  });

  it('paid highlights are limited to shipped, server-gated customer surfaces', () => {
    const operator = pricingTiers.find((t) => t.name === 'Operator')!;
    const commander = pricingTiers.find((t) => t.name === 'Commander')!;

    expect(operator.highlights).toEqual([
      'Full protocol analysis',
      'Current-stack relationship intelligence',
      'Weekly protocol calendar',
      'Diet and lifestyle framework',
      'Progress and milestone tracking',
    ]);
    expect(commander.highlights).toEqual([
      'Protocol review across run history',
      'Pattern memory snapshots',
      'Protocol drift snapshots',
      'Sequence expectation snapshots',
      'Monitoring and lab view',
      'Cross-protocol mission control',
    ]);
  });
});
