import ToolsPage from '@/app/tools/page';
import { saveAnonymousToolArtifact } from '@/lib/anonymousTools';
import { act, fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

// The calculator no longer offers daily or weekly totals. A calculation saved earlier with one of them must not
// be reinterpreted as a single amount: it opens with the amount blank and says why.

vi.mock('@/components/marketing/MarketingNav', () => ({ MarketingNav: () => <nav>Navigation</nav> }));
vi.mock('@/components/marketing/MarketingFooter', () => ({ MarketingFooter: () => <footer>Footer</footer> }));
vi.mock('@/lib/AuthProvider', () => {
  const value = { user: null, loading: false };
  return { useAuth: () => value, useOptionalAuth: () => value };
});
vi.mock('@/lib/context', () => {
  const value = { currentProfileId: null, profiles: [], setProfiles: vi.fn(), setCurrentProfileId: vi.fn() };
  return { useProfile: () => value };
});
vi.mock('@/lib/api', () => ({
  apiClient: {
    getAllKnowledgeCompounds: vi.fn().mockResolvedValue([]),
    getCompounds: vi.fn().mockResolvedValue([]),
    getProfiles: vi.fn().mockResolvedValue([]),
    createCompound: vi.fn(),
    checkOverlap: vi.fn().mockResolvedValue([]),
  },
}));

function seed(doseBasis: string) {
  saveAnonymousToolArtifact({
    calculatorType: 'dose',
    substances: ['Compound A'],
    inputs: {
      powderAmount: 5, powderUnit: 'mg', diluentVolumeMl: 2, concentrationSource: 'reconstitution',
      knownConcentration: Number.NaN, concentrationUnit: 'mcg/mL', desiredDose: 1750, desiredDoseUnit: 'mcg',
      doseBasis, splitCount: 7, primaryCompound: 'Compound A', additionalCompound: '',
    },
    outputs: { primaryAnswer: 'Calculated draw: 70 units on a U-100 syringe' },
    reconstitutionInstructions: [], storageInstructions: [], compatibilityFindings: [],
  });
}

async function openSaved() {
  render(<ToolsPage />);
  await act(async () => { await Promise.resolve(); });
  fireEvent.click(screen.getByRole('button', { name: /Saved calculations/ }));
  fireEvent.click(screen.getAllByRole('button', { name: 'Open' })[0]);
}

describe('opening a saved calculation', () => {
  beforeEach(() => window.localStorage.clear());

  it('blanks the amount of a calculation saved with a weekly total, and explains', async () => {
    seed('weekly-total');
    await openSaved();
    expect(screen.getByLabelText(/Amount to convert/)).toHaveValue(null);
    expect(screen.getByLabelText('Powder amount')).toHaveValue(5);
    expect(screen.getByText(/saved with a daily or weekly total, which is no longer offered/)).toBeInTheDocument();
    expect(screen.queryByRole('meter', { name: /calculated draw/i })).not.toBeInTheDocument();
  });

  it('restores a per-amount calculation unchanged', async () => {
    seed('per-dose');
    await openSaved();
    expect(screen.getByLabelText(/Amount to convert/)).toHaveValue(1750);
    expect(screen.getByText('Calculation opened. Edit any field, then save again.')).toBeInTheDocument();
  });

  it('no longer offers a dose basis or a doses-per-period field', async () => {
    render(<ToolsPage />);
    await act(async () => { await Promise.resolve(); });
    expect(screen.queryByLabelText('Dose basis')).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/^Doses per/)).not.toBeInTheDocument();
    expect(screen.getByText('Optional: label this calculation')).toBeInTheDocument();
  });
});
