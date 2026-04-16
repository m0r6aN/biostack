import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { OnboardingExperience } from '@/components/marketing/OnboardingExperience';

export default function MapPage() {
  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <OnboardingExperience mode="existing" />
      <MarketingFooter />
    </div>
  );
}
