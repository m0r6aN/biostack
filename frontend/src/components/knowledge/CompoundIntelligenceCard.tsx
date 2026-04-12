import { ContextualRecommendations } from '@/components/recommendations/ContextualRecommendations';
import { GlassCard } from '@/components/ui/GlassCard';
import { useProfile } from '@/lib/context';
import {
    getContextTagsForKnowledgeEntry,
    getRecommendationsForKnowledgeEntry,
    type RecommendationSurface,
} from '@/lib/recommendations';
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
  recommendationSurface = 'compound-detail',
}: CompoundIntelligenceCardProps) {
  const { currentProfileId, profiles } = useProfile();
  const { settings } = useSettings();
  const currentProfile = profiles.find(p => p.id === currentProfileId);
  const recommendationTags = getContextTagsForKnowledgeEntry(entry);
  const recommendations = getRecommendationsForKnowledgeEntry(entry, 3, recommendationSurface);
  const optimizationSupplements = Array.isArray(entry.optimizationSupplements)
    ? entry.optimizationSupplements
    : entry.optimizationSupplements
      ? [entry.optimizationSupplements]
      : [];

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

        {/* Personalized Guidance Section */}
        {currentProfile && (
          <div className="p-4 rounded-xl bg-emerald-500/[0.04] border border-emerald-500/10 space-y-3">
            <div className="flex items-center gap-2">
              <span className="text-emerald-400 text-xs">🧬</span>
              <p className="text-xs font-semibold text-emerald-400/80 uppercase tracking-wider">Personalized Guidance</p>
            </div>
            
            <div className="grid grid-cols-2 gap-4">
              <div className="text-xs">
                <p className="text-white/30 uppercase tracking-tighter mb-1">Based on Profile</p>
                <p className="text-white/60">
                  {currentProfile.displayName} ({currentProfile.sex}, {currentProfile.age || '??'}y, {formatWeight(currentProfile.weight, settings.weightUnit)})
                </p>
              </div>
              
              {entry.recommendedDosage && (
                <div className="text-xs">
                  <p className="text-white/30 uppercase tracking-tighter mb-1">Personalized Adjustments</p>
                  <p className="text-emerald-300/80 italic">
                    {currentProfile.weight > 90 ? 'Weight may be useful context when reviewing the listed reference range.' : 'Knowledge-base range shown for reference.'}
                  </p>
                </div>
              )}
            </div>

            {entry.canonicalName === 'MOTS-C' && currentProfile.age && currentProfile.age > 40 && (
              <p className="text-[11px] text-emerald-300/50 leading-relaxed border-t border-emerald-500/5 pt-2">
                * Note: As a mitochondrial peptide, MOTS-C response can vary significantly with biological age. 
                Focus on synergistic NAD+ support as indicated in your profile goals.
              </p>
            )}
          </div>
        )}

        {/* Synergies & Blends */}
        {(entry.pairsWellWith.length > 0 || entry.avoidWith.length > 0 || entry.compatibleBlends.length > 0) && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 p-4 rounded-xl bg-white/[0.03] border border-white/5">
            {entry.pairsWellWith.length > 0 && (
              <div>
                <p className="text-[10px] uppercase tracking-wider text-emerald-400/60 mb-2">Pairs Well With</p>
                <div className="flex flex-wrap gap-1.5">
                  {entry.pairsWellWith.map((item, i) => (
                    <span key={i} className="text-[11px] px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-300 border border-emerald-500/20">
                      {item}
                    </span>
                  ))}
                </div>
              </div>
            )}
            {entry.avoidWith.length > 0 && (
              <div>
                <p className="text-[10px] uppercase tracking-wider text-rose-400/60 mb-2">Avoid With</p>
                <div className="flex flex-wrap gap-1.5">
                  {entry.avoidWith.map((item, i) => (
                    <span key={i} className="text-[11px] px-2 py-0.5 rounded bg-rose-500/10 text-rose-300 border border-rose-500/20">
                      {item}
                    </span>
                  ))}
                </div>
              </div>
            )}
            {entry.compatibleBlends.length > 0 && (
              <div className="md:col-span-2">
                <p className="text-[10px] uppercase tracking-wider text-blue-400/60 mb-2">Compatible Blends (Vial)</p>
                <div className="flex flex-wrap gap-1.5">
                  {entry.compatibleBlends.map((item, i) => (
                    <span key={i} className="text-[11px] px-2 py-0.5 rounded bg-blue-500/10 text-blue-300 border border-blue-500/20">
                      {item}
                    </span>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        {/* Knowledge-base reference */}
        {(entry.recommendedDosage || entry.frequency || entry.preferredTimeOfDay || entry.weeklyDosageSchedule.length > 0) && (
          <div className="space-y-3">
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 border-b border-white/5 pb-1">Knowledge-base reference</p>
            <div className="grid grid-cols-3 gap-2 text-center">
              {entry.recommendedDosage && (
                <div className="p-2 rounded bg-white/5">
                  <p className="text-[10px] text-white/40 uppercase">Dosage</p>
                  <p className="text-sm text-white font-medium">{entry.recommendedDosage}</p>
                </div>
              )}
              {entry.frequency && (
                <div className="p-2 rounded bg-white/5">
                  <p className="text-[10px] text-white/40 uppercase">Frequency</p>
                  <p className="text-sm text-white font-medium">{entry.frequency}</p>
                </div>
              )}
              {entry.preferredTimeOfDay && (
                <div className="p-2 rounded bg-white/5">
                  <p className="text-[10px] text-white/40 uppercase">Time</p>
                  <p className="text-sm text-white font-medium">{entry.preferredTimeOfDay}</p>
                </div>
              )}
            </div>
            {entry.weeklyDosageSchedule.length > 0 && (
              <div className="text-sm text-white/60 bg-white/5 p-3 rounded-lg">
                <p className="text-[10px] uppercase text-white/40 mb-2">Weekly Schedule</p>
                <ul className="space-y-1">
                  {entry.weeklyDosageSchedule.map((step, i) => (
                    <li key={i} className="flex items-center gap-2">
                      <span className="w-1 h-1 rounded-full bg-blue-400" />
                      {step}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )}

        {/* Optimization Recommendations */}
        {(entry.optimizationProtein || entry.optimizationCarbs || optimizationSupplements.length > 0 || entry.optimizationSleep || entry.optimizationExercise) && (
          <div className="space-y-3">
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 border-b border-white/5 pb-1">Optimization Guidelines</p>
            <div className="grid grid-cols-1 gap-2">
              {entry.optimizationProtein && (
                <div className="flex justify-between text-sm py-1 border-b border-white/[0.03]">
                  <span className="text-white/40">Protein</span>
                  <span className="text-white/80">{entry.optimizationProtein}</span>
                </div>
              )}
              {entry.optimizationCarbs && (
                <div className="flex justify-between text-sm py-1 border-b border-white/[0.03]">
                  <span className="text-white/40">Carbs</span>
                  <span className="text-white/80">{entry.optimizationCarbs}</span>
                </div>
              )}
              {optimizationSupplements.length > 0 && (
                <div className="flex justify-between text-sm py-1 border-b border-white/[0.03]">
                  <span className="text-white/40">Supplements</span>
                  <span className="text-white/80">{optimizationSupplements.join(', ')}</span>
                </div>
              )}
              {entry.optimizationExercise && (
                <div className="flex justify-between text-sm py-1 border-b border-white/[0.03]">
                  <span className="text-white/40">Exercise</span>
                  <span className="text-white/80">{entry.optimizationExercise}</span>
                </div>
              )}
              {entry.optimizationSleep && (
                <div className="flex justify-between text-sm py-1">
                  <span className="text-white/40">Sleep</span>
                  <span className="text-white/80">{entry.optimizationSleep}</span>
                </div>
              )}
            </div>
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

        <ContextualRecommendations
          recommendations={recommendations}
          surface={recommendationSurface}
          contextTags={recommendationTags}
        />
      </div>

      <SafetyDisclaimer type="educational" />
    </GlassCard>
  );
}
