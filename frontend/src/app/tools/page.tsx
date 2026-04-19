import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { ToolsDecisionSurface } from '@/components/tools/ToolsDecisionSurface';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Dose, Mix, and Compatibility Tools | BioStack',
  description: 'Free dosage, reconstitution, unit conversion, and compatibility tools. No account required.',
};

export default function ToolsPage() {
  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <ToolsDecisionSurface />
      <MarketingFooter />
    </div>
  );
}
