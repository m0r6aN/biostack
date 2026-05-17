export const RELATIONSHIP_EDGE_KEYS: ReadonlySet<string> = new Set([
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

export function normalizeEdgeType(raw: string | null | undefined): string {
  if (!raw) return '';
  return raw.toString().replace(/[-_\s]/g, '').toLowerCase();
}

export function normalizeCompoundId(raw: string | null | undefined): string {
  if (!raw) return '';
  const stripped = raw.toString().replace(/^compound:/i, '');
  return normalizeEdgeType(stripped);
}

export function isRelationshipEdge(edgeType: string | null | undefined): boolean {
  return RELATIONSHIP_EDGE_KEYS.has(normalizeEdgeType(edgeType));
}

const RELATIONSHIP_LABELS: Record<string, string> = {
  synergizeswith: 'May work well together',
  pairswith: 'May work well together',
  complements: 'May support the same goal differently',
  redundantwith: 'May overlap',
  conflictswith: 'Potential conflict',
  opposeseffect: 'Potential conflict',
  opposingeffect: 'Potential conflict',
  avoidwith: 'Caution signal',
  hascommunitysignal: 'Community-reported pairing',
  contradictedby: 'Contradicted by evidence',
};

export function getRelationshipLabel(edgeType: string | null | undefined): string {
  return RELATIONSHIP_LABELS[normalizeEdgeType(edgeType)] ?? 'Related compound';
}

const EVIDENCE_TIER_LABELS: Record<string, string> = {
  strong: 'Strong evidence',
  moderate: 'Moderate evidence',
  limited: 'Limited evidence',
  mechanistic: 'Mechanistic evidence',
  anecdotal: 'Community report: not clinically verified',
};

export function getEvidenceTierLabel(tier: string | null | undefined): string {
  if (!tier) return 'Evidence level unknown';
  return EVIDENCE_TIER_LABELS[tier.toLowerCase()] ?? 'Evidence level unknown';
}

const COMMUNITY_SIGNAL_LABELS: Record<string, string> = {
  isolated: 'Rarely reported in community',
  recurring: 'Commonly reported in community',
  widespread: 'Widely reported across communities',
};

export function getCommunitySignalLabel(strength: string | null | undefined): string | null {
  if (!strength) return null;
  return COMMUNITY_SIGNAL_LABELS[strength.toLowerCase()] ?? null;
}
