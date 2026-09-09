import type { ProtocolAnalyzerEntry, ProtocolAnalyzerInputType, ProtocolAnalyzerResult } from './types';

export const ANALYZER_ANALYSIS_HISTORY_KEY = 'biostack.analyzer.analysisHistory.v1';
export const ANALYZER_PROTOCOL_DRAFT_KEY = 'biostack.analyzer.protocolDraft.v1';
export const ANALYZER_DRAFT_COMPOUND_SOURCE = 'Protocol Analyzer';

const SCHEMA_VERSION = 1;
const MAX_DRAFT_ENTRIES = 50;
const MAX_NAME_LENGTH = 120;
const MAX_LABEL_LENGTH = 40;
const MAX_IMPORTED_PROFILE_IDS = 50;
const draftListeners = new Set<() => void>();

export interface AnalyzerDraftImportStatus {
  status: 'pending' | 'imported';
  importedAt?: string;
  importedProfileIds: string[];
}

export interface AnalyzerDraftCompoundImport {
  name: string;
  asEntered: string;
  notes: string;
}

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
  /** Unique per save; older drafts are compared using their complete immutable contents. */
  revision?: string;
  schemaVersion: number;
  sourceAnalysisId: string;
  name: string;
  protocol: ProtocolAnalyzerResult['protocol'];
  optimizedProtocol: ProtocolAnalyzerResult['protocol'];
  goal: string;
  createdAt: string;
  /** Drafts written before continuation existed lack this in storage; the reader fills in `pending`. */
  importStatus: AnalyzerDraftImportStatus;
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
    revision: crypto.randomUUID(),
    schemaVersion: SCHEMA_VERSION,
    sourceAnalysisId: input.sourceAnalysisId,
    name: `${input.goal || 'BioStack'} alternative protocol`,
    protocol: input.protocol,
    optimizedProtocol: input.optimizedProtocol,
    goal: input.goal,
    createdAt: now,
    importStatus: { status: 'pending', importedProfileIds: [] },
  };

  window.localStorage.setItem(ANALYZER_PROTOCOL_DRAFT_KEY, JSON.stringify(draft));
  notifyDraftListeners();
  return draft;
}

/**
 * Same-tab change notification for the draft key (the browser `storage` event
 * only fires in *other* tabs). Returns an unsubscribe function.
 */
export function subscribeToAnalyzerProtocolDraft(listener: () => void): () => void {
  draftListeners.add(listener);
  return () => {
    draftListeners.delete(listener);
  };
}

/** Raw stored value for the draft key, or null when storage is unavailable/empty. */
export function readAnalyzerProtocolDraftRaw(): string | null {
  return readStorageItem(ANALYZER_PROTOCOL_DRAFT_KEY);
}

/**
 * Reads the saved analyzer draft from device storage. Device storage is
 * untrusted: unavailable storage, non-JSON, wrong schema, or malformed
 * entries all yield `null` rather than a partially-trusted object.
 */
export function readAnalyzerProtocolDraft(): AnalyzerProtocolDraft | null {
  return parseAnalyzerProtocolDraftRaw(readStorageItem(ANALYZER_PROTOCOL_DRAFT_KEY));
}

/** Parses a raw stored string (as returned by `readAnalyzerProtocolDraftRaw`) into a draft, or null. */
export function parseAnalyzerProtocolDraftRaw(raw: string | null): AnalyzerProtocolDraft | null {
  if (!raw) {
    return null;
  }

  try {
    return parseAnalyzerProtocolDraft(JSON.parse(raw));
  } catch {
    return null;
  }
}

/** Validates and sanitizes an untrusted value into a draft, or returns null. */
export function parseAnalyzerProtocolDraft(value: unknown): AnalyzerProtocolDraft | null {
  if (!isRecord(value) || value.schemaVersion !== SCHEMA_VERSION) {
    return null;
  }

  const id = readString(value.id, MAX_NAME_LENGTH);
  const revision = value.revision === undefined ? undefined : readString(value.revision, MAX_NAME_LENGTH);
  const sourceAnalysisId = readString(value.sourceAnalysisId, MAX_NAME_LENGTH);
  const name = readString(value.name, MAX_NAME_LENGTH);
  const goal = readString(value.goal, MAX_NAME_LENGTH, true);
  const createdAt = readString(value.createdAt, MAX_LABEL_LENGTH);
  const protocol = readEntries(value.protocol);
  const optimizedProtocol = readEntries(value.optimizedProtocol, true);
  const importStatus = readImportStatus(value.importStatus);

  if (
    id === null ||
    revision === null ||
    sourceAnalysisId === null ||
    name === null ||
    goal === null ||
    createdAt === null ||
    protocol === null ||
    protocol.length === 0 ||
    optimizedProtocol === null ||
    importStatus === null
  ) {
    return null;
  }

  return {
    id,
    ...(revision === undefined ? {} : { revision }),
    schemaVersion: SCHEMA_VERSION,
    sourceAnalysisId,
    name,
    protocol,
    optimizedProtocol,
    goal,
    createdAt,
    importStatus,
  };
}

export function hasPendingAnalyzerProtocolDraft(
  draft: AnalyzerProtocolDraft | null = readAnalyzerProtocolDraft()
): draft is AnalyzerProtocolDraft {
  return Boolean(draft) && draft?.importStatus.status !== 'imported';
}

/** Identity of the reviewed contents, excluding mutable import bookkeeping. */
export function getAnalyzerProtocolDraftRevision(draft: AnalyzerProtocolDraft): string {
  return stableStringify(Object.fromEntries(Object.entries(draft).filter(([key]) => key !== 'importStatus')));
}

/** Marks only the confirmed draft, leaving a replacement saved during the import pending. */
export function markAnalyzerProtocolDraftImported(
  profileId: string,
  confirmedDraft: AnalyzerProtocolDraft
): AnalyzerProtocolDraft | null {
  const current = readAnalyzerProtocolDraft();
  if (!current || getAnalyzerProtocolDraftRevision(current) !== getAnalyzerProtocolDraftRevision(confirmedDraft)) {
    return null;
  }

  const importedProfileIds = Array.from(
    new Set([...current.importStatus.importedProfileIds, profileId])
  ).slice(-MAX_IMPORTED_PROFILE_IDS);
  const next: AnalyzerProtocolDraft = {
    ...current,
    importStatus: { status: 'imported', importedAt: new Date().toISOString(), importedProfileIds },
  };

  if (!writeStorageItem(ANALYZER_PROTOCOL_DRAFT_KEY, JSON.stringify(next))) {
    return null;
  }

  notifyDraftListeners();
  return next;
}

export function clearAnalyzerProtocolDraft(): void {
  if (typeof window === 'undefined') {
    return;
  }

  try {
    window.localStorage.removeItem(ANALYZER_PROTOCOL_DRAFT_KEY);
  } catch {
    // Storage unavailable — nothing to clear.
  }
  notifyDraftListeners();
}

/** Human-readable "as entered" summary of one analyzer entry, e.g. "500 mcg · daily · 4 weeks". */
export function formatAnalyzerEntry(entry: ProtocolAnalyzerEntry): string {
  const dose = entry.dose > 0 ? `${String(entry.dose)}${entry.unit ? ` ${entry.unit}` : ''}` : '';
  return [dose, entry.frequency, entry.duration].map((part) => part.trim()).filter(Boolean).join(' · ');
}

/**
 * Builds compound-creation inputs from the draft's ORIGINAL (user-entered)
 * protocol only. The analyzer-generated alternative is never imported here.
 * Names already present on the target profile (case-insensitive) are skipped.
 */
export function buildAnalyzerDraftCompoundImports(
  draft: AnalyzerProtocolDraft,
  existingCompoundNames: string[]
): AnalyzerDraftCompoundImport[] {
  const seen = new Set(existingCompoundNames.map((name) => name.trim().toLowerCase()).filter(Boolean));
  const imports: AnalyzerDraftCompoundImport[] = [];

  for (const entry of draft.protocol) {
    const name = entry.compoundName.trim();
    const key = name.toLowerCase();
    if (!name || seen.has(key)) {
      continue;
    }

    seen.add(key);
    const asEntered = formatAnalyzerEntry(entry);
    imports.push({
      name,
      asEntered,
      notes: asEntered
        ? `Added from your Protocol Analyzer entry (as entered: ${asEntered}). Recorded exactly as you wrote it.`
        : 'Added from your Protocol Analyzer entry. Recorded exactly as you wrote it.',
    });
  }

  return imports;
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

function notifyDraftListeners(): void {
  for (const listener of draftListeners) {
    listener();
  }
}

function readStorageItem(key: string): string | null {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

function writeStorageItem(key: string, value: string): boolean {
  if (typeof window === 'undefined') {
    return false;
  }

  try {
    window.localStorage.setItem(key, value);
    return true;
  } catch {
    return false;
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function readString(value: unknown, maxLength: number, allowEmpty = false): string | null {
  if (typeof value !== 'string') {
    return null;
  }

  const trimmed = value.trim();
  if ((!allowEmpty && trimmed.length === 0) || trimmed.length > maxLength) {
    return null;
  }

  return trimmed;
}

function readEntry(value: unknown): ProtocolAnalyzerEntry | null {
  if (!isRecord(value)) {
    return null;
  }

  const compoundName = readString(value.compoundName, MAX_NAME_LENGTH);
  const unit = readString(value.unit, MAX_LABEL_LENGTH, true);
  const frequency = readString(value.frequency, MAX_LABEL_LENGTH, true);
  const duration = readString(value.duration, MAX_LABEL_LENGTH, true);
  const dose = typeof value.dose === 'number' && Number.isFinite(value.dose) && value.dose >= 0 ? value.dose : null;

  if (compoundName === null || unit === null || frequency === null || duration === null || dose === null) {
    return null;
  }

  return { compoundName, dose, unit, frequency, duration };
}

function readEntries(value: unknown, allowEmpty = false): ProtocolAnalyzerEntry[] | null {
  if (!Array.isArray(value) || value.length > MAX_DRAFT_ENTRIES || (!allowEmpty && value.length === 0)) {
    return null;
  }

  const entries: ProtocolAnalyzerEntry[] = [];
  for (const item of value) {
    const entry = readEntry(item);
    if (!entry) {
      return null;
    }
    entries.push(entry);
  }

  return entries;
}

function readImportStatus(value: unknown): AnalyzerDraftImportStatus | null {
  if (value === undefined) {
    return { status: 'pending', importedProfileIds: [] };
  }

  if (!isRecord(value) || (value.status !== 'pending' && value.status !== 'imported')) {
    return null;
  }

  const ids = value.importedProfileIds;
  if (!Array.isArray(ids) || ids.length > MAX_IMPORTED_PROFILE_IDS || !ids.every((id) => typeof id === 'string')) {
    return null;
  }

  const importedAt = value.importedAt === undefined ? undefined : readString(value.importedAt, MAX_LABEL_LENGTH);
  if (importedAt === null) {
    return null;
  }

  return { status: value.status, importedAt, importedProfileIds: ids as string[] };
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
