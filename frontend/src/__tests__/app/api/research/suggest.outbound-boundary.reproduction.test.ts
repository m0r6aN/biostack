import { POST } from '@/app/api/research/suggest/route';

const unauthenticatedBody = {
  compound: { name: 'Synthetic Compound Alpha' },
  candidate: { blockers: [] },
  evidencePacket: null,
  planItems: [],
};

describe('research suggestion outbound boundary reproduction', () => {
  const originalApiKey = process.env.OPENAI_API_KEY;
  const originalSuggestEnabled = process.env.RESEARCH_AI_SUGGEST_ENABLED;

  afterEach(() => {
    if (originalApiKey === undefined) delete process.env.OPENAI_API_KEY;
    else process.env.OPENAI_API_KEY = originalApiKey;

    if (originalSuggestEnabled === undefined) delete process.env.RESEARCH_AI_SUGGEST_ENABLED;
    else process.env.RESEARCH_AI_SUGGEST_ENABLED = originalSuggestEnabled;

    vi.unstubAllGlobals();
  });

  it('does not relay a provider request without an authenticated or consented caller', async () => {
    process.env.OPENAI_API_KEY = 'synthetic-test-key';
    process.env.RESEARCH_AI_SUGGEST_ENABLED = 'true';
    const interceptedProviderFetch = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      output_text: JSON.stringify({
        decision: 'request-changes',
        confidence: 'low',
        summary: 'Synthetic response.',
        rationale: ['Synthetic rationale.'],
        claimIdsToApprove: [],
        reviewQueueItemIdsToResolve: [],
        clearsSoftPromotionBlockers: false,
        draftNotes: '',
        safetyWarnings: [],
        openQuestions: [],
      }),
    }), { status: 200 }));
    vi.stubGlobal('fetch', interceptedProviderFetch);

    const inboundRequest = new Request('http://localhost/api/research/suggest', {
      method: 'POST',
      body: JSON.stringify(unauthenticatedBody),
      // Deliberately no Cookie, Authorization, or consent-bearing header.
    });

    const response = await POST(inboundRequest);

    expect(
      interceptedProviderFetch,
      `Unauthenticated request reached the configured provider; routeStatus=${response.status}`,
    ).not.toHaveBeenCalled();
  });
});
