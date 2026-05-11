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

function request(body: unknown) {
  return new Request('http://localhost/api/research/suggest', { method: 'POST', body: JSON.stringify(body) });
}

describe('research AI suggestion route', () => {
  const originalKey = process.env.OPENAI_API_KEY;
  const originalModel = process.env.OPENAI_REVIEW_MODEL;

  afterEach(() => {
    if (originalKey === undefined) delete process.env.OPENAI_API_KEY;
    else process.env.OPENAI_API_KEY = originalKey;
    if (originalModel === undefined) delete process.env.OPENAI_REVIEW_MODEL;
    else process.env.OPENAI_REVIEW_MODEL = originalModel;
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
    const openAiFetch = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      output_text: JSON.stringify({
        decision: 'approve-for-promotion', confidence: 'high', summary: 'Looks ready.',
        rationale: ['AI wanted to promote.'], claimIdsToApprove: ['claim-1', 'unknown-claim'],
        reviewQueueItemIdsToResolve: ['bpc-157-ops-review-1', 'unknown-queue-item'],
        clearsSoftPromotionBlockers: true, draftNotes: 'Promote the draft.', safetyWarnings: [], openQuestions: [],
      }),
    }), { status: 200 }));
    vi.stubGlobal('fetch', openAiFetch);

    const response = await POST(request(baseBody));
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
    expect(openAiFetch).toHaveBeenCalledWith('https://api.openai.com/v1/responses', expect.objectContaining({ method: 'POST' }));
    const requestInit = openAiFetch.mock.calls[0][1] as RequestInit;
    expect(JSON.parse(String(requestInit.body)).model).toBe('gpt-5.5');
  });
});