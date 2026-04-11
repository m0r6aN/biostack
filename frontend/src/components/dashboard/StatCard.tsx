interface StatCardProps {
  title: string;
  value: string | number;
  unit?: string;
  icon: string;
  trend?: {
    value: number;
    direction: 'up' | 'down' | 'neutral';
  };
  color?: 'emerald' | 'blue' | 'amber' | 'red' | 'default';
}

export function StatCard({
  title,
  value,
  unit,
  icon,
  trend,
  color = 'emerald',
}: StatCardProps) {
  const colorClasses = {
    emerald: 'border-emerald-400/20 bg-white/[0.04] backdrop-blur-xl',
    blue: 'border-blue-400/20 bg-white/[0.04] backdrop-blur-xl',
    amber: 'border-amber-400/20 bg-white/[0.04] backdrop-blur-xl',
    red: 'border-red-400/20 bg-white/[0.04] backdrop-blur-xl',
    default: 'border-white/[0.08] bg-white/[0.03] backdrop-blur-xl',
  };

  const glowClasses = {
    emerald: 'bg-emerald-400/[0.08]',
    blue: 'bg-blue-400/[0.08]',
    amber: 'bg-amber-400/[0.08]',
    red: 'bg-red-400/[0.08]',
    default: 'bg-white/[0.04]',
  };

  const trendIcons = {
    up: '\u2191',
    down: '\u2193',
    neutral: '\u2192',
  };

  return (
    <div className={`p-5 rounded-2xl border relative overflow-hidden shadow-[0_8px_32px_rgba(0,0,0,0.4)] ${colorClasses[color]}`}>
      {/* Per-color ambient corner glow */}
      <div className={`absolute -top-6 -right-6 w-24 h-24 rounded-full blur-2xl pointer-events-none ${glowClasses[color]}`} />
      <div className="flex items-start justify-between">
        <div className="flex-1 relative">
          <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-2">{title}</p>
          <div className="flex items-baseline gap-2">
            <span className="text-3xl font-semibold text-white">{value}</span>
            {unit && <span className="text-sm text-white/40 ml-1">{unit}</span>}
          </div>
          {trend && (
            <p className={`text-xs mt-2 ${
              trend.direction === 'up' ? 'text-emerald-300' :
              trend.direction === 'down' ? 'text-red-400' :
              'text-white/40'
            }`}>
              {trendIcons[trend.direction]} {Math.abs(trend.value)}% from last period
            </p>
          )}
        </div>
        <div className="text-2xl opacity-40">{icon}</div>
      </div>
    </div>
  );
}
