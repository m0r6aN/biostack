import { redirect } from 'next/navigation';

interface OnboardingPageProps {
  searchParams?: Promise<{ mode?: string }> | { mode?: string };
}

export default async function OnboardingPage({ searchParams }: OnboardingPageProps) {
  const params = searchParams ? await searchParams : {};
  redirect(params?.mode === 'existing' ? '/start?mode=existing' : '/start');
}
