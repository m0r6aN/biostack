import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AccountDataPage from '@/app/account/data/page';
import ConfirmAccountDeletionPage from '@/app/account/data/delete/confirm/page';

vi.mock('@/components/Header', () => ({ Header: ({ title }: { title: string }) => <header>{title}</header> }));
vi.mock('next/link', () => ({ default: ({ href, children, ...rest }: { href: string; children: React.ReactNode }) => <a href={href} {...rest}>{children}</a> }));

const summary = {
  tables: [
    { name: 'PersonProfiles', count: 2, onDeletion: 'removed', note: '' },
    { name: 'CompoundRecords', count: 9, onDeletion: 'removed', note: '' },
    { name: 'SpineEntries', count: 4, onDeletion: 'kept', note: '' },
    { name: 'Subscriptions', count: 0, onDeletion: 'removed', note: '' },
  ],
  backupRetentionDays: 35,
  confirmationPhrase: 'DELETE MY ACCOUNT',
  deletionAvailable: true,
};

const json = (status: number, body: unknown) => ({ ok: status >= 200 && status < 300, status, json: async () => body, blob: async () => new Blob(['x']) });
let fetchMock: ReturnType<typeof vi.fn>;

beforeEach(() => {
  fetchMock = vi.fn();
  vi.stubGlobal('fetch', fetchMock);
  vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
  Object.assign(URL, { createObjectURL: vi.fn(() => 'blob:x'), revokeObjectURL: vi.fn() });
});
afterEach(() => { cleanup(); vi.unstubAllGlobals(); window.history.replaceState(null, '', '/'); });

describe('account data page', () => {
  it('lists what is held, with counts, and says backups age out within 35 days', async () => {
    fetchMock.mockResolvedValueOnce(json(200, summary));
    render(<AccountDataPage />);
    expect(await screen.findByText('Profiles')).toBeInTheDocument();
    expect(screen.getByText('9')).toBeInTheDocument();
    expect(screen.queryByText('Subscription records')).not.toBeInTheDocument(); // empty lists stay out
    expect(screen.getByText(/age out within 35 days/)).toBeInTheDocument();
  });

  it('downloads JSON through the export endpoint', async () => {
    fetchMock.mockResolvedValueOnce(json(200, summary)).mockResolvedValueOnce(json(200, {}));
    render(<AccountDataPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Download JSON' }));
    expect(await screen.findByText('Your download has started.')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenLastCalledWith('/api/v1/account/export?format=json', expect.objectContaining({ credentials: 'include' }));
  });

  it('asks the person to wait when the export is rate limited', async () => {
    fetchMock.mockResolvedValueOnce(json(200, summary)).mockResolvedValueOnce(json(429, {}));
    render(<AccountDataPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Download CSV (zip)' }));
    expect(await screen.findByText(/Wait about ten minutes/)).toBeInTheDocument();
  });

  it('keeps the delete button off until the exact phrase is typed, then asks for the email link', async () => {
    fetchMock.mockResolvedValueOnce(json(200, summary)).mockResolvedValueOnce(json(202, {}));
    render(<AccountDataPage />);
    const button = await screen.findByRole('button', { name: 'Email me a confirmation link' });
    expect(button).toBeDisabled();
    fireEvent.change(screen.getByLabelText('Type the phrase to continue'), { target: { value: 'delete my account' } });
    expect(button).toBeDisabled();
    fireEvent.change(screen.getByLabelText('Type the phrase to continue'), { target: { value: 'DELETE MY ACCOUNT' } });
    expect(button).toBeEnabled();
    fireEvent.click(button);
    expect(await screen.findByText(/Nothing has been deleted yet/)).toBeInTheDocument();
    const [, init] = fetchMock.mock.calls[1];
    expect(JSON.parse(init.body)).toEqual({ confirmation: 'DELETE MY ACCOUNT' });
  });

  it('explains that operator accounts cannot self-delete', async () => {
    fetchMock.mockResolvedValueOnce(json(200, { ...summary, deletionAvailable: false }));
    render(<AccountDataPage />);
    expect(await screen.findByText(/Operator accounts can’t be deleted from here/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Email me a confirmation link' })).not.toBeInTheDocument();
  });

  it('still offers export and deletion when the summary cannot load', async () => {
    fetchMock.mockResolvedValueOnce(json(500, {}));
    render(<AccountDataPage />);
    expect(await screen.findByRole('button', { name: 'Reload summary' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Download JSON' })).toBeEnabled();
    expect(screen.getByLabelText('Type the phrase to continue')).toBeInTheDocument();
  });
});

describe('confirm account deletion page', () => {
  it('sends nothing until the button is pressed, strips the token from the address bar, then shows the receipt', async () => {
    window.history.replaceState(null, '', '/account/data/delete/confirm#token=abc123');
    fetchMock.mockResolvedValueOnce(json(200, {
      receiptId: 'r-1', deletedOnUtc: '2026-10-29T00:00:00Z', backupsAgeOutByUtc: '2026-12-03T00:00:00Z', backupRetentionDays: 35, receiptEmailQueued: true,
    }));
    render(<ConfirmAccountDeletionPage />);
    const button = await screen.findByRole('button', { name: 'Permanently delete my account' });
    expect(window.location.hash).toBe('');
    expect(fetchMock).not.toHaveBeenCalled();

    fireEvent.click(button);
    expect(await screen.findByText('Your account has been deleted')).toBeInTheDocument();
    expect(screen.getByText('2026-12-03', { exact: false })).toBeInTheDocument();
    expect(screen.getByText(/We sent a receipt/)).toBeInTheDocument();
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe('/api/v1/account/deletion/confirm');
    expect(JSON.parse(init.body)).toEqual({ token: 'abc123' });
  });

  it('shows the server message and allows retry when the link is rejected', async () => {
    window.history.replaceState(null, '', '/account/data/delete/confirm?token=bad');
    fetchMock.mockResolvedValueOnce(json(400, { message: 'This deletion link is invalid, expired, or already used.' }));
    render(<ConfirmAccountDeletionPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Permanently delete my account' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('invalid, expired, or already used');
    await waitFor(() => expect(screen.getByRole('button', { name: 'Permanently delete my account' })).toBeEnabled());
  });

  it('tells a signed-out browser to sign in first', async () => {
    window.history.replaceState(null, '', '/account/data/delete/confirm?token=t');
    fetchMock.mockResolvedValueOnce(json(401, {}));
    render(<ConfirmAccountDeletionPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Permanently delete my account' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(/not signed in in this browser/);
  });

  it('asks for the emailed link when no token is present', async () => {
    window.history.replaceState(null, '', '/account/data/delete/confirm');
    render(<ConfirmAccountDeletionPage />);
    expect(await screen.findByText(/needs the link from your confirmation email/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Permanently delete my account' })).not.toBeInTheDocument();
  });
});
