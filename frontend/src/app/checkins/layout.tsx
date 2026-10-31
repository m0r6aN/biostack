import type { Metadata } from 'next';
import type { ReactNode } from 'react';

// WCAG 2.4.2 (Page Titled): this route renders a client component, which cannot export metadata, so the
// distinct, descriptive document title lives in the route layout. Account and app routes stay noindex via the root metadata.
export const metadata: Metadata = { title: 'Check-ins | BioStack' };

export default function Layout({ children }: { children: ReactNode }) {
  return children;
}
