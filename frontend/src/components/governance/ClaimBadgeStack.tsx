import {
  Shield,
  AlertTriangle,
  FlaskConical,
  Scale,
  Ban,
  UserCheck,
  MessageSquare,
  CheckCircle2,
} from 'lucide-react';
import { cn } from '@/lib/utils';

export type ClaimBadge =
  | 'source-backed'
  | 'review-required'
  | 'limited-evidence'
  | 'regulatory-boundary'
  | 'not-executable'
  | 'provider-review-elevated'
  | 'commentary-only'
  | 'receipt-verified';

interface ClaimBadgeStackProps {
  badges: ClaimBadge[];
  size?: 'xs' | 'sm';
  className?: string;
}

const BADGE_CONFIG: Record<ClaimBadge, {
  icon: React.ElementType;
  label: string;
  text: string;
  bg: string;
  border: string;
}> = {
  'source-backed': {
    icon: Shield,
    label: 'Source-backed',
    text: 'text-emerald-400',
    bg: 'bg-emerald-400/10',
    border: 'border-emerald-400/20',
  },
  'review-required': {
    icon: AlertTriangle,
    label: 'Review-required',
    text: 'text-amber-400',
    bg: 'bg-amber-400/10',
    border: 'border-amber-400/20',
  },
  'limited-evidence': {
    icon: FlaskConical,
    label: 'Limited-evidence',
    text: 'text-yellow-400',
    bg: 'bg-yellow-400/10',
    border: 'border-yellow-400/20',
  },
  'regulatory-boundary': {
    icon: Scale,
    label: 'Regulatory-boundary',
    text: 'text-purple-400',
    bg: 'bg-purple-400/10',
    border: 'border-purple-400/20',
  },
  'not-executable': {
    icon: Ban,
    label: 'Not-executable',
    text: 'text-white/40',
    bg: 'bg-white/5',
    border: 'border-white/10',
  },
  'provider-review-elevated': {
    icon: UserCheck,
    label: 'Provider-review-elevated',
    text: 'text-orange-400',
    bg: 'bg-orange-400/10',
    border: 'border-orange-400/20',
  },
  'commentary-only': {
    icon: MessageSquare,
    label: 'Commentary-only',
    text: 'text-blue-400',
    bg: 'bg-blue-400/10',
    border: 'border-blue-400/20',
  },
  'receipt-verified': {
    icon: CheckCircle2,
    label: 'Receipt-verified',
    text: 'text-teal-400',
    bg: 'bg-teal-400/10',
    border: 'border-teal-400/20',
  },
};

export function ClaimBadgeStack({ badges, size = 'sm', className }: ClaimBadgeStackProps) {
  return (
    <div className={cn('flex flex-wrap gap-1', className)}>
      {badges.map((badge) => {
        const { icon: Icon, label, text, bg, border } = BADGE_CONFIG[badge];
        return (
          <span
            key={badge}
            title={label}
            className={cn(
              'inline-flex items-center gap-1 rounded border px-1.5 py-0.5 text-[10px] font-medium',
              text,
              bg,
              border,
            )}
          >
            <Icon className="w-2.5 h-2.5 shrink-0" />
            {size === 'sm' && label}
          </span>
        );
      })}
    </div>
  );
}
