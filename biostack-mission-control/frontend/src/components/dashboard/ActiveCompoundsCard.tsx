import { CompoundRecord } from '@/lib/types';
import Link from 'next/link';

interface ActiveCompoundsCardProps {
  compounds: CompoundRecord[];
}

export function ActiveCompoundsCard({ compounds }: ActiveCompoundsCardProps) {
  const activeCompounds = compounds.filter(c => c.status === 'Active');

  return (
    <div className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
      <h3 className="text-lg font-semibold text-white mb-4">Active Compounds</h3>
      {activeCompounds.length === 0 ? (
        <p className="text-sm text-white/45">No active compounds</p>
      ) : (
        <div className="space-y-3">
          {activeCompounds.slice(0, 5).map((compound) => (
            <Link
              key={compound.id}
              href={`/compounds?detail=${compound.id}`}
              className="block p-3 rounded-xl border border-white/[0.06] bg-white/[0.025] hover:bg-white/[0.04] hover:border-white/[0.12] transition-all duration-150"
            >
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-medium text-white">{compound.name}</p>
                  <p className="text-xs text-white/45 mt-1">{compound.category}</p>
                </div>
                <span className="text-xs px-2.5 py-1 rounded-full bg-emerald-500/10 text-emerald-300 border border-emerald-400/20">
                  Active
                </span>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
