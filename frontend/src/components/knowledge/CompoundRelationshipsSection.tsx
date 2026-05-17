'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useAuth } from '@/lib/AuthProvider';
import { fetchCompoundGraph } from '@/lib/research/loader';
import { toSlug } from '@/lib/research/slugs';
import type { CompoundGraphNode } from '@/lib/research/types';
import {
  getCommunitySignalLabel,
  getEvidenceTierLabel,
  getRelationshipLabel,
  isRelationshipEdge,
  normalizeCompoundId,
} from '@/lib/research/compoundGraphRelationships';

interface Props {
  compoundName: string;
  aliases?: string[];
}

const TIER_RANK: Record<string, number> = {
  strong: 5,
  moderate: 4,
  limited: 3,
  mechanistic: 2,
  anecdotal: 1,
};

interface MatchedEdge {
  counterpartLabel: string;
  counterpartSlug: string | null;
  relationshipLabel: string;
  evidenceTierLabel: string;
  tierRank: number;
  communitySignalLabel: string | null;
  needsReview: boolean;
}

function tierRank(tier: string | null | undefined): number {
  if (!tier) return 0;
  return TIER_RANK[tier.toLowerCase()] ?? 0;
}

export function CompoundRelationshipsSection({ compoundName, aliases }: Props) {
  const { user } = useAuth();
  const [matched, setMatched] = useState<MatchedEdge[] | null>(null);
  const aliasKey = aliases?.join('\x00') ?? '';

  useEffect(() => {
    if (!user) return;

    // The artifact API is dev-only (/api/research/artifacts returns 404 in production).
    // useAuth() exposes no token. In dev/fixture mode, auth is not enforced.
    // In production, the 404 is caught below and the section renders null silently.
    fetchCompoundGraph('')
      .then(graph => {
        const aliasList = aliasKey ? aliasKey.split('\x00') : [];
        const myIds = new Set([
          normalizeCompoundId(compoundName),
          ...aliasList.map(a => normalizeCompoundId(a)),
        ]);

        const nodeMap = new Map<string, CompoundGraphNode>(
          graph.nodes.map(n => [n.nodeId, n])
        );

        const edges: MatchedEdge[] = [];

        for (const edge of graph.edges) {
          try {
            if (!isRelationshipEdge(edge.edgeType)) continue;

            const fromNorm = normalizeCompoundId(edge.from);
            const toNorm = normalizeCompoundId(edge.to);
            const fromMatch = myIds.has(fromNorm);
            const toMatch = myIds.has(toNorm);

            if (!fromMatch && !toMatch) continue;

            const counterpartId = fromMatch ? edge.to : edge.from;
            const counterpartNode = nodeMap.get(counterpartId);
            if (!counterpartNode) continue;

            let counterpartSlug: string | null = null;
            if (counterpartNode.nodeType === 'compound') {
              const slug = toSlug(counterpartNode.label);
              if (slug.length > 0) counterpartSlug = slug;
            }

            const signalStrength =
              edge.communitySignal?.present ? edge.communitySignal.signalStrength : null;

            edges.push({
              counterpartLabel: counterpartNode.label,
              counterpartSlug,
              relationshipLabel: getRelationshipLabel(edge.edgeType),
              evidenceTierLabel: getEvidenceTierLabel(edge.evidenceTier),
              tierRank: tierRank(edge.evidenceTier),
              communitySignalLabel: getCommunitySignalLabel(signalStrength),
              needsReview: edge.needsReview ?? false,
            });
          } catch {
            // Skip malformed edges
          }
        }

        edges.sort((a, b) => {
          if (b.tierRank !== a.tierRank) return b.tierRank - a.tierRank;
          if (a.needsReview !== b.needsReview) return a.needsReview ? -1 : 1;
          return a.counterpartLabel.localeCompare(b.counterpartLabel);
        });

        setMatched(edges);
      })
      .catch(() => setMatched([]));
  }, [user, compoundName, aliasKey]);

  if (!matched || matched.length === 0) return null;

  return (
    <section className="max-w-3xl mx-auto mt-6 px-4">
      <div className="rounded-2xl border border-white/10 bg-white/[0.03] p-6">
        <div className="flex items-center gap-3 mb-1">
          <h2 className="text-sm font-semibold uppercase tracking-[0.15em] text-white/40">
            Relationships
          </h2>
          <span className="text-xs px-2 py-0.5 rounded-full bg-white/[0.06] text-white/40 border border-white/10">
            {matched.length}
          </span>
        </div>
        <p className="text-xs text-white/30 mb-5">
          Educational reference only. These are research observations, not recommendations.
        </p>

        <ul className="space-y-3">
          {matched.map((edge) => (
            <li
              key={`${edge.counterpartLabel}:${edge.relationshipLabel}:${edge.tierRank}:${String(edge.needsReview)}`}
              className="flex flex-col gap-1 px-4 py-3 rounded-xl border border-white/[0.07] bg-white/[0.02]"
            >
              <div className="flex flex-wrap items-center gap-2">
                {edge.counterpartSlug ? (
                  <Link
                    href={`/knowledge/${edge.counterpartSlug}`}
                    className="text-sm font-semibold text-white/80 hover:text-white transition-colors"
                  >
                    {edge.counterpartLabel}
                  </Link>
                ) : (
                  <span className="text-sm font-semibold text-white/80">
                    {edge.counterpartLabel}
                  </span>
                )}
                <span className="text-xs px-2 py-0.5 rounded-full bg-white/[0.06] text-white/50 border border-white/[0.08]">
                  {edge.relationshipLabel}
                </span>
                <span className="text-xs text-white/35">
                  {edge.evidenceTierLabel}
                </span>
              </div>
              {edge.communitySignalLabel && (
                <p className="text-xs text-sky-300/70 mt-0.5">
                  {edge.communitySignalLabel}
                </p>
              )}
              {edge.needsReview && (
                <p className="text-xs text-amber-300/70 mt-0.5">
                  Awaiting research review · Advisory signal only
                </p>
              )}
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
