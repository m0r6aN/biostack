import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { PublicCalculatorExperience } from '@/components/marketing/PublicCalculatorExperience';

export default function ReconstitutionCalculatorPage() {
  const howToSchema = {
    '@context': 'https://schema.org',
    '@type': 'HowTo',
    name: 'Use the BioStack Reconstitution Calculator',
    description: 'Calculate concentration from powder amount and diluent volume.',
    step: [
      { '@type': 'HowToStep', text: 'Enter the powder amount in milligrams.' },
      { '@type': 'HowToStep', text: 'Enter the diluent volume in milliliters.' },
      { '@type': 'HowToStep', text: 'Review the resulting concentration and formula output.' },
    ],
  };

  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(howToSchema) }}
      />
      <PublicCalculatorExperience kind="reconstitution" />
      <MarketingFooter />
    </div>
  );
}
