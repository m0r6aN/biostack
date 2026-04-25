import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { ProtocolAnalyzerExperience } from '@/components/tools/ProtocolAnalyzerExperience';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Analyze Any Protocol | BioStack',
  description: 'Paste, upload, scan, or link any protocol and get a parsed, scored BioStack analysis.',
};

export default function AnalyzerPage() {
  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <ProtocolAnalyzerExperience />
      <MarketingFooter />
    </div>
  );
}
