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
  /** Heading level for the title. Defaults to 2 because empty states sit directly under the page's h1. */
  headingLevel?: 2 | 3;
}

export function EmptyState({ title, description, icon, action, secondaryAction, headingLevel = 2 }: EmptyStateProps) {
  const Heading = headingLevel === 3 ? 'h3' : 'h2';
  return (
    <div className="flex flex-col items-center justify-center py-12 px-4">
      {icon && <div className="mb-4 text-4xl opacity-50">{icon}</div>}
      <Heading className="text-lg font-semibold text-white mb-2">{title}</Heading>
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
