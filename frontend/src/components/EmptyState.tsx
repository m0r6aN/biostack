import Link from 'next/link';

interface EmptyStateProps {
  title: string;
  description: string;
  icon?: React.ReactNode;
  action?: {
    label: string;
    onClick: () => void;
  };
  /** A secondary, lower-emphasis link — e.g. pointing elsewhere in the app instead of triggering an in-place action. */
  secondaryAction?: {
    label: string;
    href: string;
  };
}

export function EmptyState({ title, description, icon, action, secondaryAction }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-12 px-4">
      {icon && <div className="mb-4 text-4xl opacity-50">{icon}</div>}
      <h3 className="text-lg font-semibold text-white mb-2">{title}</h3>
      <p className="text-sm text-white/50 mb-6 text-center max-w-sm">{description}</p>
      <div className="flex flex-wrap items-center justify-center gap-3">
        {action && (
          <button
            onClick={action.onClick}
            className="px-4 py-2 bg-emerald-500 text-slate-950 hover:bg-emerald-400 rounded-xl text-sm font-medium transition-colors"
          >
            {action.label}
          </button>
        )}
        {secondaryAction && (
          <Link
            href={secondaryAction.href}
            className="inline-flex min-h-11 items-center px-4 py-2 rounded-xl border border-white/10 text-sm font-medium text-white/70 hover:bg-white/5 hover:text-white transition-colors"
          >
            {secondaryAction.label}
          </Link>
        )}
      </div>
    </div>
  );
}
