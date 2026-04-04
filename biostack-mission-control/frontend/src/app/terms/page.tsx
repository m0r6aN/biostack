import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';

export default function TermsPage() {
  return (
    <div className="min-h-screen" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <main className="mx-auto max-w-4xl px-5 py-16 sm:px-8 lg:py-20">
        <p className="text-xs font-semibold uppercase tracking-[0.32em] text-amber-300/70">
          Legal review required
        </p>
        <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white">Terms of Service</h1>
        <p className="mt-5 text-lg leading-8 text-white/62">
          This route now exists for launch plumbing, but the copy is intentionally not final.
          Section J of the commercialization brief marks Terms as a critical legal dependency before
          Stripe goes live.
        </p>
      </main>
      <MarketingFooter />
    </div>
  );
}
