import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { PublicCalculatorExperience } from '@/components/marketing/PublicCalculatorExperience';

export default function VolumeCalculatorPage() {
  const howToSchema = {
    '@context': 'https://schema.org',
    '@type': 'HowTo',
    name: 'Use the BioStack Reconstitution and Dosing Calculator',
    description: 'Calculate the volume required for a target dose from concentration, including daily and weekly splits.',
    step: [
      { '@type': 'HowToStep', text: 'Enter the target dose in micrograms.' },
      { '@type': 'HowToStep', text: 'Use the calculated concentration or enter a known concentration.' },
      { '@type': 'HowToStep', text: 'Choose per-dose, daily total, or weekly total splitting.' },
      { '@type': 'HowToStep', text: 'Review the exact draw volume and schedule totals.' },
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
