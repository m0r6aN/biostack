import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';

const boundaries = [
  'BioStack does not prescribe, diagnose, or recommend compounds.',
  'Calculators provide mathematical results, not medical interpretation.',
  'Overlap flags are prompts for review, not verdicts.',
  'Evidence tiers describe source strength, not personal suitability.',
];

export default function SafetyPage() {
  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main className="mx-auto max-w-5xl px-5 py-12 sm:px-8 lg:py-16">
        <section>
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-emerald-300/70">
            Safety
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-6xl">
            BioStack is not a doctor.
          </h1>
          <p className="mt-5 max-w-2xl text-lg leading-8 text-white/62">
            It is infrastructure for tracking, math, and clarity.
          </p>
        </section>

        <section className="mt-10 grid gap-4 md:grid-cols-2">
          {boundaries.map((boundary) => (
            <div key={boundary} className="rounded-lg border border-white/10 bg-white/[0.035] p-5">
              <p className="text-base leading-7 text-white/72">{boundary}</p>
            </div>
          ))}
        </section>

        <section className="mt-10 rounded-lg border border-emerald-300/16 bg-emerald-400/[0.06] p-5">
          <p className="text-lg font-semibold text-white">No prescriptions. No guesswork. Just structure.</p>
          <p className="mt-3 max-w-3xl text-sm leading-6 text-white/58">
            Use BioStack to organize protocol data and calculations. Work with qualified professionals for medical decisions.
          </p>
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
