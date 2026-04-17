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
      <div className="mx-auto grid min-h-[calc(90svh-61px)] max-w-7xl gap-5 px-5 pb-7 pt-5 sm:min-h-[calc(90svh-73px)] sm:px-8 sm:pt-8 lg:min-h-[calc(88svh-73px)] lg:grid-cols-[0.86fr_1.14fr] lg:items-center lg:gap-x-12 lg:gap-y-7 lg:py-7">
        <div className="max-w-3xl">
          <p className="text-[11px] font-semibold uppercase tracking-[0.28em] text-emerald-300/70 sm:text-xs">
            Track peptides, compounds, and layered protocols
          </p>
          <h1 className="mt-3 text-[2.28rem] font-semibold leading-[0.96] tracking-tight text-white sm:mt-4 sm:text-5xl lg:text-6xl">
            Stop guessing what to take—or what your stack is actually doing.
          </h1>
          <p className="mt-4 max-w-2xl text-base leading-6 text-white/64 sm:mt-5 sm:text-lg">
            Organize compounds by date, protocol, overlap, and check-in history before you decide what comes next.
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

        <HeroSystemVisual />
      </div>
    </section>
  );
}

function HeroSystemVisual() {
  const compounds = [
    {
      name: 'BPC-157',
      start: 'Apr 04',
      status: 'Active',
      signal: 'Tendon support',
      tone: 'emerald',
    },
    {
      name: 'TB-500',
      start: 'Apr 07',
      status: 'Overlap',
      signal: 'Shared recovery window',
      tone: 'amber',
    },
    {
      name: 'NAD+',
      start: 'Apr 11',
      status: 'Check-in',
      signal: 'Energy tracked Day 7',
      tone: 'sky',
    },
    {
      name: 'Retatrutide',
      start: 'Apr 15',
      status: 'Review',
      signal: 'Appetite + weight',
      tone: 'white',
    },
  ];

  return (
    <div className="relative overflow-hidden rounded-lg border border-white/10 bg-[#0D131A]/90 p-4 shadow-[0_20px_70px_rgba(0,0,0,0.28)] sm:p-5 lg:col-start-2 lg:row-start-1 lg:p-6">
      <div className="absolute inset-0 bg-[linear-gradient(135deg,rgba(255,255,255,0.06),transparent_38%),linear-gradient(180deg,rgba(34,197,94,0.05),transparent_58%)]" />

      <div className="relative">
        <div className="flex items-start justify-between gap-4 border-b border-white/8 pb-4">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-emerald-200/70">
              Protocol Surface
            </p>
            <p className="mt-2 max-w-sm text-sm leading-6 text-white/58">
              Active compounds, start dates, overlap context, and check-ins in one working view.
            </p>
          </div>
          <span className="hidden rounded-lg border border-white/10 bg-white/[0.035] px-3 py-2 text-xs font-semibold text-white/62 sm:inline-flex">
            Day 14
          </span>
        </div>

        <div className="mt-4 grid gap-3 lg:grid-cols-[1fr_0.62fr]">
          <div className="overflow-hidden rounded-lg border border-white/8 bg-black/20">
            <div className="grid grid-cols-[1fr_4.5rem_5.25rem] border-b border-white/8 px-3 py-2 text-[10px] font-semibold uppercase tracking-[0.16em] text-white/36 sm:grid-cols-[1fr_5.5rem_6.25rem]">
              <span>Compound</span>
              <span>Start</span>
              <span>Signal</span>
            </div>
            <div className="divide-y divide-white/8">
              {compounds.map((compound) => (
                <div
                  key={compound.name}
                  className="grid min-h-[60px] grid-cols-[1fr_4.5rem_5.25rem] items-center px-3 py-2 sm:grid-cols-[1fr_5.5rem_6.25rem]"
                >
                  <div className="min-w-0">
                    <p className="truncate text-sm font-semibold text-white">{compound.name}</p>
                    <p className="mt-1 truncate text-xs text-white/42">{compound.signal}</p>
                  </div>
                  <p className="text-xs font-semibold text-white/62">{compound.start}</p>
                  <span
                    className={`w-fit rounded-lg border px-2 py-1 text-[11px] font-semibold ${
                      compound.tone === 'emerald'
                        ? 'border-emerald-300/18 bg-emerald-400/10 text-emerald-100'
                        : compound.tone === 'amber'
                          ? 'border-amber-200/20 bg-amber-300/10 text-amber-100'
                          : compound.tone === 'sky'
                            ? 'border-sky-300/18 bg-sky-400/10 text-sky-100'
                            : 'border-white/10 bg-white/[0.035] text-white/68'
                    }`}
                  >
                    {compound.status}
                  </span>
                </div>
              ))}
            </div>
          </div>

          <div className="grid gap-3">
            <div className="rounded-lg border border-amber-200/14 bg-amber-300/[0.055] p-3">
              <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-amber-100/58">
                Overlap
              </p>
              <p className="mt-2 text-sm font-semibold text-white">Recovery window overlap</p>
              <p className="mt-1 text-xs leading-5 text-white/50">BPC-157 and TB-500 share an active phase.</p>
            </div>
            <div className="rounded-lg border border-sky-300/14 bg-sky-400/[0.05] p-3">
              <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-sky-100/58">
                Check-in
              </p>
              <p className="mt-2 text-sm font-semibold text-white">Energy + appetite due</p>
              <p className="mt-1 text-xs leading-5 text-white/50">Next review captures Day 7 signal.</p>
            </div>
          </div>
        </div>

        <div className="mt-3 grid grid-cols-3 gap-2">
          {[
            ['Timeline', '14 days'],
            ['Protocols', '2 active'],
            ['Clients', '4 tracked'],
          ].map(([label, value]) => (
            <div key={label} className="rounded-lg border border-white/8 bg-white/[0.035] p-3">
              <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-white/34">{label}</p>
              <p className="mt-1 text-sm font-semibold text-white/78">{value}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
