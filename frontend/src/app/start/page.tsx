import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { OnboardingExperience } from '@/components/marketing/OnboardingExperience';
import { createPublicPageMetadata } from '@/lib/site';

export const metadata = createPublicPageMetadata({
  title: 'Start With BioStack',
  description: 'Choose a new or existing protocol path and begin organizing your observational record.',
  path: '/start',
});

interface StartPageProps {
  searchParams?: Promise<{ mode?: string }> | { mode?: string };
}

export default async function StartPage({ searchParams }: StartPageProps) {
  const params = searchParams ? await searchParams : {};
  const mode = params?.mode === 'existing' ? 'existing' : 'new';

  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <OnboardingExperience mode={mode} />
      <MarketingFooter />
    </div>
  );
}
