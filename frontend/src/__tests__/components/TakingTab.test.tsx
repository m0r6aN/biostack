import { TakingTab } from '@/components/protocol-portal/tabs/TakingTab';
import { apiClient } from '@/lib/api';
import type { CompoundRecord, TakingItem, TakingResponse } from '@/lib/types';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/api', () => ({ apiClient: { getTaking: vi.fn(), updateCompound: vi.fn() } }));
vi.mock('next/link', () => ({ default: ({ href, children }: { href: string; children: React.ReactNode }) => <a href={href}>{children}</a> }));

const record = (id: string, name: string, status: string, extra: Partial<CompoundRecord> = {}): CompoundRecord => ({
  id, personId: 'profile-1', name, status, category: 'Supplement', startDate: '2026-10-01',
  endDate: null, notes: '', sourceType: 'Manual', doseAmount: 2, doseUnit: 'capsule',
  doseFrequency: 'twice weekly', frequencyKind: 'weekdays', frequencyWeekdays: [1, 4], frequencyTimesOfDay: ['08:00'],
  verificationState: 'unverified', categorySource: 'user', includeInProviderSummary: true, ...extra,
});

const item = (compound: CompoundRecord, extra: Partial<TakingItem> = {}): TakingItem =>
  ({ compound, countsTowardCap: true, suggestions: [], ...extra });

const taking = (items: TakingItem[], cap = { limit: 8, counted: 1 }): TakingResponse => ({ items, cap });

beforeEach(() => vi.resetAllMocks());

describe('Everything I\'m Taking', () => {
  it('loads only the selected profile, shows active and paused entries, and logs only an explicitly entered amount', async () => {
    vi.mocked(apiClient.getTaking).mockResolvedValue(taking([
      item(record('1', 'Creatine', 'Active')), item(record('2', 'Paused item', 'Paused')), item(record('3', 'Stopped item', 'Discontinued')),
    ]));
    const onLog = vi.fn().mockResolvedValue(undefined);
    const user = userEvent.setup();
    render(<TakingTab profileId="profile-1" onLogAmount={onLog} />);
    const activeName = await screen.findByText('Creatine');
    expect(apiClient.getTaking).toHaveBeenCalledWith('profile-1');
    expect(screen.getByText('Paused item')).toBeInTheDocument();
    expect(screen.queryByText('Stopped item')).not.toBeInTheDocument();
    const card = activeName.closest('article')!;
    expect(within(card).getByText('2 capsule · twice weekly · Mon, Thu · 08:00')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Add or manage items' })).toHaveAttribute('href', '/compounds');
    await user.click(within(card).getByRole('button', { name: 'Log amount taken' }));
    expect(within(card).getByLabelText('Amount taken')).toHaveValue(null);
    expect(within(card).getByLabelText('Unit taken')).toHaveValue('');
    await user.type(within(card).getByLabelText('Amount taken'), '3');
    await user.selectOptions(within(card).getByLabelText('Unit taken'), 'capsule');
    await user.click(within(card).getByRole('button', { name: 'Save amount' }));
    expect(onLog).toHaveBeenCalledWith(expect.objectContaining({ compoundId: '1' }), { amount: 3, unit: 'capsule', note: '' });
    const pausedCard = screen.getByText('Paused item').closest('article')!;
    expect(within(pausedCard).queryByRole('button', { name: 'Log amount taken' })).not.toBeInTheDocument();
  });

  it('shows a retryable error instead of reporting an empty list when loading fails', async () => {
    vi.mocked(apiClient.getTaking).mockRejectedValueOnce(new Error('network')).mockResolvedValueOnce(taking([]));
    const user = userEvent.setup();
    render(<TakingTab profileId="profile-1" onLogAmount={vi.fn()} />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Items could not be loaded');
    await user.click(screen.getByRole('button', { name: 'Try again' }));
    expect(await screen.findByText('No active or paused items yet.')).toBeInTheDocument();
  });

  it('labels each item from the server-derived state: In BioStack links to its page, anything else is Unverified', async () => {
    vi.mocked(apiClient.getTaking).mockResolvedValue(taking([
      item(record('1', 'Creatine', 'Active', { catalogSlug: 'creatine', verificationState: 'catalog', categorySource: 'catalog' }), { countsTowardCap: false }),
      item(record('2', 'My custom blend', 'Active')),
      // a forged-looking state with no slug is still not "In BioStack"
      item(record('3', 'Odd row', 'Active', { verificationState: 'catalog', catalogSlug: null })),
    ], { limit: 8, counted: 2 }));
    render(<TakingTab profileId="profile-1" onLogAmount={vi.fn()} />);

    const linked = (await screen.findByText('Creatine')).closest('article')!;
    expect(within(linked).getByRole('link', { name: 'In BioStack' })).toHaveAttribute('href', '/knowledge/creatine');
    expect(within(screen.getByText('My custom blend').closest('article')!).getByText('Unverified')).toBeInTheDocument();
    expect(within(screen.getByText('Odd row').closest('article')!).getByText('Unverified')).toBeInTheDocument();
    expect(screen.getByTestId('cap-line')).toHaveTextContent('Research or unconfirmed items: 2 of 8. Confirmed supplements, vitamins, minerals and amino acids don\'t count.');
  });

  it('filters by category group and by Unverified', async () => {
    vi.mocked(apiClient.getTaking).mockResolvedValue(taking([
      item(record('1', 'BPC-157', 'Active', { category: 'Peptide', catalogSlug: 'bpc-157', verificationState: 'catalog', categorySource: 'catalog' })),
      item(record('2', 'Ostarine', 'Active', { category: 'Sarm', catalogSlug: 'ostarine', verificationState: 'catalog', categorySource: 'catalog' })),
      item(record('3', 'Vitamin D3', 'Active', { category: 'Vitamin', catalogSlug: 'vitamin-d3', verificationState: 'catalog', categorySource: 'catalog' }), { countsTowardCap: false }),
      item(record('4', 'Custom thing', 'Active', { category: 'Supplement' })),
    ]));
    const user = userEvent.setup();
    render(<TakingTab profileId="profile-1" onLogAmount={vi.fn()} />);
    await screen.findByText('BPC-157');

    await user.click(screen.getByRole('button', { name: 'Peptides & research compounds' }));
    expect(screen.getByText('BPC-157')).toBeInTheDocument();
    expect(screen.queryByText('Ostarine')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'SARMs/SERMs/hormones' }));
    expect(screen.getByText('Ostarine')).toBeInTheDocument();
    expect(screen.queryByText('BPC-157')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Vitamins & minerals' }));
    expect(screen.getByText('Vitamin D3')).toBeInTheDocument();
    expect(screen.queryByText('Custom thing')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Unverified' }));
    expect(screen.getByText('Custom thing')).toBeInTheDocument();
    expect(screen.queryByText('Vitamin D3')).not.toBeInTheDocument();
  });

  it('offers a match with "Same as" and links only when the user presses Link it, never changing amounts', async () => {
    const custom = record('1', 'Cholecalciferol', 'Active', { doseAmount: 5, doseUnit: 'drop' });
    vi.mocked(apiClient.getTaking)
      .mockResolvedValueOnce(taking([item(custom, { suggestions: [{ slug: 'vitamin-d3', name: 'Vitamin D3', category: 'Vitamin', matchedOn: 'alias' }] })]))
      .mockResolvedValueOnce(taking([item(record('1', 'Cholecalciferol', 'Active', { catalogSlug: 'vitamin-d3', verificationState: 'catalog', categorySource: 'catalog', category: 'Vitamin' }), { countsTowardCap: false })]));
    vi.mocked(apiClient.updateCompound).mockResolvedValue(custom);
    const user = userEvent.setup();
    render(<TakingTab profileId="profile-1" onLogAmount={vi.fn()} />);

    const card = (await screen.findByText('Cholecalciferol')).closest('article')!;
    expect(within(card).getByText(/Same as/)).toHaveTextContent('Same as Vitamin D3 (matches its alternate name)?');
    expect(card.textContent).not.toMatch(/Related/);
    expect(apiClient.updateCompound).not.toHaveBeenCalled(); // an offer alone changes nothing

    await user.click(within(card).getByRole('button', { name: 'Link Cholecalciferol to Vitamin D3' }));

    expect(apiClient.updateCompound).toHaveBeenCalledTimes(1);
    const [profileId, id, body] = vi.mocked(apiClient.updateCompound).mock.calls[0];
    expect(profileId).toBe('profile-1');
    expect(id).toBe('1');
    expect(body).toMatchObject({ catalogSlug: 'vitamin-d3', doseAmount: 5, doseUnit: 'drop', name: 'Cholecalciferol' });
    // response-only fields are never sent back
    expect(body).not.toHaveProperty('verificationState');
    expect(body).not.toHaveProperty('categorySource');
    expect(await screen.findByRole('link', { name: 'In BioStack' })).toBeInTheDocument();
  });

  it('toggles an item\'s inclusion in the provider summary without touching its link or amounts', async () => {
    const linked = record('1', 'Creatine', 'Active', { catalogSlug: 'creatine', verificationState: 'catalog', categorySource: 'catalog' });
    vi.mocked(apiClient.getTaking).mockResolvedValue(taking([item(linked, { countsTowardCap: false })]));
    vi.mocked(apiClient.updateCompound).mockResolvedValue(linked);
    const user = userEvent.setup();
    render(<TakingTab profileId="profile-1" onLogAmount={vi.fn()} />);

    const box = await screen.findByRole('checkbox', { name: 'Include Creatine in provider summary' });
    expect(box).toBeChecked();
    await user.click(box);

    const body = vi.mocked(apiClient.updateCompound).mock.calls[0][2];
    expect(body).toMatchObject({ includeInProviderSummary: false, doseAmount: 2 });
    expect(body).not.toHaveProperty('catalogSlug'); // omitted: the server keeps the existing link
  });

  it('reports a failed update instead of pretending it worked', async () => {
    vi.mocked(apiClient.getTaking).mockResolvedValue(taking([item(record('1', 'Creatine', 'Active'))]));
    vi.mocked(apiClient.updateCompound).mockRejectedValue(new Error('boom'));
    const user = userEvent.setup();
    render(<TakingTab profileId="profile-1" onLogAmount={vi.fn()} />);

    await user.click(await screen.findByRole('checkbox', { name: 'Include Creatine in provider summary' }));

    expect(await screen.findByRole('alert')).toHaveTextContent("Couldn't update Creatine. Nothing was changed.");
  });
});
