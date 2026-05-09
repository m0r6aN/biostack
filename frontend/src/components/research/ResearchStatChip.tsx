interface ResearchStatChipProps {
  label: string;
  value: number | string;
  color: 'red' | 'amber' | 'green' | 'blue' | 'neutral';
}

const colorMap = {
  red:     'text-rose-400 bg-rose-400/5 border-rose-400/20',
  amber:   'text-amber-400 bg-amber-400/5 border-amber-400/20',
  green:   'text-emerald-400 bg-emerald-400/5 border-emerald-400/20',
  blue:    'text-blue-400 bg-blue-400/5 border-blue-400/20',
  neutral: 'text-white/50 bg-white/[0.03] border-white/10',
};

export function ResearchStatChip({ label, value, color }: ResearchStatChipProps) {
  return (
    <div className={`rounded-2xl border p-4 ${colorMap[color]}`}>
      <p className="text-[10px] uppercase font-bold tracking-widest opacity-60">{label}</p>
      <p className="text-2xl font-bold mt-1 text-white">{value}</p>
    </div>
  );
}
