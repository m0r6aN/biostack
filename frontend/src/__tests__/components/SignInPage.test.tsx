import SignInPage from '@/app/auth/signin/page';
import { render, screen, waitFor } from '@testing-library/react';
import type { ComponentProps } from 'react';
import { describe, expect, it, vi } from 'vitest';

const getProvidersMock = vi.fn();

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: ComponentProps<'a'>) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams('callbackUrl=http%3A%2F%2Flocalhost%3A3043%2Fprofiles'),
}));

vi.mock('next-auth/react', () => ({
  getProviders: (...args: unknown[]) => getProvidersMock(...args),
  signIn: vi.fn(),
}));

describe('SignInPage', () => {
  it('offers a local-mode continuation when no providers are configured', async () => {
    getProvidersMock.mockResolvedValue({});

    render(<SignInPage />);

    await waitFor(() => {
      expect(
        screen.getByText('Sign-in providers are not configured yet. You can still continue locally and use BioStack on this device.')
      ).toBeInTheDocument();
    });

    expect(screen.getByRole('link', { name: 'Continue in Local Mode' })).toHaveAttribute('href', '/profiles');
  });
});