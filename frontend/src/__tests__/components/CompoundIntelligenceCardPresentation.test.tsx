import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CompoundIntelligenceCard } from '@/components/knowledge/CompoundIntelligenceCard';
import type { KnowledgeEntry } from '@/lib/types';
import type { ReviewedStudyDesign } from '@/lib/reviewedStudyDesign';
const { acceptedExplanations } = vi.hoisted(() => ({ acceptedExplanations: [] as ReviewedStudyDesign[] }));
vi.mock('@/lib/generated/reviewed-study-design.json', () => ({ default: acceptedExplanations }));
beforeEach(() => { acceptedExplanations.length = 0; });

vi.mock('@/lib/context', () => ({ useProfile: () => ({ profiles: [], currentProfileId: null }) }));
vi.mock('@/lib/settings', () => ({ useSettings: () => ({ settings: { weightUnit: 'kg' } }) }));
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
    benefits: ['recovery', 'joint comfort'],
    pairsWellWith: [],
    avoidWith: [],
    compatibleBlends: [],
    vialCompatibility: '',
    recommendedDosage: '',
    standardDosageRange: '',
    maxReportedDose: '',
    frequency: '',
    preferredTimeOfDay: '',
    weeklyDosageSchedule: [],
    incrementalEscalationSteps: [],
    drugInteractions: [],
    optimizationProtein: '',
    optimizationCarbs: '',
    optimizationSupplements: [],
    optimizationSleep: '',
    optimizationExercise: '',
  };

function show(overrides: Partial<KnowledgeEntry> = {}) {
  return render(<CompoundIntelligenceCard entry={{ ...entry, ...overrides }} />);
}

describe('Knowledge presentation', () => {
  it('links one PubMed search honestly without a fake expansion control', () => {
    show({ sourceReferences: ['https://pubmed.ncbi.nlm.nih.gov/?term=BPC-157+review'] });
    const link = screen.getByRole('link', { name: /PubMed search: BPC-157 review/ });
    expect(link).toHaveAttribute('href', 'https://pubmed.ncbi.nlm.nih.gov/?term=BPC-157+review');
    expect(link).toHaveAttribute('rel', 'noopener noreferrer');
    expect(screen.queryByRole('button', { name: /sources/ })).not.toBeInTheDocument();
  });

  it('renders HTTP and HTTPS citations as links and all other references as text', () => {
    const references = ['http://example.org/paper', 'https://example.org/review', 'A retained citation'];
    show({ sourceReferences: references });
    expect(screen.getAllByRole('link')).toHaveLength(2);
    expect(screen.getByText('A retained citation').closest('a')).toBeNull();
  });

  it.each(['javascript:alert(1)', 'data:text/html,test', 'file:///tmp/report', '/relative', 'https://user:password@example.org'])('does not activate unsafe or non-public reference %s', ref => {
    show({ sourceReferences: [ref] });
    expect(screen.getByText(ref)).toBeVisible();
    expect(screen.queryByRole('link')).not.toBeInTheDocument();
  });

  it('expands and collapses all sources with an accessible control', async () => {
    const user = userEvent.setup();
    show({ sourceReferences: ['One', 'Two', 'Three', 'Four', 'Five'] });
    const control = screen.getByRole('button', { name: 'Show all 5 sources' });
    expect(control).toHaveAttribute('aria-expanded', 'false');
    expect(document.getElementById(control.getAttribute('aria-controls')!)).toBeInTheDocument();
    expect(screen.queryByText('Four')).not.toBeInTheDocument();
    await user.click(control);
    expect(control).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByText('Four')).toBeVisible();
    expect(screen.getByText('Five')).toBeVisible();
    await user.click(screen.getByRole('button', { name: 'Show fewer sources' }));
    expect(screen.queryByText('Four')).not.toBeInTheDocument();
  });

  it('preserves all supplied qualifiers and describes missing data as a record limitation', () => {
    const qualifiers = {
      mechanismSummary: 'Experimental mechanism; clinical relevance unproven.',
      notes: 'Educational reference only. Not a therapeutic recommendation.',
      regulatoryStatus: 'Regulatory status not established by this record',
      benefits: ['Preclinical signal; human efficacy not established'],
      pathways: ['Tissue repair (preclinical)'],
      avoidWith: ['Review required; not individualized advice'],
      drugInteractions: ['Data not currently available'],
    };
    show(qualifiers);
    for (const value of Object.values(qualifiers).flat()) expect(screen.getByText(value)).toBeVisible();
    expect(screen.getByRole('region', { name: 'Evidence and limitations' })).toHaveTextContent(qualifiers.notes);
    expect(screen.getByText('Human/preclinical evidence breakdown is not available in this record.')).toBeVisible();
    expect(screen.queryByText(/no human evidence|no interactions|safe to combine/i)).not.toBeInTheDocument();
  });
});

const syntheticExplanation: ReviewedStudyDesign = {
  canonicalName: 'Synthetic compound', kind: 'dated-review-study-design', claimId: 'synthetic-design',
  statement: 'A synthetic review searching through 3 June 2024 included three studies.',
  citation: { displayLabel: 'Synthetic et al., 2025', sourceItemId: 'synthetic-paper', pmid: '12345', doi: '10.1234/test', url: 'https://pubmed.ncbi.nlm.nih.gov/12345/' },
  searchCutoff: '2024-06-03', languageScope: 'English-language literature',
  limitations: 'This describes that review, not current evidence or clinical benefit.',
  acceptanceDecisionId: 'synthetic-approval', expiresAt: null,
};

it('shows an accepted dated explanation with adjacent citation without replacing other content', () => {
  acceptedExplanations.push(syntheticExplanation);
  show({ canonicalName: '  SYNTHETIC COMPOUND  ', notes: 'Existing record qualification', sourceReferences: ['https://example.org/original'] });
  const explanation = screen.getByTestId('reviewed-study-design');
  expect(explanation).toHaveTextContent(syntheticExplanation.statement);
  expect(explanation).toHaveTextContent(syntheticExplanation.limitations);
  expect(explanation.querySelector('a')).toHaveAttribute('href', syntheticExplanation.citation.url);
  expect(screen.getByText('Tissue support')).toBeInTheDocument();
  expect(screen.getByText('Existing record qualification')).toBeInTheDocument();
  expect(screen.queryByText('Human/preclinical evidence breakdown is not available in this record.')).not.toBeInTheDocument();
});

it('does not attach an accepted explanation to a different compound or an alias', () => {
  acceptedExplanations.push(syntheticExplanation);
  show({ aliases: ['Synthetic compound'] });
  expect(screen.queryByTestId('reviewed-study-design')).not.toBeInTheDocument();
  expect(screen.getByText('Human/preclinical evidence breakdown is not available in this record.')).toBeInTheDocument();
});

it('hides an expired explanation in an older deployed asset', () => {
  acceptedExplanations.push({ ...syntheticExplanation, expiresAt: '2000-01-01T00:00:00Z' });
  show({ canonicalName: 'Synthetic compound' });
  expect(screen.queryByTestId('reviewed-study-design')).not.toBeInTheDocument();
});
