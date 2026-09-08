import { readFile } from 'fs/promises';
import { NextRequest } from 'next/server';
import { GET } from '@/app/api/research/artifacts/route';

vi.mock('fs/promises', () => {
  const readFile = vi.fn();
  return { readFile, default: { readFile } };
});

describe('research artifact containment boundary', () => {
  afterEach(() => { vi.unstubAllEnvs(); vi.clearAllMocks(); });

  it('filters a historical packet at read time without rewriting the original', async () => {
    vi.stubEnv('NODE_ENV', 'test');
    vi.stubEnv('RESEARCH_DATA_SOURCE', 'api');
    vi.stubEnv('RESEARCH_ARTIFACTS_PATH', 'research/output/historical');
    const original = JSON.stringify({
      sources: [{ sourceId: 'legacy-record', url: 'https://go.drugbank.com/drugs/DB00010' }],
      claims: [{ sourceRefs: ['legacy-record'], extractedEvidence: [
        { sourceRef: 'legacy-record', quote: 'synthetic historical restricted quote', pageOrSection: 'section A' },
        { sourceRef: 'dailymed-test', quote: 'synthetic permitted quote', pageOrSection: 'section B' },
      ] }], ops: { qualityFlags: ['existing'] },
    });
    vi.mocked(readFile).mockResolvedValue(original);
    const response = await GET(new NextRequest('http://localhost/api/research/artifacts?artifact=evidence-packet/test-compound', {
      headers: { authorization: 'Bearer synthetic-local-test' },
    }));
    expect(response.status).toBe(200);
    const payload = await response.json();
    expect(payload.claims[0].extractedEvidence[0]).toEqual({ sourceRef: 'legacy-record', quote: null, pageOrSection: 'section A' });
    expect(payload.claims[0].extractedEvidence[1].quote).toBe('synthetic permitted quote');
    expect(payload.ops.qualityFlags).toEqual(['existing', 'restricted-source-excerpt-withheld']);
    expect(JSON.parse(original).claims[0].extractedEvidence[0].quote).toBe('synthetic historical restricted quote');
    expect(readFile).toHaveBeenCalledTimes(1);
  });

  it('preserves the production denial before reading artifacts', async () => {
    vi.stubEnv('NODE_ENV', 'production');
    const response = await GET(new NextRequest('http://localhost/api/research/artifacts?artifact=evidence-packet/test-compound'));
    expect(response.status).toBe(404);
    expect(readFile).not.toHaveBeenCalled();
  });
});
