import SignInPage from '@/app/auth/signin/page';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

vi.stubGlobal('fetch', vi.fn());
vi.mock('next/navigation', () => ({ useSearchParams: () => new URLSearchParams('') }));
vi.mock('@/lib/passkeys', async () => {
  const actual = await vi.importActual<typeof import('@/lib/passkeys')>('@/lib/passkeys');
  return { ...actual, passkeysSupported: () => false };
});

describe('SignInPage exits', () => {
  it('has a way back to the home page and to help, since the page has no site navigation', () => {
    render(<SignInPage />);
    expect(screen.getByRole('link', { name: 'BioStack home' })).toHaveAttribute('href', '/');
    expect(screen.getByRole('link', { name: 'Help and support' })).toHaveAttribute('href', '/support');
  });
});
