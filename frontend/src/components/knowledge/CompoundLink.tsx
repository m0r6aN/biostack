'use client';

import Link from 'next/link';
import { cn } from '@/lib/utils';

export interface CompoundLinkProps {
  displayName: string;
  slug?: string;
  className?: string;
}

export function CompoundLink({ displayName, slug, className }: CompoundLinkProps) {
  const resolvedSlug = slug ?? displayName.toLowerCase().replace(/\s+/g, '-');

  return (
    <Link
      href={`/knowledge/${resolvedSlug}`}
      className={cn(
        'hover:underline underline-offset-2 decoration-white/30 transition-colors',
        className,
      )}
    >
      {displayName}
    </Link>
  );
}
