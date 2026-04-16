import { LandingPathCard } from './LandingPathCard';

const entryPaths = [
  {
    label: 'Starter',
    title: "I'm getting started",
    body: 'Help me figure out what to take and how to begin',
    href: '/start',
    tone: 'emerald',
    signal: 'Guided build',
    path: 'starter',
  },
  {
    label: 'Experienced',
    title: 'I already have a stack',
    body: "Show me what overlaps, what works, and what doesn't",
    href: '/map',
    tone: 'sky',
    signal: 'Quick analysis',
    path: 'experienced',
  },
  {
    label: 'Retail / Provider',
    title: 'I work with clients',
    body: 'Manage client protocols with structure and clarity',
    href: '/providers',
    tone: 'gold',
    signal: 'Client workflows',
    path: 'provider',
  },
] as const;

const toneClasses = {
  emerald:
    'border-emerald-300/24 bg-emerald-400/[0.09] hover:border-emerald-300/40 hover:bg-emerald-400/[0.13] focus-visible:ring-emerald-300/60',
  sky:
    'border-sky-300/24 bg-sky-400/[0.075] shadow-[inset_0_0_0_1px_rgba(56,189,248,0.035),0_12px_34px_rgba(0,0,0,0.2)] hover:border-sky-200/42 hover:bg-sky-400/[0.11] focus-visible:ring-sky-300/55',
  gold:
    'border-amber-200/24 bg-[linear-gradient(135deg,rgba(251,191,36,0.09),rgba(255,255,255,0.035))] hover:border-amber-100/42 hover:bg-amber-300/[0.09] focus-visible:ring-amber-200/55',
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
      <div className="mx-auto grid min-h-[calc(100svh-61px)] max-w-7xl gap-6 px-5 pb-5 pt-5 sm:min-h-[calc(100svh-73px)] sm:px-8 sm:pt-10 lg:grid-cols-[1.03fr_0.97fr] lg:items-center lg:gap-12 lg:py-8">
        <div>
          <div className="max-w-4xl">
            <p className="text-[11px] font-semibold uppercase tracking-[0.28em] text-emerald-300/70 sm:text-xs">
              BioStack
            </p>
            <h1 className="mt-3 text-[2.28rem] font-semibold leading-[0.96] tracking-tight text-white sm:mt-4 sm:text-5xl lg:text-6xl">
              Stop guessing what to take—or what your stack is actually doing.
            </h1>
            <p className="mt-4 max-w-2xl text-base leading-6 text-white/64 sm:mt-5 sm:text-lg">
              Start from scratch, analyze what you&apos;re already using, or manage protocols at scale.
            </p>
          </div>

          <div className="mt-4 grid gap-2.5 sm:mt-6">
            {entryPaths.map((path) => (
              <LandingPathCard
                key={path.href}
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
        </div>

        <HeroSystemVisual />
      </div>
    </section>
  );
}

function HeroSystemVisual() {
  return (
    <div className="relative min-h-[240px] overflow-hidden rounded-lg border border-white/10 bg-[#0D131A]/82 p-5 shadow-[0_20px_70px_rgba(0,0,0,0.28)] sm:min-h-[360px] lg:min-h-[560px]">
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_24%_20%,rgba(34,197,94,0.16),transparent_28%),radial-gradient(circle_at_78%_32%,rgba(56,189,248,0.12),transparent_30%),linear-gradient(180deg,rgba(255,255,255,0.045),transparent_48%)]" />
      <div className="absolute inset-x-6 top-8 h-px bg-gradient-to-r from-transparent via-white/30 to-transparent" />
      <div className="absolute inset-y-8 left-8 w-px bg-gradient-to-b from-transparent via-emerald-300/22 to-transparent" />

      <div className="relative flex h-full min-h-[200px] flex-col justify-between sm:min-h-[320px] lg:min-h-[520px]">
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.22em] text-emerald-200/70">
              Relationship Map
            </p>
            <p className="mt-2 max-w-xs text-sm leading-6 text-white/58">
              Inputs become structure before they become decisions.
            </p>
          </div>
          <span className="rounded-lg border border-emerald-300/16 bg-emerald-400/10 px-3 py-2 text-xs font-semibold text-emerald-100">
            Live
          </span>
        </div>

        <div className="relative mx-auto my-6 h-36 w-full max-w-md sm:h-52 lg:h-64">
          <svg className="absolute inset-0 h-full w-full" viewBox="0 0 420 260" fill="none" aria-hidden="true">
            <path d="M58 188 C112 118 170 104 224 138 C274 170 318 138 370 72" stroke="rgba(148,163,184,0.26)" strokeWidth="1.4" />
            <path d="M64 78 C128 126 178 136 236 104 C292 72 330 98 376 152" stroke="rgba(52,211,153,0.34)" strokeWidth="1.4" />
            <path d="M88 218 C136 170 196 154 252 172 C312 192 340 176 390 126" stroke="rgba(56,189,248,0.25)" strokeWidth="1.4" />
            <path d="M52 128 C106 144 162 168 216 150 C276 130 318 172 384 204" stroke="rgba(251,191,36,0.2)" strokeWidth="1.2" />
            <path d="M118 44 C168 68 204 74 248 58 C292 42 330 48 374 94" stroke="rgba(255,255,255,0.14)" strokeWidth="1" />
          </svg>

          {[
            ['left-[10%] top-[66%]', 'Stack A'],
            ['left-[22%] top-[22%]', 'Stack B'],
            ['left-[49%] top-[44%]', 'Overlap'],
            ['left-[79%] top-[20%]', 'Signal'],
            ['left-[83%] top-[62%]', 'Timeline'],
            ['left-[27%] top-[82%]', 'Client set'],
            ['left-[70%] top-[80%]', 'Protocol'],
          ].map(([position, label], index) => (
            <div
              key={label}
              className={`absolute ${position} -translate-x-1/2 -translate-y-1/2 rounded-lg border border-white/10 bg-black/45 px-3 py-2 shadow-[0_0_24px_rgba(0,0,0,0.22)] backdrop-blur-sm`}
            >
              <div className="flex items-center gap-2">
                <span
                  className={`h-2 w-2 rounded-full ${
                    index === 2
                      ? 'bg-emerald-300 shadow-[0_0_18px_rgba(52,211,153,0.8)]'
                      : index === 3
                        ? 'bg-sky-300 shadow-[0_0_16px_rgba(56,189,248,0.55)]'
                        : index > 4
                          ? 'bg-amber-200 shadow-[0_0_14px_rgba(251,191,36,0.38)]'
                          : 'bg-white/45'
                  }`}
                />
                <span className="text-xs font-semibold text-white/76">{label}</span>
              </div>
            </div>
          ))}
        </div>

        <div className="grid grid-cols-3 gap-2">
          {[
            ['Flows', '3 paths'],
            ['Checks', 'Parallel'],
            ['Scale', 'Client sets'],
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
