import AdminPage from '@/app/admin/page';
import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const fetchMock = vi.fn();

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <div>{title}</div>,
}));

vi.mock('@/components/ui/GlassCard', () => ({
  GlassCard: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));

describe('AdminPage', () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
  });

  it('fetches read-only statistics without exposing or calling bulk ingest', async () => {
    fetchMock
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'dev-token' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ profiles: 3, knowledgeEntries: 7, totalCompoundRecords: 11, totalCheckIns: 13 }),
      });

    render(<AdminPage />);

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(2);
    });

    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      expect.stringContaining('/api/v1/admin/stats'),
      { headers: { Authorization: 'Bearer dev-token' } },
    );
    expect(screen.getByText('Knowledge Governance')).toBeInTheDocument();
    expect(screen.getByText('Canonical bulk ingest disabled')).toBeInTheDocument();
    expect(screen.queryByText('Bulk Knowledge Ingest')).not.toBeInTheDocument();
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Perform Upsert' })).not.toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([url]) => String(url).includes('/api/v1/admin/knowledge/ingest'))).toBe(false);
  });
});
