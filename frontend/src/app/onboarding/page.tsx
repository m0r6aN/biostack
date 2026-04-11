import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { OnboardingExperience } from '@/components/marketing/OnboardingExperience';

export default function OnboardingPage() {
  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <OnboardingExperience />
      <MarketingFooter />
    </div>
  );
}