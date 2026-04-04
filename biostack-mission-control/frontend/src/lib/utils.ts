export function cn(...classes: (string | undefined | null | false)[]) {
  return classes
    .filter(Boolean)
    .join(' ')
    .trim();
}

export function formatDate(dateString: string): string {
  try {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  } catch {
    return dateString;
  }
}

export function formatDateTime(dateString: string): string {
  try {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return dateString;
  }
}

export function getStatusColor(status: string): string {
  switch (status) {
    case 'Active':
      return 'bg-emerald-500/10 text-emerald-300 border border-emerald-400/20';
    case 'Completed':
      return 'bg-blue-500/10 text-blue-300 border border-blue-400/20';
    case 'Paused':
      return 'bg-amber-500/10 text-amber-300 border border-amber-400/20';
    default:
      return 'bg-white/10 text-white/70 border border-white/15';
  }
}

export function getEvidenceTierColor(tier: string): string {
  switch (tier) {
    case 'strong':
      return 'bg-emerald-500/10 text-emerald-300 border border-emerald-400/20';
    case 'moderate':
      return 'bg-blue-500/10 text-blue-300 border border-blue-400/20';
    case 'limited':
      return 'bg-amber-500/10 text-amber-300 border border-amber-400/20';
    case 'theoretical':
      return 'bg-white/10 text-white/50 border border-white/15';
    default:
      return 'bg-white/10 text-white/70 border border-white/15';
  }
}

export function getEventIcon(eventType: string): string {
  switch (eventType) {
    case 'compound_added':
      return '➕';
    case 'compound_ended':
      return '🛑';
    case 'phase_started':
      return '🚀';
    case 'phase_ended':
      return '✓';
    case 'check_in':
      return '📊';
    case 'knowledge_update':
      return '📚';
    default:
      return '📌';
  }
}

// ─── Weight unit conversion ───────────────────────────────────────────────────

const KG_TO_LBS = 2.20462;

/** Display a kg value in the user's preferred unit, with unit label. */
export function formatWeight(kg: number, unit: 'metric' | 'imperial'): string {
  if (unit === 'imperial') {
    return `${(kg * KG_TO_LBS).toFixed(1)} lbs`;
  }
  return `${kg} kg`;
}

/** Convert a lbs input value to kg for storage. */
export function lbsToKg(lbs: number): number {
  return parseFloat((lbs / KG_TO_LBS).toFixed(2));
}

/** Convert a kg value to lbs for display in form inputs. */
export function kgToLbs(kg: number): number {
  return parseFloat((kg * KG_TO_LBS).toFixed(1));
}

export function daysAgo(dateString: string): string {
  try {
    const date = new Date(dateString);
    const now = new Date();
    const diffTime = Math.abs(now.getTime() - date.getTime());
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return '1 day ago';
    if (diffDays < 7) return `${diffDays} days ago`;
    if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`;
    return `${Math.floor(diffDays / 30)} months ago`;
  } catch {
    return dateString;
  }
}
