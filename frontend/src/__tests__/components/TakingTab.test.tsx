import { TakingTab } from '@/components/protocol-portal/tabs/TakingTab';
import { apiClient } from '@/lib/api';
import type { CompoundRecord } from '@/lib/types';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/api', () => ({ apiClient: { getCompounds: vi.fn() } }));
vi.mock('next/link', () => ({ default: ({ href, children }: { href: string; children: React.ReactNode }) => <a href={href}>{children}</a> }));

const record = (id: string, name: string, status: string): CompoundRecord => ({
  id, personId: 'profile-1', name, status, category: 'Supplement', startDate: '2026-10-01',
  endDate: null, notes: '', sourceType: 'Manual', doseAmount: 2, doseUnit: 'capsule',
  doseFrequency: 'twice weekly', frequencyKind: 'weekdays', frequencyWeekdays: [1, 4], frequencyTimesOfDay: ['08:00'],
});

beforeEach(() => vi.resetAllMocks());

describe('Everything I\'m Taking', () => {
  it('loads only the selected profile, shows active and paused entries, and logs only an explicitly entered amount', async () => {
    vi.mocked(apiClient.getCompounds).mockResolvedValue([
      record('1', 'Creatine', 'Active'), record('2', 'Paused item', 'Paused'), record('3', 'Stopped item', 'Discontinued'),
    ]);
    const onLog = vi.fn().mockResolvedValue(undefined);
    const user = userEvent.setup();
    render(<TakingTab profileId="profile-1" onLogAmount={onLog} />);
    const activeName = await screen.findByText('Creatine');
    expect(apiClient.getCompounds).toHaveBeenCalledWith('profile-1');
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
    vi.mocked(apiClient.getCompounds).mockRejectedValueOnce(new Error('network')).mockResolvedValueOnce([]);
    const user = userEvent.setup();
    render(<TakingTab profileId="profile-1" onLogAmount={vi.fn()} />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Items could not be loaded');
    await user.click(screen.getByRole('button', { name: 'Try again' }));
    expect(await screen.findByText('No active or paused items yet.')).toBeInTheDocument();
  });
});
