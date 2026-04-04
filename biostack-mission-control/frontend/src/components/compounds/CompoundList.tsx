import { CompoundRecord } from '@/lib/types';
import { formatDate } from '@/lib/utils';
import { CompoundStatusBadge } from './CompoundStatusBadge';

interface CompoundListProps {
  compounds: CompoundRecord[];
  onSelect?: (compound: CompoundRecord) => void;
}

export function CompoundList({ compounds, onSelect }: CompoundListProps) {
  return (
    <div className="space-y-2">
      {compounds.map((compound) => (
        <div
          key={compound.id}
          onClick={() => onSelect?.(compound)}
          className={`p-4 rounded-xl border border-white/[0.06] bg-white/[0.025] hover:bg-white/[0.04] hover:border-white/[0.12] transition-all duration-150 ${
            onSelect ? 'cursor-pointer' : ''
          }`}
        >
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <h4 className="font-semibold text-white">{compound.name}</h4>
              <div className="flex items-center gap-2 mt-1">
                <span className="text-xs text-white/40">{compound.category}</span>
                {compound.goal && (
                  <>
                    <span className="text-xs text-white/35">•</span>
                    <span className="text-xs text-emerald-400/70">{compound.goal}</span>
                  </>
                )}
                <span className="text-xs text-white/35">•</span>
                <span className="text-xs text-white/40">{formatDate(compound.startDate)}</span>
              </div>
              {compound.notes && (
                <p className="text-xs text-white/65 mt-2">{compound.notes}</p>
              )}
            </div>
            <CompoundStatusBadge status={compound.status} />
          </div>
        </div>
      ))}
    </div>
  );
}
