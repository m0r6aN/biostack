import { LandingHero } from '@/components/marketing/LandingHero';
import { IntelligenceProofSection } from '@/components/marketing/IntelligenceProofSection';
import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { createPublicPageMetadata, SITE_DESCRIPTION, SITE_TITLE } from '@/lib/site';
import { GrowthLists } from '@/components/growth/GrowthLists';
import { buildGrowthFeed } from '@/lib/growthFeed';

// Growth Phase 5: the first screen settles an argument (search, today's claim, myth,
// Hype Gap, trending, head to head); tracking and tiers move below it.
export const revalidate = 3600;

export const metadata = createPublicPageMetadata({
  title: SITE_TITLE,
  description: SITE_DESCRIPTION,
  path: '/',
});

export default async function HomePage() {
  const feed = await buildGrowthFeed();
  const softwareSchema = {
    '@context': 'https://schema.org',
    '@type': 'SoftwareApplication',
    name: 'BioStack',
    applicationCategory: 'HealthApplication',
    operatingSystem: 'Web',
    description: 'A free, public library of what the research says about peptides and similar compounds, graded by evidence strength, for anyone deciding for themselves.',
  };

  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main id="main" tabIndex={-1}>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(softwareSchema) }}
        />
        <GrowthLists feed={feed} />
        <LandingHero />
        <IntelligenceProofSection />

        <section className="mx-auto max-w-7xl px-5 py-8 sm:px-8 lg:py-10">
          <div className="rounded-lg border border-white/8 bg-white/[0.025] px-4 py-4 sm:flex sm:items-center sm:justify-between sm:gap-6 sm:px-5">
            <p className="text-base font-semibold tracking-tight text-white sm:text-lg">
              BioStack is not a doctor.
            </p>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-white/56 sm:mt-0">
              BioStack organizes source-graded evidence, math, overlap context, and your own records. It does not prescribe, diagnose, recommend compounds, or replace qualified medical care.
            </p>
          </div>
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
