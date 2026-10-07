'use client';

import { useMemo, useSyncExternalStore } from 'react';
import {
  parseAnalyzerProtocolDraftRaw,
  readAnalyzerProtocolDraftRaw,
  subscribeToAnalyzerProtocolDraft,
  type AnalyzerProtocolDraft,
} from './analyzerStorage';

function subscribe(onChange: () => void): () => void {
  const unsubscribe = subscribeToAnalyzerProtocolDraft(onChange);
  window.addEventListener('storage', onChange);
  return () => {
    unsubscribe();
    window.removeEventListener('storage', onChange);
  };
}

function getServerSnapshot(): string | null {
  return null;
}

/**
 * Hydration-safe view of the saved analyzer draft on this device. The server
 * snapshot is always null, so first paint never trusts device storage; the
 * client snapshot is the raw stored string (stable across renders), re-read
 * whenever the storage helpers write or another tab changes it.
 */
export function useAnalyzerProtocolDraft(): AnalyzerProtocolDraft | null {
  const raw = useSyncExternalStore(subscribe, readAnalyzerProtocolDraftRaw, getServerSnapshot);
  return useMemo(() => parseAnalyzerProtocolDraftRaw(raw), [raw]);
}
