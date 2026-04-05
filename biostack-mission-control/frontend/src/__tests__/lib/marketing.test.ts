import { describe, expect, it } from 'vitest';
import { featuredFaqs, landingFeatures, pricingTiers } from '@/lib/marketing';

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
    expect(featured[0].annualEffective).toContain('$8');
  });

  it('keeps each pricing tier actionable and benefit-oriented', () => {
    for (const tier of pricingTiers) {
      expect(tier.ctaLabel.length).toBeGreaterThan(3);
      expect(tier.href.startsWith('/')).toBe(true);
      expect(tier.highlights.length).toBeGreaterThanOrEqual(4);
    }
  });

  it('includes the core landing-page feature themes', () => {
    expect(landingFeatures).toHaveLength(6);
    expect(landingFeatures.some((feature) => feature.includes('Pathway overlap'))).toBe(true);
    expect(landingFeatures.some((feature) => feature.includes('Unified timeline'))).toBe(true);
  });
});
