import Link from 'next/link';
import { Header } from '@/components/Header';

/**
 * Production state for the research console. The console reads compound-pipeline
 * artifacts (research summary, promotion manifest, review queues, import previews,
 * dry-run reports) from workspace files served by dev-only /api/research routes —
 * those artifacts are produced by local pipeline runs and are not part of a
 * deployment.
 */
export function ResearchConsoleLocalOnly() {
  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="Research Runs" subtitle="Compound Pipeline Review · Internal" />
      <main className="flex-1 p-6 max-w-3xl mx-auto w-full space-y-3">
        <h2 className="text-lg font-semibold text-white">Local pipeline console</h2>
        <p className="text-sm text-white/55">
          This console reads compound-pipeline artifacts — research summary, promotion manifest,
          review queues, import previews and dry-run reports — produced by local pipeline runs
          from workspace files. Those artifact files are not part of this deployment, so the
          console is available in local development only.
        </p>
        <p className="text-sm text-white/55">
          Database-backed review tools stay live:{' '}
          <Link href="/admin/research/staged-reviews" className="text-emerald-400 hover:text-emerald-300">
            staged transcript candidate reviews →
          </Link>
        </p>
      </main>
    </div>
  );
}
