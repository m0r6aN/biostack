import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { PublicCalculatorExperience } from '@/components/marketing/PublicCalculatorExperience';
import { createPublicPageMetadata } from '@/lib/site';

export const metadata = createPublicPageMetadata({
  title: 'Volume Calculator | BioStack',
  description: 'Work out the volume that corresponds to an amount you enter, from a known concentration.',
  path: '/tools/volume-calculator',
});

export default function VolumeCalculatorPage() {
  const howToSchema = {
    '@context': 'https://schema.org',
    '@type': 'HowTo',
    name: 'Use the BioStack Volume Calculator',
    description: 'Work out the volume that corresponds to an amount you enter, from a concentration.',
    step: [
      { '@type': 'HowToStep', text: 'Enter the amount to convert, in micrograms.' },
      { '@type': 'HowToStep', text: 'Use the calculated concentration or enter a known concentration.' },
      { '@type': 'HowToStep', text: 'Choose the units for each value you enter.' },
      { '@type': 'HowToStep', text: 'Review the volume and syringe units that correspond to the entered amount.' },
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
