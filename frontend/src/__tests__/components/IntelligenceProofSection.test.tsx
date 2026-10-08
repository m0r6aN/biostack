import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { IntelligenceProofSection } from '@/components/marketing/IntelligenceProofSection';
import { apiClient } from '@/lib/api';
import type { KnowledgeEntry } from '@/lib/types';

// Mock the apiClient
vi.mock('@/lib/api', () => ({
  apiClient: {
    getAllKnowledgeCompounds: vi.fn(),
  },
}));

const mockGetAllKnowledgeCompounds = vi.mocked(apiClient.getAllKnowledgeCompounds);

describe('IntelligenceProofSection', () => {
  beforeEach(() => {
    mockGetAllKnowledgeCompounds.mockClear();
  });

  // T1: Live path with overlap
  it('renders live compound names with overlap when API returns two entries with shared arrays', async () => {
    const mockEntries: KnowledgeEntry[] = [
      {
        canonicalName: 'NAD+',
        aliases: [],
        classification: 'Cofactor',
        regulatoryStatus: 'Supplement',
        mechanismSummary: 'Test mechanism',
        evidenceTier: 'Tier-1',
        sourceReferences: [],
        notes: '',
        pathways: ['cellular energy'],
        benefits: ['mitochondrial health'],
        pairsWellWith: [],
        avoidWith: [],
        drugInteractions: [],
      },
      {
        canonicalName: 'Zorbatide',
        aliases: [],
        classification: 'Peptide',
        regulatoryStatus: 'Investigational',
        mechanismSummary: 'Test mechanism',
        evidenceTier: 'Tier-2',
        sourceReferences: [],
        notes: '',
        pathways: ['cellular energy'],
        benefits: ['tissue repair'],
        pairsWellWith: [],
        avoidWith: [],
        drugInteractions: [],
      },
    ];

    mockGetAllKnowledgeCompounds.mockResolvedValue(mockEntries);

    const { container } = render(<IntelligenceProofSection />);

    // Wait for the data to load and check compound names
    await waitFor(() => {
      const text = container.textContent || '';
      expect(text).toContain('NAD+');
      expect(text).toContain('Zorbatide');
      // Check for overlap label (sorted alphabetically: NAD+ + Zorbatide)
      expect(text).toContain('NAD+ + Zorbatide');
    });
  });

  // T2: Live path without overlap
  it('renders live compound names without relationship when no overlapping arrays', async () => {
    const mockEntries: KnowledgeEntry[] = [
      {
        canonicalName: 'Alpha-GPC',
        aliases: [],
        classification: 'Nootropic',
        regulatoryStatus: 'Supplement',
        mechanismSummary: 'Test mechanism',
        evidenceTier: 'Tier-2',
        sourceReferences: [],
        notes: '',
        pathways: ['acetylcholine'],
        benefits: ['cognitive'],
        pairsWellWith: [],
        avoidWith: [],
        drugInteractions: [],
      },
      {
        canonicalName: 'Beta-Alanine',
        aliases: [],
        classification: 'Amino Acid',
        regulatoryStatus: 'Supplement',
        mechanismSummary: 'Test mechanism',
        evidenceTier: 'Tier-1',
        sourceReferences: [],
        notes: '',
        pathways: ['carnosine'],
        benefits: ['endurance'],
        pairsWellWith: [],
        avoidWith: [],
        drugInteractions: [],
      },
    ];

    mockGetAllKnowledgeCompounds.mockResolvedValue(mockEntries);

    const { container } = render(<IntelligenceProofSection />);

    // Wait for the data to load and check compound names
    await waitFor(() => {
      const text = container.textContent || '';
      expect(text).toContain('Alpha-GPC');
      expect(text).toContain('Beta-Alanine');
      // Check that no relationship label is shown
      expect(text).not.toContain('Alpha-GPC + Beta-Alanine');
    });
  });

  // T3: Fetch failure fallback
  it('renders fallback BPC-157 and TB-500 when API throws error', async () => {
    mockGetAllKnowledgeCompounds.mockRejectedValue(new Error('Network error'));

    const { container } = render(<IntelligenceProofSection />);

    // The fallback should be rendered immediately
    let text = container.textContent || '';
    expect(text).toContain('BPC-157');
    expect(text).toContain('TB-500');

    // Wait for the API call to complete
    await waitFor(() => {
      expect(mockGetAllKnowledgeCompounds).toHaveBeenCalled();
    });

    // Verify fallback is still shown after error
    text = container.textContent || '';
    expect(text).toContain('BPC-157');
    expect(text).toContain('TB-500');
  });

  // T4: Empty response fallback
  it('renders fallback BPC-157 and TB-500 when API returns empty array', async () => {
    mockGetAllKnowledgeCompounds.mockResolvedValue([]);

    const { container } = render(<IntelligenceProofSection />);

    // The fallback should be rendered immediately
    let text = container.textContent || '';
    expect(text).toContain('BPC-157');
    expect(text).toContain('TB-500');

    // Wait for the API call to complete
    await waitFor(() => {
      expect(mockGetAllKnowledgeCompounds).toHaveBeenCalled();
    });

    // Verify fallback is still shown
    text = container.textContent || '';
    expect(text).toContain('BPC-157');
    expect(text).toContain('TB-500');
  });

  // T5: Compact mode
  it('hides CTAs but shows proof content in compact mode', async () => {
    mockGetAllKnowledgeCompounds.mockResolvedValue([]);

    const { container } = render(<IntelligenceProofSection compact={true} />);

    // CTAs should not be present
    expect(screen.queryByText('Start free')).not.toBeInTheDocument();
    expect(screen.queryByText('See what Operator unlocks')).not.toBeInTheDocument();

    // Content should still be rendered
    expect(screen.getByText('See what BioStack catches')).toBeInTheDocument();
    const text = container.textContent || '';
    expect(text).toContain('BPC-157');
  });

  // T6: No-claims assertion
  it('does not render any numeric dose or recommendation strings', async () => {
    const mockEntries: KnowledgeEntry[] = [
      {
        canonicalName: 'Test-A',
        aliases: [],
        classification: 'Peptide',
        regulatoryStatus: 'Investigational',
        mechanismSummary: 'Test mechanism',
        evidenceTier: 'Tier-2',
        sourceReferences: [],
        notes: '',
        pathways: [],
        benefits: [],
        pairsWellWith: [],
        avoidWith: [],
        drugInteractions: [],
        recommendedDosage: '250mcg',
        frequency: 'twice daily',
      },
      {
        canonicalName: 'Test-B',
        aliases: [],
        classification: 'Peptide',
        regulatoryStatus: 'Investigational',
        mechanismSummary: 'Test mechanism',
        evidenceTier: 'Tier-1',
        sourceReferences: [],
        notes: '',
        pathways: [],
        benefits: [],
        pairsWellWith: [],
        avoidWith: [],
        drugInteractions: [],
        recommendedDosage: '500mg',
        frequency: 'once daily',
      },
    ];

    mockGetAllKnowledgeCompounds.mockResolvedValue(mockEntries);

    const { container } = render(<IntelligenceProofSection />);

    // Wait for the data to load
    await waitFor(() => {
      const text = container.textContent || '';
      expect(text).toContain('Test-A');
    });

    const text = container.textContent || '';

    // Assert that no dose-related strings appear
    expect(text).not.toContain('recommendedDosage');
    expect(text).not.toContain('250mcg');
    expect(text).not.toContain('500mg');
    expect(text).not.toContain('frequency');
    expect(text).not.toContain('twice daily');
    expect(text).not.toContain('once daily');
    expect(text).not.toContain('mcg');
    expect(text).not.toContain('mg/day');
  });
});
