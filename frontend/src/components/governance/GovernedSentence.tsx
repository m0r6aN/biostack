'use client';

/**
 * KE-3 GovernedSentence — wraps a text fragment and only renders it after a
 * successful policy pass from the BioStack Policy Gate.
 *
 * Architecture law: never render `text` before the policy check resolves.
 * The resolved policyHash is stored in data-policy-hash for audit trail purposes.
 */

import React from 'react';
import { cn } from '@/lib/utils';
import { usePolicyGate } from '@/lib/governance/policyGate';

export interface GovernedSentenceProps {
  /** The text fragment to render — policy-checked before display. */
  text: string;
  /**
   * The surface context for classification.
   * One of: "srb-finding" | "counterfactual-lab" | "mission-control" | "compound-dossier"
   */
  context: string;
  /** Rendered when the decision is blocked or rewrite-required (no rewrite available). */
  fallback?: React.ReactNode;
  /** Whether to render the disclaimer chip when decision is allowed-with-disclaimer. */
  showDisclaimer?: boolean;
  className?: string;
}

const DEFAULT_BLOCKED_FALLBACK = (
  <span className="text-xs text-white/30 italic font-mono">
    [Content policy: not renderable]
  </span>
);

const DEFAULT_ESCALATE_FALLBACK = (
  <span className="inline-flex items-center gap-1.5 rounded border border-amber-500/30 bg-amber-500/10 px-2 py-0.5 text-[10px] font-medium text-amber-400">
    <span className="w-1.5 h-1.5 rounded-full bg-amber-400 shrink-0" />
    Provider review required
  </span>
);

/** Skeleton shown while the policy check is in-flight. */
function PolicyCheckSkeleton() {
  return (
    <span
      aria-busy="true"
      aria-label="Policy checking..."
      className="inline-block w-32 h-3 rounded bg-white/10 animate-pulse"
    />
  );
}

/** Disclaimer chip shown when decision is allowed-with-disclaimer. */
function DisclaimerChip({ text }: { text: string }) {
  return (
    <span className="ml-1.5 inline-flex items-center gap-1 rounded border border-blue-500/30 bg-blue-500/10 px-1.5 py-0.5 text-[9px] font-medium text-blue-400 align-middle">
      <span className="w-1 h-1 rounded-full bg-blue-400 shrink-0" />
      {text}
    </span>
  );
}

export function GovernedSentence({
  text,
  context,
  fallback,
  showDisclaimer = true,
  className,
}: GovernedSentenceProps) {
  // Skip gate entirely when text is empty — render nothing.
  if (!text || !text.trim()) {
    return null;
  }

  return (
    <GovernedSentenceInner
      text={text}
      context={context}
      fallback={fallback}
      showDisclaimer={showDisclaimer}
      className={className}
    />
  );
}

/** Inner component handles the hook (hooks cannot be called conditionally). */
function GovernedSentenceInner({
  text,
  context,
  fallback,
  showDisclaimer,
  className,
}: GovernedSentenceProps & { showDisclaimer: boolean }) {
  const { result, loading } = usePolicyGate(text, context);

  if (loading || result === null) {
    return <PolicyCheckSkeleton />;
  }

  const policyHashValue = result.policyHash?.value ?? '';

  switch (result.decision) {
    case 'allowed':
      return (
        <span
          className={cn(className)}
          data-policy-hash={policyHashValue}
        >
          {text}
        </span>
      );

    case 'allowed-with-disclaimer':
      return (
        <span
          className={cn(className)}
          data-policy-hash={policyHashValue}
        >
          {text}
          {showDisclaimer && result.disclaimerText && (
            <DisclaimerChip text={result.disclaimerText} />
          )}
        </span>
      );

    case 'rewrite-required':
      if (result.rewrittenText) {
        return (
          <span
            className={cn(className)}
            data-policy-hash={policyHashValue}
          >
            {result.rewrittenText}
          </span>
        );
      }
      return (
        <span data-policy-hash={policyHashValue}>
          {fallback ?? DEFAULT_BLOCKED_FALLBACK}
        </span>
      );

    case 'escalate-to-provider-review':
      return (
        <span data-policy-hash={policyHashValue}>
          {DEFAULT_ESCALATE_FALLBACK}
        </span>
      );

    case 'blocked':
    default:
      return (
        <span data-policy-hash={policyHashValue}>
          {fallback ?? DEFAULT_BLOCKED_FALLBACK}
        </span>
      );
  }
}
