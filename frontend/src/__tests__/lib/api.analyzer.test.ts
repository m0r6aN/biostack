import { describe, expect, it, vi, beforeEach } from 'vitest';
import { ApiClient } from '@/lib/api';

describe('ApiClient analyzeProtocol', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('posts protocol text to the analyzer endpoint', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        protocol: [],
        score: 72,
        scoreExplanation: { baseScore: 50, synergy: 12, redundancy: -4, interference: -2 },
        issues: [],
        suggestions: [],
        decomposedBlends: [],
        unknownCompounds: [],
        counterfactuals: {
          baselineScore: 72,
          bestRemoveOne: [],
          bestSwapOne: [],
          bestSimplifiedProtocol: null,
          goalAwareOptions: [],
        },
        inputType: 'Paste',
        sourceName: null,
        extractionWarnings: [],
        parserWarnings: [],
        lowConfidenceExtraction: false,
        extractedTextPreview: 'BPC-157 500mcg daily',
        artifacts: [],
      }),
    });

    vi.stubGlobal('fetch', fetchMock);

    const client = new ApiClient('http://localhost:5050');
    const result = await client.analyzeProtocol({ inputText: 'BPC-157 500mcg daily', goal: 'healing' });

    expect(result.score).toBe(72);
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5050/api/analyze/protocol',
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
        body: JSON.stringify({ inputText: 'BPC-157 500mcg daily', goal: 'healing' }),
      })
    );
  });

  it('posts multipart form data for file uploads', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({
        protocol: [],
        score: 72,
        scoreExplanation: { baseScore: 50, synergy: 12, redundancy: -4, interference: -2 },
        issues: [],
        suggestions: [],
        decomposedBlends: [],
        unknownCompounds: [],
        counterfactuals: {
          baselineScore: 72,
          bestRemoveOne: [],
          bestSwapOne: [],
          bestSimplifiedProtocol: null,
          goalAwareOptions: [],
        },
        inputType: 'FileUpload',
        sourceName: 'stack.pdf',
        extractionWarnings: [],
        parserWarnings: [],
        lowConfidenceExtraction: false,
        extractedTextPreview: 'stack text',
        artifacts: [],
      }),
    });

    vi.stubGlobal('fetch', fetchMock);

    const client = new ApiClient('http://localhost:5050');
    await client.analyzeProtocol({
      inputType: 'FileUpload',
      file: new File(['pdf-bytes'], 'stack.pdf', { type: 'application/pdf' }),
      goal: 'healing',
      maxCompounds: 5,
    });

    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5050/api/analyze/protocol',
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
        body: expect.any(FormData),
      })
    );
  });
});
