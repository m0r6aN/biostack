import AdminPage from '@/app/admin/page';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const fetchMock = vi.fn();

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <div>{title}</div>,
}));

vi.mock('@/components/ui/Button', () => ({
  Button: ({ children, ...props }: React.ButtonHTMLAttributes<HTMLButtonElement>) => <button {...props}>{children}</button>,
}));

vi.mock('@/components/ui/GlassCard', () => ({
  GlassCard: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));

describe('AdminPage', () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
  });

  it('shows ingest fetch failures as ingest errors instead of invalid JSON', async () => {
    fetchMock
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'dev-token' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ profiles: 0, knowledgeEntries: 0, totalCompoundRecords: 0, totalCheckIns: 0 }),
      })
      .mockRejectedValueOnce(new TypeError('Failed to fetch'));

    render(<AdminPage />);

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(2);
    });

    fireEvent.change(screen.getByRole('textbox'), {
      target: { value: '{"canonicalName":"Semaglutide"}' },
    });

    fireEvent.click(screen.getByRole('button', { name: 'Perform Upsert' }));

    await waitFor(() => {
      expect(screen.getByText('✘ Ingest failed: Failed to fetch')).toBeInTheDocument();
    });

    expect(screen.queryByText(/Invalid JSON:/i)).not.toBeInTheDocument();
  });
});