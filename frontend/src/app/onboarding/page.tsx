import { redirect } from 'next/navigation';
import { canonicalRoutes } from '@/lib/productContract';

export default function OnboardingPage() {
  redirect(canonicalRoutes.onboarding);
}
