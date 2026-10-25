import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { CommunityColumns } from '@/components/knowledge/CommunityColumns';
import { COMMUNITY_EMPTY, fetchCommunityThemes, normalizeCommunityThemes } from '@/lib/communityThemes';
import type { KnowledgeEntry } from '@/lib/types';

const entry = {
  canonicalName: 'BPC-157',
  details: {
    studiedUses: [{ useCase: 'Tendon healing', labelStatus: '', population: '', summary: '', evidenceTier: '', sources: [] }],
    safety: { adverseEffectsCommon: ['Nausea'], adverseEffectsSerious: [], contraindications: [], warnings: [], precautions: [], monitoring: [] },
  },
} as unknown as KnowledgeEntry;

describe('normalizeCommunityThemes', () => {
  it('hides N unless at least one theme crosses k, and drops numeric labels', () => {
    expect(normalizeCommunityThemes({ submissions: 4, sideEffects: [], benefits: [] }, 'x').submissions).toBeNull();
    const t = normalizeCommunityThemes({ submissions: 12, minCount: 10, sideEffects: [{ id: 'nausea', label: 'Nausea' }, { id: 'x', label: '500 mcg did it' }], benefits: [] }, 'x');
    expect(t.submissions).toBe(12);
    expect(t.sideEffects).toEqual([{ id: 'nausea', label: 'Nausea' }]);
    expect(normalizeCommunityThemes({ submissions: 3, minCount: 1, sideEffects: [{ id: 'a', label: 'A' }] }, 'x').submissions).toBeNull();
  });

  it('collapses fetch errors to the empty state', async () => {
    const failing = vi.fn().mockRejectedValue(new Error('down')) as unknown as typeof fetch;
    expect((await fetchCommunityThemes('bpc-157', failing)).submissions).toBeNull();
  });
});

describe('CommunityColumns', () => {
  it('renders the empty state, never single stories', () => {
    render(<CommunityColumns entry={entry} initialThemes={normalizeCommunityThemes({}, 'bpc-157')} />);
    expect(screen.getByText(COMMUNITY_EMPTY)).toBeInTheDocument();
    expect(screen.getByText('Submit a claim you saw').getAttribute('href')).toBe('/submit?compound=bpc-157');
    expect(screen.getByTestId('community-columns').hasAttribute('open')).toBe(false);
  });

  it('shows side effects before benefits, the N line and the unverified note; separate anecdote column', () => {
    const themes = normalizeCommunityThemes({ submissions: 23, sideEffects: [{ id: 'nausea', label: 'Nausea' }], benefits: [{ id: 'less-pain', label: 'Less pain' }] }, 'bpc-157');
    const { container } = render(<CommunityColumns entry={entry} initialThemes={themes} />);
    const html = container.innerHTML;
    expect(html.indexOf('Side effects reported')).toBeLessThan(html.indexOf('Benefits reported'));
    expect(screen.getByTestId('community-side-effects').className).toBe(screen.getByTestId('community-benefits').className);
    expect(screen.getByText('Based on 23 submissions.')).toBeInTheDocument();
    expect(screen.getByText('Unverified. Unknown product identity and purity.')).toBeInTheDocument();
    expect(screen.getByText('ANECDOTE')).toBeInTheDocument();
    expect(screen.getByText('What people report: anecdote, not evidence')).toBeInTheDocument();
    expect(html).not.toMatch(/%|\d+(\.\d+)?\s?(mg|mcg|µg|iu|IU|units|mL|ml)\b/);
  });

  it('fetches themes for the compound when none are passed', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({ submissions: 11, sideEffects: [{ id: 'headache', label: 'Headache' }], benefits: [] }) });
    vi.stubGlobal('fetch', fetchMock);
    render(<CommunityColumns entry={entry} />);
    await waitFor(() => expect(screen.getByText('Headache')).toBeInTheDocument());
    expect(fetchMock.mock.calls[0][0]).toBe('/api/v1/community/bpc-157/themes');
    vi.unstubAllGlobals();
  });
});
