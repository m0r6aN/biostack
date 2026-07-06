'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { apiClient } from '@/lib/api';
import { useAuth } from '@/lib/AuthProvider';
import { KnowledgeEntry } from '@/lib/types';
import { CompoundIntelligenceCard } from '@/components/knowledge/CompoundIntelligenceCard';
import { CompoundRelationshipsSection } from '@/components/knowledge/CompoundRelationshipsSection';
import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { MarketingNav } from '@/components/marketing/MarketingNav';

function DossierSkeleton() {
  return (
    <div className="max-w-3xl mx-auto space-y-6 animate-pulse">
      <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-3">
        <div className="h-7 w-56 rounded-lg bg-white/10" />
        <div className="h-3 w-24 rounded bg-white/6" />
      </div>
      <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-3">
        <div className="h-3 w-20 rounded bg-white/8" />
        <div className="flex gap-2">
          <div className="h-6 w-20 rounded-full bg-white/10" />
          <div className="h-6 w-28 rounded-full bg-white/8" />
        </div>
        <div className="h-16 rounded-xl bg-white/6" />
      </div>
      <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-3">
        <div className="h-3 w-36 rounded bg-white/8" />
        <div className="h-14 rounded-xl bg-white/6" />
      </div>
      <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-3">
        <div className="h-3 w-28 rounded bg-white/8" />
        {[1, 2, 3].map(i => (
          <div key={i} className="h-12 rounded-xl bg-white/6" />
        ))}
      </div>
    </div>
  );
}

function safeDecodeSlug(value: string): string {
  try {
    return decodeURIComponent(value);
  } catch {
    return value;
  }
}

interface PageProps {
  params: { slug: string };
}

export default function CompoundDossierPage({ params }: PageProps) {
  const { slug } = params;
  const { user, loading: authLoading } = useAuth();
  const [entry, setEntry] = useState<KnowledgeEntry | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    apiClient.getKnowledgeEntry(safeDecodeSlug(slug)).then(res => {
      if (!cancelled) {
        setEntry(res);
        setLoading(false);
      }
    }).catch(err => {
      if (!cancelled) {
        setError(err?.message ?? 'Failed to load compound dossier.');
        setLoading(false);
      }
    });

    return () => { cancelled = true; };
  }, [slug]);

  if (authLoading) {
    return null;
  }

  const content = (
    <main className="min-h-screen bg-[#0a0a0b] py-8 px-4">
      <nav className="max-w-3xl mx-auto mb-6">
        <Link
          href="/knowledge"
          className="inline-flex items-center gap-1.5 text-xs text-white/40 hover:text-white/70 transition-colors"
        >
          <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
          </svg>
          Compound Library
        </Link>
      </nav>

      {loading ? (
        <DossierSkeleton />
      ) : error ? (
        <div className="max-w-3xl mx-auto rounded-2xl border border-rose-400/20 bg-rose-500/[0.06] p-6">
          <p className="text-sm text-rose-300">{error}</p>
        </div>
      ) : entry ? (
        <>
          <CompoundIntelligenceCard entry={entry} recommendationSurface="knowledge-detail" />
          <CompoundRelationshipsSection
            compoundName={entry.canonicalName}
            aliases={entry.aliases}
          />
        </>
      ) : null}
    </main>
  );

  if (!user) {
    return (
      <div>
        <MarketingNav />
        {content}
        <MarketingFooter />
      </div>
    );
  }

  return content;
}
