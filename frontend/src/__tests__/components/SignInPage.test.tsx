import SignInPage from '@/app/auth/signin/page';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const fetchMock = vi.fn();
let callbackUrl = '%2Fprofiles';

vi.stubGlobal('fetch', fetchMock);

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams(`callbackUrl=${callbackUrl}`),
}));

describe('SignInPage', () => {
  beforeEach(() => {
    fetchMock.mockReset();
    callbackUrl = '%2Fprofiles';
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
        '/api/v1/auth/start',
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

  it('reassures analyzer conversions that saved work carries through sign-in', () => {
    callbackUrl = '%2Fprotocol-console';

    render(<SignInPage />);

    expect(screen.getByText('Your saved analysis will carry through sign-in.')).toBeInTheDocument();
    expect(screen.getByText('No need to restart. Continue to create your BioStack protocol.')).toBeInTheDocument();
  });

  it('does not translate an absolute callback URL into a local return path', async () => {
    callbackUrl = 'https%3A%2F%2Fevil.example%2Fprofiles';
    fetchMock.mockResolvedValue({ ok: true, json: async () => ({}) });

    render(<SignInPage />);
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'user@example.com' } });
    fireEvent.click(screen.getByRole('button', { name: 'Continue' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/v1/auth/start',
        expect.objectContaining({
          body: JSON.stringify({
            contact: 'user@example.com',
            channel: 'email',
            redirectPath: '/protocol-console',
          }),
        }),
      );
    });
  });
});
