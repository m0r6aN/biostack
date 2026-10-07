import KnowledgePage from '@/app/knowledge/page';
import PcacVoteInsightPage from '@/app/knowledge/insights/fda-pcac-2026-peptide-vote/page';
import MethodologyPage from '@/app/knowledge/methodology/page';
import { AppShell } from '@/components/AppShell';
import { CompoundDossierExperience } from '@/components/knowledge/CompoundDossierExperience';
import { apiClient } from '@/lib/api';
import { cleanup, render, screen } from '@testing-library/react';
import type { ComponentProps, ReactElement } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

// Every knowledge route must expose exactly one `main#main` (tabindex="-1") for
// anonymous, auth-loading and authenticated visitors. AppShell owns that landmark
// on /knowledge only when a user is present; otherwise it passes the page through
// and the page must own it — with marketing navigation/footer OUTSIDE the main so
// the root skip link actually bypasses them.
const SKIP_LINK_TARGET_ID = 'main';

// Test-only fixtures. Fictional user, fictional compound, no network, no real
// token/cookie/storage, no navigation, no search/overlap submission.
const FIXTURE_USER = { id: 'fixture-user', email: 'fixture@example.invalid', displayName: 'Fixture', role: 0 };
const FIXTURE_SLUG = 'fixturamide-zz';
const FIXTURE_ENTRY = {
  canonicalName: 'Fixturamide ZZ (fictional)',
  aliases: ['fixture-alias'],
  classification: 'Fixture class',
  regulatoryStatus: 'fixture-status',
  mechanismSummary: 'Fixture mechanism — not a real compound.',
  evidenceTier: 'fixture',
  sourceReferences: [],
  notes: '',
  pathways: [],
  benefits: [],
  pairsWellWith: [],
  avoidWith: [],
  compatibleBlends: [],
  recommendedDosage: '',
  frequency: '',
  preferredTimeOfDay: '',
  weeklyDosageSchedule: [],
  drugInteractions: [],
  optimizationProtein: '',
  optimizationCarbs: '',
  optimizationSupplements: '',
  optimizationSleep: '',
  optimizationExercise: '',
};
const FIXTURE_ERROR = 'fixture: dossier unavailable';

const navigationFixture = vi.hoisted(() => ({ pathname: '/knowledge' }));
const authFixture = vi.hoisted(() => ({ user: null as typeof FIXTURE_USER | null, loading: false }));
const NEVER_SETTLES = vi.hoisted(() => new Promise<never>(() => undefined));

vi.mock('next/navigation', () => ({
  usePathname: () => navigationFixture.pathname,
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

vi.mock('@/lib/api', () => ({
  apiClient: {
    getAllKnowledgeCompounds: vi.fn(() => Promise.reject(new Error('fixture: search is never exercised'))),
    checkOverlap: vi.fn(() => Promise.reject(new Error('fixture: overlap is never exercised'))),
    getKnowledgeEntry: vi.fn(),
  },
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

// Content-card adapters: the dossier cards render the fictional entry name only
// (no real compound payload, no relationship fetch).
vi.mock('@/components/knowledge/CompoundIntelligenceCard', () => ({
  CompoundIntelligenceCard: ({ entry }: { entry: { canonicalName: string } }) => (
    <article>{entry.canonicalName}</article>
  ),
}));

vi.mock('@/components/knowledge/CompoundRelationshipsSection', () => ({
  CompoundRelationshipsSection: ({ compoundName }: { compoundName: string }) => (
    <section>Relationships for {compoundName}</section>
  ),
}));

type Auth = 'anonymous' | 'loading' | 'authenticated';

type RouteCase = {
  route: string;
  auth: Auth;
  element: ReactElement;
  dossier?: 'entry' | 'pending' | 'error';
  ready: () => Promise<HTMLElement> | HTMLElement;
};

const dossierReady: Record<NonNullable<RouteCase['dossier']>, RouteCase['ready']> = {
  entry: () => screen.findByText('Fixturamide ZZ (fictional)'),
  pending: () => screen.getByRole('link', { name: 'How evidence is graded' }),
  error: () => screen.findByText(FIXTURE_ERROR),
};

function dossierCase(dossier: NonNullable<RouteCase['dossier']>, auth: Auth): [string, RouteCase] {
  return [
    `/knowledge/${FIXTURE_SLUG} ${dossier} ${auth}`,
    {
      route: `/knowledge/${FIXTURE_SLUG}`,
      auth,
      dossier,
      element: (
        <CompoundDossierExperience slug={FIXTURE_SLUG} initialEntry={dossier === 'entry' ? FIXTURE_ENTRY : null} />
      ),
      ready: dossierReady[dossier],
    },
  ];
}

const indexReady = () => screen.getByRole('button', { name: 'Search' });
const methodologyReady = () => screen.getByRole('heading', { level: 1 });
const insightReady = () => screen.getByRole('heading', { level: 1 });

// The 13 knowledge states.
const cases: Array<[label: string, routeCase: RouteCase]> = [
  ['/knowledge anonymous', { route: '/knowledge', auth: 'anonymous', element: <KnowledgePage />, ready: indexReady }],
  ['/knowledge loading', { route: '/knowledge', auth: 'loading', element: <KnowledgePage />, ready: indexReady }],
  ['/knowledge authenticated', { route: '/knowledge', auth: 'authenticated', element: <KnowledgePage />, ready: indexReady }],
  ['/knowledge/methodology anonymous', { route: '/knowledge/methodology', auth: 'anonymous', element: <MethodologyPage />, ready: methodologyReady }],
  ['/knowledge/methodology authenticated', { route: '/knowledge/methodology', auth: 'authenticated', element: <MethodologyPage />, ready: methodologyReady }],
  ['/knowledge/insights/fda-pcac-2026-peptide-vote anonymous', { route: '/knowledge/insights/fda-pcac-2026-peptide-vote', auth: 'anonymous', element: <PcacVoteInsightPage />, ready: insightReady }],
  ['/knowledge/insights/fda-pcac-2026-peptide-vote authenticated', { route: '/knowledge/insights/fda-pcac-2026-peptide-vote', auth: 'authenticated', element: <PcacVoteInsightPage />, ready: insightReady }],
  dossierCase('entry', 'anonymous'),
  dossierCase('entry', 'authenticated'),
  dossierCase('pending', 'anonymous'),
  dossierCase('pending', 'authenticated'),
  dossierCase('error', 'anonymous'),
  dossierCase('error', 'authenticated'),
];

function applyAuth(auth: Auth) {
  authFixture.user = auth === 'authenticated' ? FIXTURE_USER : null;
  authFixture.loading = auth === 'loading';
}

function arrange(routeCase: RouteCase) {
  navigationFixture.pathname = routeCase.route;
  applyAuth(routeCase.auth);
  if (routeCase.dossier === 'pending') {
    vi.mocked(apiClient.getKnowledgeEntry).mockReturnValue(NEVER_SETTLES);
  } else if (routeCase.dossier === 'error') {
    vi.mocked(apiClient.getKnowledgeEntry).mockRejectedValue(new Error(FIXTURE_ERROR));
  }
  return render(<AppShell>{routeCase.element}</AppShell>);
}

function expectSingleFocusableSkipTarget(container: HTMLElement) {
  const mains = screen.getAllByRole('main');
  expect(mains).toHaveLength(1);
  const [main] = mains;
  expect(main.tagName).toBe('MAIN');
  expect(main).toHaveAttribute('id', SKIP_LINK_TARGET_ID);
  expect(main).toHaveAttribute('tabindex', '-1');
  expect(container.querySelectorAll('main')).toHaveLength(1);
  expect(container.querySelectorAll(`#${SKIP_LINK_TARGET_ID}`)).toHaveLength(1);
  expect(container.querySelector(`#${SKIP_LINK_TARGET_ID}`)).toBe(main);
  expect(container.querySelectorAll('main main')).toHaveLength(0);

  main.focus();
  expect(document.activeElement).toBe(main);
  return main;
}

function expectAnonymousOwnership(main: HTMLElement) {
  // Page-owned main: marketing chrome stays outside it so the skip link bypasses it.
  const nav = screen.getByRole('navigation', { name: 'Marketing' });
  const footer = screen.getByRole('contentinfo');
  expect(main).not.toContainElement(nav);
  expect(main).not.toContainElement(footer);
  expect(nav.compareDocumentPosition(main) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  expect(main.compareDocumentPosition(footer) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  expect(screen.queryByText('Sidebar')).not.toBeInTheDocument();
  expect(screen.queryByLabelText('App-wide disclaimer')).not.toBeInTheDocument();
  expect(screen.queryByRole('status')).not.toBeInTheDocument();
}

function expectAuthenticatedOwnership(main: HTMLElement) {
  // Shell-owned main: sidebar and app-wide disclaimer preserved, content inside.
  const sidebar = screen.getByText('Sidebar');
  const disclaimer = screen.getByLabelText('App-wide disclaimer');
  expect(main).not.toContainElement(sidebar);
  expect(main).toContainElement(disclaimer);
  expect(screen.queryByRole('status')).not.toBeInTheDocument();
}

function expectNoSideEffects() {
  expect(apiClient.getAllKnowledgeCompounds).not.toHaveBeenCalled();
  expect(apiClient.checkOverlap).not.toHaveBeenCalled();
  for (const [slug] of vi.mocked(apiClient.getKnowledgeEntry).mock.calls) {
    expect(slug).toBe(FIXTURE_SLUG);
  }
}

describe('knowledge routes own exactly one focusable main#main inside AppShell', () => {
  beforeEach(() => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockClear();
    vi.mocked(apiClient.checkOverlap).mockClear();
    vi.mocked(apiClient.getKnowledgeEntry).mockReset();
    vi.mocked(apiClient.getKnowledgeEntry).mockReturnValue(NEVER_SETTLES);
  });

  afterEach(() => {
    cleanup();
  });

  it.each(cases)('%s', async (_label, routeCase) => {
    const { container } = arrange(routeCase);
    const content = await routeCase.ready();

    const main = expectSingleFocusableSkipTarget(container);
    expect(main).toContainElement(content);
    if (routeCase.auth === 'authenticated') {
      expectAuthenticatedOwnership(main);
    } else {
      expectAnonymousOwnership(main);
    }
    expectNoSideEffects();
  });

  it('hands main ownership from the page to AppShell when an anonymous visitor becomes a user', async () => {
    const [, anonymous] = cases[0];
    const { container, rerender } = arrange(anonymous);
    await anonymous.ready();
    const pageMain = expectSingleFocusableSkipTarget(container);
    expectAnonymousOwnership(pageMain);

    applyAuth('authenticated');
    rerender(<AppShell>{anonymous.element}</AppShell>);
    await anonymous.ready();

    const shellMain = expectSingleFocusableSkipTarget(container);
    expect(shellMain).not.toBe(pageMain);
    expectAuthenticatedOwnership(shellMain);
    expect(shellMain).toContainElement(screen.getByRole('button', { name: 'Search' }));
    expectNoSideEffects();
  });
});
