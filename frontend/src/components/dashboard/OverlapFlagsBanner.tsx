import { InteractionFlag } from '@/lib/types';

interface OverlapFlagsBannerProps {
  flags: InteractionFlag[];
}

export function OverlapFlagsBanner({ flags }: OverlapFlagsBannerProps) {
  if (flags.length === 0) return null;

  return (
    <div className="p-4 rounded-2xl border border-amber-400/15 bg-amber-500/10">
      <div className="flex items-start gap-3">
        <span className="text-xl">⚠️</span>
        <div className="flex-1">
          <h4 className="font-semibold text-amber-200 text-sm mb-1">
            {flags.length} Pathway Overlap {flags.length === 1 ? 'Detected' : 'Flags Detected'}
          </h4>
          <p className="text-xs text-amber-100/70 mb-2">
            Review pathway overlaps in the Knowledge panel for more details.
          </p>
          <p className="text-xs text-amber-100/70">
            Compounds: {flags.map(f => f.compoundNames.join(', ')).join('; ')}
          </p>
        </div>
      </div>
    </div>
  );
}
