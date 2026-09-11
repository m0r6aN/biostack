'use client';

import { useAuth } from '@/lib/AuthProvider';
import type { ComponentPropsWithoutRef } from 'react';

type KnowledgeMainProps = ComponentPropsWithoutRef<'div'>;

// Knowledge-route content landmark. AppShell owns `main#main` on /knowledge routes
// only when a user is present (`isAppRoute` requires `Boolean(user)` there); with no
// user — anonymous or still loading — it passes the page through untouched, so the
// page must own the skip-link target itself. Mirror that exact rule so each knowledge
// route has one `main#main` and no nested mains across the anonymous-to-user handoff.
export function KnowledgeMain({ children, ...props }: KnowledgeMainProps) {
  const { user } = useAuth();

  if (user) {
    return <div {...props}>{children}</div>;
  }

  return (
    <main id="main" tabIndex={-1} {...props}>
      {children}
    </main>
  );
}
