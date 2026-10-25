'use client';

import { useEffect } from 'react';

/** Logs the query hashed server-side (no identity) so unmatched demand feeds the editorial queue. */
export function CheckLogger({ query, matchedClaimId, doseIntent }: { query: string; matchedClaimId: string | null; doseIntent: boolean }) {
  useEffect(() => {
    if (query.trim().length < 2) return;
    void fetch('/api/v1/check/log', {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ query, matchedClaimId, doseIntent }),
    }).catch(() => {});
  }, [query, matchedClaimId, doseIntent]);
  return null;
}
