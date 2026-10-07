import { RESTRICTED_EXCERPT_FLAG, withholdDrugBankExcerpts } from '@/lib/research/restricted-excerpts';
import { readFileSync } from 'node:fs';
import path from 'node:path';

function packet(sourceId: string, url?: string) {
  return {
    sources: [{ sourceId, url }],
    claims: [{ sourceRefs: [sourceId], reviewFlags: ['existing-review'], extractedEvidence: [
      { sourceRef: sourceId, quote: 'synthetic restricted excerpt', pageOrSection: 'section 1' },
      { sourceRef: 'dailymed-test', quote: 'synthetic allowed excerpt', pageOrSection: 'section 2' },
    ] }],
    ops: { needsReview: true, qualityFlags: ['existing-flag'] },
  };
}

describe('DrugBank excerpt containment', () => {
  it('matches the same cross-application contract as the worker', () => {
    const fixture = JSON.parse(readFileSync(path.resolve(process.cwd(), '../shared/source-rights/drugbank-containment.conformance.v1.json'), 'utf8')) as {
      packet: { claims: { extractedEvidence: { quote: unknown }[] }[] };
      expectedQuotes: unknown[];
    };
    const before = JSON.stringify(fixture.packet);
    const result = withholdDrugBankExcerpts(fixture.packet);
    expect(result.claims[0].extractedEvidence.map(item => item.quote)).toEqual(fixture.expectedQuotes);
    expect(JSON.stringify(fixture.packet)).toBe(before);
  });

  it('withholds a known alias with no registry or source entry and preserves provenance and inputs', () => {
    const original = packet(' BASARIA-2013-JGERONTOL-RCT ');
    original.sources = [];
    const before = JSON.stringify(original);
    const result = withholdDrugBankExcerpts(original);
    expect(result.claims[0].extractedEvidence).toEqual([
      { sourceRef: ' BASARIA-2013-JGERONTOL-RCT ', quote: null, pageOrSection: 'section 1' },
      { sourceRef: 'dailymed-test', quote: 'synthetic allowed excerpt', pageOrSection: 'section 2' },
    ]);
    expect(result.claims[0].sourceRefs).toEqual(original.claims[0].sourceRefs);
    expect(result.claims[0].reviewFlags).toEqual(['existing-review']);
    expect(result.ops.qualityFlags).toEqual(['existing-flag', RESTRICTED_EXCERPT_FLAG]);
    expect(result.ops.needsReview).toBe(true);
    expect(JSON.stringify(original)).toBe(before);
    expect(withholdDrugBankExcerpts(result)).toEqual(result);
  });

  it.each([
    'https://go.drugbank.com/drugs/DB00010',
    'https://DRUGBANK.COM./record',
  ])('contains a previously unknown ID on the actual host %s', url => {
    expect(withholdDrugBankExcerpts(packet('unknown-record', url)).claims[0].extractedEvidence[0].quote).toBeNull();
  });

  it.each([
    'https://notdrugbank.com/record',
    'https://drugbank.com.example.org/record',
    'https://example.org/?reference=drugbank.com',
    'invalid-url',
  ])('does not classify unrelated hosts or a query substring as DrugBank: %s', url => {
    const original = packet('unrelated-record', url);
    expect(withholdDrugBankExcerpts(original)).toBe(original);
  });

  it('preserves ChEMBL and other lanes; client rights declarations cannot release C1', () => {
    expect(withholdDrugBankExcerpts(packet('chembl-chembl5095142')).claims[0].extractedEvidence[0].quote).toBeTruthy();
    const original = { ...packet('drugbank-db00010'), rights: { reviewStatus: 'approved' }, permittedContent: ['all'] };
    expect(withholdDrugBankExcerpts(original).claims[0].extractedEvidence[0].quote).toBeNull();
    expect(withholdDrugBankExcerpts(null)).toBeNull();
  });
});
