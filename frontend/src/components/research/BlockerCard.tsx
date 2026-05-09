interface BlockerCardProps {
  blocker: string;
}

export function BlockerCard({ blocker }: BlockerCardProps) {
  const isHard = blocker.startsWith('blocked:');
  return (
    <div
      className={`flex gap-3 items-start rounded-xl border px-3 py-2.5 ${
        isHard
          ? 'bg-rose-500/8 border-rose-400/25'
          : 'bg-amber-500/8 border-amber-400/25'
      }`}
    >
      <span
        className={`text-xs mt-0.5 flex-shrink-0 font-bold ${
          isHard ? 'text-rose-400' : 'text-amber-400'
        }`}
      >
        {isHard ? '✕' : '⚠'}
      </span>
      <span
        className={`text-xs leading-relaxed ${
          isHard ? 'text-rose-300' : 'text-amber-300'
        }`}
      >
        {blocker}
      </span>
    </div>
  );
}
