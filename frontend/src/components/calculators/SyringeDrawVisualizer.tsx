import { formatNumber, type UnifiedDosingResult } from '@/lib/dosingCalculator';

interface SyringeDrawVisualizerProps {
  result: UnifiedDosingResult;
}

export function SyringeDrawVisualizer({ result }: SyringeDrawVisualizerProps) {
  const syringeUnits = result.u100UnitsPerAdministration;
  const clampedUnits = clamp(syringeUnits, 0, 100);
  const barrelX = 70;
  const barrelY = 56;
  const barrelWidth = 270;
  const barrelHeight = 54;
  const fillWidth = (clampedUnits / 100) * barrelWidth;
  const overCapacity = syringeUnits > 100;
  const majorTicks = [0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
  const tickMarks = Array.from({ length: 21 }, (_, index) => index * 5);

  return (
    <div className="rounded-lg border border-emerald-400/20 bg-emerald-500/10 p-5">
      <div className="grid gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-200/70">
            Calculated draw
          </p>
          <p className="mt-2 text-4xl font-semibold tracking-tight text-white">
            {formatNumber(result.volumePerAdministrationMl, 4)} mL
          </p>
          <p className="mt-2 text-sm text-emerald-100/75">
            {formatNumber(syringeUnits, 1)} units on a U-100 syringe
          </p>
          {overCapacity && (
            <p className="mt-3 rounded-lg border border-amber-300/25 bg-amber-400/10 px-3 py-2 text-sm font-semibold text-amber-100">
              Draw exceeds one U-100 syringe.
            </p>
          )}
        </div>

        <div
          role="meter"
          aria-label="U-100 syringe draw visualizer"
          data-orientation="horizontal"
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuenow={Math.round(clampedUnits * 10) / 10}
          aria-valuetext={`${formatNumber(clampedUnits, 1)} units shown on a U-100 syringe`}
          className="w-full"
        >
          <svg viewBox="0 0 420 165" className="h-[165px] w-full" aria-hidden="true">
            <line x1="38" y1="83" x2="70" y2="83" stroke="#A8B0B7" strokeWidth="8" strokeLinecap="round" />
            <line x1="38" y1="52" x2="38" y2="114" stroke="#A8B0B7" strokeWidth="10" strokeLinecap="round" />
            <rect x="60" y="44" width="12" height="78" rx="2" fill="#A8B0B7" />

            <rect x={barrelX} y={barrelY} width={barrelWidth} height={barrelHeight} rx="5" fill="#E7ECEF" />
            <rect x={barrelX} y={barrelY + 7} width={fillWidth} height={barrelHeight - 14} rx="3" fill="#22C55E" />
            <rect x={barrelX} y={barrelY} width={barrelWidth} height={barrelHeight} rx="5" fill="none" stroke="#A8B0B7" strokeWidth="4" />

            {tickMarks.map((units) => {
              const x = barrelX + (units / 100) * barrelWidth;
              const isMajor = units % 10 === 0;
              return (
                <line
                  key={units}
                  x1={x}
                  y1={isMajor ? barrelY + 9 : barrelY + 17}
                  x2={x}
                  y2={barrelY + barrelHeight - 8}
                  stroke="#0F141B"
                  strokeWidth={isMajor ? 1.4 : 1}
                />
              );
            })}

            {majorTicks.map((units) => (
              <text
                key={units}
                x={barrelX + (units / 100) * barrelWidth}
                y="136"
                fontSize="10"
                fontWeight="600"
                fill="#CFE5DB"
                textAnchor="middle"
              >
                {units}
              </text>
            ))}

            <rect x="338" y="67" width="32" height="32" rx="4" fill="#A8B0B7" />
            <line x1="370" y1="83" x2="402" y2="83" stroke="#A8B0B7" strokeWidth="3" strokeLinecap="round" />
          </svg>
          <p className="text-center text-sm font-semibold text-emerald-100">
            {formatNumber(clampedUnits, 1)} units filled on a U-100 syringe
          </p>
        </div>
      </div>
    </div>
  );
}

function clamp(value: number, minimum: number, maximum: number): number {
  return Math.min(maximum, Math.max(minimum, value));
}
