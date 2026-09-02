import { POST } from '@/app/api/research/suggest/route';

const baseBody = {
  compound: {
    name: 'BPC-157', classification: 'Peptide', overallEvidenceTier: 'Limited', completeness: 'partial',
    needsReview: true, reviewQueueItemCount: 2, promotionReadiness: 'blocked',
    promotionBlockers: ['blocked: missing authoritative support'], reviewDecisionIds: [], qualityFlags: [], reviewReasons: [],
  },
  candidate: {
    name: 'BPC-157', classification: 'Peptide', readiness: 'blocked', overallEvidenceTier: 'Limited', completeness: 'partial',
    reviewQueueItemCount: 2, reviewDecisionIds: [], blockers: ['blocked: missing authoritative support'],
    qualityFlags: ['missing-authoritative-support'], requiredNextActions: [],
  },
  evidencePacket: {
    schemaVersion: '1.0.0', recordType: 'compound-evidence-packet',
    packet: { packetId: 'bpc-packet', category: 'peptides', agentId: 'agent', generatedAt: '', sourceRegistryVersion: 'sources' },
    compound: { canonicalName: 'BPC-157', aliases: [], classification: 'Peptide', compoundFamily: null, externalIdentifiers: {} },
    sources: [], conflicts: [], ops: { completeness: 'partial', needsReview: true, reviewReasons: [], qualityFlags: [] },
    claims: [{
      claimId: 'claim-1', claimType: 'evidence-gap', statement: 'Human evidence is limited.',
      context: { population: null, route: null, formulation: null, useCase: null, doseText: null },
      evidenceTier: 'Limited', confidence: 'high', fieldAuthorityRequired: false, sourceRefs: [], extractedEvidence: [], reviewFlags: [],
    }],
  },
  reviewQueueItems: [{ itemId: 'bpc-157-ops-review-1', compoundName: 'BPC-157', severity: 'review', reason: 'Pilot review.', references: [] }],
  planItems: [],
};

function request(body: unknown, url = 'http://localhost/api/research/suggest', headers?: HeadersInit) {
  return new Request(url, { method: 'POST', headers, body: JSON.stringify(body) });
}

describe('research AI suggestion route', () => {
  const originalKey = process.env.OPENAI_API_KEY;
  const originalModel = process.env.OPENAI_REVIEW_MODEL;
  const originalApiUrl = process.env.API_URL;
  const originalNextPublicApiUrl = process.env.NEXT_PUBLIC_API_URL;
  const originalSuggestEnabled = process.env.RESEARCH_AI_SUGGEST_ENABLED;

  afterEach(() => {
    if (originalKey === undefined) delete process.env.OPENAI_API_KEY;
    else process.env.OPENAI_API_KEY = originalKey;
    if (originalModel === undefined) delete process.env.OPENAI_REVIEW_MODEL;
    else process.env.OPENAI_REVIEW_MODEL = originalModel;
    if (originalApiUrl === undefined) delete process.env.API_URL;
    else process.env.API_URL = originalApiUrl;
    if (originalNextPublicApiUrl === undefined) delete process.env.NEXT_PUBLIC_API_URL;
    else process.env.NEXT_PUBLIC_API_URL = originalNextPublicApiUrl;
    if (originalSuggestEnabled === undefined) delete process.env.RESEARCH_AI_SUGGEST_ENABLED;
    else process.env.RESEARCH_AI_SUGGEST_ENABLED = originalSuggestEnabled;
    vi.unstubAllGlobals();
  });

  it('requires the OpenAI API key', async () => {
    delete process.env.OPENAI_API_KEY;

    const response = await POST(request(baseBody));

    expect(response.status).toBe(503);
    expect(await response.json()).toEqual({ error: 'OPENAI_API_KEY is not configured.' });
  });

  it('calls OpenAI and normalizes unsafe promotion suggestions when hard blockers remain', async () => {
    process.env.OPENAI_API_KEY = 'sk-test';
    process.env.OPENAI_REVIEW_MODEL = 'gpt-5.5';
    process.env.RESEARCH_AI_SUGGEST_ENABLED = 'true';
    process.env.API_URL = 'http://trusted-biostack.test/';
    delete process.env.NEXT_PUBLIC_API_URL;
    let consentCalls = 0;
    let providerCalls = 0;
    const localFetch = vi.fn(async (input: string | URL | Request, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;
      if (url === 'http://trusted-biostack.test/api/v1/consent') {
        consentCalls += 1;
        expect(init?.method).toBe('GET');
        expect(init?.body).toBeUndefined();
        expect(init?.cache).toBe('no-store');
        expect(init?.redirect).toBe('manual');
        expect(init?.signal).toBeInstanceOf(AbortSignal);
        expect(init?.signal?.aborted).toBe(false);
        expect([...new Headers(init?.headers).entries()]).toEqual([
          ['cookie', 'biostack_session=synthetic-session'],
        ]);
        return new Response(JSON.stringify({
          accepted: true,
          consentAcceptedAtUtc: '2026-09-02T00:00:00Z',
          consentVersion: 'v1',
          declined: false,
          consentDeclinedAtUtc: null,
          consentDeclinedVersion: null,
          currentVersion: 'v1',
        }), { status: 200, headers: { 'content-type': 'application/json; charset=utf-8' } });
      }
      if (url === 'https://api.openai.com/v1/responses') {
        providerCalls += 1;
        expect(init?.method).toBe('POST');
        const providerHeaders = new Headers(init?.headers);
        expect(providerHeaders.get('authorization')).toBe('Bearer sk-test');
        expect(providerHeaders.get('content-type')).toBe('application/json');
        expect(providerHeaders.has('cookie')).toBe(false);
        expect(JSON.stringify(init)).not.toContain('biostack_session');
        expect(JSON.stringify(init)).not.toContain('decoy-cookie');
        expect(JSON.stringify(init)).not.toContain('inbound-token');
        return new Response(JSON.stringify({
          output_text: JSON.stringify({
            decision: 'approve-for-promotion', confidence: 'high', summary: 'Looks ready.',
            rationale: ['AI wanted to promote.'], claimIdsToApprove: ['claim-1', 'unknown-claim'],
            reviewQueueItemIdsToResolve: ['bpc-157-ops-review-1', 'unknown-queue-item'],
            clearsSoftPromotionBlockers: true, draftNotes: 'Promote the draft.', safetyWarnings: [], openQuestions: [],
          }),
        }), { status: 200 });
      }
      throw new Error(`Unexpected non-local fetch target: ${url}`);
    });
    vi.stubGlobal('fetch', localFetch);

    const response = await POST(request(
      baseBody,
      'https://hostile-client.invalid/api/research/suggest?consented=true',
      {
        cookie: 'decoy_cookie=decoy-cookie; biostack_session=synthetic-session; preferences=private',
        authorization: 'Bearer inbound-token',
        'x-consent-accepted': 'true',
      },
    ));
    const payload = await response.json();

    expect(response.status).toBe(200);
    expect(payload.modelUsed).toBe('gpt-5.5');
    expect(payload.suggestion).toMatchObject({
      decision: 'request-changes',
      claimIdsToApprove: ['claim-1'],
      reviewQueueItemIdsToResolve: ['bpc-157-ops-review-1'],
      clearsSoftPromotionBlockers: false,
    });
    expect(payload.suggestion.rationale[0]).toMatch(/hard blockers/i);
    expect(consentCalls).toBe(1);
    expect(providerCalls).toBe(1);
    expect(localFetch).toHaveBeenCalledTimes(2);
    const requestInit = localFetch.mock.calls[1][1] as RequestInit;
    expect(JSON.parse(String(requestInit.body)).model).toBe('gpt-5.5');
  });
});
