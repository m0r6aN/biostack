import ToolsPage from '@/app/tools/page';
import { apiClient } from '@/lib/api';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/components/marketing/MarketingNav', () => ({ MarketingNav: () => <nav>Navigation</nav> }));
vi.mock('@/components/marketing/MarketingFooter', () => ({ MarketingFooter: () => <footer>Footer</footer> }));
vi.mock('@/lib/AuthProvider', () => {
  const value = { user: { id: 'user-1', email: 'user@example.test' } };
  return { useAuth: () => value };
});
vi.mock('@/lib/context', () => {
  // Stable references: fresh functions each render would re-trigger the page's effects forever.
  const value = { currentProfileId: 'profile-1', profiles: [{ id: 'profile-1' }], setProfiles: vi.fn(), setCurrentProfileId: vi.fn() };
  return { useProfile: () => value };
});
vi.mock('@/lib/api', () => ({
  apiClient: {
    getAllKnowledgeCompounds: vi.fn(),
    getCompounds: vi.fn(),
    getProfiles: vi.fn(),
    createCompound: vi.fn(),
    checkOverlap: vi.fn(),
  },
}));

function enterValues() {
  fireEvent.change(screen.getByLabelText('Powder amount'), { target: { value: '5' } });
  fireEvent.change(screen.getByText('How much liquid was added?').closest('label')!.querySelector('input')!, { target: { value: '2' } });
  fireEvent.change(screen.getByText('What amount are you calculating?').closest('label')!.querySelector('input')!, { target: { value: '250' } });
}

describe('"Track this in my stack" does not write calculator output into the record', () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([]);
    vi.mocked(apiClient.getCompounds).mockResolvedValue([]);
    vi.mocked(apiClient.getProfiles).mockResolvedValue([]);
    vi.mocked(apiClient.checkOverlap).mockResolvedValue([]);
    vi.mocked(apiClient.createCompound).mockImplementation(async (_profileId, data) => ({ ...data, id: 'created-1' }) as never);
  });

  it('creates the compound with generic notes only, even with a calculated draw on screen', async () => {
    render(<ToolsPage />);
    await act(async () => { await Promise.resolve(); });
    enterValues();
    // A result is on screen: 5 mg in 2 mL, 250 mcg => 0.1 mL => 10 U-100 units.
    expect(screen.getByRole('meter', { name: /calculated draw/i })).toHaveAttribute('aria-valuenow', '10');
    expect(screen.getByText(/Calculated draw: 10 units on a U-100 syringe/)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Track this in my stack' }));
    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));

    const [profileId, payload] = vi.mocked(apiClient.createCompound).mock.calls[0];
    expect(profileId).toBe('profile-1');
    expect(payload.notes).toBe('Tracked from tools.');
    await waitFor(() => expect(screen.getByText('Tracked in your stack. Calculation saved.')).toBeInTheDocument());
    // Nothing the calculator produced or was given may appear anywhere in the record payload.
    const serialized = JSON.stringify(payload);
    for (const forbidden of [/Calculated draw/i, /units/i, /U-100/i, /\bmL\b/, /\bmcg\b/, /2,?500/, /\b250\b/, /\b0\.1\b/]) {
      expect(serialized).not.toMatch(forbidden);
    }
  });

  it('writes the same generic notes when no result is showing', async () => {
    render(<ToolsPage />);
    await act(async () => { await Promise.resolve(); });
    fireEvent.click(screen.getByRole('button', { name: 'Track this in my stack' }));
    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));
    expect(vi.mocked(apiClient.createCompound).mock.calls[0][1].notes).toBe('Tracked from tools.');
    // No calculation was entered, so no "saved" claim is made and nothing throws.
    await waitFor(() => expect(screen.getByText('Tracked in your stack.')).toBeInTheDocument());
  });
});
