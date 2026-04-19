import { LandingHero } from '@/components/marketing/LandingHero';
import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';

export default function HomePage() {
  const softwareSchema = {
    '@context': 'https://schema.org',
    '@type': 'SoftwareApplication',
    name: 'BioStack',
    applicationCategory: 'HealthApplication',
    operatingSystem: 'Web',
    description: 'Tracking, calculator, and stack mapping infrastructure for compound protocols.',
  };

  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(softwareSchema) }}
        />
        <LandingHero />

        <section className="mx-auto max-w-7xl px-5 py-8 sm:px-8 lg:py-10">
          <div className="rounded-lg border border-white/8 bg-white/[0.025] px-4 py-4 sm:flex sm:items-center sm:justify-between sm:gap-6 sm:px-5">
            <p className="text-base font-semibold tracking-tight text-white sm:text-lg">
              BioStack is not a doctor.
            </p>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-white/56 sm:mt-0">
              BioStack organizes tracking, math, overlap context, and evidence references. It does not prescribe, diagnose, recommend compounds, or replace qualified medical care.
            </p>
          </div>
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
