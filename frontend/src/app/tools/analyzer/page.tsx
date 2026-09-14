import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { AnalyzerExperience } from '@/components/tools/analyzer/AnalyzerExperience';
import { createPublicPageMetadata } from '@/lib/site';

export const metadata = createPublicPageMetadata({
  title: 'Analyze a Protocol | BioStack',
  description: 'Operator and Commander members can paste, upload, or link a protocol for a parsed, structural BioStack analysis.',
  path: '/tools/analyzer',
});

export default function AnalyzerPage() {
  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <AnalyzerExperience />
      <MarketingFooter />
    </div>
  );
}
