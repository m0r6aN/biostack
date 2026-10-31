import { CatalogLookupPanel, CatalogOffers, LOOKUP_POLL_DELAYS_MS } from '@/components/catalog/CatalogLookupOffers';
import { apiClient } from '@/lib/api';
import type { CatalogLookup } from '@/lib/types';
import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/api', () => ({
  apiClient: { getCatalogLookup: vi.fn(), linkCatalogLookup: vi.fn(), declineCatalogLookup: vi.fn() },
}));
vi.mock('next/link', () => ({ default: ({ href, children }: { href: string; children: React.ReactNode }) => <a href={href}>{children}</a> }));

const lookup = (status: string, candidates: CatalogLookup['candidates'] = []): CatalogLookup => ({
  id: 'lookup-1', profileId: 'profile-1', compoundId: 'item-1', queryName: 'Ozempic', status, candidates,
  createdAtUtc: '2026-10-30T10:00:00Z', completedAtUtc: status === 'pending' ? null : '2026-10-30T10:00:01Z',
});
const sameAs = { slug: 'semaglutide', name: 'Semaglutide', kind: 'alias', label: 'same_as', reason: 'alternate name' };
const similar = { slug: 'creatine', name: 'Creatine', kind: 'fuzzy', label: 'similar', reason: 'close spelling' };

beforeEach(() => vi.resetAllMocks());
afterEach(() => vi.useRealTimers());

describe('CatalogOffers', () => {
  it('labels an exact or alternate-name match "Same as" and anything close "Similar", and links nothing by itself', async () => {
    const onLink = vi.fn();
    const onDecline = vi.fn();
    const user = userEvent.setup();
    render(<CatalogOffers itemName="Ozempic" candidates={[sameAs, similar]} busy={false} onLink={onLink} onDecline={onDecline} />);

    expect(screen.getByText(/^Same as/)).toHaveTextContent('Same as Semaglutide?');
    expect(screen.getByText(/^Similar/)).toHaveTextContent('Similar Creatine?');
    expect(onLink).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: /^Link it: link Ozempic to Creatine$/ }));
    expect(onLink).toHaveBeenCalledWith('creatine');
    await user.click(screen.getByRole('button', { name: /^Not the same: Ozempic is not the same as the suggestion$/ }));
    expect(onDecline).toHaveBeenCalledTimes(1);
  });

  it('hides an offer the card already shows, and renders nothing when none is left', () => {
    const { container } = render(<CatalogOffers itemName="Ozempic" candidates={[sameAs]} busy={false} onLink={vi.fn()} onDecline={vi.fn()} hideSlugs={['semaglutide']} />);
    expect(container).toBeEmptyDOMElement();
  });

  it('disables the actions while a change is saving', () => {
    render(<CatalogOffers itemName="Ozempic" candidates={[sameAs]} busy onLink={vi.fn()} onDecline={vi.fn()} />);
    expect(screen.getByRole('button', { name: /^Link it: link Ozempic to Semaglutide$/ })).toBeDisabled();
    expect(screen.getByRole('button', { name: /^Not the same: Ozempic is not the same as the suggestion$/ })).toBeDisabled();
  });
});

describe('CatalogLookupPanel', () => {
  it('says to wait, polls with backoff, then shows the offers without linking them', async () => {
    vi.useFakeTimers();
    vi.mocked(apiClient.getCatalogLookup)
      .mockResolvedValueOnce(lookup('pending'))
      .mockResolvedValueOnce(lookup('pending'))
      .mockResolvedValueOnce(lookup('matched', [sameAs]));
    render(<CatalogLookupPanel lookupId="lookup-1" itemName="Ozempic" />);
    await act(async () => { await vi.advanceTimersByTimeAsync(0); });
    expect(screen.getByRole('status')).toHaveTextContent('Please wait while we look this up');

    await act(async () => { await vi.advanceTimersByTimeAsync(LOOKUP_POLL_DELAYS_MS[0]); });
    expect(apiClient.getCatalogLookup).toHaveBeenCalledTimes(2);
    await act(async () => { await vi.advanceTimersByTimeAsync(LOOKUP_POLL_DELAYS_MS[1]); });

    expect(apiClient.getCatalogLookup).toHaveBeenCalledTimes(3);
    expect(screen.getByText(/Same as/)).toHaveTextContent('Same as Semaglutide?');
    expect(apiClient.linkCatalogLookup).not.toHaveBeenCalled();
    await act(async () => { await vi.advanceTimersByTimeAsync(20000); });
    expect(apiClient.getCatalogLookup).toHaveBeenCalledTimes(3);   // stops polling once answered
  });

  it('stops polling after the short window and says a note will follow', async () => {
    vi.useFakeTimers();
    vi.mocked(apiClient.getCatalogLookup).mockResolvedValue(lookup('pending'));
    render(<CatalogLookupPanel lookupId="lookup-1" itemName="Ozempic" />);
    await act(async () => { await vi.advanceTimersByTimeAsync(60000); });

    expect(apiClient.getCatalogLookup).toHaveBeenCalledTimes(LOOKUP_POLL_DELAYS_MS.length + 1);
    expect(screen.getByRole('status')).toHaveTextContent('We\u2019ll leave a note here if we find a match');
  });

  it('links only on the person\'s click, then reports it and tells the parent', async () => {
    vi.mocked(apiClient.getCatalogLookup).mockResolvedValue(lookup('matched', [sameAs]));
    vi.mocked(apiClient.linkCatalogLookup).mockResolvedValue(lookup('linked', [sameAs]));
    const onSettled = vi.fn();
    const user = userEvent.setup();
    render(<CatalogLookupPanel lookupId="lookup-1" itemName="Ozempic" onSettled={onSettled} />);

    await user.click(await screen.findByRole('button', { name: /^Link it: link Ozempic to Semaglutide$/ }));

    expect(apiClient.linkCatalogLookup).toHaveBeenCalledWith('lookup-1', 'semaglutide');
    expect(await screen.findByRole('status')).toHaveTextContent('Linked to Semaglutide.');
    expect(onSettled).toHaveBeenCalledWith('linked');
  });

  it('keeps the item as entered when the person says it is not the same', async () => {
    vi.mocked(apiClient.getCatalogLookup).mockResolvedValue(lookup('matched', [similar]));
    vi.mocked(apiClient.declineCatalogLookup).mockResolvedValue(lookup('declined', [similar]));
    const onSettled = vi.fn();
    const user = userEvent.setup();
    render(<CatalogLookupPanel lookupId="lookup-1" itemName="cretine" onSettled={onSettled} />);

    await user.click(await screen.findByRole('button', { name: /^Not the same: cretine is not the same as the suggestion$/ }));

    expect(await screen.findByRole('status')).toHaveTextContent('it stays as you entered it');
    expect(onSettled).toHaveBeenCalledWith('declined');
    expect(apiClient.linkCatalogLookup).not.toHaveBeenCalled();
  });

  it('shows an error and changes nothing when linking fails', async () => {
    vi.mocked(apiClient.getCatalogLookup).mockResolvedValue(lookup('matched', [sameAs]));
    vi.mocked(apiClient.linkCatalogLookup).mockRejectedValue(new Error('nope'));
    const onSettled = vi.fn();
    const user = userEvent.setup();
    render(<CatalogLookupPanel lookupId="lookup-1" itemName="Ozempic" onSettled={onSettled} />);

    await user.click(await screen.findByRole('button', { name: /^Link it: link Ozempic to Semaglutide$/ }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Nothing was changed');
    expect(onSettled).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: /^Link it: link Ozempic to Semaglutide$/ })).toBeEnabled();
  });

  it('says nothing was found without offering anything, and a failed check never blocks the item', async () => {
    vi.mocked(apiClient.getCatalogLookup).mockResolvedValueOnce(lookup('no_match'));
    const { unmount } = render(<CatalogLookupPanel lookupId="lookup-1" itemName="Zzyzx" />);
    expect(await screen.findByRole('status')).toHaveTextContent('stays in your list as Unverified');
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
    unmount();

    vi.mocked(apiClient.getCatalogLookup).mockRejectedValueOnce(new Error('offline'));
    render(<CatalogLookupPanel lookupId="lookup-2" itemName="Zzyzx" />);
    expect(await screen.findByRole('status')).toHaveTextContent('Your item is saved as you entered it');
  });
});
