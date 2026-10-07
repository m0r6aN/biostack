'use client';

import { usePathname } from 'next/navigation';
import { ResearchConsoleLocalOnly } from '@/components/research/ResearchConsoleLocalOnly';

// Every surface under /admin/research reads dev-only /api/research artifact
// routes except staged-reviews, which queries live admin APIs. Gate the
// artifact-based pages as a group in production instead of letting each one
// fail against the dev-only routes.
export default function ResearchLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const isLiveSurface = pathname.startsWith('/admin/research/staged-reviews');

  if (process.env.NODE_ENV === 'production' && !isLiveSurface) {
    return <ResearchConsoleLocalOnly />;
  }

  return <>{children}</>;
}
