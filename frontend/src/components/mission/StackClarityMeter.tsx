'use client';

import { deriveSignalClarity } from '@/lib/derive/signalClarity';
import { getStackClarityBand } from '@/styles/tokens';
import { WhyDrawer } from '@/components/intel/WhyDrawer';
import { DeltaBadge } from '@/components/intel/DeltaBadge';
import { cn } from '@/lib/utils';
import type { CurrentStackIntelligence, CompoundRecord } from '@/lib/types';

interface StackClarityMeterProps {
  stackIntelligence: CurrentStackIntelligence | null;
  compounds: CompoundRecord[];
  checkInCount: number;
  hasActiveRun: boolean;
  className?: string;
}

export function StackClarityMeter({ stackIntelligence, compounds, checkInCount, hasActiveRun, className }: StackClarityMeterProps) {
  const result = deriveSignalClarity(stackIntelligence, compounds, checkInCount, hasActiveRun);
  const band = getStackClarityBand(result.score);

  // Arc geometry for the SVG meter
  const R = 52;
  const cx = 64;
  const cy = 64;
  const strokeWidth = 8;
  const startAngle = -220;
  const endAngle = 40;
  const totalSweep = endAngle - startAngle;
  const scoreSweep = (result.score / 100) * totalSweep;

  function polarToCart(angle: number, r: number) {
    const rad = ((angle - 90) * Math.PI) / 180;
    return { x: cx + r * Math.cos(rad), y: cy + r * Math.sin(rad) };
  }

  function describeArc(start: number, end: number) {
    const s = polarToCart(start, R);
    const e = polarToCart(end, R);
    const largeArc = Math.abs(end - start) > 180 ? 1 : 0;
    return `M ${s.x} ${s.y} A ${R} ${R} 0 ${largeArc} 1 ${e.x} ${e.y}`;
  }

  const trackPath = describeArc(startAngle, endAngle);
  const scorePath = result.score > 0 ? describeArc(startAngle, startAngle + scoreSweep) : '';

  const colorClass = band.color.replace('text-', '');

  return (
    <div className={cn('rounded-3xl border border-white/5 bg-white/[0.02] p-5', className)}>
      <div className="flex items-center justify-between mb-4">
        <p className="text-[10px] font-bold text-white/20 uppercase tracking-widest">Stack Clarity</p>
        <WhyDrawer
          surface="Stack Clarity"
          title="How is clarity calculated?"
          inputs={result.whyInputs}
          reasoning={result.reasoning}
          caveats={['Clarity score is observational — it reflects data completeness, not outcome quality.']}
        />
      </div>

      <div className="flex items-center gap-5">
        {/* Arc meter */}
        <div className="shrink-0">
          <svg width="128" height="90" viewBox="0 0 128 90" className="overflow-visible">
            {/* Track */}
            <path d={trackPath} fill="none" stroke="rgba(255,255,255,0.06)" strokeWidth={strokeWidth} strokeLinecap="round" />
            {/* Score arc */}
            {scorePath && (
              <path
                d={scorePath}
                fill="none"
                strokeWidth={strokeWidth}
                strokeLinecap="round"
                className={`stroke-current ${band.color.replace('text-', 'text-')}`}
                style={{ transition: 'stroke-dashoffset 0.6s ease' }}
              />
            )}
            {/* Score label */}
            <text x={cx} y={cy + 4} textAnchor="middle" className={cn('font-bold text-xl fill-current', band.color)} style={{ fontSize: '22px', fontWeight: 700 }}>
              {result.score}
            </text>
            <text x={cx} y={cy + 18} textAnchor="middle" style={{ fontSize: '9px', fill: 'rgba(255,255,255,0.3)', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.1em' }}>
              / 100
            </text>
          </svg>
        </div>

        {/* Band + limiters */}
        <div className="flex-1 min-w-0">
          <p className={cn('text-sm font-bold mb-2', band.color)}>{result.band}</p>
          {result.limiters.length > 0 ? (
            <div className="space-y-1.5">
              {result.limiters.map((limiter) => (
                <div key={limiter.label} className="flex items-start gap-2">
                  <DeltaBadge value={limiter.delta} className="shrink-0 mt-0.5" />
                  <span className="text-[11px] text-white/50 leading-snug">{limiter.description}</span>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-[11px] text-white/40">All clarity signals nominal.</p>
          )}
        </div>
      </div>
    </div>
  );
}
