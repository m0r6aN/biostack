import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ANALYZER_ANALYSIS_HISTORY_KEY,
  ANALYZER_PROTOCOL_DRAFT_KEY,
  buildAnalyzerDraftCompoundImports,
  clearAnalyzerProtocolDraft,
  formatAnalyzerEntry,
  hasPendingAnalyzerProtocolDraft,
  markAnalyzerProtocolDraftImported,
  parseAnalyzerProtocolDraft,
  readAnalyzerProtocolDraft,
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

  describe('readAnalyzerProtocolDraft (post-sign-in consumer)', () => {
    const original = [
      { compoundName: 'BPC-157', dose: 500, unit: 'mcg', frequency: 'daily', duration: '4 weeks' },
      { compoundName: 'NAD+', dose: 100, unit: 'mg', frequency: 'daily', duration: '' },
    ];
    const alternative = [{ compoundName: 'TB-500', dose: 2000, unit: 'mcg', frequency: 'twice-weekly', duration: '' }];

    it('round-trips a saved draft as pending, keeping the original and alternative distinct', () => {
      const saved = saveAnalyzerProtocolDraft({ sourceAnalysisId: 'analysis-001', goal: 'Recovery', protocol: original, optimizedProtocol: alternative });

      const draft = readAnalyzerProtocolDraft();
      expect(draft).not.toBeNull();
      expect(draft!.id).toBe(saved.id);
      expect(draft!.protocol).toEqual(original);
      expect(draft!.optimizedProtocol).toEqual(alternative);
      expect(hasPendingAnalyzerProtocolDraft(draft)).toBe(true);
    });

    it('treats a legacy draft without importStatus as pending', () => {
      const legacy = {
        id: 'protocol-draft-legacy',
        schemaVersion: 1,
        sourceAnalysisId: 'analysis-001',
        name: 'Recovery alternative protocol',
        protocol: original,
        optimizedProtocol: alternative,
        goal: 'Recovery',
        createdAt: '2026-09-01T00:00:00.000Z',
      };
      window.localStorage.setItem(ANALYZER_PROTOCOL_DRAFT_KEY, JSON.stringify(legacy));

      const draft = readAnalyzerProtocolDraft();
      expect(draft?.importStatus).toEqual({ status: 'pending', importedProfileIds: [] });
      expect(hasPendingAnalyzerProtocolDraft(draft)).toBe(true);
    });

    it('returns null when nothing is stored', () => {
      expect(readAnalyzerProtocolDraft()).toBeNull();
      expect(hasPendingAnalyzerProtocolDraft()).toBe(false);
    });

    it.each([
      ['invalid JSON', 'not-json{'],
      ['a non-object', JSON.stringify('draft')],
      ['an array', JSON.stringify([original])],
      ['the wrong schema version', JSON.stringify({ schemaVersion: 2, id: 'x', sourceAnalysisId: 'a', name: 'n', goal: '', createdAt: 'c', protocol: original, optimizedProtocol: [] })],
      ['an empty protocol', JSON.stringify({ schemaVersion: 1, id: 'x', sourceAnalysisId: 'a', name: 'n', goal: '', createdAt: 'c', protocol: [], optimizedProtocol: [] })],
      ['a malformed entry (dose is a string)', JSON.stringify({ schemaVersion: 1, id: 'x', sourceAnalysisId: 'a', name: 'n', goal: '', createdAt: 'c', protocol: [{ compoundName: 'BPC-157', dose: '500', unit: 'mcg', frequency: 'daily', duration: '' }], optimizedProtocol: [] })],
      ['an entry with an empty compound name', JSON.stringify({ schemaVersion: 1, id: 'x', sourceAnalysisId: 'a', name: 'n', goal: '', createdAt: 'c', protocol: [{ compoundName: '   ', dose: 500, unit: 'mcg', frequency: 'daily', duration: '' }], optimizedProtocol: [] })],
      ['a malformed alternative entry', JSON.stringify({ schemaVersion: 1, id: 'x', sourceAnalysisId: 'a', name: 'n', goal: '', createdAt: 'c', protocol: original, optimizedProtocol: [{ compoundName: 'TB-500' }] })],
      ['a malformed importStatus', JSON.stringify({ schemaVersion: 1, id: 'x', sourceAnalysisId: 'a', name: 'n', goal: '', createdAt: 'c', protocol: original, optimizedProtocol: [], importStatus: { status: 'done', importedProfileIds: 'p1' } })],
      ['a missing id', JSON.stringify({ schemaVersion: 1, sourceAnalysisId: 'a', name: 'n', goal: '', createdAt: 'c', protocol: original, optimizedProtocol: [] })],
    ])('returns null for corrupted storage: %s', (_label, raw) => {
      window.localStorage.setItem(ANALYZER_PROTOCOL_DRAFT_KEY, raw);
      expect(readAnalyzerProtocolDraft()).toBeNull();
      expect(hasPendingAnalyzerProtocolDraft()).toBe(false);
    });

    it('rejects oversized drafts rather than trusting them', () => {
      const entry = original[0];
      const huge = { schemaVersion: 1, id: 'x', sourceAnalysisId: 'a', name: 'n', goal: '', createdAt: 'c', protocol: Array.from({ length: 51 }, () => entry), optimizedProtocol: [] };
      expect(parseAnalyzerProtocolDraft(huge)).toBeNull();

      const longName = { ...huge, protocol: [{ ...entry, compoundName: 'x'.repeat(121) }] };
      expect(parseAnalyzerProtocolDraft(longName)).toBeNull();
    });

    it('sanitizes strings and drops unknown fields instead of passing the raw object through', () => {
      const raw = {
        schemaVersion: 1,
        id: '  protocol-draft-1 ',
        sourceAnalysisId: 'analysis-001',
        name: 'Recovery alternative protocol',
        goal: ' Recovery ',
        createdAt: '2026-09-01T00:00:00.000Z',
        protocol: [{ compoundName: ' BPC-157 ', dose: 500, unit: 'mcg', frequency: 'daily', duration: '', extra: 'ignored' }],
        optimizedProtocol: [],
        extra: 'ignored',
      };

      const draft = parseAnalyzerProtocolDraft(JSON.parse(JSON.stringify(raw)));
      expect(draft).toEqual({
        id: 'protocol-draft-1',
        schemaVersion: 1,
        sourceAnalysisId: 'analysis-001',
        name: 'Recovery alternative protocol',
        goal: 'Recovery',
        createdAt: '2026-09-01T00:00:00.000Z',
        protocol: [{ compoundName: 'BPC-157', dose: 500, unit: 'mcg', frequency: 'daily', duration: '' }],
        optimizedProtocol: [],
        importStatus: { status: 'pending', importedProfileIds: [] },
      });
    });

    it('returns null when device storage is unavailable', () => {
      const getItem = vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
        throw new Error('SecurityError');
      });
      try {
        expect(readAnalyzerProtocolDraft()).toBeNull();
        expect(hasPendingAnalyzerProtocolDraft()).toBe(false);
      } finally {
        getItem.mockRestore();
      }
    });

    it('marks a draft imported for a profile so it is not offered again, and keeps the content', () => {
      const confirmed = saveAnalyzerProtocolDraft({ sourceAnalysisId: 'analysis-001', goal: 'Recovery', protocol: original, optimizedProtocol: alternative });

      const marked = markAnalyzerProtocolDraftImported('profile-1', confirmed);
      expect(marked?.importStatus.status).toBe('imported');
      expect(marked?.importStatus.importedProfileIds).toEqual(['profile-1']);
      expect(marked?.protocol).toEqual(original);

      const reread = readAnalyzerProtocolDraft();
      expect(reread?.importStatus.status).toBe('imported');
      expect(hasPendingAnalyzerProtocolDraft(reread)).toBe(false);
      expect(markAnalyzerProtocolDraftImported('profile-1', confirmed)?.importStatus.importedProfileIds).toEqual(['profile-1']);
    });

    it('a fresh convert after import produces a pending draft again', () => {
      const confirmed = saveAnalyzerProtocolDraft({ sourceAnalysisId: 'analysis-001', goal: 'Recovery', protocol: original, optimizedProtocol: alternative });
      markAnalyzerProtocolDraftImported('profile-1', confirmed);
      saveAnalyzerProtocolDraft({ sourceAnalysisId: 'analysis-002', goal: 'Sleep', protocol: original, optimizedProtocol: [] });

      expect(hasPendingAnalyzerProtocolDraft()).toBe(true);
    });

    it('does not mark a replacement imported, even for identical content saved in the same millisecond', () => {
      vi.useFakeTimers();
      try {
        vi.setSystemTime(new Date('2026-09-09T12:00:00Z'));
        const input = { sourceAnalysisId: 'analysis-001', goal: 'Recovery', protocol: original, optimizedProtocol: alternative };
        const confirmed = saveAnalyzerProtocolDraft(input);
        const replacement = saveAnalyzerProtocolDraft(input);
        expect(replacement.id).toBe(confirmed.id);
        expect(replacement.createdAt).toBe(confirmed.createdAt);
        expect(markAnalyzerProtocolDraftImported('profile-1', confirmed)).toBeNull();
        expect(readAnalyzerProtocolDraft()).toEqual(replacement);
        expect(hasPendingAnalyzerProtocolDraft()).toBe(true);
      } finally {
        vi.useRealTimers();
      }
    });

    it('clearAnalyzerProtocolDraft removes the draft and tolerates unavailable storage', () => {
      saveAnalyzerProtocolDraft({ sourceAnalysisId: 'analysis-001', goal: 'Recovery', protocol: original, optimizedProtocol: [] });
      clearAnalyzerProtocolDraft();
      expect(readAnalyzerProtocolDraft()).toBeNull();

      const removeItem = vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(() => {
        throw new Error('SecurityError');
      });
      try {
        expect(() => clearAnalyzerProtocolDraft()).not.toThrow();
      } finally {
        removeItem.mockRestore();
      }
    });
  });

  describe('buildAnalyzerDraftCompoundImports', () => {
    const draft = {
      id: 'protocol-draft-1',
      schemaVersion: 1,
      sourceAnalysisId: 'analysis-001',
      name: 'Recovery alternative protocol',
      goal: 'Recovery',
      createdAt: '2026-09-01T00:00:00.000Z',
      protocol: [
        { compoundName: 'BPC-157', dose: 500, unit: 'mcg', frequency: 'daily', duration: '4 weeks' },
        { compoundName: 'bpc-157', dose: 250, unit: 'mcg', frequency: 'daily', duration: '' },
        { compoundName: 'NAD+', dose: 0, unit: '', frequency: '', duration: '' },
        { compoundName: 'Creatine', dose: 5, unit: 'g', frequency: 'daily', duration: '' },
      ],
      optimizedProtocol: [{ compoundName: 'TB-500', dose: 2000, unit: 'mcg', frequency: 'twice-weekly', duration: '' }],
      importStatus: { status: 'pending' as const, importedProfileIds: [] },
    };

    it('imports only the user-entered protocol, never the analyzer alternative', () => {
      const imports = buildAnalyzerDraftCompoundImports(draft, []);
      expect(imports.map((item) => item.name)).toEqual(['BPC-157', 'NAD+', 'Creatine']);
      expect(imports.some((item) => item.name === 'TB-500')).toBe(false);
    });

    it('records the entered dose as a note without turning it into a recommendation', () => {
      const [bpc, nad] = buildAnalyzerDraftCompoundImports(draft, []);
      expect(bpc.asEntered).toBe('500 mcg · daily · 4 weeks');
      expect(bpc.notes).toContain('as entered: 500 mcg · daily · 4 weeks');
      expect(bpc.notes).toContain('Recorded exactly as you wrote it');
      expect(nad.asEntered).toBe('');
      expect(nad.notes).not.toContain('as entered');
    });

    it('skips compounds already on the target profile, case-insensitively', () => {
      const imports = buildAnalyzerDraftCompoundImports(draft, ['nad+', ' Creatine ']);
      expect(imports.map((item) => item.name)).toEqual(['BPC-157']);
    });

    it('formats entries as entered', () => {
      expect(formatAnalyzerEntry({ compoundName: 'X', dose: 2.5, unit: 'mg', frequency: 'weekly', duration: '' })).toBe('2.5 mg · weekly');
      expect(formatAnalyzerEntry({ compoundName: 'X', dose: 0.0005, unit: 'mg', frequency: 'daily', duration: '' })).toBe('0.0005 mg · daily');
      const [small] = buildAnalyzerDraftCompoundImports({ ...draft, protocol: [{ ...draft.protocol[0], dose: 0.0005, unit: 'mg' }] }, []);
      expect(small.notes).toContain('as entered: 0.0005 mg');
      expect(formatAnalyzerEntry({ compoundName: 'X', dose: 0, unit: 'mg', frequency: '', duration: '' })).toBe('');
    });
  });
});
