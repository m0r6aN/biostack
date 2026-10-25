import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { canonicalPair, curatedPairsFor, isAllowedRuling, isCuratedPair, rulingFor, vsPath, type VsSide } from '@/lib/vsPairs';
import { VsLedger, NOT_CURATED_COPY, NO_RULING_COPY, OVERLAP_FRAMING } from '@/components/vs/VsLedger';
import { vsShareText } from '@/components/vs/VsShare';
import type { KnowledgeEntry } from '@/lib/types';

const side = (name: string, verdict: VsSide['verdict'], hypeBand = ''): VsSide => ({ name, claim: null, verdict, hypeBand });

describe('/vs pairs', () => {
  it('is alphabetical-canonical and knows the curated seed list', () => {
    expect(canonicalPair('tb-500', 'bpc-157')).toEqual(['bpc-157', 'tb-500']);
    expect(vsPath('tb-500', 'bpc-157')).toBe('/vs/bpc-157/tb-500');
    expect(isCuratedPair('tb-500', 'bpc-157')).toBe(true);
    expect(isCuratedPair('bpc-157', 'semax')).toBe(false);
    expect(curatedPairsFor('ipamorelin')).toEqual([['ibutamoren', 'ipamorelin'], ['cjc-1295', 'ipamorelin']]);
  });

  it('derives rulings only from the approved templates and published verdicts', () => {
    expect(rulingFor(side('BPC-157', 'ANIMAL ONLY'), side('TB-500', 'THIN'))).toBe('More human data: neither.');
    expect(rulingFor(side('A', 'PROMISING'), side('B', 'ANIMAL ONLY'))).toBe('More human data: A.');
    expect(rulingFor(side('A', 'PROMISING'), side('B', 'PROVEN IN HUMANS'))).toBe('Higher-quality evidence: B (PROVEN IN HUMANS vs PROMISING).');
    expect(rulingFor(side('A', 'THIN', 'High'), side('B', 'THIN', 'Low'))).toBe('Same tier. Different hype.');
    expect(rulingFor(side('A', 'THIN'), side('B', 'THIN'))).toBe('Both THIN. The argument is louder than the evidence.');
    expect(rulingFor(side('A', 'NOT YET GRADED'), side('B', 'THIN'))).toBeNull();
    expect(rulingFor(side('A', 'MYTH'), side('B', 'THIN'))).toBeNull();
  });

  it('rejects ruling text outside the shapes or with banned words', () => {
    expect(isAllowedRuling('A is better than B.')).toBe(false);
    expect(isAllowedRuling('More human data: A, go with it.')).toBe(false);
    expect(isAllowedRuling('Pick A.')).toBe(false);
    expect(isAllowedRuling('More human data: neither.')).toBe(true);
  });

  it('share text follows the voice guide', () => {
    expect(vsShareText('BPC-157', 'TB-500', '/vs/bpc-157/tb-500')).toBe('BPC-157 vs TB-500, the evidence ledger. biostack.cc/vs/bpc-157/tb-500');
  });
});

function entry(name: string, pathways: string[]): KnowledgeEntry {
  return {
    canonicalName: name, evidenceTier: 'Limited', regulatoryStatus: '', pathways,
    details: { publicClaims: [], evidenceGaps: ['No human trial at 5 mg.'], safety: { adverseEffectsCommon: [], adverseEffectsSerious: [] } },
  } as unknown as KnowledgeEntry;
}

describe('VsLedger', () => {
  it('frames overlap as pathways, redacts doses and shows the no-ruling copy', () => {
    render(<VsLedger a={entry('BPC-157', ['tissue-repair'])} b={entry('TB-500', ['tissue-repair'])}
      left={side('BPC-157', 'NOT YET GRADED')} right={side('TB-500', 'NOT YET GRADED')} ruling={null} curated />);
    expect(screen.getByTestId('vs-ruling')).toHaveTextContent(NO_RULING_COPY);
    expect(screen.getByText(OVERLAP_FRAMING)).toBeInTheDocument();
    expect(document.body.textContent).not.toMatch(/5 mg/);
    expect(document.body.textContent).not.toMatch(/\b(better|stronger|winner|stack with)\b/i);
  });

  it('uses the not-curated empty state for uncurated pairs', () => {
    render(<VsLedger a={entry('A', [])} b={entry('B', [])} left={side('A', 'THIN')} right={side('B', 'THIN')} ruling="Both THIN. The argument is louder than the evidence." curated={false} />);
    expect(screen.getByTestId('vs-ruling')).toHaveTextContent(NOT_CURATED_COPY);
  });
});
