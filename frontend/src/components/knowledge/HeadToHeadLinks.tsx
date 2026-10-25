import Link from 'next/link';
import { compoundSlug } from '@/lib/compoundSlug';
import { curatedPairsFor, vsPath } from '@/lib/vsPairs';

/** Dossier "Head to head" row: curated /vs links only (03 §2). */
export function HeadToHeadLinks({ name }: { name: string }) {
  const slug = compoundSlug(name);
  const pairs = curatedPairsFor(slug);
  if (pairs.length === 0) return null;
  return (
    <nav aria-label="Head to head" className="max-w-3xl mx-auto mt-6 text-sm text-white/70 flex flex-wrap gap-3">
      <span className="text-white/50">Head to head:</span>
      {pairs.map(([a, b]) => {
        const other = a === slug ? b : a;
        return <Link key={other} href={vsPath(a, b)} className="text-cyan-300 underline underline-offset-4">vs {other}</Link>;
      })}
    </nav>
  );
}
