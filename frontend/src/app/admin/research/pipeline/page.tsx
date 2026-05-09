'use client';
import { useEffect, useState } from 'react';
import { Header } from '@/components/Header';
import { GlassCard } from '@/components/ui/GlassCard';
import { ReadinessBadge } from '@/components/research/ReadinessBadge';
import {
  fetchPromotionManifest,
  fetchImportPreview,
  fetchDryRunReport,
  fetchExportManifest,
} from '@/lib/research/loader';
import type { PromotionManifest, PromotionImportPreview } from '@/lib/research/types';

function SectionHeader({ title, open, toggle }: { title: string; open: boolean; toggle: () => void }) {
  return (
    <button onClick={toggle} className="w-full flex items-center justify-between text-left">
      <h3 className="text-[10px] font-bold uppercase tracking-widest text-white/40">{title}</h3>
      <span className="text-white/30 text-xs">{open ? '▲' : '▼'}</span>
    </button>
  );
}

function ArtifactEmpty({ name }: { name: string }) {
  return (
    <p className="text-sm text-white/30 py-2">
      Artifact not yet generated for this run: <code className="text-white/50">{name}</code>
    </p>
  );
}

export default function PipelinePage() {
  const [manifest, setManifest] = useState<PromotionManifest | null>(null);
  const [importPreview, setImportPreview] = useState<PromotionImportPreview | null>(null);
  const [dryRun, setDryRun] = useState<unknown>(null);
  const [exportManifest, setExportManifest] = useState<unknown>(null);
  const [open, setOpen] = useState({ manifest: true, export: false, import: false, dryRun: false });

  useEffect(() => { load(); }, []);

  async function load() {
    const [m, ip, dr, em] = await Promise.allSettled([
      fetchPromotionManifest(''),
      fetchImportPreview(''),
      fetchDryRunReport(''),
      fetchExportManifest(''),
    ]);
    if (m.status === 'fulfilled') setManifest(m.value);
    if (ip.status === 'fulfilled') setImportPreview(ip.value);
    if (dr.status === 'fulfilled') setDryRun(dr.value);
    if (em.status === 'fulfilled') setExportManifest(em.value);
  }

  function toggle(key: keyof typeof open) {
    setOpen(prev => ({ ...prev, [key]: !prev[key] }));
  }

  const allCandidates = manifest
    ? [...manifest.blocked, ...manifest.reviewRequired, ...manifest.candidatesForPromotion]
    : [];

  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="Promotion Pipeline" subtitle="Export · Import Preview · Dry-Run · Internal" />
      <main className="flex-1 p-6 max-w-5xl mx-auto w-full flex flex-col gap-4">

        <GlassCard className="p-5">
          <SectionHeader title="Promotion Manifest" open={open.manifest} toggle={() => toggle('manifest')} />
          {open.manifest && (
            manifest ? (
              <div className="mt-4 flex flex-col gap-3">
                <div className="flex gap-6 text-[12px]">
                  <span className="text-white/60">Total: <strong className="text-white">{manifest.counts.totalDrafts}</strong></span>
                  <span className="text-rose-400">Blocked: {manifest.counts.blocked}</span>
                  <span className="text-amber-400">Review Req.: {manifest.counts.reviewRequired}</span>
                  <span className="text-emerald-400">Candidates: {manifest.counts.candidatesForPromotion}</span>
                </div>
                <div className="overflow-x-auto">
                  <table className="w-full text-[11px]">
                    <thead>
                      <tr className="text-white/30 text-left border-b border-white/[0.06]">
                        <th className="py-2 pr-4">Name</th>
                        <th className="py-2 pr-4">Classification</th>
                        <th className="py-2 pr-4">Readiness</th>
                        <th className="py-2 pr-4">Evidence Tier</th>
                      </tr>
                    </thead>
                    <tbody>
                      {allCandidates.map(c => (
                        <tr key={c.name} className="border-b border-white/[0.04] hover:bg-white/[0.02]">
                          <td className="py-2 pr-4 text-white/80 font-medium">{c.name}</td>
                          <td className="py-2 pr-4 text-white/50">{c.classification}</td>
                          <td className="py-2 pr-4"><ReadinessBadge readiness={c.readiness} /></td>
                          <td className="py-2 pr-4 text-white/60">{c.overallEvidenceTier}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            ) : <ArtifactEmpty name="promotion-manifest.json" />
          )}
        </GlassCard>

        <GlassCard className="p-5">
          <SectionHeader title="Export Manifest" open={open.export} toggle={() => toggle('export')} />
          {open.export && (
            exportManifest
              ? <pre className="mt-3 text-[10px] text-white/60 bg-black/30 rounded-xl p-3 overflow-x-auto">{JSON.stringify(exportManifest, null, 2)}</pre>
              : <ArtifactEmpty name="promotion-export/promotion-export-manifest.json" />
          )}
        </GlassCard>

        <GlassCard className="p-5">
          <SectionHeader title="Import Preview" open={open.import} toggle={() => toggle('import')} />
          {open.import && (
            importPreview ? (
              <div className="mt-4 flex flex-col gap-3">
                <div className="flex gap-6 text-[12px]">
                  <span className="text-white/60">Exported: {importPreview.counts.totalExported}</span>
                  <span className="text-emerald-400">Create: {importPreview.counts.wouldCreate}</span>
                  <span className="text-blue-400">Update: {importPreview.counts.wouldUpdate}</span>
                  <span className="text-white/40">Skip: {importPreview.counts.wouldSkip}</span>
                </div>
                <table className="w-full text-[11px]">
                  <thead>
                    <tr className="text-white/30 text-left border-b border-white/[0.06]">
                      <th className="py-2 pr-4">Name</th>
                      <th className="py-2 pr-4">Action</th>
                      <th className="py-2 pr-4">Schema Valid</th>
                      <th className="py-2 pr-4">Active</th>
                    </tr>
                  </thead>
                  <tbody>
                    {importPreview.items.map(item => (
                      <tr key={item.name} className="border-b border-white/[0.04]">
                        <td className="py-2 pr-4 text-white/80">{item.name}</td>
                        <td className="py-2 pr-4">
                          <span className={`px-2 py-0.5 rounded-full text-[9px] font-bold uppercase ${
                            item.action === 'create' ? 'bg-emerald-500/15 text-emerald-400'
                            : item.action === 'update' ? 'bg-blue-500/15 text-blue-400'
                            : 'bg-white/5 text-white/30'
                          }`}>{item.action}</span>
                        </td>
                        <td className="py-2 pr-4 text-white/60">{item.schemaValid ? '✓' : '✕'}</td>
                        <td className="py-2 pr-4 text-white/60">{item.isActive ? '✓' : '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : <ArtifactEmpty name="promotion-import-preview.json" />
          )}
        </GlassCard>

        <GlassCard className="p-5">
          <SectionHeader title="Dry-Run Report" open={open.dryRun} toggle={() => toggle('dryRun')} />
          {open.dryRun && (
            dryRun
              ? <pre className="mt-3 text-[10px] text-white/60 bg-black/30 rounded-xl p-3 overflow-x-auto">{JSON.stringify(dryRun, null, 2)}</pre>
              : <ArtifactEmpty name="import-dry-run/promotion-import-dry-run-report.json" />
          )}
        </GlassCard>

      </main>
    </div>
  );
}
