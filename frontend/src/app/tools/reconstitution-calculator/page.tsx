import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { PublicCalculatorExperience } from '@/components/marketing/PublicCalculatorExperience';

export default function ReconstitutionCalculatorPage() {
  const howToSchema = {
    '@context': 'https://schema.org',
    '@type': 'HowTo',
    name: 'Use the BioStack Reconstitution and Dosing Calculator',
    description: 'Calculate concentration, dose volume, and daily or weekly dose splits.',
    step: [
      { '@type': 'HowToStep', text: 'Enter the powder amount and unit.' },
      { '@type': 'HowToStep', text: 'Enter the diluent volume in milliliters.' },
      { '@type': 'HowToStep', text: 'Enter the desired dose and choose whether it is per dose, daily total, or weekly total.' },
      { '@type': 'HowToStep', text: 'Review concentration, draw volume, syringe units, and split totals.' },
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
