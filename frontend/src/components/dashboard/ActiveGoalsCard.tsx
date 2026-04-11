import { GoalDefinition } from '@/lib/types';
import { GoalDisplay } from '@/components/goals/GoalDisplay';
import Link from 'next/link';

interface ActiveGoalsCardProps {
  goals: GoalDefinition[];
  profileId?: string;
}

export function ActiveGoalsCard({ goals, profileId }: ActiveGoalsCardProps) {
  return (
    <div className="p-6 rounded-2xl border border-white/[0.08] bg-[#121923]/90 shadow-[0_8px_24px_rgba(0,0,0,0.35)]">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-white">Your Goals</h2>
        {profileId && (
          <Link
            href={`/profiles/${profileId}`}
            className="text-emerald-400 hover:text-emerald-300 text-sm"
          >
            View profile
          </Link>
        )}
      </div>
      <GoalDisplay goals={goals} />
    </div>
  );
}
