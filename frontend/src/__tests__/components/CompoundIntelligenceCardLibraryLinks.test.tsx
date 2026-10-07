import { CompoundIntelligenceCard } from '@/components/knowledge/CompoundIntelligenceCard';
import type { KnowledgeEntry } from '@/lib/types';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/context', () => ({
  useProfile: () => ({ currentProfileId: null, profiles: [] }),
}));

vi.mock('@/lib/settings', () => ({
  useSettings: () => ({ settings: { weightUnit: 'kg' } }),
}));

const entry: KnowledgeEntry = {
  canonicalName: 'BPC-157',
  aliases: [],
  classification: 'Peptide',
  regulatoryStatus: 'Research',
  mechanismSummary: 'Tissue support',
  evidenceTier: 'Moderate',
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
} as unknown as KnowledgeEntry;

describe('CompoundIntelligenceCard — library links both ways', () => {
  it('shows no dossier link or add-to-protocol action by default (no surface specified)', () => {
    render(<CompoundIntelligenceCard entry={entry} />);
    expect(screen.queryByRole('link', { name: 'Open full dossier' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /add to protocol/i })).not.toBeInTheDocument();
  });

  it('the /compounds side panel (compound-detail surface) links to the full dossier by slug', () => {
    render(<CompoundIntelligenceCard entry={entry} recommendationSurface="compound-detail" />);
    expect(screen.getByRole('link', { name: 'Open full dossier' })).toHaveAttribute('href', '/knowledge/bpc-157');
  });

  it('does not show a dossier link on the dossier page itself', () => {
    render(<CompoundIntelligenceCard entry={entry} recommendationSurface="knowledge-detail" isSignedIn />);
    expect(screen.queryByRole('link', { name: 'Open full dossier' })).not.toBeInTheDocument();
  });

  it('does not show a dossier link on knowledge-search results', () => {
    render(<CompoundIntelligenceCard entry={entry} recommendationSurface="knowledge-search" />);
    expect(screen.queryByRole('link', { name: 'Open full dossier' })).not.toBeInTheDocument();
  });

  it('signed-in visitors on the dossier get an "Add to protocol" deep link with the compound slug', () => {
    render(<CompoundIntelligenceCard entry={entry} recommendationSurface="knowledge-detail" isSignedIn />);
    expect(screen.getByRole('link', { name: 'Add to protocol' })).toHaveAttribute(
      'href',
      '/compounds?compound=bpc-157'
    );
  });

  it('anonymous visitors on the dossier see the sign-in CTA instead, returning to this dossier', () => {
    render(<CompoundIntelligenceCard entry={entry} recommendationSurface="knowledge-detail" isSignedIn={false} />);
    expect(screen.queryByRole('link', { name: 'Add to protocol' })).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sign in to add to protocol' })).toHaveAttribute(
      'href',
      '/auth/signin?callbackUrl=%2Fknowledge%2Fbpc-157'
    );
  });

  it('never shows the "Add to protocol" action from the /compounds side panel', () => {
    render(<CompoundIntelligenceCard entry={entry} recommendationSurface="compound-detail" isSignedIn />);
    expect(screen.queryByRole('link', { name: /add to protocol/i })).not.toBeInTheDocument();
  });
});
