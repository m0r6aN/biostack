import { useId, useState } from 'react';
import { GlassCard } from '@/components/ui/GlassCard';
import { HelpTip } from '@/components/ui/HelpTip';
import { useProfile } from '@/lib/context';
import type { RecommendationSurface } from '@/lib/recommendations';
import { useSettings } from '@/lib/settings';
import { KnowledgeEntry } from '@/lib/types';
import { formatWeight } from '@/lib/utils';
import { getReviewedStudyDesign } from '@/lib/reviewedStudyDesign';
import { SafetyDisclaimer } from '../SafetyDisclaimer';
import { EvidenceTierBadge } from './EvidenceTierBadge';

// Some pathway / benefit / interaction entries in the source data are short
// tags ("cellular-energy"); others are full sentences copied from the
// literature. A rounded-full chip only reads well for the former, so long
// or sentence-shaped entries render as a bordered callout paragraph instead.
const LONG_FORM_LENGTH_THRESHOLD = 40;
const SENTENCE_PUNCTUATION = /[.!?;]/;

function isLongFormEntry(value: string): boolean {
  return value.length > LONG_FORM_LENGTH_THRESHOLD || SENTENCE_PUNCTUATION.test(value);
}

const TAG_LIST_STYLES = {
  emerald: {
    chip: 'text-xs px-2.5 py-1 rounded-full border border-emerald-400/20 bg-emerald-500/10 text-emerald-300',
    callout: 'rounded-xl border border-emerald-400/20 bg-emerald-500/10 px-4 py-3 text-sm leading-relaxed text-emerald-100/90',
  },
  rose: {
    chip: 'text-xs px-2.5 py-1 rounded-full border border-rose-500/20 bg-rose-500/10 text-rose-300',
    callout: 'rounded-xl border border-rose-500/20 bg-rose-500/10 px-4 py-3 text-sm leading-relaxed text-rose-100/90',
  },
} as const;

function TagList({ items, tone }: { items: string[]; tone: keyof typeof TAG_LIST_STYLES }) {
  const chips = items.filter(item => !isLongFormEntry(item));
  const callouts = items.filter(isLongFormEntry);
  const styles = TAG_LIST_STYLES[tone];

  return (
    <>
      {chips.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {chips.map((item, i) => (
            <span key={i} className={styles.chip}>{item}</span>
          ))}
        </div>
      )}
      {callouts.length > 0 && (
        <div className={chips.length > 0 ? 'mt-2 space-y-2' : 'space-y-2'}>
          {callouts.map((item, i) => (
            <p key={i} className={styles.callout}>{item}</p>
          ))}
        </div>
      )}
    </>
  );
}

function referenceLink(value: string): { href: string; label: string } | null {
  try {
    const url = new URL(value);
    if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password) return null;
    const isPubMedSearch = url.hostname === 'pubmed.ncbi.nlm.nih.gov' && url.searchParams.has('term');
    return { href: url.href, label: isPubMedSearch ? `PubMed search: ${url.searchParams.get('term') || 'query'}` : value };
  } catch {
    return null;
  }
}

interface CompoundIntelligenceCardProps {
  entry: KnowledgeEntry;
  recommendationSurface?: Exclude<RecommendationSurface, 'overlap-results'>;
}

export function CompoundIntelligenceCard({
  entry,
}: CompoundIntelligenceCardProps) {
  const studyDesign = getReviewedStudyDesign(entry.canonicalName);
  const [showAllReferences, setShowAllReferences] = useState(false);
  const referenceListId = useId();
  const { currentProfileId, profiles } = useProfile();
  const { settings } = useSettings();
  const currentProfile = profiles.find(p => p.id === currentProfileId);
  return (
    <GlassCard variant="default" className="p-4 sm:p-6 relative overflow-hidden break-words">
      <div className="absolute -top-8 -right-8 w-32 h-32 rounded-full bg-emerald-500/[0.06] blur-2xl pointer-events-none" />
      <div className="flex flex-wrap items-start justify-between gap-3 mb-4">
        <div>
          <h3 className="text-lg font-semibold text-white">{entry.canonicalName}</h3>
          {entry.aliases.length > 0 && (
            <p className="text-xs text-white/35 mt-1">Also known as: {entry.aliases.join(', ')}</p>
          )}
        </div>
        <EvidenceTierBadge tier={entry.evidenceTier} />
      </div>

      <div className="space-y-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
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
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-1">Overview</p>
            <p className="text-sm text-white/65">{entry.mechanismSummary}</p>
          </div>
        )}

        <section aria-label="Evidence and limitations" className="rounded-xl border border-white/10 bg-white/[0.03] p-4 space-y-2">
          <h4 className="text-sm font-medium text-white/80">Evidence and limitations</h4>
          {studyDesign ? (
            <div data-testid="reviewed-study-design" className="space-y-2">
              <p className="text-sm leading-6 text-white/65">{studyDesign.statement}</p>
              <a href={studyDesign.citation.url} target="_blank" rel="noopener noreferrer"
                className="inline-flex min-h-11 items-center text-sm text-cyan-300 underline underline-offset-4 [overflow-wrap:anywhere]">
                {studyDesign.citation.displayLabel} · PMID {studyDesign.citation.pmid} (opens in new tab)
              </a>
              <p className="text-sm leading-6 text-white/65">{studyDesign.limitations}</p>
            </div>
          ) : (
            <p className="text-sm leading-6 text-white/65">
              Human/preclinical evidence breakdown is not available in this record.
            </p>
          )}
          {entry.notes && <p className="text-sm leading-6 text-white/65">{entry.notes}</p>}
        </section>

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

        {entry.pathways && entry.pathways.length > 0 && (
          <div>
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-2">Pathways</p>
            <TagList items={entry.pathways} tone="emerald" />
          </div>
        )}

        {entry.benefits.length > 0 && (
          <div>
            <p className="text-xs uppercase tracking-[0.15em] text-white/40 mb-2">Benefits</p>
            <TagList items={entry.benefits} tone="emerald" />
          </div>
        )}

        {(entry.avoidWith.length > 0 || entry.drugInteractions.length > 0) && (
          <div className="p-4 rounded-xl bg-white/[0.03] border border-white/5 space-y-4">
            <h4 className="text-sm font-medium text-white/80">Interactions & cautions</h4>

            {entry.avoidWith.length > 0 && (
              <div>
                <p className="text-[10px] uppercase tracking-wider text-rose-400/60 mb-2">
                  <HelpTip tipKey="flaggedInSourceData">Flagged in source data</HelpTip>
                </p>
                <TagList items={entry.avoidWith} tone="rose" />
              </div>
            )}

            {entry.drugInteractions.length > 0 && (
              <div>
                <p className="text-[10px] uppercase tracking-wider text-rose-400/60 mb-2">
                  <HelpTip tipKey="reportedDrugInteractions">Drug interactions</HelpTip>
                </p>
                <TagList items={entry.drugInteractions} tone="rose" />
              </div>
            )}

            <p className="text-xs leading-5 text-white/45">
              These are observational flags for review, not individualized instructions.
            </p>
          </div>
        )}

        {entry.sourceReferences.length > 0 && (
          <section aria-label="Sources">
            <h4 className="text-sm font-medium text-white/80 mb-2">Sources</h4>
            <ul id={referenceListId} className="text-sm space-y-2">
              {entry.sourceReferences.slice(0, showAllReferences ? undefined : 3).map((ref, i) => {
                const link = referenceLink(ref);
                return (
                  <li key={i} className="min-w-0 [overflow-wrap:anywhere] text-white/65">
                    {link ? (
                      <a href={link.href} target="_blank" rel="noopener noreferrer"
                        className="inline-flex min-h-11 items-center rounded py-2 text-cyan-300 underline underline-offset-4 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-cyan-300">
                        {link.label}<span className="sr-only"> (opens in a new tab)</span>
                      </a>
                    ) : ref}
                  </li>
                );
              })}
            </ul>
            {entry.sourceReferences.length > 3 && (
              <button type="button" aria-expanded={showAllReferences} aria-controls={referenceListId}
                onClick={() => setShowAllReferences(value => !value)}
                className="mt-2 min-h-11 rounded px-2 py-2 text-sm text-cyan-300 underline underline-offset-4 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-cyan-300">
                {showAllReferences ? 'Show fewer sources' : `Show all ${entry.sourceReferences.length} sources`}
              </button>
            )}
          </section>
        )}



      </div>

      <SafetyDisclaimer type="educational" />
    </GlassCard>
  );
}
