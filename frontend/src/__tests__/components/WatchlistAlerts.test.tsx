import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const authState: { user: unknown; loading: boolean } = { user: null, loading: false };
vi.mock('@/lib/AuthProvider', () => ({ useOptionalAuth: () => authState }));

import { WatchButton } from '@/components/alerts/WatchButton';
import { UnsubscribeConfirm } from '@/components/alerts/UnsubscribeConfirm';
import { noticeLine } from '@/components/alerts/AlertSettings';
import { changelogRows } from '@/lib/changelog';
import { watchTargetPath } from '@/lib/watchlist';
import type { KnowledgeEntry } from '@/lib/types';

const fetchMock = vi.fn();
beforeEach(() => { fetchMock.mockReset(); vi.stubGlobal('fetch', fetchMock); });
afterEach(() => { vi.unstubAllGlobals(); authState.user = null; });
const json = (body: unknown, status = 200) => Promise.resolve(new Response(status === 204 ? null : JSON.stringify(body), { status }));

describe('WatchButton', () => {
  it('asks signed-out visitors to sign in, returning to the page', () => {
    render(<WatchButton targetType="compound" targetId="bpc-157" returnTo="/knowledge/bpc-157" />);
    expect(screen.getByRole('link', { name: 'Sign in to watch' })).toHaveAttribute('href', '/auth/signin?callbackUrl=%2Fknowledge%2Fbpc-157');
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('watches with one tap and unwatches with a second', async () => {
    authState.user = { id: 'u1' };
    fetchMock
      .mockImplementationOnce(() => json([]))
      .mockImplementationOnce(() => json({ id: 'w1', targetType: 'compound', targetId: 'bpc-157', createdAtUtc: '' }, 201))
      .mockImplementationOnce(() => json(null, 204));
    render(<WatchButton targetType="compound" targetId="BPC-157" returnTo="/knowledge/bpc-157" />);
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
    fireEvent.click(screen.getByRole('button', { name: 'Watch for changes' }));
    await screen.findByRole('button', { name: 'Watching' });
    expect(JSON.parse(fetchMock.mock.calls[1][1].body)).toEqual({ targetType: 'compound', targetId: 'bpc-157' });
    fireEvent.click(screen.getByRole('button', { name: 'Watching' }));
    await screen.findByRole('button', { name: 'Watch for changes' });
    expect(fetchMock.mock.calls[2][0]).toBe('/api/v1/watchlist/w1');
    expect(fetchMock.mock.calls[2][1].method).toBe('DELETE');
  });
});

describe('UnsubscribeConfirm', () => {
  it('only unsubscribes on an explicit click (link scanners prefetch GETs)', async () => {
    fetchMock.mockImplementationOnce(() => json(null, 204));
    render(<UnsubscribeConfirm token="abc123abc123abc123" />);
    expect(fetchMock).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Stop alert emails' }));
    await screen.findByText(/You won.t get alert emails/);
    expect(fetchMock.mock.calls[0][0]).toBe('/api/v1/alerts/unsubscribe/abc123abc123abc123');
    expect(fetchMock.mock.calls[0][1].method).toBe('POST');
  });
});

describe('changelog and notices', () => {
  const entry = {
    canonicalName: 'Retatrutide',
    details: { publicClaims: [{
      id: 'c1', statement: 'Weight loss at 12 mg', changelog: [
        { version: 1, kind: 'new', verdict: 'THIN', reason: 'first', publishedAt: '2026-09-01T00:00:00Z', reviewers: ['a/m1', 'b/m2'] },
        { version: 2, kind: 'regrade', verdict: 'PROMISING', reason: 'Phase 3 at 8 mg reported.', publishedAt: '2026-11-02T00:00:00Z', reviewers: [] },
        { version: 3, kind: 'regrade', verdict: 'MYTH', reason: 'draft', publishedAt: null, reviewers: [] },
      ],
    }] },
  } as unknown as KnowledgeEntry;

  it('lists published versions newest first with doses redacted', () => {
    const rows = changelogRows(entry);
    expect(rows.map(r => r.version)).toEqual([2, 1]);
    expect(JSON.stringify(rows)).not.toMatch(/\d\s?mg/);
    expect(rows[1].reviewers).toEqual(['a/m1', 'b/m2']);
  });

  it('writes notice lines and target paths', () => {
    expect(noticeLine({ id: '1', alertType: 'verdict-change', compoundSlug: 'r', claimId: 'c', version: 2, oldVerdict: 'THIN', newVerdict: 'PROMISING', reason: 'Trial.', createdAtUtc: '' }))
      .toBe('Verdict moved: THIN → PROMISING. Trial.');
    expect(watchTargetPath('vs', 'bpc-157/tb-500')).toBe('/vs/bpc-157/tb-500');
    expect(watchTargetPath('claim', 'c1')).toBe('/c/c1');
  });
});
