import contract from '@/contracts/product-contract.v1.json';

export type ProductPlanCode = 'observer' | 'operator' | 'commander';
export type ProductTierName = 'Observer' | 'Operator' | 'Commander';

// Domain ordering of the tiers: a subscription covers every tier below it
// (Commander ⊃ Operator ⊃ Observer). Used to decide whether a plan card is
// already covered, the current plan, or a genuine upgrade.
const TIER_RANK: Record<ProductTierName, number> = { Observer: 0, Operator: 1, Commander: 2 };

export function tierRank(tier: string): number {
  return TIER_RANK[tier as ProductTierName] ?? -1;
}

export interface ProductPlanContract {
  code: ProductPlanCode;
  tier: ProductTierName;
  displayName: string;
  tagline: string;
  monthlyPriceCents: number;
  stripePriceConfigurationKey: string | null;
  marketingCtaPath: string;
}

export const productContract = contract;
export const productContractVersion = contract.contractVersion;
export const productPlans = contract.billing.plans as ProductPlanContract[];
export const canonicalRoutes = contract.routes.canonical;
export const routeAliases = contract.routes.aliases;
export const publicRoutePrefixes = contract.routes.publicPrefixes;
export const healthRoutes = contract.health;

export function isPublicRoutePath(pathname: string) {
  if (pathname === '/' || contract.routes.publicAssets.includes(pathname)) {
    return true;
  }

  return publicRoutePrefixes.some((prefix) =>
    pathname === prefix || pathname.startsWith(`${prefix}/`),
  );
}

export function getProductPlan(code: ProductPlanCode) {
  const plan = productPlans.find((candidate) => candidate.code === code);
  if (!plan) {
    throw new Error(`Product contract does not define ${code}.`);
  }
  return plan;
}

export function formatMonthlyPrice(plan: ProductPlanContract) {
  if (plan.monthlyPriceCents === 0) {
    return '$0';
  }

  return `$${plan.monthlyPriceCents / 100}/mo`;
}
