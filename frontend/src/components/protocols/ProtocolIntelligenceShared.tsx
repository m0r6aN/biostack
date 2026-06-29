export function UnknownPanel({ title, question }: { title: string; question: string }) {
  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#101820]/95 p-5">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/35">{title}</p>
      <h3 className="mt-2 text-lg font-semibold text-white">{question}</h3>
      <div className="mt-4 rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
        <p className="text-sm font-semibold text-white">Unknown</p>
        <p className="mt-1 text-sm text-white/55">No reviewed artifact exists. BioStack will not show fake confidence.</p>
      </div>
    </section>
  );
}

export function EvidenceMeta({
  evidenceTier,
  confidence,
  sourceRefsCount,
  reviewStatus,
  userFacingBoundary,
}: {
  evidenceTier: string;
  confidence: string;
  sourceRefsCount: number;
  reviewStatus: string;
  userFacingBoundary: string;
}) {
  return (
    <div className="mt-3 grid gap-2 text-xs text-white/55 sm:grid-cols-2 lg:grid-cols-5">
      <span>Evidence tier: {evidenceTier}</span>
      <span>Confidence: {confidence}</span>
      <span>Source refs: {sourceRefsCount}</span>
      <span>Review status: {reviewStatus}</span>
      <span>Boundary: {userFacingBoundary}</span>
    </div>
  );
}
