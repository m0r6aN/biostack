'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { apiClient } from '@/lib/api';
import type { KnowledgeEntry } from '@/lib/types';
import type { OnboardingRelationshipCandidate } from '@/lib/onboardingIntelligence';
import { StackIntelligencePanel } from './StackIntelligencePanel';

// Fallback values (original hardcoded example)
const FALLBACK_COMPOUNDS = ['BPC-157', 'TB-500'];
const FALLBACK_RELATIONSHIPS: OnboardingRelationshipCandidate[] = [
  {
    type: 'overlap',
    label: 'BPC-157 + TB-500',
    detail: 'tissue-repair overlap: educational reference only, with full evidence detail in Operator.',
  },
];

function detectOverlap(
  entry1: KnowledgeEntry,
  entry2: KnowledgeEntry
): OnboardingRelationshipCandidate | null {
  // Check for overlapping values in pairsWellWith, benefits, or pathways (case-insensitive)
  // Combine all relevant arrays from both entries
  const entry1Values = [
    ...(entry1.pairsWellWith || []),
    ...(entry1.benefits || []),
    ...(entry1.pathways || []),
  ].map(v => v.toLowerCase());

  const entry2Values = [
    ...(entry2.pairsWellWith || []),
    ...(entry2.benefits || []),
    ...(entry2.pathways || []),
  ].map(v => v.toLowerCase());

  // Check if any values overlap between the two entries
  const hasOverlap = entry1Values.some(v => entry2Values.includes(v));

  if (hasOverlap) {
    return {
      type: 'overlap',
      label: `${entry1.canonicalName} + ${entry2.canonicalName}`,
      detail: 'shared mechanism: educational reference only, with full evidence detail in Operator.',
    };
  }

  return null;
}

export function IntelligenceProofSection({ compact = false }: { compact?: boolean }) {
  const [compounds, setCompounds] = useState<string[]>(FALLBACK_COMPOUNDS);
  const [relationships, setRelationships] = useState<OnboardingRelationshipCandidate[]>(
    FALLBACK_RELATIONSHIPS
  );

  useEffect(() => {
    async function fetchKnowledgeData() {
      try {
        const entries = await apiClient.getAllKnowledgeCompounds();

        // If we have at least 2 entries, use them
        if (entries.length >= 2) {
          // Sort alphabetically by canonicalName
          const sorted = [...entries].sort((a, b) =>
            a.canonicalName.localeCompare(b.canonicalName)
          );

          // Take the first two
          const [first, second] = sorted;
          setCompounds([first.canonicalName, second.canonicalName]);

          // Detect overlap
          const overlap = detectOverlap(first, second);
          setRelationships(overlap ? [overlap] : []);
        }
        // Otherwise, keep the fallback (no state update needed)
      } catch {
        // On error, keep the fallback (no state update needed)
      }
    }

    fetchKnowledgeData();
  }, []);

  return (
    <section className="border-y border-white/8 bg-black/15">
      <div className="mx-auto grid max-w-7xl gap-7 px-5 py-10 sm:px-8 lg:grid-cols-[0.82fr_1.18fr] lg:items-center lg:py-14">
        <div>
          <h2 className="text-3xl font-semibold tracking-tight text-white sm:text-4xl">
            See what BioStack catches
          </h2>
          <p className="mt-4 text-base leading-7 text-white/62">
            Explore an example list below. BioStack shows the listed items and a preview of their context; build
            your own list through Start free, and review paid capabilities on the pricing page.
          </p>
          {!compact && (
            <div className="mt-5 flex flex-wrap gap-3">
              <Link
                href="/start"
                className="rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-transform motion-safe:hover:-translate-y-0.5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white focus-visible:ring-offset-2 focus-visible:ring-offset-[#0B0F14]"
              >
                Start free
              </Link>
              <Link
                href="/pricing"
                className="rounded-lg border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24 focus-visible:outline-none focus-visible:ring-2"
              >
                See what Operator unlocks
              </Link>
            </div>
          )}
        </div>

        <StackIntelligencePanel
          contentOverrides={{
            simple: {
              nextAction: 'Explore these compounds in the evidence library, or start free to track your own stack.',
            },
          }}
          compoundNames={compounds}
          relationshipCandidates={relationships}
        />
      </div>
    </section>
  );
}
