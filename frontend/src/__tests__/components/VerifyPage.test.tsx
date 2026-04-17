import VerifyPage from '@/app/auth/verify/page';
import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const locationMock = {
  href: '',
};

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams('token=abc123'),
}));

describe('VerifyPage', () => {
  beforeEach(() => {
    locationMock.href = '';
    vi.stubGlobal('location', locationMock);
  });

  it('exchanges magic links through the same-origin proxy', async () => {
    render(<VerifyPage />);

    expect(screen.getByText('Signing you in…')).toBeInTheDocument();
    await waitFor(() => {
      expect(locationMock.href).toBe('/api/auth/verify?token=abc123');
    });
  });
});
