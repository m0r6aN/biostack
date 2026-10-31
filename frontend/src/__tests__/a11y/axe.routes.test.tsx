import ConsentPage from '@/app/onboarding/consent/page';
import SignInPage from '@/app/auth/signin/page';
import ContactPage from '@/app/contact/page';
import AccountDataPage from '@/app/account/data/page';
import FaqPage from '@/app/faq/page';
import HowItWorksPage from '@/app/how-it-works/page';
import PricingPage from '@/app/pricing/page';
import PrivacyPage from '@/app/privacy/page';
import SafetyPage from '@/app/safety/page';
import SupportPage from '@/app/support/page';
import NotFoundPage from '@/app/not-found';
import RouteError from '@/app/error';
import TermsPage from '@/app/terms/page';
import { NoticeBell } from '@/components/NoticeBell';
import { Sidebar } from '@/components/Sidebar';
import { DoseLogForm } from '@/components/protocol-portal/DoseLogForm';
import { TakingTab } from '@/components/protocol-portal/tabs/TakingTab';
import { apiClient } from '@/lib/api';
import type { CompoundRecord, TakingResponse, UserNotice } from '@/lib/types';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ComponentProps, ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { blockingViolations, expectNoBlockingViolations, runAxe } from './axeHelper';

/**
 * Automated WCAG 2.2 AA gate (axe-core under jsdom) for the key routes and widgets. Runs with the normal
 * `npm test` / CI vitest job. Fails on any serious or critical violation. See docs/INITIATIVES/accessibility/AUDIT.md.
 *
 * Contrast is not checkable under jsdom; it is covered by contrastFloor.test.ts and the Playwright scan.
 */

const fetchMock = vi.fn();
const auth = vi.hoisted(() => ({
  value: { user: { id: 'u1', email: 'a@example.com', displayName: 'A Tester', role: 0 }, loading: false, logout: () => undefined } as {
    user: { id: string; email: string; displayName: string; role: number } | null; loading: boolean; logout: () => void;
  },
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/protocol-console',
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn(), prefetch: vi.fn() }),
  useSearchParams: () => new URLSearchParams(''),
}));
vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: ComponentProps<'a'>) => <a href={href} {...props}>{children}</a>,
}));
vi.mock('@/lib/AuthProvider', () => ({
  useAuth: () => auth.value,
  useOptionalAuth: () => auth.value,
}));
vi.mock('@/lib/context', () => ({
  useProfile: () => ({ isSidebarOpen: false, setSidebarOpen: vi.fn(), currentProfileId: null, profiles: [] }),
}));
vi.mock('@/lib/settings', () => ({ useSettings: () => ({ settings: { weightUnit: 'metric' } }) }));
vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <header><h1>{title}</h1></header>,
}));
vi.mock('@/components/marketing/ProviderAccessForm', () => ({ ProviderAccessForm: () => <form aria-label="Provider access"><button type="button">Request access</button></form> }));
vi.mock('@/lib/api', () => ({
  apiClient: {
    getTaking: vi.fn(), updateCompound: vi.fn(), listOpenCatalogLookups: vi.fn(), createCatalogLookup: vi.fn(),
    linkCatalogLookup: vi.fn(), declineCatalogLookup: vi.fn(),
    getCurrentSubscription: vi.fn().mockResolvedValue({ tier: 'Observer' }),
    getUnreadNoticeCount: vi.fn(), getNotices: vi.fn(), markNoticeRead: vi.fn(), markAllNoticesRead: vi.fn(), dismissNotice: vi.fn(),
  },
}));

const json = (status: number, body: unknown) => ({ ok: status >= 200 && status < 300, status, json: async () => body });

beforeEach(() => {
  fetchMock.mockReset();
  fetchMock.mockResolvedValue(json(200, {}));
  vi.stubGlobal('fetch', fetchMock);
  auth.value = { user: { id: 'u1', email: 'a@example.com', displayName: 'A Tester', role: 0 }, loading: false, logout: () => undefined };
});
afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function Shell({ children }: { children: ReactNode }) {
  // Mirrors the authenticated layout: skip link, sidebar, one main landmark.
  return (
    <>
      <a href="#main">Skip to main content</a>
      <Sidebar />
      <main id="main" tabIndex={-1}>{children}</main>
    </>
  );
}

describe('axe gate self-check', () => {
  it('actually fails on a serious/critical violation (an unnamed button and an unlabelled input)', async () => {
    const { container } = render(<main><button type="button" /><input type="text" /></main>);
    const ids = blockingViolations(await runAxe(container)).map((violation) => violation.id);
    expect(ids).toEqual(expect.arrayContaining(['button-name', 'label']));
  });
});

describe('axe: public marketing and legal pages', () => {
  const pages: Array<[string, () => ReactNode]> = [
    ['/faq', () => <FaqPage />],
    ['/how-it-works', () => <HowItWorksPage />],
    ['/safety', () => <SafetyPage />],
    ['/privacy', () => <PrivacyPage />],
    ['/terms', () => <TermsPage />],
    ['/support', () => <SupportPage />],
    ['404 (not-found)', () => <NotFoundPage />],
    ['route error boundary', () => <RouteError error={new Error('x')} reset={() => undefined} />],
  ];

  it.each(pages)('%s has no serious or critical violations', async (route, page) => {
    const { container } = render(<>{page()}</>);
    await expectNoBlockingViolations(container, route);
  });

  it('/pricing has no serious or critical violations', async () => {
    const { container } = render(await PricingPage());
    await expectNoBlockingViolations(container, '/pricing');
  });
});

describe('axe: sign-in, consent, contact', () => {
  it('/auth/signin', async () => {
    auth.value = { user: null, loading: false, logout: () => undefined };
    const { container } = render(<SignInPage />);
    await expectNoBlockingViolations(container, '/auth/signin');
  });

  it('/auth/signin with an error and the email step', async () => {
    fetchMock.mockResolvedValue({ ok: false, status: 500, json: async () => ({}) });
    const { container } = render(<SignInPage />);
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'user@example.com' } });
    fireEvent.click(screen.getByRole('button', { name: /email me a sign-in link/i }));
    await screen.findByRole('alert');
    await expectNoBlockingViolations(container, '/auth/signin (error)');
  });

  it('/onboarding/consent (details expanded)', async () => {
    fetchMock.mockResolvedValue(json(200, { accepted: false, declined: false, currentVersion: 'bio-observational-v2' }));
    const { container } = render(<ConsentPage />);
    await screen.findByText(/Consent record:/);
    container.querySelectorAll('details').forEach((details) => details.setAttribute('open', ''));
    await expectNoBlockingViolations(container, '/onboarding/consent');
  });

  it('/contact', async () => {
    const { container } = render(<ContactPage />);
    await expectNoBlockingViolations(container, '/contact');
  });
});

describe('axe: account export and deletion', () => {
  it('/account/data', async () => {
    fetchMock.mockResolvedValue(json(200, {
      tables: [{ name: 'PersonProfiles', count: 2, onDeletion: 'removed', note: '' }],
      backupRetentionDays: 35, confirmationPhrase: 'DELETE MY ACCOUNT', deletionAvailable: true,
    }));
    const { container } = render(<Shell><AccountDataPage /></Shell>);
    await screen.findByText('Profiles');
    await expectNoBlockingViolations(container, '/account/data');
  });
});

describe('axe: app shell, notices bell, taking tab and dose log', () => {
  const record = (id: string, name: string): CompoundRecord => ({
    id, personId: 'profile-1', name, status: 'Active', category: 'Supplement', startDate: '2026-10-01', endDate: null, notes: '',
    sourceType: 'Manual', doseAmount: 2, doseUnit: 'capsule', doseFrequency: 'daily', verificationState: 'unverified',
    categorySource: 'user', includeInProviderSummary: true,
  });
  const taking = (): TakingResponse => ({
    items: [
      { compound: record('1', 'Creatine'), countsTowardCap: true, suggestions: [{ slug: 'creatine-monohydrate', name: 'Creatine monohydrate', matchedOn: 'alias' }] },
      { compound: record('2', 'Kail Creatine Gummies'), countsTowardCap: true, suggestions: [] },
    ],
    cap: { limit: 8, counted: 2 },
  } as unknown as TakingResponse);

  it('sidebar (signed in, expanded)', async () => {
    const { container } = render(<Shell><h1>Dashboard</h1></Shell>);
    await expectNoBlockingViolations(container, 'sidebar');
  });

  it('notices bell, open with notes', async () => {
    const note: UserNotice = {
      id: 'n1', kind: 'catalog_match', titleKey: 'notice.catalog_match',
      params: { queryName: 'Ozempic', candidateName: 'Semaglutide', label: 'same_as' },
      actionPath: '/my-protocol?tab=taking', relatedId: null, createdAtUtc: '2026-10-30T10:00:00Z', updatedAtUtc: '2026-10-30T10:00:00Z', read: false,
    };
    vi.mocked(apiClient.getUnreadNoticeCount).mockResolvedValue(1);
    vi.mocked(apiClient.getNotices).mockResolvedValue([note]);
    const { container } = render(<NoticeBell />);
    await userEvent.click(await screen.findByRole('button', { name: 'Notes, 1 unread' }));
    await screen.findByRole('dialog', { name: 'Notes' });
    await expectNoBlockingViolations(container, 'notices bell');
  });

  it('Everything I\'m Taking, including a pending lookup status and an open dose log form', async () => {
    vi.mocked(apiClient.getTaking).mockResolvedValue(taking());
    vi.mocked(apiClient.listOpenCatalogLookups).mockResolvedValue([
      { id: 'l1', profileId: 'profile-1', compoundId: '2', queryName: 'Kail Creatine Gummies', status: 'pending', createdAtUtc: '2026-10-30T10:00:00Z', completedAtUtc: null, candidates: [] },
    ]);
    vi.mocked(apiClient.createCatalogLookup).mockResolvedValue({ id: 'x', profileId: 'profile-1', compoundId: '2', queryName: 'x', status: 'pending', createdAtUtc: '', completedAtUtc: null, candidates: [] });
    const { container } = render(<main><h1>Your Protocol</h1><TakingTab profileId="profile-1" onLogAmount={async () => undefined} /></main>);
    await screen.findByText('Please wait while we look this up…');
    const [firstLog] = screen.getAllByRole('button', { name: /^Log amount taken/ });
    await userEvent.click(firstLog);
    await expectNoBlockingViolations(container, 'taking tab');
  });

  it('dose log form with a validation error', async () => {
    const { container } = render(<main><h1>Log</h1><DoseLogForm item={{ name: 'Creatine', time: '', detail: '', compoundId: 'c1' }} onLog={async () => undefined} /></main>);
    await userEvent.click(screen.getByRole('button', { name: /^Log amount taken/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Save amount' }));
    await screen.findByRole('alert');
    await expectNoBlockingViolations(container, 'dose log (error state)');
  });
});
