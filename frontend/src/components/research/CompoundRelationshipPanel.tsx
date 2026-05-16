'use client';
import { CommunitySignalBadge } from '@/components/research/CommunitySignalBadge';
import { ReviewRequiredBadge } from '@/components/research/ReviewRequiredBadge';
import { EvidenceTierBadge } from '@/components/knowledge/EvidenceTierBadge';
import { GlassCard } from '@/components/ui/GlassCard';
import { toSlug } from '@/lib/research/slugs';
import type {
  CompoundGraph,
  CompoundGraphEdge,
  CompoundGraphNode,
  CompoundGraphReviewFinding,
} from '@/lib/research/types';
import Link from 'next/link';

interface CompoundRelationshipPanelProps {
  graph: CompoundGraph | null;
  compound: { canonicalName: string; slug: string; aliases?: string[] };
  knownSlugs?: ReadonlySet<string>;
}

// Edge types that represent cross-compound relationships (not taxonomic).
const RELATIONSHIP_EDGE_KEYS = new Set([
  'pairswith',
  'conflictswith',
  'redundantwith',
  'synergizeswith',
  'complements',
  'hascommunitysignal',
  'contradictedby',
  'opposeseffect',
  'opposingeffect',
  'avoidwith',
]);

function normalizeKey(value: string | undefined | null): string {
  if (!value) return '';
  return value.toString().replace(/[-_\s]/g, '').toLowerCase();
}

function kebabOrPascalToTitle(value: string | undefined | null): string {
  if (!value) return '';
  const spaced = value
    .replace(/[-_]/g, ' ')
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2');
  return spaced
    .split(/\s+/)
    .filter(Boolean)
    .map(w => w.charAt(0).toUpperCase() + w.slice(1).toLowerCase())
    .join(' ');
}

function severityClasses(sev: string | undefined | null): string {
  switch (normalizeKey(sev)) {
    case 'critical': return 'bg-rose-500/15 text-rose-300 border-rose-400/30';
    case 'high':     return 'bg-rose-500/10 text-rose-300 border-rose-400/20';
    case 'moderate': return 'bg-amber-500/10 text-amber-300 border-amber-400/20';
    case 'low':      return 'bg-blue-500/10 text-blue-300 border-blue-400/20';
    default:         return 'bg-white/[0.05] text-white/50 border-white/10';
  }
}

interface MatchedEdge {
  edge: CompoundGraphEdge;
  counterpartNode: CompoundGraphNode | null;
  counterpartLabel: string;
  counterpartSlug: string | null;
}

function getEdgeFromTo(edge: CompoundGraphEdge): { from: string; to: string } {
  // tolerate either { from, to } or { fromNodeId, toNodeId } if 3B differs
  const e = edge as unknown as Record<string, unknown>;
  const from = (e.from ?? e.fromNodeId ?? '') as string;
  const to = (e.to ?? e.toNodeId ?? '') as string;
  return { from, to };
}

function getNodeId(node: CompoundGraphNode): string {
  const n = node as unknown as Record<string, unknown>;
  return ((n.nodeId ?? n.id ?? '') as string);
}

function getEdgeId(edge: CompoundGraphEdge, fallback: string): string {
  const e = edge as unknown as Record<string, unknown>;
  return ((e.edgeId ?? e.id ?? fallback) as string);
}

function getAssertedEdgeType(edge: CompoundGraphEdge): string | undefined {
  const e = edge as unknown as Record<string, unknown>;
  return (e.assertedEdgeType ?? e.assertedRelationshipType) as string | undefined;
}

export function CompoundRelationshipPanel({ graph, compound, knownSlugs }: CompoundRelationshipPanelProps) {
  if (!graph) {
    return (
      <GlassCard className="p-5">
        <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-3">Cross-Compound Relationships</h3>
        <p className="text-sm text-white/30">No cross-compound relationships recorded for this compound yet.</p>
      </GlassCard>
    );
  }

  const nodes: CompoundGraphNode[] = graph.nodes ?? [];
  const edges: CompoundGraphEdge[] = graph.edges ?? [];
  const findings: CompoundGraphReviewFinding[] = graph.reviewFindings ?? [];

  // Build lookup of compound-type nodes by id, with reverse "is this node the current compound" check.
  const nodeById = new Map<string, CompoundGraphNode>();
  for (const n of nodes) {
    const id = getNodeId(n);
    if (id) nodeById.set(id, n);
  }

  const canonicalSlug = compound.slug || toSlug(compound.canonicalName);
  const aliasSlugs = new Set((compound.aliases ?? []).map(toSlug));

  function isCurrentCompoundNode(node: CompoundGraphNode | undefined): boolean {
    if (!node) return false;
    const n = node as unknown as Record<string, unknown>;
    const nodeType = normalizeKey(n.nodeType as string);
    if (nodeType && nodeType !== 'compound') return false;

    // 1. canonical name match
    if (typeof node.label === 'string' && node.label === compound.canonicalName) return true;

    // 2. alias match
    const aliases = (node.aliases ?? []) as string[];
    if (aliases.some(a => a === compound.canonicalName)) return true;
    if (aliases.some(a => aliasSlugs.has(toSlug(a)))) return true;

    // 3. slug fallback — node id like `compound:<slug>` or label slug match
    const nodeId = getNodeId(node);
    if (nodeId && nodeId.toLowerCase().endsWith(`:${canonicalSlug}`)) return true;
    if (nodeId.toLowerCase() === `compound:${canonicalSlug}`) return true;
    if (typeof node.label === 'string' && toSlug(node.label) === canonicalSlug) return true;

    return false;
  }

  // Filter relationship edges where this compound is either endpoint.
  const matched: MatchedEdge[] = [];
  for (const edge of edges) {
    const e = edge as unknown as Record<string, unknown>;
    const edgeTypeKey = normalizeKey(e.edgeType as string);
    if (!RELATIONSHIP_EDGE_KEYS.has(edgeTypeKey)) continue;

    const { from, to } = getEdgeFromTo(edge);
    const fromNode = nodeById.get(from);
    const toNode = nodeById.get(to);

    let counterpart: CompoundGraphNode | null = null;
    if (isCurrentCompoundNode(fromNode)) {
      counterpart = toNode ?? null;
    } else if (isCurrentCompoundNode(toNode)) {
      counterpart = fromNode ?? null;
    } else {
      continue;
    }

    // Only show compound-to-compound or compound-to-source-family edges with relationship semantics.
    // Build a label even if counterpart is missing.
    let counterpartLabel = 'Unknown';
    let counterpartSlug: string | null = null;
    if (counterpart) {
      counterpartLabel = counterpart.label || getNodeId(counterpart) || 'Unknown';
      const cType = normalizeKey((counterpart as unknown as Record<string, unknown>).nodeType as string);
      if (cType === 'compound' || !cType) {
        const cnId = getNodeId(counterpart);
        // node id might look like "compound:<slug>"
        const idLower = cnId.toLowerCase();
        const colonIdx = idLower.indexOf(':');
        const slugFromId = colonIdx >= 0 ? idLower.slice(colonIdx + 1) : '';
        counterpartSlug = slugFromId || toSlug(counterpartLabel);
      }
    }

    matched.push({
      edge,
      counterpartNode: counterpart,
      counterpartLabel,
      counterpartSlug,
    });
  }

  // Unresolved findings referencing this compound.
  const unresolvedFindings = findings.filter(f => {
    const refs = f.compoundRefs ?? [];
    const refsThisCompound = refs.includes(compound.canonicalName)
      || refs.some(r => toSlug(r) === canonicalSlug);
    if (!refsThisCompound) return false;
    const needs = (f as unknown as Record<string, unknown>).needsHumanReview;
    // Treat findings as "unresolved" when needsHumanReview is true OR not explicitly false.
    return needs !== false;
  });

  if (matched.length === 0 && unresolvedFindings.length === 0) {
    return (
      <GlassCard className="p-5">
        <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-3">Cross-Compound Relationships</h3>
        <p className="text-sm text-white/30">No cross-compound relationships recorded for this compound yet.</p>
      </GlassCard>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <GlassCard className="p-5">
        <div className="mb-4 flex items-baseline justify-between gap-3">
          <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35">
            Cross-Compound Relationships
          </h3>
          <p className="text-[10px] text-white/30">{matched.length} relationship{matched.length === 1 ? '' : 's'}</p>
        </div>

        {matched.length === 0 ? (
          <p className="text-sm text-white/30">No relationship edges for this compound.</p>
        ) : (
          <ul className="flex flex-col gap-3">
            {matched.map((m, idx) => {
              const e = m.edge as unknown as Record<string, unknown>;
              const edgeTypeRaw = (e.edgeType as string) ?? '';
              const assertedRaw = getAssertedEdgeType(m.edge);
              const showAsserted = assertedRaw && normalizeKey(assertedRaw) !== normalizeKey(edgeTypeRaw);
              const evidenceTier = (e.evidenceTier as string) ?? '';
              const communitySignal = (e.communitySignal as CompoundGraphEdge['communitySignal']) ?? null;
              const needsReview = Boolean(e.needsReview);
              const edgeKey = getEdgeId(m.edge, `edge-${idx}`);

              const linkAvailable =
                m.counterpartSlug && (knownSlugs ? knownSlugs.has(m.counterpartSlug) : true);

              return (
                <li
                  key={edgeKey}
                  className="rounded-xl border border-white/[0.08] bg-white/[0.025] px-4 py-3 flex flex-col gap-2"
                >
                  <div className="flex flex-wrap items-center gap-2">
                    {linkAvailable && m.counterpartSlug ? (
                      <Link
                        href={`/admin/research/compounds/${m.counterpartSlug}`}
                        className="text-sm font-semibold text-emerald-300 hover:text-emerald-200 transition-colors"
                      >
                        {m.counterpartLabel}
                      </Link>
                    ) : (
                      <span className="text-sm font-semibold text-white/85">{m.counterpartLabel}</span>
                    )}

                    <span className="text-[10px] font-semibold tracking-wide uppercase px-2 py-1 rounded-full border bg-white/[0.05] border-white/10 text-white/65">
                      {kebabOrPascalToTitle(edgeTypeRaw) || 'Relationship'}
                    </span>

                    {evidenceTier && (
                      <EvidenceTierBadge tier={evidenceTier} variant="research" />
                    )}

                    {communitySignal && (communitySignal as { present?: boolean }).present === true && (
                      <CommunitySignalBadge signal={communitySignal} />
                    )}

                    {needsReview && <ReviewRequiredBadge />}
                  </div>

                  {showAsserted && (
                    <p className="text-[11px] text-white/45">
                      Asserted: <span className="text-white/65">{kebabOrPascalToTitle(assertedRaw)}</span>
                      <span className="px-1 text-white/30">→</span>
                      effective: <span className="text-white/65">{kebabOrPascalToTitle(edgeTypeRaw)}</span>
                    </p>
                  )}
                </li>
              );
            })}
          </ul>
        )}
      </GlassCard>

      {unresolvedFindings.length > 0 && (
        <GlassCard className="p-5">
          <h3 className="text-[9px] font-bold uppercase tracking-widest text-white/35 mb-3">
            Review Findings ({unresolvedFindings.length})
          </h3>
          <ul className="flex flex-col gap-3">
            {unresolvedFindings.map((f, idx) => {
              const f2 = f as unknown as Record<string, unknown>;
              const findingId = ((f2.findingId ?? f2.id ?? `finding-${idx}`) as string);
              return (
                <li
                  key={findingId}
                  className="rounded-xl border border-white/[0.08] bg-white/[0.025] px-4 py-3 flex flex-col gap-2"
                >
                  <div className="flex flex-wrap items-center gap-2">
                    <span
                      className={`text-[9px] font-bold tracking-widest uppercase px-2 py-1 rounded-full border ${severityClasses(f.severity)}`}
                    >
                      {kebabOrPascalToTitle(f.severity) || 'Unknown'}
                    </span>
                    <span className="text-[10px] font-semibold tracking-wide uppercase px-2 py-1 rounded-full border bg-white/[0.05] border-white/10 text-white/65">
                      {kebabOrPascalToTitle(f.findingType) || 'Finding'}
                    </span>
                  </div>
                  <p className="text-[12px] leading-relaxed text-white/80">{f.summary}</p>
                  {f.recommendedAction && (
                    <p className="text-[11px] leading-relaxed text-blue-200/80">
                      → {f.recommendedAction}
                    </p>
                  )}
                </li>
              );
            })}
          </ul>
        </GlassCard>
      )}
    </div>
  );
}

export function countRelationshipEdgesForCompound(
  graph: CompoundGraph | null,
  compound: { canonicalName: string; slug: string; aliases?: string[] }
): number {
  if (!graph) return 0;
  const nodes = graph.nodes ?? [];
  const edges = graph.edges ?? [];
  const nodeById = new Map<string, CompoundGraphNode>();
  for (const n of nodes) {
    const id = getNodeId(n);
    if (id) nodeById.set(id, n);
  }
  const canonicalSlug = compound.slug || toSlug(compound.canonicalName);
  const aliasSlugs = new Set((compound.aliases ?? []).map(toSlug));

  function isCurrent(node: CompoundGraphNode | undefined): boolean {
    if (!node) return false;
    const n = node as unknown as Record<string, unknown>;
    const nodeType = normalizeKey(n.nodeType as string);
    if (nodeType && nodeType !== 'compound') return false;
    if (node.label === compound.canonicalName) return true;
    const aliases = (node.aliases ?? []) as string[];
    if (aliases.some(a => a === compound.canonicalName)) return true;
    if (aliases.some(a => aliasSlugs.has(toSlug(a)))) return true;
    const nodeId = getNodeId(node);
    if (nodeId && nodeId.toLowerCase().endsWith(`:${canonicalSlug}`)) return true;
    if (typeof node.label === 'string' && toSlug(node.label) === canonicalSlug) return true;
    return false;
  }

  let count = 0;
  for (const edge of edges) {
    const e = edge as unknown as Record<string, unknown>;
    const edgeTypeKey = normalizeKey(e.edgeType as string);
    if (!RELATIONSHIP_EDGE_KEYS.has(edgeTypeKey)) continue;
    const { from, to } = getEdgeFromTo(edge);
    if (isCurrent(nodeById.get(from)) || isCurrent(nodeById.get(to))) {
      count++;
    }
  }
  return count;
}
