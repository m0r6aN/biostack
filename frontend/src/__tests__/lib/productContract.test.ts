import {
  canonicalRoutes,
  formatMonthlyPrice,
  getProductPlan,
  healthRoutes,
  productContract,
  productContractVersion,
  productPlans,
  routeAliases,
} from '@/lib/productContract';
import { describe, expect, it } from 'vitest';

describe('versioned product contract', () => {
  it('defines monthly-only pricing and immediate past-due downgrade', () => {
    expect(productContractVersion).toBe('1.0.0');
    expect(productContract.billing.interval).toBe('month');
    expect(productContract.billing.pastDueGraceDays).toBe(0);
    expect(productContract.billing.paidAccessStatuses).toEqual(['Active', 'Trialing']);
    expect(productPlans.map((plan) => plan.code)).toEqual(['observer', 'operator', 'commander']);
    expect(formatMonthlyPrice(getProductPlan('operator'))).toBe('$12/mo');
    expect(formatMonthlyPrice(getProductPlan('commander'))).toBe('$29/mo');
  });

  it('defines canonical routes, compatibility aliases, and health probes', () => {
    expect(canonicalRoutes.onboarding).toBe('/start');
    expect(canonicalRoutes.analyzer).toBe('/tools/analyzer');
    expect(routeAliases['/onboarding']).toBe(canonicalRoutes.onboarding);
    expect(routeAliases['/map']).toBe(canonicalRoutes.analyzer);
    expect(healthRoutes).toEqual({
      livenessPath: '/health',
      keonDependencyPath: '/health/keon',
    });
  });

  it('preserves selected paid-plan intent through sign-in', () => {
    expect(getProductPlan('operator').marketingCtaPath).toBe('/billing?plan=operator');
    expect(getProductPlan('commander').marketingCtaPath).toBe('/billing?plan=commander');
  });
});
