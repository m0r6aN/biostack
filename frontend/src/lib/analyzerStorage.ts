import type { ProtocolAnalyzerInputType, ProtocolAnalyzerResult } from './types';

export const ANALYZER_ANALYSIS_HISTORY_KEY = 'biostack.analyzer.analysisHistory.v1';
export const ANALYZER_PROTOCOL_DRAFT_KEY = 'biostack.analyzer.protocolDraft.v1';

const SCHEMA_VERSION = 1;

export interface SavedAnalyzerAnalysis {
  id: string;
  schemaVersion: number;
  inputType: ProtocolAnalyzerInputType;
  sourceName: string | null;
  rawInput: string;
  normalizedPreview: string | null;
  parsedProtocol: ProtocolAnalyzerResult['protocol'];
  score: number;
  issues: ProtocolAnalyzerResult['issues'];
  counterfactuals: ProtocolAnalyzerResult['counterfactuals'];
  versionMetadata: {
    analyzerStorageVersion: number;
    resultInputType: string;
  };
  createdAt: string;
}

export interface AnalyzerProtocolDraft {
  id: string;
  schemaVersion: number;
  sourceAnalysisId: string;
  name: string;
  protocol: ProtocolAnalyzerResult['protocol'];
  optimizedProtocol: ProtocolAnalyzerResult['protocol'];
  goal: string;
  createdAt: string;
}

export function saveAnalyzerAnalysis(input: {
  inputType: ProtocolAnalyzerInputType;
  sourceName: string | null;
  rawInput: string;
  result: ProtocolAnalyzerResult;
}): SavedAnalyzerAnalysis {
  if (typeof window === 'undefined') {
    throw new Error('Analyzer analysis saving is only available in a browser.');
  }

  const now = new Date().toISOString();
  const id = `analysis-${stableHash({
    inputType: input.inputType,
    sourceName: input.sourceName,
    rawInput: input.rawInput,
    score: input.result.score,
    protocol: input.result.protocol,
  })}`;

  const analysis: SavedAnalyzerAnalysis = {
    id,
    schemaVersion: SCHEMA_VERSION,
    inputType: input.inputType,
    sourceName: input.sourceName,
    rawInput: input.rawInput,
    normalizedPreview: input.result.extractedTextPreview,
    parsedProtocol: input.result.protocol,
    score: input.result.score,
    issues: input.result.issues,
    counterfactuals: input.result.counterfactuals,
    versionMetadata: {
      analyzerStorageVersion: SCHEMA_VERSION,
      resultInputType: input.result.inputType,
    },
    createdAt: now,
  };

  const history = readAnalyzerAnalysisHistory();
  const nextHistory = [analysis, ...history.filter((item) => item.id !== id)].slice(0, 25);
  window.localStorage.setItem(ANALYZER_ANALYSIS_HISTORY_KEY, JSON.stringify(nextHistory));
  return analysis;
}

export function saveAnalyzerProtocolDraft(input: {
  sourceAnalysisId: string;
  goal: string;
  protocol: ProtocolAnalyzerResult['protocol'];
  optimizedProtocol: ProtocolAnalyzerResult['protocol'];
}): AnalyzerProtocolDraft {
  if (typeof window === 'undefined') {
    throw new Error('Analyzer protocol draft saving is only available in a browser.');
  }

  const now = new Date().toISOString();
  const draft: AnalyzerProtocolDraft = {
    id: `protocol-draft-${stableHash({ sourceAnalysisId: input.sourceAnalysisId, optimizedProtocol: input.optimizedProtocol })}`,
    schemaVersion: SCHEMA_VERSION,
    sourceAnalysisId: input.sourceAnalysisId,
    name: `${input.goal || 'BioStack'} optimized protocol`,
    protocol: input.protocol,
    optimizedProtocol: input.optimizedProtocol,
    goal: input.goal,
    createdAt: now,
  };

  window.localStorage.setItem(ANALYZER_PROTOCOL_DRAFT_KEY, JSON.stringify(draft));
  return draft;
}

export function readAnalyzerAnalysisHistory(): SavedAnalyzerAnalysis[] {
  if (typeof window === 'undefined') {
    return [];
  }

  const raw = window.localStorage.getItem(ANALYZER_ANALYSIS_HISTORY_KEY);
  if (!raw) {
    return [];
  }

  try {
    const parsed = JSON.parse(raw) as SavedAnalyzerAnalysis[];
    return Array.isArray(parsed) ? parsed.filter((item) => item.schemaVersion === SCHEMA_VERSION) : [];
  } catch {
    return [];
  }
}

function stableHash(value: unknown): string {
  const input = stableStringify(value);
  let hash = 5381;
  for (let index = 0; index < input.length; index += 1) {
    hash = (hash * 33) ^ input.charCodeAt(index);
  }

  return (hash >>> 0).toString(16);
}

function stableStringify(value: unknown): string {
  if (Array.isArray(value)) {
    return `[${value.map(stableStringify).join(',')}]`;
  }

  if (value && typeof value === 'object') {
    return `{${Object.entries(value as Record<string, unknown>)
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([key, item]) => `${JSON.stringify(key)}:${stableStringify(item)}`)
      .join(',')}}`;
  }

  return JSON.stringify(value);
}
