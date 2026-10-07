import { createPublicPageMetadata } from '@/lib/site';

export const metadata = createPublicPageMetadata({
  title: 'Evidence Library | BioStack',
  description: 'Explore observational compound evidence, pathways, and review-first overlap signals.',
  path: '/knowledge',
});

export default function KnowledgeLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return children;
}
