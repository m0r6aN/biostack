import type { CommunitySignal } from '@/lib/research/types';

interface CommunitySignalBadgeProps {
  signal: CommunitySignal;
}

const strengthClasses: Record<string, string> = {
  none:       'bg-white/[0.05] text-white/40 border-white/10',
  isolated:   'bg-blue-500/10 text-blue-300 border-blue-400/20',
  recurring:  'bg-violet-500/15 text-violet-300 border-violet-400/25',
  widespread: 'bg-fuchsia-500/15 text-fuchsia-300 border-fuchsia-400/25',
};

function normalize(value: string | undefined | null): string {
  if (!value) return '';
  return value.toString().replace(/[-_]/g, '').toLowerCase();
}

function kebabToTitle(value: string | undefined | null): string {
  if (!value) return '';
  // Convert PascalCase OR kebab-case to "Title Case"
  const withSpaces = value
    .replace(/[-_]/g, ' ')
    .replace(/([a-z])([A-Z])/g, '$1 $2');
  return withSpaces.charAt(0).toUpperCase() + withSpaces.slice(1).toLowerCase();
}

export function CommunitySignalBadge({ signal }: CommunitySignalBadgeProps) {
  const strengthKey = normalize(signal.signalStrength);
  const classes = strengthClasses[strengthKey] ?? 'bg-white/[0.05] text-white/40 border-white/10';
  const strengthLabel = kebabToTitle(signal.signalStrength) || 'Unknown';

  const truthStatus = signal.canonicalTruthStatus
    ? `Canonical truth: ${kebabToTitle(signal.canonicalTruthStatus)}`
    : undefined;

  return (
    <span
      title={truthStatus}
      className={`inline-flex items-center gap-1 text-[10px] font-semibold tracking-wide uppercase px-2 py-1 rounded-full border ${classes}`}
    >
      <span aria-hidden="true">◈</span>
      Community signal · {strengthLabel}
    </span>
  );
}
