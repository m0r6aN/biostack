import type { Metadata } from 'next';
import { getApiBaseUrl } from '@/lib/apiBase';
import { createPublicPageMetadata } from '@/lib/site';
import type { KnowledgeEntry } from '@/lib/types';
import { CompoundDossierExperience } from '@/components/knowledge/CompoundDossierExperience';

export const revalidate = 3600;

interface PageProps {
  params: Promise<{ slug: string }>;
}

function safeDecodeSlug(value: string): string {
  try {
    return decodeURIComponent(value);
  } catch {
    return value;
  }
}

async function fetchEntry(slug: string): Promise<KnowledgeEntry | null> {
  try {
    const response = await fetch(
      `${getApiBaseUrl()}/api/v1/knowledge/compounds/${encodeURIComponent(safeDecodeSlug(slug))}`,
      {
        next: { revalidate: 3600 },
        signal: AbortSignal.timeout(5000),
      }
    );

    if (!response.ok) {
      return null;
    }

    return (await response.json()) as KnowledgeEntry;
  } catch {
    // Server-side fetch is best-effort; the client experience fetches on its own.
    return null;
  }
}

function truncate(value: string, max: number): string {
  if (value.length <= max) {
    return value;
  }

  return `${value.slice(0, max - 1).trimEnd()}…`;
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const entry = await fetchEntry(slug);
  const path = `/knowledge/${encodeURIComponent(safeDecodeSlug(slug))}` as `/${string}`;

  if (!entry) {
    return createPublicPageMetadata({
      title: 'Compound Dossier | BioStack',
      description:
        'A public compound dossier from the BioStack evidence library: what the research says, graded by evidence strength, with sources.',
      path,
    });
  }

  const description = truncate(
    `${entry.classification} · Evidence tier: ${entry.evidenceTier}. ${entry.mechanismSummary}`,
    155
  );

  return createPublicPageMetadata({
    title: `${entry.canonicalName} Research & Evidence | BioStack`,
    description,
    path,
  });
}

function buildStructuredData(entry: KnowledgeEntry, path: string) {
  return {
    '@context': 'https://schema.org',
    '@type': 'WebPage',
    name: `${entry.canonicalName} Research & Evidence`,
    url: path,
    description: entry.mechanismSummary,
    isPartOf: {
      '@type': 'WebSite',
      name: 'BioStack',
    },
    about: {
      '@type': 'ChemicalSubstance',
      name: entry.canonicalName,
      alternateName: entry.aliases,
      description: entry.mechanismSummary,
    },
    citation: entry.sourceReferences,
  };
}

export default async function CompoundDossierPage({ params }: PageProps) {
  const { slug } = await params;
  const entry = await fetchEntry(slug);
  const path = `/knowledge/${encodeURIComponent(safeDecodeSlug(slug))}`;

  return (
    <>
      {entry ? (
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(buildStructuredData(entry, path)) }}
        />
      ) : null}
      <CompoundDossierExperience slug={slug} initialEntry={entry} />
    </>
  );
}
