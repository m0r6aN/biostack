import { createPublicPageMetadata } from '@/lib/site';
import PricingClient from './pricing-client';

export const metadata = createPublicPageMetadata({
  title: 'Pricing | BioStack',
  description: 'Compare BioStack plans for observational protocol tracking and longitudinal intelligence.',
  path: '/pricing',
});

export default function PricingPage() {
  return <PricingClient />;
}
