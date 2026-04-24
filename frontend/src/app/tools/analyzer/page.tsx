import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { ProtocolAnalyzerExperience } from '@/components/tools/ProtocolAnalyzerExperience';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Protocol Analyzer | BioStack',
  description:
    'Analyze any peptide or bio-optimization protocol from text, PDF, spreadsheet, scan, or shared link. Extract structure, score the stack, and surface better options.',
};

export default function ProtocolAnalyzerPage() {
  const softwareAppSchema = {
    '@context': 'https://schema.org',
    '@type': 'SoftwareApplication',
    name: 'BioStack Protocol Analyzer',
    applicationCategory: 'HealthApplication',
    operatingSystem: 'Web',
    description:
      'Analyze any peptide or bio-optimization protocol from text, PDF, spreadsheet, scan, or shared link.',
  };

  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(softwareAppSchema) }}
      />
      <ProtocolAnalyzerExperience />
      <MarketingFooter />
    </div>
  );
}
