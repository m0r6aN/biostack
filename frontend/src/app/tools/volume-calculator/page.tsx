import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { PublicCalculatorExperience } from '@/components/marketing/PublicCalculatorExperience';

export default function VolumeCalculatorPage() {
  const howToSchema = {
    '@context': 'https://schema.org',
    '@type': 'HowTo',
    name: 'Use the BioStack Volume Calculator',
    description: 'Calculate the volume required for a target dose from concentration.',
    step: [
      { '@type': 'HowToStep', text: 'Enter the target dose in micrograms.' },
      { '@type': 'HowToStep', text: 'Enter the concentration in micrograms per milliliter.' },
      { '@type': 'HowToStep', text: 'Review the exact draw volume output.' },
    ],
  };

  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(howToSchema) }}
      />
      <PublicCalculatorExperience kind="volume" />
      <MarketingFooter />
    </div>
  );
}
