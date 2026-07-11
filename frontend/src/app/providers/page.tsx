import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import { ProviderAccessForm } from '@/components/marketing/ProviderAccessForm';
import Link from 'next/link';

const workflow = [
  'Submit contact information for the provider pilot review queue.',
  'BioStack reviews fit and follows up with the request owner.',
  'Approved pilot participants receive the tested scope and data-sharing boundaries before access.',
];

const providerCapabilities = [
  'Multi-client protocol organization (pilot scope; not generally available)',
  'Permissioned observational summaries (pilot scope; not generally available)',
  'Evidence-linked compound context without clinical decision support',
  'Revocable client sharing and exports are requirements for the pilot, not launch promises',
];

const boundaries = [
  'BioStack is not a medical device, EHR, prescribing system, or treatment planner.',
  'BioStack does not provide dosing, diagnosis, treatment, or clinical recommendations.',
  'Providers remain responsible for professional judgment and any clinical documentation.',
];

const faqs = [
  {
    question: 'Is BioStack a medical device or EHR?',
    answer:
      'No. BioStack is an educational and observational record system. It is not a medical device, EHR, prescribing platform, or clinical decision support system.',
  },
  {
    question: 'Does BioStack give dosing or treatment recommendations?',
    answer:
      'No. BioStack organizes user-entered protocol records, evidence context, and check-ins. It does not recommend compounds, dosing, treatment, or clinical actions.',
  },
  {
    question: 'Can I manage multiple clients?',
    answer:
      'Not in the generally available product today. Multi-client observability is a provider pilot requirement and will only be offered after its access and client-permission controls are verified.',
  },
  {
    question: 'Who owns client data?',
    answer:
      'Clients own their profile data. Provider access is permissioned and limited to the observational information clients choose to share.',
  },
  {
    question: 'Can clients revoke access?',
    answer:
      'Revocation is a required pilot control. BioStack will not represent provider sharing as available until clients can grant and revoke access reliably.',
  },
  {
    question: 'Can I export or share observational summaries?',
    answer:
      'Saved users can prepare observational summaries for their own use. Provider workspace exports remain pilot scope and are not generally available.',
  },
  {
    question: 'What does BioStack cost for providers?',
    answer:
      'No public provider price is offered yet. Pricing and access terms will be shared only with qualified pilot participants after the workflow and privacy controls are verified.',
  },
  {
    question: 'What happens if a client stops using BioStack?',
    answer:
      'Existing shared summaries remain historical records, but new check-ins, protocol changes, and client updates stop unless the client resumes using BioStack.',
  },
];

export default function ProvidersPage() {
  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />
      <main className="mx-auto max-w-6xl px-5 py-12 sm:px-8 lg:py-16">
        <section className="grid gap-8 lg:grid-cols-[1.1fr_0.9fr] lg:items-center">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-amber-200/72">
              Provider
            </p>
            <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-6xl">
              Client protocol observability without clinical overreach.
            </h1>
            <p className="mt-5 max-w-2xl text-base leading-7 text-white/62 sm:text-lg">
              BioStack is validating a provider pilot for permissioned, observational workflows.
              General multi-client access is not available yet.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <Link
                href="#provider-access-request"
                className="rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
              >
                Request Provider Pilot Access
              </Link>
              <Link
                href="/knowledge"
                className="rounded-lg border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
              >
                View Evidence Library
              </Link>
            </div>
          </div>
          <div className="rounded-lg border border-white/10 bg-white/[0.035] p-6">
            <h2 className="text-lg font-semibold text-white">Provider pilot request workflow</h2>
            <ol className="mt-5 space-y-4">
              {workflow.map((item, index) => (
                <li key={item} className="flex gap-3 text-sm leading-6 text-white/68">
                  <span className="flex h-7 w-7 flex-none items-center justify-center rounded-full bg-amber-300/12 text-xs font-semibold text-amber-100">
                    {index + 1}
                  </span>
                  <span>{item}</span>
                </li>
              ))}
            </ol>
          </div>
        </section>

        <section id="provider-access-request" className="mt-12 scroll-mt-24">
          <ProviderAccessForm />
        </section>

        <section className="mt-12 grid gap-4 md:grid-cols-3">
          <div className="rounded-lg border border-white/10 bg-white/[0.03] p-5 md:col-span-2">
            <h2 className="text-xl font-semibold text-white">What providers can do</h2>
            <ul className="mt-4 grid gap-3 sm:grid-cols-2">
              {providerCapabilities.map((item) => (
                <li key={item} className="text-sm leading-6 text-white/65">
                  {item}
                </li>
              ))}
            </ul>
          </div>
          <div className="rounded-lg border border-amber-300/16 bg-amber-300/[0.06] p-5">
            <h2 className="text-xl font-semibold text-amber-50">What BioStack does not do</h2>
            <ul className="mt-4 space-y-3">
              {boundaries.map((item) => (
                <li key={item} className="text-sm leading-6 text-amber-50/72">
                  {item}
                </li>
              ))}
            </ul>
          </div>
        </section>

        <section className="mt-6 rounded-lg border border-cyan-300/14 bg-cyan-300/[0.05] p-6">
          <h2 className="text-xl font-semibold text-white">Data ownership and privacy</h2>
          <p className="mt-3 max-w-3xl text-sm leading-6 text-white/65">
            Client records stay tied to the client profile. Provider visibility is permissioned,
            observational, and intended to be revocable. BioStack does not sell client protocol data.
          </p>
        </section>

        <section className="mt-12">
          <h2 className="text-2xl font-semibold text-white">Provider FAQ</h2>
          <div className="mt-5 grid gap-3 md:grid-cols-2">
            {faqs.map((item) => (
              <article key={item.question} className="rounded-lg border border-white/10 bg-white/[0.03] p-5">
                <h3 className="text-base font-semibold text-white">{item.question}</h3>
                <p className="mt-2 text-sm leading-6 text-white/58">{item.answer}</p>
              </article>
            ))}
          </div>
        </section>
      </main>
      <MarketingFooter />
    </div>
  );
}
