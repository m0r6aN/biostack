import SignInPage from '@/app/auth/signin/page';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const fetchMock = vi.fn();

vi.stubGlobal('fetch', fetchMock);

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams('callbackUrl=http%3A%2F%2Flocalhost%3A3043%2Fprofiles'),
}));

describe('SignInPage', () => {
  beforeEach(() => {
    fetchMock.mockReset();
  });

  it('starts passwordless email auth and moves to the inbox step', async () => {
    fetchMock.mockResolvedValue({ ok: true, json: async () => ({}) });

    render(<SignInPage />);

    fireEvent.change(screen.getByLabelText('Email'), {
      target: { value: 'User@Example.com' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Continue' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        'http://localhost:5000/api/v1/auth/start',
        expect.objectContaining({
          method: 'POST',
          credentials: 'include',
          body: JSON.stringify({
            contact: 'user@example.com',
            channel: 'email',
            redirectPath: '/profiles',
          }),
        })
      );
    });

    expect(screen.getByText('Check your inbox')).toBeInTheDocument();
    expect(screen.getByText('ur**@example.com')).toBeInTheDocument();
  });

  it('keeps the form visible when the sign-in link cannot be sent', async () => {
    fetchMock.mockResolvedValue({ ok: false });

    render(<SignInPage />);

    fireEvent.change(screen.getByLabelText('Email'), {
      target: { value: 'User@Example.com' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Continue' }));

    expect(await screen.findByText('We could not send that sign-in link. Try again in a moment.')).toBeInTheDocument();
    expect(screen.queryByText('Check your inbox')).not.toBeInTheDocument();
  });
});
