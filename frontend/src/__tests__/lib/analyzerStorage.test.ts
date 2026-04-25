import { beforeEach, describe, expect, it } from 'vitest';
import {
  ANALYZER_ANALYSIS_HISTORY_KEY,
  ANALYZER_PROTOCOL_DRAFT_KEY,
  saveAnalyzerAnalysis,
  saveAnalyzerProtocolDraft,
  readAnalyzerAnalysisHistory,
} from '@/lib/analyzerStorage';
import type { ProtocolAnalyzerResult } from '@/lib/types';

const makeResult = (overrides: Partial<ProtocolAnalyzerResult> = {}): ProtocolAnalyzerResult => ({
  protocol: [{ compoundName: 'BPC-157', dose: 500, unit: 'mcg', frequency: 'daily', duration: '' }],
  score: 72,
  scoreExplanation: { baseScore: 50, synergy: 15, redundancy: 0, interference: 0 },
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
  extractedTextPreview: null,
  artifacts: [],
  ...overrides,
});

describe('analyzerStorage', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  describe('readAnalyzerAnalysisHistory', () => {
    it('returns empty array when nothing is stored', () => {
      expect(readAnalyzerAnalysisHistory()).toEqual([]);
    });

    it('returns empty array when stored value is invalid JSON', () => {
      window.localStorage.setItem(ANALYZER_ANALYSIS_HISTORY_KEY, 'not-json{');
      expect(readAnalyzerAnalysisHistory()).toEqual([]);
    });

    it('returns empty array when stored value is not an array', () => {
      window.localStorage.setItem(ANALYZER_ANALYSIS_HISTORY_KEY, JSON.stringify({ foo: 'bar' }));
      expect(readAnalyzerAnalysisHistory()).toEqual([]);
    });

    it('filters out entries with wrong schema version', () => {
      const entry = { id: 'analysis-abc', schemaVersion: 99 };
      window.localStorage.setItem(ANALYZER_ANALYSIS_HISTORY_KEY, JSON.stringify([entry]));
      expect(readAnalyzerAnalysisHistory()).toEqual([]);
    });
  });

  describe('saveAnalyzerAnalysis', () => {
    it('saves an analysis and reads it back', () => {
      const result = makeResult();
      const saved = saveAnalyzerAnalysis({
        inputType: 'Paste',
        sourceName: null,
        rawInput: 'BPC-157 500mcg daily',
        result,
      });

      expect(saved.score).toBe(72);
      expect(saved.inputType).toBe('Paste');
      expect(saved.schemaVersion).toBe(1);

      const history = readAnalyzerAnalysisHistory();
      expect(history).toHaveLength(1);
      expect(history[0].id).toBe(saved.id);
    });

    it('deduplicates identical analysis saves', () => {
      const result = makeResult();
      const input = { inputType: 'Paste' as const, sourceName: null, rawInput: 'BPC-157', result };
      saveAnalyzerAnalysis(input);
      saveAnalyzerAnalysis(input);

      expect(readAnalyzerAnalysisHistory()).toHaveLength(1);
    });

    it('prepends new analyses to history', () => {
      const first = makeResult({ score: 60 });
      const second = makeResult({ score: 80 });
      saveAnalyzerAnalysis({ inputType: 'Paste', sourceName: null, rawInput: 'first', result: first });
      saveAnalyzerAnalysis({ inputType: 'Paste', sourceName: null, rawInput: 'second', result: second });

      const history = readAnalyzerAnalysisHistory();
      expect(history[0].score).toBe(80);
      expect(history[1].score).toBe(60);
    });

    it('stores sourceName and rawInput', () => {
      const result = makeResult();
      saveAnalyzerAnalysis({ inputType: 'FileUpload', sourceName: 'protocol.pdf', rawInput: 'raw text', result });

      const history = readAnalyzerAnalysisHistory();
      expect(history[0].sourceName).toBe('protocol.pdf');
      expect(history[0].rawInput).toBe('raw text');
      expect(history[0].inputType).toBe('FileUpload');
    });
  });

  describe('saveAnalyzerProtocolDraft', () => {
    it('saves a protocol draft and stores in localStorage', () => {
      const protocol = [{ compoundName: 'BPC-157', dose: 500, unit: 'mcg', frequency: 'daily', duration: '' }];
      const optimized = [{ compoundName: 'TB-500', dose: 2000, unit: 'mcg', frequency: 'twice-weekly', duration: '' }];

      const draft = saveAnalyzerProtocolDraft({
        sourceAnalysisId: 'analysis-001',
        goal: 'Recovery',
        protocol,
        optimizedProtocol: optimized,
      });

      expect(draft.goal).toBe('Recovery');
      expect(draft.sourceAnalysisId).toBe('analysis-001');
      expect(draft.name).toContain('Recovery');
      expect(draft.schemaVersion).toBe(1);

      const raw = window.localStorage.getItem(ANALYZER_PROTOCOL_DRAFT_KEY);
      expect(raw).not.toBeNull();
      const stored = JSON.parse(raw!);
      expect(stored.id).toBe(draft.id);
    });

    it('uses BioStack as default name when goal is empty', () => {
      const draft = saveAnalyzerProtocolDraft({
        sourceAnalysisId: 'analysis-001',
        goal: '',
        protocol: [],
        optimizedProtocol: [],
      });
      expect(draft.name).toContain('BioStack');
    });
  });
});
