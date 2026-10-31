import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { PublicCalculatorExperience } from '@/components/marketing/PublicCalculatorExperience';
import { createPublicPageMetadata } from '@/lib/site';

export const metadata = createPublicPageMetadata({
  title: 'Reconstitution Calculator | BioStack',
  description: 'Calculate concentration from powder and solution volume with handling steps next to the result.',
  path: '/tools/reconstitution-calculator',
});

export default function ReconstitutionCalculatorPage() {
  const howToSchema = {
    '@context': 'https://schema.org',
    '@type': 'HowTo',
    name: 'Use the BioStack Reconstitution and Dosing Calculator',
    description: 'Work out concentration and the volume that corresponds to an amount you enter.',
    step: [
      { '@type': 'HowToStep', text: 'Enter the powder amount and unit.' },
      { '@type': 'HowToStep', text: 'Enter the diluent volume in milliliters.' },
      { '@type': 'HowToStep', text: 'Enter the amount to convert and its unit.' },
      { '@type': 'HowToStep', text: 'Review concentration, volume, and syringe units for the entered amount.' },
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
