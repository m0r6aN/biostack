import { GlassCard } from '@/components/ui/GlassCard';
import { useProfile } from '@/lib/context';
import type { RecommendationSurface } from '@/lib/recommendations';
import { useSettings } from '@/lib/settings';
import { KnowledgeEntry } from '@/lib/types';
import { formatWeight } from '@/lib/utils';
import { SafetyDisclaimer } from '../SafetyDisclaimer';
import { EvidenceTierBadge } from './EvidenceTierBadge';

interface CompoundIntelligenceCardProps {
  entry: KnowledgeEntry;
  recommendationSurface?: Exclude<RecommendationSurface, 'overlap-results'>;
}

export function CompoundIntelligenceCard({
  entry,
}: CompoundIntelligenceCardProps) {
  const { currentProfileId, profiles } = useProfile();
  const { settings } = useSettings();
  const currentProfile = profiles.find(p => p.id === currentProfileId);
  return (
    <GlassCard variant="default" className="p-6 relative overflow-hidden">
      <div className="absolute -top-8 -right-8 w-32 h-32 rounded-full bg-emerald-500/[0.06] blur-2xl pointer-events-none" />
      <div className="flex items-start justify-between mb-4">
        <div>
          <h3 className="text-lg font-semibold text-white">{entry.canonicalName}</h3>
          {entry.aliases.length > 0 && (
            <p className="text-xs text-white/35 mt-1">Also known as: {entry.aliases.join(', ')}</p>
          )}
        </div>
        <EvidenceTierBadge tier={entry.evidenceTier} />
      </div>

      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-1">Classification</p>
            <p className="text-sm text-white/65">{entry.classification}</p>
          </div>

          <div>
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-1">Regulatory Status</p>
            <p className="text-sm text-white/65">{entry.regulatoryStatus}</p>
          </div>
        </div>

        {entry.mechanismSummary && (
          <div>
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-1">Mechanism Summary</p>
            <p className="text-sm text-white/65">{entry.mechanismSummary}</p>
          </div>
        )}

        {/* Profile context section — demographics only, no dosage adjacency */}
        {currentProfile && (
          <div className="p-4 rounded-xl bg-white/[0.03] border border-white/[0.06] space-y-2">
            <div className="flex items-center gap-2">
              <span className="text-white/40 text-xs" aria-hidden="true">•</span>
              <p className="text-xs font-semibold text-white/55 uppercase tracking-wider">Profile Context</p>
            </div>
            <p className="text-xs text-white/55">
              {currentProfile.displayName} ({currentProfile.sex}, {currentProfile.age || '??'}y, {formatWeight(currentProfile.weight, settings.weightUnit)})
            </p>
          </div>
        )}

        {entry.avoidWith.length > 0 && (
          <div className="p-4 rounded-xl bg-white/[0.03] border border-white/5">
            <p className="text-[10px] uppercase tracking-wider text-rose-400/60 mb-2">Reported cautions in source data</p>
            <div className="flex flex-wrap gap-1.5">
              {entry.avoidWith.map((item, i) => (
                <span key={i} className="text-[11px] px-2 py-0.5 rounded bg-rose-500/10 text-rose-300 border border-rose-500/20">
                  {item}
                </span>
              ))}
            </div>
            <p className="mt-2 text-xs leading-5 text-white/45">
              These are observational flags for review, not individualized instructions.
            </p>
          </div>
        )}

        {entry.pathways && entry.pathways.length > 0 && (
          <div>
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-2">Pathways</p>
            <div className="flex flex-wrap gap-2">
              {entry.pathways.map((pathway, i) => (
                <span key={i} className="text-xs px-2.5 py-1 rounded-full border border-emerald-400/20 bg-emerald-500/10 text-emerald-300">
                  {pathway}
                </span>
              ))}
            </div>
          </div>
        )}

        {entry.benefits.length > 0 && (
          <div>
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-2">Benefits</p>
            <div className="flex flex-wrap gap-2">
              {entry.benefits.map((benefit, i) => (
                <span key={i} className="text-xs px-2.5 py-1 rounded-full border border-emerald-400/20 bg-emerald-500/10 text-emerald-300">
                  {benefit}
                </span>
              ))}
            </div>
          </div>
        )}

        {entry.drugInteractions.length > 0 && (
          <div>
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-2">Drug Interactions</p>
            <div className="flex flex-wrap gap-2">
              {entry.drugInteractions.map((interaction, i) => (
                <span key={i} className="text-xs px-2.5 py-1 rounded-full border border-rose-500/20 bg-rose-500/10 text-rose-300">
                  {interaction}
                </span>
              ))}
            </div>
          </div>
        )}

        {entry.sourceReferences.length > 0 && (
          <div>
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-2">References</p>
            <ul className="text-xs space-y-1">
              {entry.sourceReferences.slice(0, 3).map((ref, i) => (
                <li key={i} className="text-white/35">{ref}</li>
              ))}
              {entry.sourceReferences.length > 3 && (
                <li className="text-white/35">+{entry.sourceReferences.length - 3} more</li>
              )}
            </ul>
          </div>
        )}

        {entry.notes && (
          <div>
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-1">Notes</p>
            <p className="text-sm text-white/65">{entry.notes}</p>
          </div>
        )}

      </div>

      <SafetyDisclaimer type="educational" />
    </GlassCard>
  );
}
