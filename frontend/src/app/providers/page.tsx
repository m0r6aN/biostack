import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';
import Link from 'next/link';

const providerCapabilities = [
  {
    title: 'Organized multi-client records',
    body: 'Keep each client’s protocol history in one place instead of rebuilding context from notes, memory, or scattered messages every visit.',
  },
  {
    title: 'Structured intake',
    body: 'Clients log what they’re tracking in a consistent structure, so you start from an organized record rather than a blank page.',
  },
  {
    title: 'Shared observational summaries',
    body: 'Review a client’s protocol and check-in history in one summary view instead of piecing it together from separate conversations.',
  },
  {
    title: 'Check-in visibility over time',
    body: 'See how a client’s self-reported check-ins accumulate across an active protocol, organized alongside their tracked compounds.',
  },
];

const businessValue = [
  'Less time spent reconstructing "what are you currently taking?" at the start of every conversation.',
  'A single, evidence-aware shared reference in place of scattered notes, texts, and memory.',
  'Reduced intake friction when a client’s protocol history is already organized before you talk.',
];

const boundaries = [
  'BioStack does not prescribe, dose, diagnose, or recommend treatment.',
  'It organizes tracking and observational data — it does not interpret it clinically.',
  'The provider remains the clinician. BioStack does not replace clinical judgment.',
  'Evidence tiers describe source strength in the literature, not personal suitability.',
];

const workflowSteps = [
  {
    title: 'Provider and client each create a profile',
    body: 'You and your client set up BioStack profiles independently.',
  },
  {
    title: 'Client tracks and shares their stack',
    body: 'In this illustration, the client logs the compounds and protocol details they track, and shares that record with you.',
  },
  {
    title: 'Provider reviews the shared summary',
    body: 'You review the client’s organized protocol and observational summary instead of reconstructing it from notes.',
  },
  {
    title: 'Check-ins accumulate over time',
    body: 'As the client continues logging check-ins, the shared record builds into a longer-term reference you can both return to.',
  },
];

const providerFaqs = [
  {
    question: 'Is BioStack a medical device or EHR?',
    answer:
      'No. BioStack is an organizational and observational tool, not a medical device or an electronic health record system.',
  },
  {
    question: 'Does BioStack give dosing or treatment recommendations?',
    answer:
      'No. BioStack never prescribes, doses, diagnoses, or recommends treatment. It organizes client tracking and observational data only — clinical decisions remain yours.',
  },
  {
    question: 'Can I manage multiple clients?',
    answer:
      'Yes. BioStack is built to organize protocol records for multiple clients at once, using a separate profile for each client you work with.',
  },
  {
    question: 'Who owns client data?',
    answer:
      'The client owns their data. BioStack is designed around the principle that providers should only ever see what a client chooses to share with them.',
  },
  {
    question: 'Can clients revoke access?',
    answer:
      'Client control over shared access is a core design principle — clients are never locked into sharing their data with a provider.',
  },
  {
    question: 'Can I export or share observational summaries?',
    answer:
      'Protocols include a provider summary view built for sharing an organized, observational snapshot of a client’s tracked protocol. It’s designed to be reviewed together rather than as a full clinical export.',
  },
  {
    question: 'What does BioStack cost for providers?',
    answer:
      'Provider pricing isn’t published yet. Reach out using the request link below and we’ll follow up directly.',
  },
  {
    question: 'What happens if a client stops using BioStack?',
    answer:
      'You retain access to whatever the client already shared up to that point. Once they stop, you won’t receive further updates to their protocol or check-ins going forward.',
  },
];

export default function ProvidersPage() {
  return (
    <div className="min-h-screen pb-24 md:pb-0" style={{ position: 'relative', zIndex: 1 }}>
      <MarketingNav />

      <main className="mx-auto max-w-6xl px-5 py-12 sm:px-8 lg:py-16">
        <section className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-amber-200/72">
            For Providers
          </p>
          <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-6xl">
            A clearer picture of what your clients are taking
          </h1>
          <p className="mt-5 max-w-2xl text-base leading-7 text-white/62 sm:text-lg">
            BioStack organizes multi-client protocol tracking, reduces intake friction, and gives
            you and your client a shared, evidence-aware reference — without crossing into
            medical advice. It’s an organizational layer for people who track their own
            protocols, not a clinical or EHR system.
          </p>
        </section>

        <section className="mt-14">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-emerald-300/68">
            What providers can do
          </p>
          <div className="mt-5 grid gap-4 md:grid-cols-2">
            {providerCapabilities.map((item) => (
              <div key={item.title} className="rounded-lg border border-white/10 bg-white/[0.035] p-5">
                <p className="text-base font-medium leading-6 text-white/86">{item.title}</p>
                <p className="mt-2 text-sm leading-6 text-white/60">{item.body}</p>
              </div>
            ))}
          </div>
        </section>

        <section className="mt-14 max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-emerald-300/68">
            Why it matters
          </p>
          <ul className="mt-5 space-y-3">
            {businessValue.map((item) => (
              <li key={item} className="flex gap-3 text-base leading-7 text-white/68">
                <span className="mt-2.5 h-1.5 w-1.5 flex-none rounded-full bg-emerald-300" />
                <span>{item}</span>
              </li>
            ))}
          </ul>
        </section>

        <section className="mt-14 rounded-lg border border-amber-400/20 bg-amber-500/[0.06] p-6">
          <p className="text-lg font-semibold text-white">What BioStack does not do</p>
          <div className="mt-4 grid gap-3 sm:grid-cols-2">
            {boundaries.map((boundary) => (
              <p key={boundary} className="text-sm leading-6 text-amber-100/80">
                {boundary}
              </p>
            ))}
          </div>
        </section>

        <section className="mt-14">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-emerald-300/68">
            Example workflow
          </p>
          <p className="mt-3 max-w-2xl text-sm leading-6 text-white/50">
            This is an illustration of how the pieces fit together, not a literal step-by-step
            feature walkthrough.
          </p>
          <ol className="mt-5 grid gap-4 md:grid-cols-2">
            {workflowSteps.map((step, index) => (
              <li key={step.title} className="rounded-lg border border-white/10 bg-white/[0.035] p-5">
                <span className="text-xs font-semibold uppercase tracking-[0.2em] text-white/40">
                  Step {index + 1}
                </span>
                <p className="mt-2 text-base font-medium leading-6 text-white/86">{step.title}</p>
                <p className="mt-2 text-sm leading-6 text-white/60">{step.body}</p>
              </li>
            ))}
          </ol>
        </section>

        <section className="mt-14 max-w-3xl rounded-lg border border-white/10 bg-white/[0.03] p-6">
          <p className="text-lg font-semibold text-white">Data ownership &amp; privacy</p>
          <p className="mt-3 text-sm leading-7 text-white/60">
            Client data belongs to the client. BioStack is built around the principle that
            providers should only ever see what a client chooses to share, and that clients
            retain control over that access.
          </p>
        </section>

        <section className="mt-14">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-emerald-300/68">
            Provider FAQ
          </p>
          <dl className="mt-5 space-y-5">
            {providerFaqs.map((faq) => (
              <div key={faq.question} className="rounded-[1.75rem] border border-white/10 bg-white/[0.03] p-6">
                <dt className="text-xl font-semibold text-white">{faq.question}</dt>
                <dd className="mt-3 leading-8 text-white/64">{faq.answer}</dd>
              </div>
            ))}
          </dl>
        </section>

        <section className="mt-14">
          <div className="flex flex-wrap items-center gap-4">
            <a
              href="mailto:providers@biostack.app?subject=Provider%20Access%20Request"
              className="rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
            >
              Request Provider Access
            </a>
            <p className="text-sm text-white/50">
              Already working with BioStack?{' '}
              <Link href="/auth/signin" className="font-semibold text-white/70 underline hover:text-white">
                Sign in
              </Link>
            </p>
          </div>
        </section>
      </main>

      <MarketingFooter />
    </div>
  );
}
