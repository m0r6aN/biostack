import { NoticeBell } from '@/components/NoticeBell';
import { apiClient } from '@/lib/api';
import type { UserNotice } from '@/lib/types';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const auth = vi.hoisted(() => ({ value: undefined as unknown }));
vi.mock('@/lib/AuthProvider', () => ({ useOptionalAuth: () => auth.value }));
vi.mock('next/link', () => ({ default: ({ href, children, onClick }: { href: string; children: React.ReactNode; onClick?: () => void }) => <a href={href} onClick={onClick}>{children}</a> }));
vi.mock('@/lib/api', () => ({
  apiClient: {
    getUnreadNoticeCount: vi.fn(), getNotices: vi.fn(), markNoticeRead: vi.fn(), markAllNoticesRead: vi.fn(), dismissNotice: vi.fn(),
  },
}));

const note = (id: string, extra: Partial<UserNotice> = {}): UserNotice => ({
  id, kind: 'catalog_match', titleKey: 'notice.catalog_match',
  params: { queryName: 'Ozempic', candidateName: 'Semaglutide', label: 'same_as' },
  actionPath: '/my-protocol?tab=taking', relatedId: null, createdAtUtc: '2026-10-30T10:00:00Z', updatedAtUtc: '2026-10-30T10:00:00Z', read: false, ...extra,
});

beforeEach(() => {
  vi.resetAllMocks();
  auth.value = { user: { id: 'u1', email: 'a@example.com', displayName: 'A', role: 0 }, loading: false };
});

describe('NoticeBell', () => {
  it('renders nothing and calls nothing when signed out or outside the auth provider', () => {
    auth.value = { user: null, loading: false };
    const { container, unmount } = render(<NoticeBell />);
    expect(container).toBeEmptyDOMElement();
    unmount();
    auth.value = undefined;
    const second = render(<NoticeBell />);
    expect(second.container).toBeEmptyDOMElement();
    expect(apiClient.getUnreadNoticeCount).not.toHaveBeenCalled();
  });

  it('shows the unread count on sign-in, and no badge when there is nothing new', async () => {
    vi.mocked(apiClient.getUnreadNoticeCount).mockResolvedValue(2);
    const { unmount } = render(<NoticeBell />);
    expect(await screen.findByRole('button', { name: 'Notes, 2 unread' })).toBeInTheDocument();
    expect(screen.getByTestId('notice-count')).toHaveTextContent('2');
    unmount();

    vi.mocked(apiClient.getUnreadNoticeCount).mockResolvedValue(0);
    render(<NoticeBell />);
    await waitFor(() => expect(apiClient.getUnreadNoticeCount).toHaveBeenCalledTimes(2));
    expect(screen.getByRole('button', { name: 'Notes' })).toBeInTheDocument();
    expect(screen.queryByTestId('notice-count')).not.toBeInTheDocument();
  });

  it('stays quiet when the count cannot be loaded', async () => {
    vi.mocked(apiClient.getUnreadNoticeCount).mockRejectedValue(new Error('offline'));
    render(<NoticeBell />);
    await waitFor(() => expect(apiClient.getUnreadNoticeCount).toHaveBeenCalled());
    expect(screen.getByRole('button', { name: 'Notes' })).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('opens the list with text built from the note, and only links to in-app paths', async () => {
    vi.mocked(apiClient.getUnreadNoticeCount).mockResolvedValue(2);
    vi.mocked(apiClient.getNotices).mockResolvedValue([note('n1'), note('n2', { actionPath: 'https://evil.example/x' })]);
    const user = userEvent.setup();
    render(<NoticeBell />);

    await user.click(await screen.findByRole('button', { name: 'Notes, 2 unread' }));

    const dialog = await screen.findByRole('dialog', { name: 'Notes' });
    expect(within(dialog).getAllByText(/We found a match for/)).toHaveLength(2);
    expect(within(dialog).getAllByText(/Same as Semaglutide/)).toHaveLength(2);
    const links = within(dialog).getAllByRole('link', { name: 'Open' });
    expect(links).toHaveLength(1);                                   // the external path got no link
    expect(links[0]).toHaveAttribute('href', '/my-protocol?tab=taking');
  });

  it('marks a note read, dismisses one, and marks all read, keeping the badge in step', async () => {
    vi.mocked(apiClient.getUnreadNoticeCount).mockResolvedValue(2);
    vi.mocked(apiClient.getNotices).mockResolvedValue([note('n1'), note('n2')]);
    vi.mocked(apiClient.markNoticeRead).mockResolvedValue(undefined);
    vi.mocked(apiClient.dismissNotice).mockResolvedValue(undefined);
    vi.mocked(apiClient.markAllNoticesRead).mockResolvedValue(undefined);
    const user = userEvent.setup();
    render(<NoticeBell />);
    await user.click(await screen.findByRole('button', { name: 'Notes, 2 unread' }));
    const dialog = await screen.findByRole('dialog', { name: 'Notes' });

    await user.click(within(dialog).getAllByRole('button', { name: 'Mark read' })[0]);
    expect(apiClient.markNoticeRead).toHaveBeenCalledWith('n1');
    await waitFor(() => expect(screen.getByTestId('notice-count')).toHaveTextContent('1'));

    await user.click(within(dialog).getAllByRole('button', { name: 'Dismiss note' })[1]);
    expect(apiClient.dismissNotice).toHaveBeenCalledWith('n2');
    await waitFor(() => expect(screen.queryByTestId('notice-count')).not.toBeInTheDocument());
    expect(within(dialog).getAllByRole('button', { name: 'Dismiss note' })).toHaveLength(1);

    await user.click(within(dialog).getByRole('button', { name: 'Dismiss note' }));
    expect(await within(dialog).findByText('No notes right now.')).toBeInTheDocument();
  });

  it('marks everything read in one step', async () => {
    vi.mocked(apiClient.getUnreadNoticeCount).mockResolvedValue(2);
    vi.mocked(apiClient.getNotices).mockResolvedValue([note('n1'), note('n2')]);
    vi.mocked(apiClient.markAllNoticesRead).mockResolvedValue(undefined);
    const user = userEvent.setup();
    render(<NoticeBell />);
    await user.click(await screen.findByRole('button', { name: 'Notes, 2 unread' }));

    await user.click(await screen.findByRole('button', { name: 'Mark all read' }));

    expect(apiClient.markAllNoticesRead).toHaveBeenCalledTimes(1);
    await waitFor(() => expect(screen.queryByTestId('notice-count')).not.toBeInTheDocument());
    expect(screen.queryByRole('button', { name: 'Mark all read' })).not.toBeInTheDocument();
  });

  it('shows a retry message if the list cannot load, and closes on Escape', async () => {
    vi.mocked(apiClient.getUnreadNoticeCount).mockResolvedValue(0);
    vi.mocked(apiClient.getNotices).mockRejectedValue(new Error('offline'));
    const user = userEvent.setup();
    render(<NoticeBell />);
    await user.click(await screen.findByRole('button', { name: 'Notes' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Something went wrong');

    await user.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});
