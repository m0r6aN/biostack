import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { PublicCalculatorExperience } from '@/components/marketing/PublicCalculatorExperience';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Unit Converter | BioStack',
  description: 'Convert between mcg, mg, and g.',
};

export default function UnitConverterPage() {
  const howToSchema = {
    '@context': 'https://schema.org',
    '@type': 'HowTo',
    name: 'Use the BioStack Unit Converter',
    description: 'Convert between mass units.',
    step: [
      { '@type': 'HowToStep', text: 'Enter the value to convert.' },
      { '@type': 'HowToStep', text: 'Choose the source unit.' },
      { '@type': 'HowToStep', text: 'Choose the target unit.' },
      { '@type': 'HowToStep', text: 'Review the converted value.' },
    ],
  };

  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(howToSchema) }}
      />
      <PublicCalculatorExperience kind="conversion" />
      <MarketingFooter />
    </div>
  );
}
