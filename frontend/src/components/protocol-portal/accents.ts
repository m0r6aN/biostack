import type { ProtocolAccent } from '@/lib/types';

/**
 * Dark-theme accent token classes for the protocol portal, aligned with the
 * existing emerald/blue/amber/violet/red palette used across the app.
 */
export interface AccentClasses {
  text: string;
  bg: string;
  border: string;
  dot: string;
}

export const ACCENT_CLASSES: Record<ProtocolAccent, AccentClasses> = {
  emerald: {
    text: 'text-emerald-300',
    bg: 'bg-emerald-500/10',
    border: 'border-emerald-400/20',
    dot: 'bg-emerald-400',
  },
  blue: {
    text: 'text-blue-300',
    bg: 'bg-blue-500/10',
    border: 'border-blue-400/20',
    dot: 'bg-blue-400',
  },
  violet: {
    text: 'text-violet-300',
    bg: 'bg-violet-500/10',
    border: 'border-violet-400/20',
    dot: 'bg-violet-400',
  },
  amber: {
    text: 'text-amber-300',
    bg: 'bg-amber-500/10',
    border: 'border-amber-400/20',
    dot: 'bg-amber-400',
  },
  red: {
    text: 'text-red-300',
    bg: 'bg-red-500/10',
    border: 'border-red-400/20',
    dot: 'bg-red-400',
  },
  neutral: {
    text: 'text-white/70',
    bg: 'bg-white/[0.04]',
    border: 'border-white/10',
    dot: 'bg-white/40',
  },
};

export function accentClasses(accent: ProtocolAccent = 'neutral'): AccentClasses {
  return ACCENT_CLASSES[accent] ?? ACCENT_CLASSES.neutral;
}
