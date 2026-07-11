import { formatNumber, type UnifiedDosingResult } from '@/lib/dosingCalculator';

interface SyringeDrawVisualizerProps {
  result: UnifiedDosingResult | null;
  error?: string;
}

export function SyringeDrawVisualizer({ result, error }: SyringeDrawVisualizerProps) {
  const syringeUnits = result?.u100UnitsPerAdministration ?? Number.NaN;
  const valid = Boolean(result) && Number.isFinite(syringeUnits) && syringeUnits > 0;
  const clampedUnits = valid ? clamp(syringeUnits, 0, 100) : 0;
  const overCapacity = valid && syringeUnits > 100;
  const markerX = 70 + (clampedUnits / 100) * 270;
  const majorTicks = Array.from({ length: 11 }, (_, index) => index * 10);
  const tickMarks = Array.from({ length: 21 }, (_, index) => index * 5);

  return (
    <section
      aria-labelledby="syringe-draw-title"
      className="rounded-lg border border-emerald-400/25 bg-emerald-500/10 p-4 sm:p-5"
      data-testid="syringe-draw-visualizer"
    >
      <p className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-200/70">Visual guide</p>
      <h3 id="syringe-draw-title" className="mt-1 text-xl font-semibold text-white">Calculated U-100 syringe draw</h3>
      <p className="mt-2 text-sm leading-6 text-emerald-100/75">100 units on a U-100 syringe equals 1 mL.</p>

      {!valid ? (
        <div role="status" className="mt-4 rounded-lg border border-white/10 bg-black/15 p-4 text-sm text-white/65">
          No draw is shown. Enter positive values to calculate a visual. {error}
        </div>
      ) : (
        <>
          <p className="mt-4 text-3xl font-semibold tracking-tight text-white">
            {formatNumber(result!.volumePerAdministrationMl, 4)} mL
            <span className="ml-2 text-base font-medium text-emerald-100/75">= {formatNumber(syringeUnits, 1)} U-100 units</span>
          </p>
          {overCapacity && (
            <p role="alert" className="mt-3 rounded-lg border border-amber-300/30 bg-amber-400/10 px-3 py-2 text-sm font-semibold text-amber-100">
              This result is {formatNumber(syringeUnits, 1)} units, which is above the 100-unit capacity shown. The visual stops at one syringe.
            </p>
          )}

          <div
            role="meter"
            aria-label="Calculated draw on a 100-unit U-100 syringe"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={Math.round(clampedUnits * 10) / 10}
            aria-valuetext={`${formatNumber(syringeUnits, 1)} U-100 units, equal to ${formatNumber(result!.volumePerAdministrationMl, 4)} milliliters${overCapacity ? ', above the displayed syringe capacity' : ''}`}
            className="mt-3 w-full overflow-x-auto"
          >
            <svg viewBox="0 0 420 175" className="h-auto min-w-[320px] w-full" aria-hidden="true">
              <line x1="38" y1="83" x2="70" y2="83" stroke="#A8B0B7" strokeWidth="8" strokeLinecap="round" />
              <line x1="38" y1="52" x2="38" y2="114" stroke="#A8B0B7" strokeWidth="10" strokeLinecap="round" />
              <rect x="60" y="44" width="12" height="78" rx="2" fill="#A8B0B7" />
              <rect x="70" y="56" width="270" height="54" rx="5" fill="#E7ECEF" />
              <rect x="70" y="63" width={(clampedUnits / 100) * 270} height="40" rx="3" fill="#22C55E" />
              <rect x="70" y="56" width="270" height="54" rx="5" fill="none" stroke="#A8B0B7" strokeWidth="4" />

              {tickMarks.map((units) => {
                const x = 70 + (units / 100) * 270;
                const major = units % 10 === 0;
                return <line key={units} data-tick={major ? 'major' : 'minor'} x1={x} y1={major ? 64 : 72} x2={x} y2="101" stroke="#0F141B" strokeWidth={major ? 2 : 1} />;
              })}
              {majorTicks.map((units) => (
                <text key={units} x={70 + (units / 100) * 270} y="136" fontSize="10" fontWeight="700" fill="#CFE5DB" textAnchor="middle">{units}</text>
              ))}
              <line data-testid="draw-marker" x1={markerX} x2={markerX} y1="47" y2="116" stroke="#FBBF24" strokeWidth="4" />
              <path d={`M ${markerX - 6} 43 L ${markerX + 6} 43 L ${markerX} 51 Z`} fill="#FBBF24" />
              <rect x="338" y="67" width="32" height="32" rx="4" fill="#A8B0B7" />
              <line x1="370" y1="83" x2="402" y2="83" stroke="#A8B0B7" strokeWidth="3" strokeLinecap="round" />
              <text x="205" y="160" fontSize="11" fontWeight="700" fill="#A7F3D0" textAnchor="middle">U-100 units</text>
            </svg>
          </div>
        </>
      )}
    </section>
  );
}

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.min(maximum, Math.max(minimum, value));
}
