import ToolsPage from '@/app/tools/page';
import { UnifiedDosingCalculator } from '@/components/calculators/UnifiedDosingCalculator';
import { CALCULATOR_ROUTES } from '@/lib/calculatorSecurity';
import { onTrack } from '@/lib/telemetry';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/components/marketing/MarketingNav', () => ({ MarketingNav: () => <nav>Navigation</nav> }));
vi.mock('@/components/marketing/MarketingFooter', () => ({ MarketingFooter: () => <footer>Footer</footer> }));
vi.mock('@/lib/AuthProvider', () => ({ useAuth: () => ({ user: null }) }));
vi.mock('@/lib/context', () => {
  // Stable references: fresh functions each render would re-trigger the page's effects forever.
  const value = { currentProfileId: null, profiles: [], setProfiles: vi.fn(), setCurrentProfileId: vi.fn() };
  return { useProfile: () => value };
});

/**
 * Network spies. Every browser channel a calculator could use to send data off the page is replaced with a
 * recorder: fetch, XMLHttpRequest, sendBeacon, WebSocket, EventSource, and <img>/<script>/<link>/<iframe> src loads.
 */
const calls: string[] = [];
const originals: Record<string, unknown> = {};

function record(kind: string, detail: unknown = '') {
  calls.push(`${kind} ${String(detail)}`);
}

beforeEach(() => {
  calls.length = 0;
  window.localStorage.clear();

  originals.fetch = globalThis.fetch;
  globalThis.fetch = vi.fn(async (input: RequestInfo | URL) => {
    record('fetch', input instanceof Request ? input.url : input);
    // The page loads its compound list once on mount; answer it so the page settles. Later calls are recorded and fail.
    return new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json' } });
  }) as typeof fetch;

  originals.XMLHttpRequest = globalThis.XMLHttpRequest;
  globalThis.XMLHttpRequest = class {
    open(method: string, url: string) { record('xhr', `${method} ${url}`); }
    send() { record('xhr-send'); }
    setRequestHeader() {}
    addEventListener() {}
  } as unknown as typeof XMLHttpRequest;

  originals.sendBeacon = navigator.sendBeacon;
  Object.defineProperty(navigator, 'sendBeacon', { configurable: true, value: (url: string) => { record('beacon', url); return true; } });

  originals.WebSocket = globalThis.WebSocket;
  globalThis.WebSocket = class { constructor(url: string) { record('websocket', url); } } as unknown as typeof WebSocket;

  originals.EventSource = (globalThis as { EventSource?: unknown }).EventSource;
  (globalThis as { EventSource?: unknown }).EventSource = class { constructor(url: string) { record('eventsource', url); } };
});

afterEach(() => {
  globalThis.fetch = originals.fetch as typeof fetch;
  globalThis.XMLHttpRequest = originals.XMLHttpRequest as typeof XMLHttpRequest;
  Object.defineProperty(navigator, 'sendBeacon', { configurable: true, value: originals.sendBeacon });
  globalThis.WebSocket = originals.WebSocket as typeof WebSocket;
  (globalThis as { EventSource?: unknown }).EventSource = originals.EventSource;
});

function enter(label: string | RegExp, value: string) {
  fireEvent.change(screen.getByLabelText(label), { target: { value } });
}

function externalResourceUrls(): string[] {
  return Array.from(document.querySelectorAll<HTMLElement>('script[src], link[href], iframe[src], img[src], video[src], audio[src], source[src]'))
    .map((el) => el.getAttribute('src') ?? el.getAttribute('href') ?? '')
    .filter((url) => /^(https?:)?\/\//i.test(url));
}

describe('calculator makes no network calls while computing', () => {
  it('UnifiedDosingCalculator: zero network calls from mount through every kind of input', () => {
    render(<UnifiedDosingCalculator />);
    enter('Powder amount', '5');
    enter(/^Diluent volume/, '2');
    enter('Desired dose', '250');
    enter(/^Doses per week/, '2');
    fireEvent.click(screen.getByRole('button', { name: /daily total/i }));
    fireEvent.click(screen.getByRole('button', { name: /known concentration/i }));
    enter('Concentration', '2500');
    enter('Desired dose', '3000'); // over one syringe
    enter('Desired dose', '-1'); // invalid
    enter('Desired dose', ''); // cleared

    expect(screen.queryByRole('meter')).not.toBeInTheDocument();
    expect(calls).toEqual([]);
  });

  it('/tools page: after it settles, entering values, changing units and modes makes zero network calls', async () => {
    render(<ToolsPage />);
    await waitFor(() => expect((globalThis.fetch as ReturnType<typeof vi.fn>).mock.calls.length).toBeGreaterThanOrEqual(1));
    await act(async () => { await new Promise((resolveWait) => setTimeout(resolveWait, 400)); }); // settle debounced effects
    const onMount = [...calls];
    // Mount-time traffic is limited to the same-origin knowledge list used for compound suggestions.
    expect(onMount.every((entry) => entry.startsWith('fetch ') && /\/api\/v1\/knowledge\//.test(entry))).toBe(true);
    calls.length = 0;

    fireEvent.change(screen.getByLabelText('Powder amount'), { target: { value: '5' } });
    fireEvent.change(screen.getByText('How much liquid was added?').closest('label')!.querySelector('input')!, { target: { value: '2' } });
    fireEvent.change(screen.getByText('What amount are you calculating?').closest('label')!.querySelector('input')!, { target: { value: '250' } });
    expect(screen.getByRole('meter', { name: /calculated draw/i })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Powder amount'), { target: { value: '0' } });
    fireEvent.change(screen.getByLabelText('Powder amount'), { target: { value: '1e308' } });
    fireEvent.change(screen.getByLabelText('Powder amount'), { target: { value: '10' } });
    fireEvent.click(screen.getByRole('button', { name: /convert units/i }));
    await act(async () => { await new Promise((resolveWait) => setTimeout(resolveWait, 400)); });

    expect(calls).toEqual([]);
  });

  it('records no telemetry/analytics events and defines no analytics globals', () => {
    const events: unknown[] = [];
    const unsubscribe = onTrack((event) => events.push(event));
    const analyzerEvents: unknown[] = [];
    const handler = (event: Event) => analyzerEvents.push(event);
    window.addEventListener('biostack:analyzer_event', handler);

    render(<UnifiedDosingCalculator />);
    enter('Powder amount', '5');
    enter(/^Diluent volume/, '2');
    enter('Desired dose', '250');

    unsubscribe();
    window.removeEventListener('biostack:analyzer_event', handler);
    expect(events).toEqual([]);
    expect(analyzerEvents).toEqual([]);
    for (const name of ['dataLayer', 'gtag', 'ga', 'fbq', 'posthog', 'plausible', 'clarity', 'hj', 'analytics', 'mixpanel', 'amplitude']) {
      expect((window as unknown as Record<string, unknown>)[name]).toBeUndefined();
    }
  });

  it('renders no off-origin script, stylesheet, iframe or image', () => {
    render(<ToolsPage />);
    expect(externalResourceUrls()).toEqual([]);
  });
});

describe('calculator source stays network-free', () => {
  const root = resolve(__dirname, '../../..');
  const read = (relative: string) => readFileSync(resolve(root, relative), 'utf8');
  const stripComments = (source: string) => source.replace(/\/\*[\s\S]*?\*\//g, '').replace(/^\s*\/\/.*$/gm, '');

  // The pure calculation core and the presentational calculator components. (ToolsDecisionSurface also hosts the
  // compound-overlap feature, which legitimately calls the API when the user asks for it, so it is covered by the
  // runtime test above instead.)
  const pureFiles = [
    'src/lib/dosingCalculator.ts',
    'src/components/calculators/UnifiedDosingCalculator.tsx',
    'src/components/calculators/SyringeDrawVisualizer.tsx',
    'src/components/calculators/VialVisualizer.tsx',
    'src/components/calculators/ConversionCalc.tsx',
    'src/components/calculators/ReconstitutionCalc.tsx',
    'src/components/calculators/VolumeCalc.tsx',
  ];

  it.each(pureFiles)('%s has no network or analytics code', (file) => {
    const source = stripComments(read(file));
    expect(source).not.toMatch(/\bfetch\s*\(/);
    expect(source).not.toMatch(/XMLHttpRequest|sendBeacon|new\s+WebSocket|new\s+EventSource/);
    expect(source).not.toMatch(/from\s+['"]@\/lib\/(api|telemetry|analyzerAnalytics|apiBase)['"]/);
    expect(source).not.toMatch(/from\s+['"](next\/script|axios|ky|swr|@tanstack\/react-query)['"]/);
  });

  const pageFiles = [
    'src/app/layout.tsx',
    ...CALCULATOR_ROUTES.map((route) => `src/app${route}/page.tsx`),
    'src/components/marketing/PublicCalculatorExperience.tsx',
    'src/components/tools/ToolsDecisionSurface.tsx',
    'src/components/marketing/MarketingNav.tsx',
    'src/components/marketing/MarketingFooter.tsx',
  ];

  it.each(pageFiles)('%s loads no analytics, tracking or third-party script', (file) => {
    const source = stripComments(read(file));
    expect(source).not.toMatch(/next\/script|<Script\b/);
    expect(source).not.toMatch(/<script[^>]*\ssrc=/i);
    expect(source).not.toMatch(/googletagmanager|google-analytics|gtag\(|plausible|posthog|clarity\.ms|hotjar|segment\.(com|io)|mixpanel|amplitude|fbevents|facebook\.net|vercel\/analytics|@vercel\/speed-insights|sentry/i);
    expect(source).not.toMatch(/from\s+['"]@\/lib\/(telemetry|analyzerAnalytics)['"]/);
  });
});
