// Generated from shared/source-rights; scripts/sync-source-rights.mjs --check guards drift.
import policy from './source-rights/drugbank-containment.v1.json';

export const RESTRICTED_EXCERPT_FLAG = 'restricted-source-excerpt-withheld';
const restrictedIds = new Set(policy.sourceIds.map(id => id.trim().toLowerCase()));

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function normalizedId(value: unknown): string {
  return typeof value === 'string' ? value.trim().toLowerCase() : '';
}

function hasRestrictedHost(value: unknown): boolean {
  if (typeof value !== 'string') return false;
  try {
    const host = new URL(value).hostname.toLowerCase().replace(/\.$/, '');
    return policy.hostSuffixes.some(suffix => host === suffix || host.endsWith(`.${suffix}`));
  } catch {
    return false;
  }
}

/**
 * Apply the owner's C1 containment to a JSON evidence packet, including historical
 * artifacts and client-submitted packets. The bundled decision manifest remains
 * effective without a registry; client-supplied rights metadata cannot release it.
 * Citation metadata and the original packet are preserved. This does not authorize
 * the remaining sources or establish the provenance of arbitrary submitted text.
 */
export function withholdDrugBankExcerpts<T>(packet: T): T {
  if (!isRecord(packet) || !Array.isArray(packet.claims)) return packet;
  const sourceIds = new Set(restrictedIds);
  if (Array.isArray(packet.sources)) {
    for (const source of packet.sources) {
      if (isRecord(source) && hasRestrictedHost(source.url)) {
        sourceIds.add(normalizedId(source.sourceId));
      }
    }
  }
  sourceIds.delete('');

  let withheld = false;
  const claims = packet.claims.map(claim => {
    if (!isRecord(claim) || !Array.isArray(claim.extractedEvidence)) return claim;
    const extractedEvidence = claim.extractedEvidence.map(item => {
      if (!isRecord(item) || !sourceIds.has(normalizedId(item.sourceRef)) || item.quote == null) return item;
      withheld = true;
      return { ...item, quote: null };
    });
    return { ...claim, extractedEvidence };
  });
  if (!withheld) return packet;

  const ops = isRecord(packet.ops) ? packet.ops : {};
  const flags = Array.isArray(ops.qualityFlags) ? ops.qualityFlags : [];
  const qualityFlags = flags.includes(RESTRICTED_EXCERPT_FLAG) ? flags : [...flags, RESTRICTED_EXCERPT_FLAG];
  return { ...packet, claims, ops: { ...ops, qualityFlags } } as T;
}
