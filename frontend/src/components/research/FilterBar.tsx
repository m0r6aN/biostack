'use client';
import type { ResearchReviewCategory } from '@/lib/research/types';
import { cn } from '@/lib/utils';
import { usePathname, useRouter, useSearchParams } from 'next/navigation';
import { useState } from 'react';

interface FilterBarProps {
  researchRequestedCount?: number;
  blockedCount: number;
  reviewCount: number;
  candidateCount: number;
  categories: ResearchReviewCategory[];
  sidebar?: boolean;
}

const EVIDENCE_TIERS = ['Strong', 'Moderate', 'Limited', 'Insufficient', 'Unknown', 'Anecdotal'];
const SORT_OPTIONS = [
  { value: 'risk', label: 'Risk Priority' },
  { value: 'name', label: 'Compound Name' },
  { value: 'tier', label: 'Evidence Tier' },
  { value: 'completeness', label: 'Completeness' },
];

export function FilterBar({ researchRequestedCount = 0, blockedCount, reviewCount, candidateCount, categories, sidebar = false }: FilterBarProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const pathname = usePathname();
  const [moreOpen, setMoreOpen] = useState(false);

  const activeReadiness = searchParams.getAll('readiness');
  const activeCategories = searchParams.getAll('category');
  const activeTiers = searchParams.getAll('tier');
  const activeSort = searchParams.get('sort') ?? 'risk';

  function toggleMulti(key: string, value: string) {
    const params = new URLSearchParams(searchParams.toString());
    const current = params.getAll(key);
    params.delete(key);
    if (current.includes(value)) {
      current.filter(v => v !== value).forEach(v => params.append(key, v));
    } else {
      [...current, value].forEach(v => params.append(key, v));
    }
    router.replace(`${pathname}?${params.toString()}`);
  }

  function setSort(value: string) {
    const params = new URLSearchParams(searchParams.toString());
    params.set('sort', value);
    router.replace(`${pathname}?${params.toString()}`);
  }

  return (
    <div className={cn('grid gap-3 rounded-2xl border border-white/10 bg-white/[0.03] p-3', !sidebar && 'md:grid-cols-2')}>
      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">Readiness</p>
        <div className="flex flex-wrap gap-1.5">
          {[
            { key: 'research-requested',       label: `Research Requested (${researchRequestedCount})`, cls: 'text-violet-300 border-violet-400/30 bg-violet-400/10' },
            { key: 'blocked',                 label: `Blocked (${blockedCount})`,        cls: 'text-rose-400 border-rose-400/30 bg-rose-400/10' },
            { key: 'review-required',         label: `Review Required (${reviewCount})`, cls: 'text-amber-400 border-amber-400/30 bg-amber-400/10' },
            { key: 'candidate-for-promotion', label: `Candidate (${candidateCount})`,    cls: 'text-emerald-400 border-emerald-400/30 bg-emerald-400/10' },
          ].map(chip => (
            <button key={chip.key} onClick={() => toggleMulti('readiness', chip.key)}
              className={cn('text-[10px] px-2.5 py-1 rounded-full border transition-all',
                activeReadiness.includes(chip.key) ? chip.cls : 'text-white/40 border-white/15 bg-white/[0.03] hover:border-white/25')}>
              {chip.label}
            </button>
          ))}
        </div>
      </div>

      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">Review Category</p>
        <div className="flex flex-wrap gap-1.5">
          {categories.map(cat => (
            <button key={cat.name} onClick={() => toggleMulti('category', cat.name)}
              className={cn('text-[10px] px-2.5 py-1 rounded-full border transition-all',
                activeCategories.includes(cat.name)
                  ? 'text-white border-white/40 bg-white/15'
                  : 'text-white/40 border-white/15 bg-white/[0.03] hover:border-white/25')}>
              {cat.name}
            </button>
          ))}
        </div>
      </div>

      <button onClick={() => setMoreOpen(v => !v)}
        className="text-[10px] text-white/30 hover:text-white/60 text-left transition-colors">
        {moreOpen ? '▲ Fewer filters' : 'More filters'}
      </button>

      {moreOpen && (
        <div className={cn('border-t border-white/[0.06] pt-2', !sidebar && 'md:col-span-2')}>
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">Evidence Tier</p>
          <div className="flex flex-wrap gap-1.5">
            {EVIDENCE_TIERS.map(tier => (
              <button key={tier} onClick={() => toggleMulti('tier', tier)}
                className={cn('text-[10px] px-2.5 py-1 rounded-full border transition-all',
                  activeTiers.includes(tier)
                    ? 'text-white border-white/40 bg-white/15'
                    : 'text-white/40 border-white/15 bg-white/[0.03] hover:border-white/25')}>
                {tier}
              </button>
            ))}
          </div>
        </div>
      )}

      <div className="flex items-center gap-2">
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30">Sort</p>
        <select value={activeSort} onChange={e => setSort(e.target.value)}
          className="bg-white/[0.05] border border-white/15 text-white/70 text-[10px] rounded-lg px-2 py-1">
          {SORT_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
      </div>
    </div>
  );
}
