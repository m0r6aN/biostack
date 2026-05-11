import { cn } from '@/lib/utils';

interface TrustTimelineProps {
  completeness: string;
  needsReview: boolean;
  status: string;
  qualityFlags: string[];
}

type Step = 1 | 2 | 3;

function deriveActiveStep(completeness: string, needsReview: boolean, status: string): Step {
  if (status === 'promoted') return 3;
  if (needsReview || status === 'review-gated') return 2;
  return 1;
}

const STEPS = [
  { label: 'Research', sub: 'Data collection' },
  { label: 'Review', sub: 'Gate check' },
  { label: 'Promoted', sub: 'Public record' },
] as const;

export function TrustTimeline({ completeness, needsReview, status, qualityFlags }: TrustTimelineProps) {
  const activeStep = deriveActiveStep(completeness, needsReview, status);

  return (
    <div className="p-4 rounded-xl border border-white/8 bg-white/[0.03]">
      <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest mb-4">
        Trust Pipeline
      </p>
      <div className="flex items-center gap-0">
        {STEPS.map((step, i) => {
          const stepNum = (i + 1) as Step;
          const isActive = stepNum === activeStep;
          const isPast = stepNum < activeStep;

          return (
            <div key={step.label} className="flex items-center flex-1 last:flex-none">
              <div className="flex flex-col items-center gap-1.5">
                <div
                  className={cn(
                    'w-7 h-7 rounded-full border-2 flex items-center justify-center text-[11px] font-bold transition-colors',
                    isActive && 'border-emerald-400 bg-emerald-500/20 text-emerald-300',
                    isPast && 'border-white/30 bg-white/10 text-white/50',
                    !isActive && !isPast && 'border-white/10 bg-transparent text-white/20',
                  )}
                >
                  {isPast ? (
                    <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                    </svg>
                  ) : stepNum}
                </div>
                <div className="text-center">
                  <p className={cn(
                    'text-[11px] font-semibold',
                    isActive ? 'text-emerald-300' : isPast ? 'text-white/50' : 'text-white/20',
                  )}>
                    {step.label}
                  </p>
                  <p className={cn(
                    'text-[10px]',
                    isActive ? 'text-emerald-300/60' : 'text-white/20',
                  )}>
                    {step.sub}
                  </p>
                </div>
              </div>
              {i < STEPS.length - 1 && (
                <div className={cn(
                  'h-px flex-1 mx-2 mb-5',
                  isPast ? 'bg-white/20' : 'bg-white/8',
                )} />
              )}
            </div>
          );
        })}
      </div>

      {qualityFlags.length > 0 && (
        <div className="mt-4 pt-3 border-t border-white/5">
          <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest mb-2">Quality Flags</p>
          <div className="flex flex-wrap gap-1.5">
            {qualityFlags.map((flag, i) => (
              <span
                key={i}
                className="text-[11px] px-2 py-0.5 rounded-full bg-slate-500/15 text-slate-300/70 border border-slate-400/15"
              >
                {flag}
              </span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
