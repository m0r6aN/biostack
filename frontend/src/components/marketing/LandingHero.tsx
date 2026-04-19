import Link from 'next/link';
import { LandingPathCard } from './LandingPathCard';

const entryPaths = [
  {
    label: 'Starter',
    title: "I'm getting started",
    body: 'Set up compound tracking without rebuilding a spreadsheet.',
    href: '/start',
    tone: 'emerald',
    signal: 'Guided',
    action: 'Start',
    path: 'starter',
  },
  {
    label: 'Experienced',
    title: 'I already have a stack',
    body: 'Map active compounds, overlap signals, and timeline context.',
    href: '/map',
    tone: 'sky',
    signal: 'Fast analysis',
    action: 'Map',
    path: 'experienced',
  },
  {
    label: 'Provider',
    title: 'I work with clients',
    body: 'Track client protocol changes, notes, and check-ins.',
    href: '/providers',
    tone: 'gold',
    signal: 'Multi-client',
    action: 'Open',
    path: 'provider',
  },
] as const;

const toneClasses = {
  emerald:
    'border-emerald-300/24 bg-emerald-400/[0.09] hover:border-emerald-300/50 hover:bg-emerald-400/[0.14] hover:shadow-[0_18px_48px_rgba(16,185,129,0.16)] focus-visible:ring-emerald-300/60',
  sky:
    'border-sky-300/24 bg-sky-400/[0.075] shadow-[inset_0_0_0_1px_rgba(56,189,248,0.035),0_12px_34px_rgba(0,0,0,0.2)] hover:border-sky-200/52 hover:bg-sky-400/[0.12] hover:shadow-[0_18px_48px_rgba(56,189,248,0.14)] focus-visible:ring-sky-300/55',
  gold:
    'border-amber-200/24 bg-[linear-gradient(135deg,rgba(251,191,36,0.09),rgba(255,255,255,0.035))] hover:border-amber-100/52 hover:bg-amber-300/[0.1] hover:shadow-[0_18px_48px_rgba(251,191,36,0.12)] focus-visible:ring-amber-200/55',
};

const iconClasses = {
  emerald: 'border-emerald-300/20 text-emerald-100 group-hover:bg-emerald-400/14',
  sky: 'border-sky-200/22 text-sky-100 group-hover:bg-sky-400/14',
  gold: 'border-amber-100/22 text-amber-100 group-hover:bg-amber-300/12',
};

const railClasses = {
  emerald: 'bg-emerald-300/70 shadow-[0_0_18px_rgba(52,211,153,0.48)]',
  sky: 'bg-sky-300/78 shadow-[0_0_18px_rgba(56,189,248,0.5)]',
  gold: 'bg-amber-200/72 shadow-[0_0_18px_rgba(251,191,36,0.36)]',
};

const signalClasses = {
  emerald: 'text-emerald-100/90',
  sky: 'text-sky-100/90',
  gold: 'text-amber-100/90',
};

export function LandingHero() {
  return ( 
    <section className="border-b border-white/8">
      <div className="mx-auto flex min-h-[calc(90svh-61px)] max-w-7xl flex-col justify-center gap-5 px-5 pb-7 pt-5 sm:min-h-[calc(90svh-73px)] sm:px-8 sm:pt-8 lg:min-h-[calc(88svh-73px)] lg:gap-7 lg:py-7">
        <div className="max-w-5xl">
          <p>Built for peptides, SARMs, SERMs, and beyond</p>
          <h1 className="text-[2.45rem] font-semibold leading-[0.96] tracking-tight text-white sm:text-6xl lg:text-7xl">
            What to take. How to use it.<br />
            See what it's doing.
          </h1>
          <p className="mt-4 max-w-3xl text-base leading-6 text-white/64 sm:mt-5 sm:text-lg">
            Start with answers. Then choose to track, compare, and optimize over time.
          </p>
        </div>

        <div className="grid gap-2.5 sm:gap-3 lg:col-span-2 lg:grid-cols-3">
          {entryPaths.map((path) => (
            <LandingPathCard
              key={path.href}
              action={path.action}
              body={path.body}
              cardClassName={toneClasses[path.tone]}
              href={path.href}
              iconClassName={iconClasses[path.tone]}
              label={path.label}
              path={path.path}
              railClassName={railClasses[path.tone]}
              signal={path.signal}
              signalClassName={signalClasses[path.tone]}
              title={path.title}
            />
          ))}
        </div>

        <Link
          href="/tools"
          className="w-fit text-sm font-medium text-white/62 underline-offset-4 transition-colors hover:text-white/82 hover:underline sm:text-base"
        >
          Need help with dosage or mixing? → Start here
        </Link>
      </div>
    </section>
  );
}
