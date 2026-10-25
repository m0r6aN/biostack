import { render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { VerdictBlock, VERDICT_FOOTNOTE } from '@/components/knowledge/VerdictBlock';
import { StudyDetailsFold, STUDY_DETAILS_LABEL, STUDY_DETAILS_TOP_LINE } from '@/components/knowledge/StudyDetailsFold';
import { DOSE_REDACTION, DOSE_TOKEN, redactDoses } from '@/lib/doseRedaction';
import type { KnowledgeEntry, KnowledgePublicClaim } from '@/lib/types';

function claim(overrides: Partial<KnowledgePublicClaim> = {}): KnowledgePublicClaim {
  return {
    id: 'c1', statement: 'It melts fat.', population: 'adults with obesity', outcome: 'weight', tier: 'Established',
    verdict: 'PROVEN IN HUMANS', qualifier: 'In adults with obesity, for weight loss, under trial conditions.',
    note: 'Large trials showed weight loss vs placebo.', hypeBand: 'High', hypeMethodNote: 'Hype is scored by our editors from public search and social signals.',
    sources: [], version: 1, verifiedAt: '2026-10-20T00:00:00Z', recheckDueAt: '2027-04-20T00:00:00Z',
    publishedAt: '2026-11-02T00:00:00Z', published: true, status: 'published', reviewedBy: 'ai-council',
    reviewers: ['a/x', 'b/y'], changelog: [], ...overrides,
  } as KnowledgePublicClaim;
}

function entry(claims: KnowledgePublicClaim[]): KnowledgeEntry {
  return { canonicalName: 'Retatrutide', details: { publicClaims: claims } } as unknown as KnowledgeEntry;
}

describe('VerdictBlock', () => {
  it('renders name, quoted claim, chip, qualifier, hype band, as-of and footnote', () => {
    render(<VerdictBlock entry={entry([claim()])} path="/knowledge/retatrutide" />);
    const block = screen.getByTestId('verdict-block');
    expect(within(block).getByRole('heading', { level: 1 })).toHaveTextContent('Retatrutide');
    expect(screen.getByTestId('headline-claim')).toHaveTextContent('“It melts fat.”');
    expect(block).toHaveTextContent('PROVEN IN HUMANS');
    expect(block).toHaveTextContent('In adults with obesity, for weight loss, under trial conditions.');
    expect(block).toHaveTextContent('Hype Gap: High');
    expect(block).toHaveTextContent('Evidence as of Nov 2, 2026 · v1');
    expect(block).toHaveTextContent(VERDICT_FOOTNOTE);
    expect(screen.getByRole('button', { name: 'Settle it' })).toBeInTheDocument();
    expect(screen.getByTestId('trials-internet-gap')).toHaveTextContent('Large trials showed weight loss vs placebo.');
  });

  it('shows NOT YET GRADED and no hype band for an unpublished claim', () => {
    render(<VerdictBlock entry={entry([claim({ published: false, status: 'pending-council', publishedAt: null })])} path="/k" />);
    const block = screen.getByTestId('verdict-block');
    expect(block).toHaveTextContent('NOT YET GRADED');
    expect(block).not.toHaveTextContent('PROVEN IN HUMANS');
    expect(block).not.toHaveTextContent('Hype Gap: High');
    expect(block).not.toHaveTextContent('Large trials');
  });

  it('renders nothing without public claims', () => {
    const { container } = render(<VerdictBlock entry={entry([])} path="/k" />);
    expect(container).toBeEmptyDOMElement();
  });
});

describe('StudyDetailsFold', () => {
  it('is collapsed by default with the trial-conditions label and top line', () => {
    render(<StudyDetailsFold><p>12 mg once weekly</p></StudyDetailsFold>);
    const fold = screen.getByTestId('study-details-fold');
    expect(fold.tagName).toBe('DETAILS');
    expect(fold).not.toHaveAttribute('open');
    expect(within(fold).getByText(STUDY_DETAILS_LABEL)).toBeInTheDocument();
    expect(within(fold).getByText(STUDY_DETAILS_TOP_LINE)).toBeInTheDocument();
  });
});

describe('redactDoses', () => {
  it('removes dose tokens from text but keeps dose reports and URLs', () => {
    const out = redactDoses({
      claims: [{ claim: 'TRIUMPH-1 −25.0% at 80 wk (4 / 9 / 12 mg)' }],
      warnings: ['Doses of 2.4mg caused nausea', 'up to 1-12 mg'],
      doseReports: [{ amount: 12, unit: 'mg', notes: '12 mg arm' }],
      sourceUrl: 'https://x.test/5mg',
    });
    expect(out.claims[0].claim).toBe(`TRIUMPH-1 −25.0% at 80 wk (${DOSE_REDACTION})`);
    expect(JSON.stringify({ c: out.claims, w: out.warnings })).not.toMatch(DOSE_TOKEN);
    expect(out.doseReports[0].notes).toBe('12 mg arm');
    expect(out.sourceUrl).toBe('https://x.test/5mg');
  });
});
