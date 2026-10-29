import ConsentPage from '@/app/onboarding/consent/page';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  CONSENT_DETAILS,
  CONSENT_SHORT_VERSION,
  CONSENT_TITLE_NEW,
  CONSENT_TITLE_RETURNING,
  CONSENT_WHAT_CHANGED,
  bulletText,
} from '@/lib/consentTextV2';
import { approvedSections, plain } from '../lib/consentApproved';

const fetchMock = vi.fn();
const locationMock = { replace: vi.fn() };
let returnTo = '%2Fprofiles%3Fbootstrap%3Dtools';

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams(`returnTo=${returnTo}`),
}));

const pendingStatus = {
  accepted: false,
  declined: false,
  currentVersion: 'bio-observational-v2',
};

describe('ConsentPage', () => {
  beforeEach(() => {
    fetchMock.mockReset();
    locationMock.replace.mockReset();
    returnTo = '%2Fprofiles%3Fbootstrap%3Dtools';
    vi.stubGlobal('fetch', fetchMock);
    vi.stubGlobal('location', locationMock);
  });

  it('records the server consent version and restores the approved return path', async () => {
    fetchMock
      .mockResolvedValueOnce({ ok: true, status: 200, json: async () => pendingStatus })
      .mockResolvedValueOnce({ ok: true, status: 200, json: async () => ({ ...pendingStatus, accepted: true }) });

    render(<ConsentPage />);

    expect(await screen.findByText('Consent record: bio-observational-v2')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'I agree and continue' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenLastCalledWith(
        '/api/v1/consent',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ consentVersion: 'bio-observational-v2' }),
        }),
      );
      expect(locationMock.replace).toHaveBeenCalledWith('/profiles?bootstrap=tools');
    });
  });

  it('persists refusal, logs out, and leaves unsaved preview on the device', async () => {
    fetchMock
      .mockResolvedValueOnce({ ok: true, status: 200, json: async () => pendingStatus })
      .mockResolvedValueOnce({ ok: true, status: 200, json: async () => ({ ...pendingStatus, declined: true }) })
      .mockResolvedValueOnce({ ok: true, status: 204 });

    render(<ConsentPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Not now' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/v1/consent/decline',
        expect.objectContaining({ method: 'POST' }),
      );
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/v1/auth/logout',
        expect.objectContaining({ method: 'POST' }),
      );
      expect(locationMock.replace).toHaveBeenCalledWith('/?consent=declined');
    });
  });

  it('sends an expired session back through sign-in without losing the return path', async () => {
    fetchMock.mockResolvedValueOnce({ ok: false, status: 401 });

    render(<ConsentPage />);

    await waitFor(() => {
      expect(locationMock.replace).toHaveBeenCalledWith(
        '/auth/signin?callbackUrl=%2Fonboarding%2Fconsent%3FreturnTo%3D%252Fprofiles%253Fbootstrap%253Dtools',
      );
    });
  });

  it('shows the approved text to a new person, with no "what changed" box', async () => {
    fetchMock.mockResolvedValueOnce({ ok: true, status: 200, json: async () => ({ ...pendingStatus, consentVersion: null }) });

    render(<ConsentPage />);

    expect(await screen.findByRole('heading', { level: 1, name: CONSENT_TITLE_NEW })).toBeInTheDocument();
    expect(screen.queryByText('What changed')).not.toBeInTheDocument();
    expect(screen.queryByText(CONSENT_TITLE_RETURNING)).not.toBeInTheDocument();
    for (const paragraph of CONSENT_SHORT_VERSION) expect(screen.getByText(paragraph)).toBeInTheDocument();
  });

  it('tells a returning v1 person what changed and uses the returning title', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true, status: 200,
      json: async () => ({ ...pendingStatus, consentVersion: 'bio-observational-v1' }),
    });

    render(<ConsentPage />);

    expect(await screen.findByRole('heading', { level: 1, name: CONSENT_TITLE_RETURNING })).toBeInTheDocument();
    expect(screen.getByText('What changed')).toBeInTheDocument();
    expect(screen.getByText(CONSENT_WHAT_CHANGED)).toBeInTheDocument();
    expect(screen.queryByRole('heading', { level: 1, name: CONSENT_TITLE_NEW })).not.toBeInTheDocument();
  });

  it('renders every approved detail section, bullet and paragraph, collapsed by default', async () => {
    fetchMock.mockResolvedValueOnce({ ok: true, status: 200, json: async () => pendingStatus });

    const { container } = render(<ConsentPage />);
    await screen.findByRole('button', { name: 'I agree and continue' });

    const sections = [...container.querySelectorAll('details')];
    expect(sections).toHaveLength(CONSENT_DETAILS.length);
    for (const details of sections) expect(details.open).toBe(false);

    const rendered = sections.map((details) => plain(details.textContent ?? ''));
    CONSENT_DETAILS.forEach((section, index) => {
      expect(rendered[index]).toContain(plain(section.heading));
      if (section.paragraph) expect(rendered[index]).toContain(plain(section.paragraph));
      for (const bullet of section.bullets ?? []) expect(rendered[index]).toContain(plain(bulletText(bullet)));
    });
  });

  it('renders the same words the approved document contains', async () => {
    fetchMock.mockResolvedValueOnce({ ok: true, status: 200, json: async () => pendingStatus });

    const { container } = render(<ConsentPage />);
    await screen.findByRole('button', { name: 'I agree and continue' });
    const page = plain(container.textContent ?? '');

    const sections = approvedSections();
    const lines = Object.entries(sections)
      .filter(([name]) => name.startsWith('Short version') || name.startsWith('Details'))
      .flatMap(([, value]) => value)
      .map((line) => plain(line.replace(/^>\s*/, '').replace(/^-\s+/, '')));
    expect(lines.length).toBeGreaterThan(20);
    for (const line of lines) expect(page).toContain(line);
  });

  it('keeps the approved button labels', async () => {
    fetchMock.mockResolvedValueOnce({ ok: true, status: 200, json: async () => pendingStatus });

    render(<ConsentPage />);

    expect(await screen.findByRole('button', { name: 'I agree and continue' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Not now' })).toBeEnabled();
  });
});
