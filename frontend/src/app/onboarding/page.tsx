import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { OnboardingExperience } from '@/components/marketing/OnboardingExperience';

interface OnboardingPageProps {
  searchParams?: Promise<{ mode?: string }> | { mode?: string };
}

export default async function OnboardingPage({ searchParams }: OnboardingPageProps) {
  const params = searchParams ? await searchParams : {};
  const mode = params.mode === 'existing' ? 'existing' : 'new';

  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <OnboardingExperience mode={mode} />
      <MarketingFooter />
    </div>
  );
}
