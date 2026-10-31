import CompoundDossierPage, { generateMetadata } from '@/app/knowledge/[slug]/page';
import { beforeEach, describe, expect, it, vi } from 'vitest';

// An unknown or non-public compound slug must be a real HTTP 404 (not a 200 page that says "not
// found"), while a transient API failure must still render the client fallback that retries.

const lookup = vi.hoisted(() => vi.fn());

vi.mock('next/navigation', () => ({
  notFound: () => {
    throw new Error('NEXT_NOT_FOUND');
  },
  permanentRedirect: (path: string) => {
    throw new Error(`NEXT_REDIRECT:${path}`);
  },
}));
vi.mock('@/lib/knowledgeServer', async () => {
  const actual = await vi.importActual<typeof import('@/lib/knowledgeServer')>('@/lib/knowledgeServer');
  return { ...actual, lookupKnowledgeEntry: lookup };
});
vi.mock('@/components/knowledge/CompoundDossierExperience', () => ({
  CompoundDossierExperience: () => null,
}));

const params = Promise.resolve({ slug: 'not-a-real-compound' });

describe('/knowledge/[slug] for a slug the API does not know', () => {
  beforeEach(() => lookup.mockReset());

  it('calls notFound() when the API answered 404', async () => {
    lookup.mockResolvedValue({ entry: null, missing: true });
    await expect(CompoundDossierPage({ params })).rejects.toThrow('NEXT_NOT_FOUND');
  });

  it('keeps a confirmed-missing slug out of the search index', async () => {
    lookup.mockResolvedValue({ entry: null, missing: true });
    const metadata = await generateMetadata({ params });
    expect(metadata.robots).toEqual({ index: false, follow: false });
  });

  it('still renders the client fallback when the API is unreachable', async () => {
    lookup.mockResolvedValue({ entry: null, missing: false });
    await expect(CompoundDossierPage({ params })).resolves.toBeTruthy();
    const metadata = await generateMetadata({ params });
    expect(metadata.robots).toEqual({ index: true, follow: true });
  });
});
