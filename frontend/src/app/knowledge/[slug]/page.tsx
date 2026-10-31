import type { Metadata } from 'next';
import { notFound, permanentRedirect } from 'next/navigation';
import { compoundPath, compoundSlug } from '@/lib/compoundSlug';
import { dossierCardAlt, dossierCardUrl, dossierDescription, dossierShareTitle } from '@/lib/dossierMeta';
import { redactDoseText } from '@/lib/doseRedaction';
import { dossierVersionDate, lookupKnowledgeEntry, safeDecodeSlug } from '@/lib/knowledgeServer';
import { createPublicPageMetadata } from '@/lib/site';
import type { KnowledgeEntry } from '@/lib/types';
import { CompoundDossierExperience } from '@/components/knowledge/CompoundDossierExperience';

export const revalidate = 3600;

interface PageProps {
  params: Promise<{ slug: string }>;
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const { entry, missing } = await lookupKnowledgeEntry(slug);

  if (!entry) {
    // A confirmed-missing record is served as a real 404 by the page; keep it out of the index.
    if (missing) return { title: 'Compound not found | BioStack', robots: { index: false, follow: false } };
    return createPublicPageMetadata({
      title: 'Compound Dossier | BioStack',
      description:
        'A public compound dossier from the BioStack evidence library: what the research says, graded by evidence strength, with sources.',
      path: `/knowledge/${encodeURIComponent(safeDecodeSlug(slug))}`,
    });
  }

  return createPublicPageMetadata({
    title: dossierShareTitle(entry),
    description: dossierDescription(entry),
    path: compoundPath(entry.canonicalName),
    image: {
      url: dossierCardUrl(entry),
      width: 1200,
      height: 630,
      alt: dossierCardAlt(entry),
    },
  });
}

function buildStructuredData(entry: KnowledgeEntry, path: string) {
  return {
    '@context': 'https://schema.org',
    '@type': 'WebPage',
    name: `${entry.canonicalName}: what the evidence actually says`,
    url: path,
    description: redactDoseText(entry.mechanismSummary),
    dateModified: dossierVersionDate(entry) ?? undefined,
    isPartOf: {
      '@type': 'WebSite',
      name: 'BioStack',
    },
    about: {
      '@type': 'ChemicalSubstance',
      name: entry.canonicalName,
      alternateName: entry.aliases,
      description: redactDoseText(entry.mechanismSummary),
    },
    citation: entry.sourceReferences,
  };
}

export default async function CompoundDossierPage({ params }: PageProps) {
  const { slug } = await params;
  const { entry, missing } = await lookupKnowledgeEntry(slug);

  // An unknown or non-public slug is a 404, not a 200 page that says "not found". A timeout or API
  // error (missing === false) still renders the client fallback, which retries the lookup.
  if (!entry && missing) notFound();

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
