'use client';

import { Header } from '@/components/Header';
import { LoadingState } from '@/components/LoadingState';
import { CompoundIntelligenceCard } from '@/components/knowledge/CompoundIntelligenceCard';
import { OverlapResults } from '@/components/knowledge/OverlapResults';
import { GlassCard } from '@/components/ui/GlassCard';
import { apiClient } from '@/lib/api';
import { InteractionFlag, KnowledgeEntry } from '@/lib/types';
import { useState } from 'react';

export default function KnowledgePage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<KnowledgeEntry[]>([]);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);

  const [selectedCompounds, setSelectedCompounds] = useState<string[]>([]);
  const [overlapResults, setOverlapResults] = useState<InteractionFlag[]>([]);
  const [checkingOverlaps, setCheckingOverlaps] = useState(false);
  const [overlapError, setOverlapError] = useState<string | null>(null);
  const [hasCheckedOverlaps, setHasCheckedOverlaps] = useState(false);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!searchQuery.trim()) return;
    try {
      setSearching(true);
      setSearchError(null);
      const results = await apiClient.getAllKnowledgeCompounds();
      const q = searchQuery.toLowerCase();
      setSearchResults(
        results.filter(
          r =>
            r.canonicalName.toLowerCase().includes(q) ||
            r.aliases.some(a => a.toLowerCase().includes(q)) ||
            r.classification.toLowerCase().includes(q)
        )
      );
    } catch {
      setSearchError('Failed to search knowledge base');
    } finally {
      setSearching(false);
    }
  };

  const handleCheckOverlaps = async () => {
    if (selectedCompounds.length < 2) {
      setOverlapError('Select at least 2 compounds');
      return;
    }
    try {
      setCheckingOverlaps(true);
      setOverlapError(null);
      const results = await apiClient.checkOverlap(selectedCompounds);
      setOverlapResults(results);
      setHasCheckedOverlaps(true);
    } catch {
      setOverlapError('Failed to check overlaps');
    } finally {
      setCheckingOverlaps(false);
    }
  };

  const toggleCompound = (name: string) => {
    setSelectedCompounds(prev =>
      prev.includes(name) ? prev.filter(c => c !== name) : [...prev, name]
    );
    setHasCheckedOverlaps(false);
    setOverlapResults([]);
  };

  return (
    <div className="w-full min-h-screen">
      <Header title="Knowledge Base" subtitle="Intelligence · Pathway analysis" />

      <div className="p-8 space-y-8 max-w-5xl">

        {/* ── Disclaimer ──────────────────────────────────────── */}
        <div className="flex items-center gap-3 px-4 py-3 rounded-xl border border-amber-400/15 bg-amber-500/[0.06] text-amber-200/70 text-xs">
          <span className="text-amber-400 shrink-0">⚠</span>
          Educational reference only — not medical advice.
        </div>

        {/* ── Search ──────────────────────────────────────────── */}
        <GlassCard variant="default" className="p-6">
          <h2 className="text-sm font-semibold uppercase tracking-[0.15em] text-white/50 mb-4">
            Search Compounds
          </h2>
          <form onSubmit={handleSearch} className="flex gap-3">
            <div className="relative flex-1">
              <svg
                className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-white/30"
                fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}
              >
                <path strokeLinecap="round" strokeLinejoin="round"
                  d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              <input
                type="text"
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                placeholder="Search compounds, supplements, substances…"
                className="w-full h-12 pl-11 pr-4 bg-white/[0.05] border border-white/[0.08] rounded-xl text-white placeholder:text-white/25 focus:outline-none focus:border-emerald-500/40 focus:bg-white/[0.07] focus:shadow-[0_0_0_1px_rgba(34,197,94,0.15)] transition-all"
              />
            </div>
            <button
              type="submit"
              disabled={searching}
              className="px-6 py-3 bg-emerald-500 hover:bg-emerald-400 disabled:bg-white/10 disabled:text-white/30 text-slate-950 font-semibold rounded-xl transition-all shadow-[0_0_20px_rgba(34,197,94,0.25)] hover:shadow-[0_0_28px_rgba(34,197,94,0.4)]"
            >
              {searching ? 'Searching…' : 'Search'}
            </button>
          </form>

          {searchError && (
            <p className="mt-3 text-sm text-red-300 bg-red-500/10 border border-red-500/20 rounded-lg px-3 py-2">
              {searchError}
            </p>
          )}
        </GlassCard>

        {/* ── Results ─────────────────────────────────────────── */}
        {searching ? (
          <LoadingState />
        ) : searchResults.length > 0 ? (
          <div className="space-y-4">
            <p className="text-xs uppercase tracking-[0.15em] text-white/35">
              {searchResults.length} result{searchResults.length !== 1 ? 's' : ''} found
            </p>
            {searchResults.map(result => (
              <CompoundIntelligenceCard
                key={result.canonicalName}
                entry={result}
                recommendationSurface="knowledge-search"
              />
            ))}
          </div>
        ) : searchQuery && !searching ? (
          <GlassCard variant="base" className="p-8 text-center">
            <p className="text-white/40 text-sm">No results for &ldquo;{searchQuery}&rdquo;</p>
          </GlassCard>
        ) : null}

        {/* ── Pathway Overlap Checker ──────────────────────────── */}
        <GlassCard variant="hero" className="p-6 overflow-hidden relative">
          {/* Accent glow */}
          <div className="absolute -top-12 -right-12 w-48 h-48 rounded-full bg-amber-500/[0.08] blur-3xl pointer-events-none" />

          <div className="relative">
            <div className="flex items-center gap-2.5 mb-1">
              <span className="text-amber-400 text-base">⚡</span>
              <h2 className="text-base font-semibold text-white">Pathway Overlap Checker</h2>
            </div>
            <p className="text-sm text-white/50 mb-5">
              Select two or more compounds to surface shared pathways and potential interactions.
            </p>

            {/* Selection chips from search results */}
            {searchResults.length > 0 && (
              <div className="mb-5">
                <p className="text-xs uppercase tracking-[0.15em] text-white/35 mb-2">From search results</p>
                <div className="flex flex-wrap gap-2">
                  {searchResults.map(r => (
                    <button
                      key={r.canonicalName}
                      onClick={() => toggleCompound(r.canonicalName)}
                      className={`text-xs px-3 py-1.5 rounded-full border font-medium transition-all ${
                        selectedCompounds.includes(r.canonicalName)
                          ? 'bg-amber-500/20 text-amber-300 border-amber-500/40 shadow-[0_0_12px_rgba(245,158,11,0.2)]'
                          : 'bg-white/[0.04] text-white/55 border-white/[0.08] hover:bg-white/[0.08] hover:text-white/80'
                      }`}
                    >
                      {selectedCompounds.includes(r.canonicalName) && (
                        <span className="mr-1">✓</span>
                      )}
                      {r.canonicalName}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* Selection state */}
            <div className="flex items-center justify-between mb-4">
              <p className="text-sm text-white/50">
                {selectedCompounds.length > 0
                  ? <><span className="text-amber-300 font-medium">{selectedCompounds.length}</span> compound{selectedCompounds.length !== 1 ? 's' : ''} selected</>
                  : <span className="text-white/30">No compounds selected</span>
                }
              </p>
              {selectedCompounds.length > 0 && (
                <button
                  onClick={() => {
                    setSelectedCompounds([]);
                    setHasCheckedOverlaps(false);
                    setOverlapResults([]);
                  }}
                  className="text-xs text-white/30 hover:text-white/60 transition-colors"
                >
                  Clear
                </button>
              )}
            </div>

            {overlapError && (
              <p className="mb-3 text-sm text-red-300 bg-red-500/10 border border-red-500/20 rounded-lg px-3 py-2">
                {overlapError}
              </p>
            )}

            <button
              onClick={handleCheckOverlaps}
              disabled={checkingOverlaps || selectedCompounds.length < 2}
              className="px-5 py-2.5 bg-amber-500 hover:bg-amber-400 disabled:bg-white/10 disabled:text-white/25 text-slate-950 font-semibold rounded-xl transition-all text-sm shadow-[0_0_16px_rgba(245,158,11,0.25)] hover:shadow-[0_0_24px_rgba(245,158,11,0.4)] disabled:shadow-none"
            >
              {checkingOverlaps ? 'Checking…' : 'Check Overlaps'}
            </button>

            {checkingOverlaps && <div className="mt-4"><LoadingState /></div>}

            {hasCheckedOverlaps && !checkingOverlaps && selectedCompounds.length >= 2 && (
              <div className="mt-5">
                <OverlapResults flags={overlapResults} inputCount={selectedCompounds.length} />
              </div>
            )}
          </div>
        </GlassCard>

        {/* ── About ───────────────────────────────────────────── */}
        <GlassCard variant="base" className="p-5">
          <div className="flex items-start gap-3">
            <span className="text-blue-400 text-lg mt-0.5">📡</span>
            <div>
              <h3 className="text-sm font-semibold text-blue-300 mb-1">About This Knowledge Base</h3>
              <p className="text-sm text-white/45 leading-relaxed">
                Information is curated from scientific literature for observational and educational
                purposes only. Not a substitute for professional medical advice.
              </p>
            </div>
          </div>
        </GlassCard>

      </div>
    </div>
  );
}
