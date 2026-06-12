import Link from 'next/link';
import { StackIntelligencePanel } from './StackIntelligencePanel';

export function IntelligenceProofSection({ compact = false }: { compact?: boolean }) {
  return (
    <section className="border-y border-white/8 bg-black/15">
      <div className="mx-auto grid max-w-7xl gap-7 px-5 py-10 sm:px-8 lg:grid-cols-[0.82fr_1.18fr] lg:items-center lg:py-14">
        <div>
          <h2 className="text-3xl font-semibold tracking-tight text-white sm:text-4xl">
            See what BioStack catches
          </h2>
          <p className="mt-4 text-base leading-7 text-white/62">
            Paste a stack and BioStack turns raw compound names into structured context: parsed items,
            relationship checks, evidence-aware previews, and a clear upgrade path when deeper analysis is useful.
          </p>
          {!compact && (
            <div className="mt-5 flex flex-wrap gap-3">
              <Link
                href="/tools/analyzer"
                className="rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
              >
                Analyze a stack
              </Link>
              <Link
                href="/pricing"
                className="rounded-lg border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
              >
                See what Operator unlocks
              </Link>
            </div>
          )}
        </div>

        <StackIntelligencePanel
          compoundNames={['BPC-157', 'TB-500']}
          relationshipCandidates={[
            {
              type: 'overlap',
              label: 'BPC-157 + TB-500',
              detail: 'tissue-repair overlap: educational reference only, with full evidence detail in Operator.',
            },
          ]}
        />
      </div>
    </section>
  );
}
