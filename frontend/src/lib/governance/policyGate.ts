/**
 * KE-3 Policy Gate — client-side hook + in-memory cache.
 *
 * Calls POST /api/v1/policy/check and returns the classification decision
 * before any recommendation-like sentence is rendered to the user.
 */

import { getApiBaseUrl } from '@/lib/apiBase';

export type PolicyDecision =
  | 'allowed'
  | 'allowed-with-disclaimer'
  | 'rewrite-required'
  | 'blocked'
  | 'escalate-to-provider-review';

export interface PolicyGateCheckRequest {
  text: string;
  context: string;
  tenantId?: string;
  actorId?: string;
}

export interface PolicyGateCheckResult {
  decision: PolicyDecision;
  disclaimerText: string | null;
  rewrittenText: string | null;
  blockReason: string | null;
  policyHash: { value: string; version: string };
  locallyClassified: boolean;
}

// ── In-memory result cache ────────────────────────────────────────────────────
// Keyed by "<text>::<context>" — avoids redundant API calls for identical fragments.
const resultCache = new Map<string, PolicyGateCheckResult>();

function cacheKey(text: string, context: string): string {
  return `${text}::${context}`;
}

/**
 * Classify and policy-check a text fragment by calling the backend gate.
 * Results are cached in-memory for the lifetime of the page.
 */
export async function checkPolicy(
  request: PolicyGateCheckRequest,
): Promise<PolicyGateCheckResult> {
  const key = cacheKey(request.text, request.context);
  const cached = resultCache.get(key);
  if (cached) return cached;

  const baseUrl = getApiBaseUrl();
  const response = await fetch(`${baseUrl}/api/v1/policy/check`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({
      text: request.text,
      context: request.context,
      tenantId: request.tenantId ?? '',
      actorId: request.actorId ?? '',
    }),
  });

  if (!response.ok) {
    // Fail-closed: treat API errors as blocked.
    const blocked: PolicyGateCheckResult = {
      decision: 'blocked',
      disclaimerText: null,
      rewrittenText: null,
      blockReason: `policy-gate-error: HTTP ${response.status}`,
      policyHash: { value: 'error', version: '0.0.0' },
      locallyClassified: false,
    };
    return blocked;
  }

  const result: PolicyGateCheckResult = await response.json();
  resultCache.set(key, result);
  return result;
}

// ── React hook ────────────────────────────────────────────────────────────────

import { useState, useEffect } from 'react';

export interface UsePolicyGateState {
  result: PolicyGateCheckResult | null;
  loading: boolean;
  error: Error | null;
}

/**
 * React hook that calls `checkPolicy` and returns { result, loading, error }.
 * Skips the gate call when `text` is empty or whitespace.
 */
export function usePolicyGate(text: string, context: string): UsePolicyGateState {
  const [result, setResult] = useState<PolicyGateCheckResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    if (!text || !text.trim()) {
      setResult(null);
      setLoading(false);
      setError(null);
      return;
    }

    let cancelled = false;

    setLoading(true);
    setError(null);

    checkPolicy({ text, context })
      .then((r) => {
        if (!cancelled) {
          setResult(r);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err : new Error(String(err)));
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [text, context]);

  return { result, loading, error };
}
