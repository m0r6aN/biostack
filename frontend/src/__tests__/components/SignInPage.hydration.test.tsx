import SignInPage from '@/app/auth/signin/page';
import { act } from '@testing-library/react';
import { hydrateRoot } from 'react-dom/client';
import { renderToString } from 'react-dom/server';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

// The passkey control depends on browser capability, which the server cannot know. The page must
// render the same markup on the server and on the first client render, then reveal the passkey
// option after hydration. A mismatch is a React hydration error in the console and, if the form is
// submitted before hydration finishes, a native GET.
const passkeysSupportedMock = vi.fn(() => true);

vi.stubGlobal('fetch', vi.fn(async () => ({ ok: true, json: async () => ({ enabled: true }) })));

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams(''),
}));

vi.mock('@/lib/passkeys', async () => {
  const actual = await vi.importActual<typeof import('@/lib/passkeys')>('@/lib/passkeys');
  return { ...actual, passkeysSupported: () => passkeysSupportedMock() };
});

describe('SignInPage hydration', () => {
  let errorSpy: ReturnType<typeof vi.spyOn>;
  let container: HTMLDivElement;

  beforeEach(() => {
    passkeysSupportedMock.mockReset().mockReturnValue(true);
    errorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    container = document.createElement('div');
    document.body.appendChild(container);
  });

  afterEach(() => {
    errorSpy.mockRestore();
    container.remove();
  });

  it('server-renders without the passkey button even when the browser supports passkeys', () => {
    const html = renderToString(<SignInPage />);
    expect(html).toContain('Email me a sign-in link');
    expect(html).not.toContain('Sign in with a passkey');
  });

  it('hydrates the server markup without a mismatch, then offers the passkey option', async () => {
    container.innerHTML = renderToString(<SignInPage />);

    await act(async () => {
      hydrateRoot(container, <SignInPage />);
    });

    const hydrationErrors = errorSpy.mock.calls.filter(([message]: unknown[]) =>
      /hydrat|did not match|server rendered HTML/i.test(String(message)),
    );
    expect(hydrationErrors).toEqual([]);
    expect(container.textContent).toContain('Sign in with a passkey');
    expect(container.textContent).toContain('Email me a sign-in link');
  });

  it('never offers passkeys when the browser does not support them', async () => {
    passkeysSupportedMock.mockReturnValue(false);
    container.innerHTML = renderToString(<SignInPage />);

    await act(async () => {
      hydrateRoot(container, <SignInPage />);
    });

    expect(container.textContent).not.toContain('Sign in with a passkey');
  });
});
