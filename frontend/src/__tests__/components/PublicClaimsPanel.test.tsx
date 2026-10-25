import { render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { HYPE_METHOD_HREF, PublicClaimsPanel } from '@/components/knowledge/PublicClaimsPanel';
import type { KnowledgePublicClaim } from '@/lib/types';

function claim(overrides: Partial<KnowledgePublicClaim> = {}): KnowledgePublicClaim {
  return {
    id: 'x-heals', statement: 'X heals injuries.', population: 'adults', outcome: 'healing', tier: 'Plausible Theory',
    verdict: 'NOT YET GRADED', qualifier: '', note: '', hypeBand: '', hypeMethodNote: '',
    sources: [{ id: 's1', title: 'A review', url: 'https://pubmed.ncbi.nlm.nih.gov/1/', publisher: 'PubMed', sourceType: 'review' }],
    version: 1, verifiedAt: '2026-10-24T00:00:00Z', recheckDueAt: null, publishedAt: null, published: false,
    reviewers: [], changelog: [], ...overrides,
  };
}

describe('PublicClaimsPanel', () => {
  it('renders nothing without claims', () => {
    const { container } = render(<PublicClaimsPanel claims={[]} />);
    expect(container).toBeEmptyDOMElement();
  });

  it('shows an unreviewed claim as NOT YET GRADED with sources and no date or band', () => {
    render(<PublicClaimsPanel claims={[claim({ verdict: 'ANIMAL ONLY' as never, note: 'leaked' })]} />);
    const row = screen.getByTestId('public-claim');
    expect(within(row).getByTestId('verdict-chip')).toHaveTextContent('NOT YET GRADED');
    expect(row).toHaveTextContent('Awaiting the two-reviewer evidence council.');
    expect(row).not.toHaveTextContent('leaked');
    expect(row).not.toHaveTextContent('As of');
    expect(within(row).getByRole('link', { name: /A review/ })).toHaveAttribute('href', 'https://pubmed.ncbi.nlm.nih.gov/1/');
  });

  it('shows a published PROVEN claim with qualifier, band, method link and as-of date', () => {
    render(<PublicClaimsPanel claims={[claim({
      verdict: 'PROVEN IN HUMANS', published: true, publishedAt: '2026-11-02T00:00:00Z', status: 'published',
      reviewedBy: 'ai-council', reviewers: ['openai-compatible/model-a', 'anthropic/model-b'],
      qualifier: 'In adults with obesity, for weight loss, under trial conditions.', note: 'Trials show it.', hypeBand: 'Low',
    })]} />);
    const row = screen.getByTestId('public-claim');
    expect(within(row).getByTestId('verdict-chip')).toHaveTextContent('PROVEN IN HUMANS');
    expect(row).toHaveTextContent('In adults with obesity, for weight loss, under trial conditions.');
    expect(row).toHaveTextContent('Hype Gap: Low');
    expect(within(row).getByRole('link', { name: 'How we measure hype' })).toHaveAttribute('href', HYPE_METHOD_HREF);
    expect(row).toHaveTextContent('Evidence as of Nov 2, 2026 · v1');
    expect(row).toHaveTextContent('Reviewed by the AI evidence council (openai-compatible/model-a, anthropic/model-b)');
  });

  it('labels an escalated claim without showing a verdict', () => {
    render(<PublicClaimsPanel claims={[claim({ status: 'escalated-to-owner' })]} />);
    expect(screen.getByTestId('public-claim')).toHaveTextContent('The evidence council disagreed. Awaiting a final decision.');
  });

  it('never renders a dose unit in the panel copy', () => {
    render(<PublicClaimsPanel claims={[claim()]} />);
    expect(screen.getByTestId('public-claims').textContent).not.toMatch(/\b\d+(\.\d+)?\s?(mg|mcg|µg|iu|ml)\b/i);
  });
});
