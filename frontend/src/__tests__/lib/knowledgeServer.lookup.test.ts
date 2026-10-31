import { fetchKnowledgeEntry, lookupKnowledgeEntry } from '@/lib/knowledgeServer';
import { afterEach, describe, expect, it, vi } from 'vitest';

const fetchMock = vi.fn();
vi.stubGlobal('fetch', fetchMock);

afterEach(() => fetchMock.mockReset());

describe('lookupKnowledgeEntry', () => {
  it('returns the entry on 200', async () => {
    fetchMock.mockResolvedValue({ ok: true, status: 200, json: async () => ({ canonicalName: 'Semaglutide' }) });
    expect(await lookupKnowledgeEntry('semaglutide')).toEqual({ entry: { canonicalName: 'Semaglutide' }, missing: false });
  });

  it('marks the record missing only on a 404 (unknown or non-public slug)', async () => {
    fetchMock.mockResolvedValue({ ok: false, status: 404 });
    expect(await lookupKnowledgeEntry('nope')).toEqual({ entry: null, missing: true });
  });

  it('does not call a record missing when the API errors or times out', async () => {
    fetchMock.mockResolvedValue({ ok: false, status: 503 });
    expect(await lookupKnowledgeEntry('semaglutide')).toEqual({ entry: null, missing: false });

    fetchMock.mockRejectedValue(new Error('timeout'));
    expect(await lookupKnowledgeEntry('semaglutide')).toEqual({ entry: null, missing: false });
  });

  it('keeps fetchKnowledgeEntry returning the entry or null', async () => {
    fetchMock.mockResolvedValue({ ok: false, status: 404 });
    expect(await fetchKnowledgeEntry('nope')).toBeNull();
  });
});
