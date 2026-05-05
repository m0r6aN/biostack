import { render, screen, fireEvent } from '@testing-library/react';
import { ClaimCard } from '@/components/research/ClaimCard';
import type { EvidenceClaim } from '@/lib/research/types';

const claim: EvidenceClaim = {
  claimId: 'c-001', claimType: 'warning',
  statement: 'No human safety data exist for systemic use.',
  context: { population: 'General', route: null, formulation: null, useCase: 'Research', doseText: null },
  evidenceTier: 'Limited', confidence: 'low', fieldAuthorityRequired: true,
  sourceRefs: ['src-001'],
  extractedEvidence: [{ sourceRef: 'src-001', quote: 'No human trials.', pageOrSection: 'p.12' }],
  reviewFlags: ['needs-expert-review'],
};

describe('ClaimCard', () => {
  it('renders claim type uppercased', () => {
    render(<ClaimCard claim={claim} />);
    expect(screen.getByText(/warning/i)).toBeInTheDocument();
  });

  it('renders the statement', () => {
    render(<ClaimCard claim={claim} />);
    expect(screen.getByText('No human safety data exist for systemic use.')).toBeInTheDocument();
  });

  it('shows Field Authority Required badge when fieldAuthorityRequired is true', () => {
    render(<ClaimCard claim={claim} />);
    expect(screen.getByText('Field Authority Required')).toBeInTheDocument();
  });

  it('shows — for null context fields', () => {
    render(<ClaimCard claim={claim} />);
    const dashes = screen.getAllByText('—');
    expect(dashes.length).toBeGreaterThan(0);
  });

  it('shows non-null context values directly', () => {
    render(<ClaimCard claim={claim} />);
    expect(screen.getByText('General')).toBeInTheDocument();
  });

  it('toggles extracted evidence on click', () => {
    render(<ClaimCard claim={claim} />);
    expect(screen.queryByText(/No human trials/i)).not.toBeInTheDocument();
    fireEvent.click(screen.getByText(/Show evidence/i));
    expect(screen.getByText(/No human trials/i)).toBeInTheDocument();
  });
});
