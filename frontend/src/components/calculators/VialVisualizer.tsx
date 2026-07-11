import type { ConcentrationSource, MassUnit } from '@/lib/dosingCalculator';

interface VialVisualizerProps {
  powderAmount: number;
  powderUnit: MassUnit;
  diluentVolumeMl: number;
  mode: 'dose' | 'mix';
  concentrationSource: ConcentrationSource;
}

export function VialVisualizer({ powderAmount, powderUnit, diluentVolumeMl, mode, concentrationSource }: VialVisualizerProps) {
  const validPowder = Number.isFinite(powderAmount) && powderAmount > 0;
  const validDiluent = Number.isFinite(diluentVolumeMl) && diluentVolumeMl > 0;
  const showLiquid = concentrationSource === 'reconstitution' && validDiluent;
  const label = validPowder ? `${powderAmount} ${powderUnit}` : 'Amount needed';

  return (
    <section aria-labelledby="vial-visual-title" className="rounded-lg border border-sky-300/20 bg-sky-500/10 p-4 sm:p-5" data-testid="vial-visualizer">
      <p className="text-xs font-semibold uppercase tracking-[0.2em] text-sky-200/70">Visual guide</p>
      <h3 id="vial-visual-title" className="mt-1 text-xl font-semibold text-white">Vial contents</h3>
      <p className="mt-2 text-sm leading-6 text-sky-100/70">
        {mode === 'mix' ? 'This representation separates the entered dry powder from the liquid added.' : 'This representation restates the vial values used in the calculation.'}
      </p>
      <div role="img" aria-label={`Vial showing ${validPowder ? `${powderAmount} ${powderUnit} of dry powder` : 'no valid powder amount'}${showLiquid ? ` and ${diluentVolumeMl} milliliters of added liquid` : ', with no added liquid shown'}`} className="mx-auto mt-3 max-w-[230px]">
        <svg viewBox="0 0 240 290" className="h-auto w-full" aria-hidden="true">
          <defs>
            <linearGradient id="dynamic-vial-glass" x1="0" x2="1"><stop stopColor="#D7F7FF" stopOpacity=".58" /><stop offset="1" stopColor="#FFF" stopOpacity=".2" /></linearGradient>
          </defs>
          <rect x="84" y="12" width="72" height="36" rx="10" fill="#A8B0B7" />
          <rect x="76" y="42" width="88" height="20" rx="6" fill="#D4D9DD" />
          <path d="M70 60h100c8 18 12 38 12 60v108c0 30-24 54-54 54h-16c-30 0-54-24-54-54V120c0-22 4-42 12-60Z" fill="url(#dynamic-vial-glass)" stroke="#D6E3E8" strokeWidth="4" />
          {showLiquid && <path data-testid="vial-liquid" d="M61 170h118v58c0 28-23 51-51 51h-16c-28 0-51-23-51-51Z" fill="#38BDF8" fillOpacity=".42" />}
          <ellipse data-testid="vial-powder" cx="120" cy="253" rx="45" ry={validPowder ? 13 : 4} fill="#F3F4F6" fillOpacity={validPowder ? '.9' : '.25'} />
          <rect x="70" y="105" width="100" height="62" rx="5" fill="#0B1117" />
          <text x="120" y="126" textAnchor="middle" fontSize="11" fontWeight="700" fill="#7DD3FC">ENTERED LABEL</text>
          <text x="120" y="151" textAnchor="middle" fontSize="22" fontWeight="900" fill="#FFFFFF">{label}</text>
        </svg>
      </div>
      <dl className="grid grid-cols-2 gap-2 text-sm">
        <div className="rounded-md bg-black/15 p-2"><dt className="text-white/50">Dry powder</dt><dd className="font-semibold text-white">{label}</dd></div>
        <div className="rounded-md bg-black/15 p-2"><dt className="text-white/50">Liquid added</dt><dd className="font-semibold text-white">{showLiquid ? `${diluentVolumeMl} mL` : 'Not used'}</dd></div>
      </dl>
      <p className="mt-3 text-xs leading-5 text-white/50">Educational representation only; it does not depict preparation or administration technique.</p>
    </section>
  );
}
