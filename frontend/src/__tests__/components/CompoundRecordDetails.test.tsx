import { render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { CompoundRecordDetails, DOSE_FRAMING } from '@/components/knowledge/CompoundRecordDetails';
import type { KnowledgeEntryDetails } from '@/lib/types';

function details(overrides: Partial<KnowledgeEntryDetails> = {}): KnowledgeEntryDetails {
  return {
    compoundFamily: 'GLP-1/GIP/glucagon triple receptor agonist',
    requiresPrescription: false,
    jurisdiction: 'US',
    approvedIndications: [],
    labelStatusByUseCase: [],
    offLabelNotes: [],
    studiedUses: [{ useCase: 'obesity research', labelStatus: 'not-indicated', population: 'Adults', summary: 'Phase 3 trials studied weight change.', evidenceTier: 'Strong', sources: [] }],
    safety: {
      contraindications: [],
      warnings: [{ item: 'Dysesthesia reported more than placebo', severity: 'moderate', notes: '', sources: [] }],
      precautions: [],
      adverseEffectsCommon: ['nausea'],
      adverseEffectsSerious: [],
      monitoring: [],
    },
    doseReports: [{
      side: 'study', context: 'Phase 3 arm', amount: 12, unit: 'mg', frequency: 'once weekly', duration: '80 weeks',
      route: 'subcutaneous', population: '2,339 adults', outcome: '−25.0% body weight', sourceType: 'trial',
      sourceTitle: 'TRIUMPH-1', sourceUrl: 'https://pubmed.ncbi.nlm.nih.gov/42814954/', grade: 'Established', notes: '',
    }],
    labelDosing: [],
    claims: [{ claim: 'Weight fell vs placebo in randomized trials.', tier: 'Strong', confidence: 'high', sources: [] }],
    evidenceGaps: ['Long-term safety not reported.'],
    controversies: [],
    interactions: [],
    formulations: [],
    sources: [{ title: 'TRIUMPH-1', url: 'https://pubmed.ncbi.nlm.nih.gov/42814954/', publisher: 'PubMed', sourceType: 'paper' }],
    recordClass: 'B',
    unofficialSourceFields: [],
    reviewStatus: 'reviewed',
    ...overrides,
  };
}

describe('CompoundRecordDetails', () => {
  it('shows study doses with source link and grade, and n/a for the reported side', () => {
    render(<CompoundRecordDetails details={details()} />);
    expect(screen.getByText(DOSE_FRAMING)).toBeInTheDocument();
    const studies = screen.getByTestId('doses-studies');
    expect(within(studies).getByText(/12 mg · once weekly/)).toBeInTheDocument();
    expect(within(studies).getByText('Grade: Established')).toBeInTheDocument();
    expect(within(studies).getByRole('link', { name: /TRIUMPH-1/ })).toHaveAttribute('href', 'https://pubmed.ncbi.nlm.nih.gov/42814954/');
    expect(within(screen.getByTestId('doses-reported')).getByText('n/a')).toBeInTheDocument();
  });

  it('separates known from unknown and always shows dangers', () => {
    render(<CompoundRecordDetails details={details()} />);
    const knownUnknown = screen.getByRole('region', { name: 'Known and unknown' });
    expect(within(knownUnknown).getByText('Weight fell vs placebo in randomized trials.')).toBeInTheDocument();
    expect(within(knownUnknown).getByText('Long-term safety not reported.')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Dangers' })).toHaveTextContent('Dysesthesia');
    expect(screen.getByRole('region', { name: 'Studied uses' })).toHaveTextContent('not indicated');
  });

  it('labels unofficial safety items instead of hiding them', () => {
    render(<CompoundRecordDetails details={details({ unofficialSourceFields: ['warnings'] })} />);
    expect(screen.getByText(/Unofficial source/)).toBeInTheDocument();
    expect(screen.getByText('Dysesthesia reported more than placebo')).toBeInTheDocument();
  });

  it('shows a reported-side row with its platform and grade', () => {
    render(<CompoundRecordDetails details={details({ doseReports: [{
      side: 'reported', context: '', amount: 2, unit: 'mg', frequency: 'once weekly', duration: '', route: '', population: '',
      outcome: '', sourceType: 'reddit', sourceTitle: 'Thread title', sourceUrl: 'https://www.reddit.com/r/x/comments/1',
      grade: 'Anecdotal Signal', notes: '',
    }] })} />);
    const reported = screen.getByTestId('doses-reported');
    expect(within(reported).getByText('Reddit')).toBeInTheDocument();
    expect(within(reported).getByText('Grade: Anecdotal Signal')).toBeInTheDocument();
    expect(within(screen.getByTestId('doses-studies')).getByText('n/a')).toBeInTheDocument();
  });
});
