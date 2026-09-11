import AccountSecurityPage from '@/app/account/security/page';
import SignInPage from '@/app/auth/signin/page';
import VerifyPage from '@/app/auth/verify/page';
import ConsentPage from '@/app/onboarding/consent/page';
import PrivacyPage from '@/app/privacy/page';
import TermsPage from '@/app/terms/page';
import { AppShell } from '@/components/AppShell';
import { cleanup, render, screen } from '@testing-library/react';
import type { ComponentProps, ComponentType } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

// The root layout renders `<a href="#main">Skip to main content</a>` above every
// page. AppShell owns `#main` on app routes and on withheld protected routes; on
// public routes and on non-app authenticated routes (e.g. /account/security) it
// passes children through untouched, so the page-owned <main> — including the
// Suspense fallback <main> on the search-param pages — must carry the target.
const SKIP_LINK_TARGET_ID = 'main';

// Test-only fixtures. Nothing here reaches a network, a real token, a real
// credential, or a real navigation.
const FIXTURE_USER = { id: 'fixture-user', email: 'fixture@example.invalid', displayName: 'Fixture', role: 0 };
const FIXTURE_CONSENT = { accepted: false, declined: false, currentVersion: 'fixture-consent-v0' };
const FIXTURE_TOKEN = 'fixture-not-a-real-token';

const navigationFixture = vi.hoisted(() => ({ pathname: '/', search: '', suspend: false }));
const authFixture = vi.hoisted(() => ({ user: null as typeof FIXTURE_USER | null, loading: false }));
// A promise that never settles: thrown from useSearchParams it keeps the page
// suspended, which is exactly the state in which Next renders the fallback.
const NEVER_SETTLES = vi.hoisted(() => new Promise<never>(() => undefined));

vi.mock('next/navigation', () => ({
  usePathname: () => navigationFixture.pathname,
  useSearchParams: () => {
    if (navigationFixture.suspend) {
      throw NEVER_SETTLES;
    }
    return new URLSearchParams(navigationFixture.search);
  },
}));

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: ComponentProps<'a'>) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('@/lib/AuthProvider', () => ({
  useAuth: () => ({ user: authFixture.user, loading: authFixture.loading }),
}));

vi.mock('@/lib/passkeys', () => ({
  passkeysSupported: () => false,
  registerPasskey: vi.fn(() => Promise.reject(new Error('fixture: passkey creation is never exercised'))),
  authenticateWithPasskey: vi.fn(() => Promise.reject(new Error('fixture: passkey sign-in is never exercised'))),
}));

vi.mock('@/components/Sidebar', () => ({
  Sidebar: () => <aside>Sidebar</aside>,
}));

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <header>{title}</header>,
}));

vi.mock('@/components/marketing/MarketingNav', () => ({
  MarketingNav: () => <nav aria-label="Marketing">Marketing nav</nav>,
}));

vi.mock('@/components/marketing/MarketingFooter', () => ({
  MarketingFooter: () => <footer>Marketing footer</footer>,
}));

const fetchMock = vi.fn((input: string | URL, init?: RequestInit) => {
  const url = String(input);
  const method = init?.method ?? 'GET';
  if (method === 'GET' && url.endsWith('/api/v1/auth/passkeys/status')) {
    return Promise.resolve({ ok: true, status: 200, json: async () => ({ enabled: false }) });
  }
  if (method === 'GET' && url.endsWith('/api/v1/consent')) {
    return Promise.resolve({ ok: true, status: 200, json: async () => FIXTURE_CONSENT });
  }
  if (url.endsWith('/api/v1/auth/verify')) {
    // The exchange is left pending so the page stays on "Signing you in…" and
    // never redirects. No token is ever exchanged.
    return NEVER_SETTLES;
  }
  return Promise.resolve({ ok: false, status: 404, json: async () => ({}) });
});
const locationFixture = { hash: '', replace: vi.fn() };

type PageCase = {
  route: string;
  Page: ComponentType;
  auth: 'anonymous' | 'authenticated';
  search?: string;
  ready: () => Promise<HTMLElement> | HTMLElement;
};

const resolvedPages: Array<[label: string, pageCase: PageCase]> = [
  ['/terms', { route: '/terms', Page: TermsPage, auth: 'anonymous', ready: () => screen.getByRole('heading', { name: 'Terms of Service' }) }],
  ['/privacy', { route: '/privacy', Page: PrivacyPage, auth: 'anonymous', ready: () => screen.getByRole('heading', { name: 'Privacy Policy' }) }],
  ['/auth/signin', { route: '/auth/signin', Page: SignInPage, auth: 'anonymous', search: 'callbackUrl=%2Fprofiles', ready: () => screen.getByRole('heading', { name: 'Sign in to BioStack' }) }],
  ['/auth/verify', { route: '/auth/verify', Page: VerifyPage, auth: 'anonymous', search: `token=${FIXTURE_TOKEN}`, ready: () => screen.findByRole('heading', { name: 'Signing you in…' }) }],
  ['/onboarding/consent', { route: '/onboarding/consent', Page: ConsentPage, auth: 'anonymous', search: 'returnTo=%2Fprofiles', ready: () => screen.findByText('Consent record: fixture-consent-v0') }],
  ['/account/security', { route: '/account/security', Page: AccountSecurityPage, auth: 'authenticated', ready: () => screen.findByText('Passkeys are not enabled for this deployment. Email sign-in and recovery are unchanged.') }],
];

const suspenseFallbackPages: Array<[label: string, pageCase: PageCase]> = [
  ['/auth/signin', { route: '/auth/signin', Page: SignInPage, auth: 'anonymous', search: 'callbackUrl=%2Fprofiles', ready: () => screen.getByRole('main') }],
  ['/auth/verify', { route: '/auth/verify', Page: VerifyPage, auth: 'anonymous', search: `token=${FIXTURE_TOKEN}`, ready: () => screen.getByRole('main') }],
  ['/onboarding/consent', { route: '/onboarding/consent', Page: ConsentPage, auth: 'anonymous', search: 'returnTo=%2Fprofiles', ready: () => screen.getByRole('main') }],
];

function arrange(pageCase: PageCase, { suspend = false } = {}) {
  navigationFixture.pathname = pageCase.route;
  navigationFixture.search = pageCase.search ?? '';
  navigationFixture.suspend = suspend;
  authFixture.user = pageCase.auth === 'authenticated' ? FIXTURE_USER : null;
  authFixture.loading = false;
  return render(
    <AppShell>
      <pageCase.Page />
    </AppShell>,
  );
}

function expectSingleFocusableSkipTarget(container: HTMLElement) {
  const mains = screen.getAllByRole('main');
  expect(mains).toHaveLength(1);
  const [main] = mains;
  expect(main.tagName).toBe('MAIN');
  expect(main).toHaveAttribute('id', SKIP_LINK_TARGET_ID);
  expect(container.querySelectorAll(`#${SKIP_LINK_TARGET_ID}`)).toHaveLength(1);
  expect(container.querySelector(`#${SKIP_LINK_TARGET_ID}`)).toBe(main);
  expect(main).toHaveAttribute('tabindex', '-1');

  main.focus();
  expect(document.activeElement).toBe(main);
  return main;
}

function expectPassThroughShell() {
  // AppShell passed the page through: no app chrome and no shell-owned status main.
  expect(screen.queryByText('Sidebar')).not.toBeInTheDocument();
  expect(screen.queryByLabelText('App-wide disclaimer')).not.toBeInTheDocument();
  expect(screen.queryByRole('status')).not.toBeInTheDocument();
}

function expectNoSideEffects() {
  expect(locationFixture.replace).not.toHaveBeenCalled();
  for (const [input, init] of fetchMock.mock.calls) {
    const method = init?.method ?? 'GET';
    if (method === 'GET') {
      continue;
    }
    // The only permitted non-GET call is the verify page's mount-time exchange of the
    // fictional token, which the fixture leaves pending forever (nothing is exchanged,
    // nothing redirects). Sign-in, consent, passkey and logout writes must never occur.
    expect(method).toBe('POST');
    expect(String(input)).toMatch(/\/api\/v1\/auth\/verify$/);
    expect(init?.body).toBe(JSON.stringify({ token: FIXTURE_TOKEN }));
  }
}

describe('page-owned pass-through surfaces expose the skip-link target', () => {
  beforeEach(() => {
    fetchMock.mockClear();
    locationFixture.replace.mockReset();
    localStorage.clear();
    vi.stubGlobal('fetch', fetchMock);
    vi.stubGlobal('location', locationFixture);
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it.each(resolvedPages)('%s resolved page renders exactly one focusable main#main that it owns', async (_label, pageCase) => {
    const { container } = arrange(pageCase);
    const content = await pageCase.ready();

    const main = expectSingleFocusableSkipTarget(container);
    expect(main).toContainElement(content);
    expectPassThroughShell();
    expectNoSideEffects();
  });

  it.each(suspenseFallbackPages)('%s Suspense fallback renders exactly one focusable main#main with no page content', async (_label, pageCase) => {
    const { container } = arrange(pageCase, { suspend: true });
    const fallback = await pageCase.ready();

    const main = expectSingleFocusableSkipTarget(container);
    expect(main).toBe(fallback);
    expect(main.childElementCount).toBe(0);
    expect(screen.queryByRole('heading')).not.toBeInTheDocument();
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
    expectPassThroughShell();
    expect(fetchMock).not.toHaveBeenCalled();
    expectNoSideEffects();
  });
});

describe('AppShell keeps ownership of #main when it withholds protected content', () => {
  beforeEach(() => {
    fetchMock.mockClear();
    locationFixture.replace.mockReset();
    vi.stubGlobal('fetch', fetchMock);
    vi.stubGlobal('location', locationFixture);
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it.each([
    ['anonymous', { user: null, loading: false }, 'Redirecting to sign in…'],
    ['loading', { user: null, loading: true }, 'Checking your session…'],
  ])('%s /account/security shows the shell status main and never mounts the page', (_label, auth, status) => {
    navigationFixture.pathname = '/account/security';
    navigationFixture.search = '';
    navigationFixture.suspend = false;
    authFixture.user = auth.user;
    authFixture.loading = auth.loading;

    const { container } = render(
      <AppShell>
        <AccountSecurityPage />
      </AppShell>,
    );

    const main = expectSingleFocusableSkipTarget(container);
    expect(main).toContainElement(screen.getByRole('status'));
    expect(screen.getByRole('status')).toHaveTextContent(status);
    expect(screen.queryByText('Account security')).not.toBeInTheDocument();
    expect(screen.queryByText('Passkeys')).not.toBeInTheDocument();
    expect(screen.queryByText('Sidebar')).not.toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
    expectNoSideEffects();
  });
});
