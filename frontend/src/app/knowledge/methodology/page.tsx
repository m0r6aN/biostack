import Link from 'next/link';
import { createPublicPageMetadata } from '@/lib/site';
import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';

export const metadata = createPublicPageMetadata({
  title: 'How BioStack Grades Evidence | BioStack',
  description:
    'The rubric behind every evidence tier in the BioStack library: what Unknown, Limited, Moderate, Strong, and Mechanistic mean, where sources come from, and what a grade does not claim.',
  path: '/knowledge/methodology',
});

const tiers = [
  {
    name: 'Unknown',
    definition:
      'No graded human or preclinical literature has been reviewed for this claim, or the available material does not meet the sourcing bar below. Unknown is the default state — a compound starts here and earns its way out.',
  },
  {
    name: 'Limited',
    definition:
      'Sparse evidence: isolated case reports, small uncontrolled studies, or findings that have not been replicated. Enough to describe, not enough to weigh.',
  },
  {
    name: 'Moderate',
    definition:
      'Multiple consistent studies or at least one well-designed controlled trial, with limitations in size, duration, or population that keep the picture incomplete.',
  },
  {
    name: 'Strong',
    definition:
      'Replicated controlled human trials or systematic reviews pointing the same direction. Strong describes the state of the literature — it is not an endorsement of use.',
  },
  {
    name: 'Mechanistic',
    definition:
      'Evidence describes how a compound acts (receptor, pathway, in-vitro or animal data) without adequate human outcome data. A plausible mechanism is not an outcome.',
  },
];

const notClaims = [
  'A grade never means a compound is safe, effective, or appropriate for any person.',
  'A grade never ranks compounds against each other or suggests one over another.',
  'A grade never accounts for an individual’s health status, medications, or context — only a qualified clinician can.',
  'A higher tier is not a recommendation. It means more is known, including, sometimes, more about limitations and risks.',
];

export default function MethodologyPage() {
  return (
    <div>
      <MarketingNav />
      <main className="min-h-screen bg-[#0a0a0b] px-5 py-12 sm:px-8">
        <div className="mx-auto max-w-3xl">
          <p className="text-[11px] font-medium uppercase tracking-[0.18em] text-emerald-200/78">
            Methodology
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">
            How BioStack grades evidence.
          </h1>
          <p className="mt-5 max-w-2xl text-lg leading-8 text-white/62">
            Every compound dossier in the library carries an evidence tier. The tier describes the
            strength of the published research behind what the dossier reports — nothing more. This
            page is the rubric, in full, so the grading is inspectable rather than taken on trust.
          </p>

          <section aria-labelledby="tiers-heading" className="mt-12">
            <h2 id="tiers-heading" className="text-2xl font-semibold tracking-tight text-white">
              The five tiers
            </h2>
            <div className="mt-6 space-y-4">
              {tiers.map((tier) => (
                <div
                  key={tier.name}
                  className="rounded-2xl border border-white/10 bg-white/[0.04] p-6"
                >
                  <h3 className="text-base font-semibold text-emerald-200/90">{tier.name}</h3>
                  <p className="mt-2 text-sm leading-6 text-white/62">{tier.definition}</p>
                </div>
              ))}
            </div>
          </section>

          <section aria-labelledby="sources-heading" className="mt-12">
            <h2 id="sources-heading" className="text-2xl font-semibold tracking-tight text-white">
              Where sources come from
            </h2>
            <p className="mt-4 text-sm leading-6 text-white/62">
              Dossier claims cite published literature — peer-reviewed studies, reviews, and
              regulatory documentation — and every dossier lists its references so a claim can be
              followed back to its source. Social posts, influencer content, and anecdote are not
              sources, whatever their reach. When the literature is thin, the dossier says so
              instead of borrowing confidence it has not earned.
            </p>
          </section>

          <section aria-labelledby="not-heading" className="mt-12">
            <h2 id="not-heading" className="text-2xl font-semibold tracking-tight text-white">
              What a grade is not
            </h2>
            <ul className="mt-4 space-y-3">
              {notClaims.map((claim) => (
                <li key={claim} className="flex gap-3 text-sm leading-6 text-white/62">
                  <span aria-hidden="true" className="mt-1 h-1.5 w-1.5 flex-shrink-0 rounded-full bg-white/30" />
                  {claim}
                </li>
              ))}
            </ul>
            <p className="mt-6 text-sm leading-6 text-white/62">
              BioStack is not a doctor. The library exists so decisions people are already making
              are grounded in what the research actually says — the decision, and the conversation
              with a qualified clinician, stays with you.
            </p>
          </section>

          <div className="mt-12">
            <Link
              href="/knowledge"
              className="inline-flex items-center gap-2 rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5 focus-visible:outline-none focus-visible:ring-2"
            >
              Browse the evidence library
            </Link>
          </div>
        </div>
      </main>
      <MarketingFooter />
    </div>
  );
}
