/**
 * Compound records folded into a canonical record (2026-10-25 duplicate consolidation).
 * The retired slug and claim ids keep working through permanent redirects (next.config.ts),
 * and the sitemap never lists a retired slug, even if a stale row is still served by the API.
 */
export type MergedRecord = {
  from: string;
  to: string;
  /** Retired claim id → surviving claim id carrying the same statement. */
  claims: Record<string, string>;
};

export const MERGED_RECORDS: readonly MergedRecord[] = [
  {
    from: 'testolone',
    to: 'vosilasarm',
    claims: {
      'rad140-record-muscle': 'testolone-muscle',
      'rad140-record-safe': 'testolone-safe',
      'rad140-record-legal-supplement': 'testolone-legal-supplement',
    },
  },
  {
    from: 'creatine-monohydrate',
    to: 'creatine',
    claims: {
      'creatine-monohydrate-performance': 'creatine-performance',
      'creatine-monohydrate-hair-loss': 'creatine-hair-loss',
      'creatine-monohydrate-unsafe': 'creatine-unsafe',
    },
  },
];

export const RETIRED_SLUGS: ReadonlySet<string> = new Set(MERGED_RECORDS.map((m) => m.from));

type Redirect = { source: string; destination: string; permanent: true };

/** Permanent redirects for every retired slug and claim id. Specific claim routes come first. */
export function mergedRecordRedirects(): Redirect[] {
  const out: Redirect[] = [];
  for (const { from, to, claims } of MERGED_RECORDS) {
    for (const [oldId, newId] of Object.entries(claims)) {
      out.push({ source: `/knowledge/${from}/claims/${oldId}`, destination: `/knowledge/${to}/claims/${newId}`, permanent: true });
      out.push({ source: `/c/${oldId}`, destination: `/c/${newId}`, permanent: true });
      out.push({ source: `/embed/verdict/${oldId}`, destination: `/embed/verdict/${newId}`, permanent: true });
    }
    out.push({ source: `/knowledge/${from}`, destination: `/knowledge/${to}`, permanent: true });
    out.push({ source: `/knowledge/${from}/:path*`, destination: `/knowledge/${to}/:path*`, permanent: true });
    out.push({ source: `/vs/${from}/:other`, destination: `/vs/${to}/:other`, permanent: true });
    out.push({ source: `/vs/:other/${from}`, destination: `/vs/:other/${to}`, permanent: true });
    out.push({ source: `/embed/vs/${from}/:other`, destination: `/embed/vs/${to}/:other`, permanent: true });
    out.push({ source: `/embed/vs/:other/${from}`, destination: `/embed/vs/:other/${to}`, permanent: true });
  }
  return out;
}
