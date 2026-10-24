import type { Metadata } from 'next';
import { permanentRedirect } from 'next/navigation';
import { compoundPath, compoundSlug } from '@/lib/compoundSlug';
import { dossierCardUrl, dossierDescription, dossierTitle } from '@/lib/dossierMeta';
import { tierLabel } from '@/lib/evidenceTiers';
import { dossierVersionDate, fetchKnowledgeEntry, safeDecodeSlug } from '@/lib/knowledgeServer';
import { createPublicPageMetadata } from '@/lib/site';
import type { KnowledgeEntry } from '@/lib/types';
import { CompoundDossierExperience } from '@/components/knowledge/CompoundDossierExperience';

export const revalidate = 3600;

interface PageProps {
  params: Promise<{ slug: string }>;
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const entry = await fetchKnowledgeEntry(slug);

  if (!entry) {
    return createPublicPageMetadata({
      title: 'Compound Dossier | BioStack',
      description:
        'A public compound dossier from the BioStack evidence library: what the research says, graded by evidence strength, with sources.',
      path: `/knowledge/${encodeURIComponent(safeDecodeSlug(slug))}`,
    });
  }

  return createPublicPageMetadata({
    title: dossierTitle(entry.canonicalName),
    description: dossierDescription(entry),
    path: compoundPath(entry.canonicalName),
    image: {
      url: dossierCardUrl(entry),
      width: 1200,
      height: 630,
      alt: `${entry.canonicalName}: BioStack evidence grade ${tierLabel(entry.evidenceTier)}`,
    },
  });
}

function buildStructuredData(entry: KnowledgeEntry, path: string) {
  return {
    '@context': 'https://schema.org',
    '@type': 'WebPage',
    name: `${entry.canonicalName}: what the evidence actually says`,
    url: path,
    description: entry.mechanismSummary,
    dateModified: dossierVersionDate(entry) ?? undefined,
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
  const entry = await fetchKnowledgeEntry(slug);

  // One public URL per compound: /knowledge/Retatrutide, /knowledge/LY3437943 (alias) and
  // /knowledge/retatrutide all resolve to the canonical slug.
  if (entry && safeDecodeSlug(slug) !== compoundSlug(entry.canonicalName)) {
    permanentRedirect(compoundPath(entry.canonicalName));
  }

  const path = entry ? compoundPath(entry.canonicalName) : `/knowledge/${encodeURIComponent(safeDecodeSlug(slug))}`;

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
