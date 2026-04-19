'use client';

import { type InteractionFlag } from './types';

export const ANONYMOUS_TOOL_PAYLOAD_KEY = 'biostack.anonymousTools.v1';
const SCHEMA_VERSION = 1;

export type ToolMode = 'dose' | 'mix' | 'convert';

export interface SavedToolArtifact {
  id: string;
  contentHash: string;
  calculatorType: ToolMode;
  substances: string[];
  inputs: Record<string, unknown>;
  outputs: Record<string, unknown>;
  reconstitutionInstructions: string[];
  storageInstructions: string[];
  compatibilityFindings: Array<Pick<InteractionFlag, 'compoundNames' | 'overlapType' | 'pathwayTag' | 'description' | 'evidenceConfidence'>>;
  source: 'local_device_bootstrap';
  createdAt: string;
  updatedAt: string;
}

export interface DraftStackItem {
  id: string;
  name: string;
  sourceArtifactId: string;
  source: 'local_device_bootstrap';
  createdAt: string;
}

export interface AnonymousToolPayload {
  schemaVersion: number;
  savedCalculations: SavedToolArtifact[];
  savedSetups: SavedToolArtifact[];
  savedCompatibilityChecks: SavedToolArtifact[];
  draftStackItems: DraftStackItem[];
  importStatus: {
    status: 'pending' | 'imported';
    importedAt?: string;
    importedProfileIds: string[];
  };
  createdAt: string;
  updatedAt: string;
}

export function readAnonymousToolPayload(): AnonymousToolPayload | null {
  if (typeof window === 'undefined') {
    return null;
  }

  const raw = window.localStorage.getItem(ANONYMOUS_TOOL_PAYLOAD_KEY);
  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as AnonymousToolPayload;
    if (parsed.schemaVersion !== SCHEMA_VERSION || !Array.isArray(parsed.savedCalculations)) {
      return null;
    }

    return parsed;
  } catch {
    return null;
  }
}

export function hasPendingAnonymousToolData(payload = readAnonymousToolPayload()): boolean {
  if (!payload || payload.importStatus.status === 'imported') {
    return false;
  }

  return (
    payload.savedCalculations.length > 0 ||
    payload.savedSetups.length > 0 ||
    payload.savedCompatibilityChecks.length > 0 ||
    payload.draftStackItems.length > 0
  );
}

export function saveAnonymousToolArtifact(artifact: Omit<SavedToolArtifact, 'id' | 'contentHash' | 'source' | 'createdAt' | 'updatedAt'>): AnonymousToolPayload {
  if (typeof window === 'undefined') {
    throw new Error('Local saving is only available in a browser.');
  }

  const now = new Date().toISOString();
  const contentHash = stableHash({
    calculatorType: artifact.calculatorType,
    substances: artifact.substances,
    inputs: artifact.inputs,
    outputs: artifact.outputs,
    compatibilityFindings: artifact.compatibilityFindings,
  });
  const id = `local-tool-${contentHash}`;
  const nextArtifact: SavedToolArtifact = {
    ...artifact,
    id,
    contentHash,
    source: 'local_device_bootstrap',
    createdAt: now,
    updatedAt: now,
  };

  const current = readAnonymousToolPayload() ?? createEmptyPayload(now);
  const savedCalculations = upsertByHash(current.savedCalculations, nextArtifact);
  const savedSetups =
    artifact.calculatorType === 'dose' ? upsertByHash(current.savedSetups, nextArtifact) : current.savedSetups;
  const savedCompatibilityChecks =
    artifact.compatibilityFindings.length > 0
      ? upsertByHash(current.savedCompatibilityChecks, nextArtifact)
      : current.savedCompatibilityChecks;
  const draftStackItems = upsertDraftItems(current.draftStackItems, nextArtifact, now);

  const payload: AnonymousToolPayload = {
    ...current,
    savedCalculations,
    savedSetups,
    savedCompatibilityChecks,
    draftStackItems,
    importStatus: current.importStatus.status === 'imported'
      ? { status: 'pending', importedProfileIds: current.importStatus.importedProfileIds }
      : current.importStatus,
    updatedAt: now,
  };

  window.localStorage.setItem(ANONYMOUS_TOOL_PAYLOAD_KEY, JSON.stringify(payload));
  return payload;
}

export function deleteAnonymousToolArtifact(artifactId: string): AnonymousToolPayload | null {
  if (typeof window === 'undefined') {
    return null;
  }

  const current = readAnonymousToolPayload();
  if (!current) {
    return null;
  }

  const now = new Date().toISOString();
  const payload: AnonymousToolPayload = {
    ...current,
    savedCalculations: current.savedCalculations.filter((item) => item.id !== artifactId),
    savedSetups: current.savedSetups.filter((item) => item.id !== artifactId),
    savedCompatibilityChecks: current.savedCompatibilityChecks.filter((item) => item.id !== artifactId),
    draftStackItems: current.draftStackItems.filter((item) => item.sourceArtifactId !== artifactId),
    updatedAt: now,
  };

  window.localStorage.setItem(ANONYMOUS_TOOL_PAYLOAD_KEY, JSON.stringify(payload));
  return payload;
}

export function markAnonymousToolPayloadImported(profileId: string): AnonymousToolPayload | null {
  if (typeof window === 'undefined') {
    return null;
  }

  const current = readAnonymousToolPayload();
  if (!current) {
    return null;
  }

  const now = new Date().toISOString();
  const payload: AnonymousToolPayload = {
    ...current,
    importStatus: {
      status: 'imported',
      importedAt: now,
      importedProfileIds: Array.from(new Set([...current.importStatus.importedProfileIds, profileId])),
    },
    updatedAt: now,
  };

  window.localStorage.setItem(ANONYMOUS_TOOL_PAYLOAD_KEY, JSON.stringify(payload));
  return payload;
}

export function buildImportedProfileNotes(payload: AnonymousToolPayload | null): string {
  if (!payload || !hasPendingAnonymousToolData(payload)) {
    return '';
  }

  const lines = [
    'Imported from local device bootstrap.',
    '',
    'Saved calculations:',
    ...payload.savedCalculations.slice(0, 6).map((artifact) => {
      const primary = artifact.outputs.primaryAnswer ?? artifact.outputs.result ?? 'Calculation saved';
      return `- ${artifact.substances.join(' + ') || 'Saved tool'}: ${String(primary)}`;
    }),
  ];

  return lines.join('\n');
}

function createEmptyPayload(now: string): AnonymousToolPayload {
  return {
    schemaVersion: SCHEMA_VERSION,
    savedCalculations: [],
    savedSetups: [],
    savedCompatibilityChecks: [],
    draftStackItems: [],
    importStatus: {
      status: 'pending',
      importedProfileIds: [],
    },
    createdAt: now,
    updatedAt: now,
  };
}

function upsertByHash(items: SavedToolArtifact[], artifact: SavedToolArtifact): SavedToolArtifact[] {
  const existingIndex = items.findIndex((item) => item.contentHash === artifact.contentHash);
  if (existingIndex === -1) {
    return [artifact, ...items];
  }

  return items.map((item, index) =>
    index === existingIndex ? { ...artifact, createdAt: item.createdAt } : item
  );
}

function upsertDraftItems(items: DraftStackItem[], artifact: SavedToolArtifact, now: string): DraftStackItem[] {
  const names = artifact.substances.map((name) => name.trim()).filter(Boolean);
  const existingKeys = new Set(items.map((item) => `${item.name.toLowerCase()}|${item.sourceArtifactId}`));
  const additions = names
    .map((name) => ({
      id: `local-draft-${stableHash({ name, artifactId: artifact.id })}`,
      name,
      sourceArtifactId: artifact.id,
      source: 'local_device_bootstrap' as const,
      createdAt: now,
    }))
    .filter((item) => !existingKeys.has(`${item.name.toLowerCase()}|${item.sourceArtifactId}`));

  return [...items, ...additions];
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
