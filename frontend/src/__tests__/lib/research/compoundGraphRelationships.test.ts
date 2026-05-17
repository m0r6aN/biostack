import { describe, expect, it } from 'vitest';
import {
  RELATIONSHIP_EDGE_KEYS,
  getCommunitySignalLabel,
  getEvidenceTierLabel,
  getRelationshipLabel,
  isRelationshipEdge,
  normalizeCompoundId,
  normalizeEdgeType,
} from '@/lib/research/compoundGraphRelationships';

describe('normalizeEdgeType', () => {
  it('strips dashes and lowercases', () => {
    expect(normalizeEdgeType('synergizes-with')).toBe('synergizeswith');
  });
  it('strips underscores and whitespace', () => {
    expect(normalizeEdgeType('conflicts_With ')).toBe('conflictswith');
  });
  it('returns empty string for null/undefined', () => {
    expect(normalizeEdgeType(null)).toBe('');
    expect(normalizeEdgeType(undefined)).toBe('');
  });
});

describe('normalizeCompoundId', () => {
  it('strips compound: prefix then normalizes', () => {
    expect(normalizeCompoundId('compound:bpc-157')).toBe('bpc157');
  });
  it('produces identical output for all BPC-157 variants', () => {
    const expected = 'bpc157';
    expect(normalizeCompoundId('BPC-157')).toBe(expected);
    expect(normalizeCompoundId('bpc-157')).toBe(expected);
    expect(normalizeCompoundId('bpc 157')).toBe(expected);
    expect(normalizeCompoundId('bpc157')).toBe(expected);
    expect(normalizeCompoundId('compound:BPC-157')).toBe(expected);
  });
  it('returns empty string for null/undefined', () => {
    expect(normalizeCompoundId(null)).toBe('');
    expect(normalizeCompoundId(undefined)).toBe('');
  });
});

describe('isRelationshipEdge', () => {
  it('returns true for all whitelist entries', () => {
    for (const key of RELATIONSHIP_EDGE_KEYS) {
      expect(isRelationshipEdge(key)).toBe(true);
    }
  });
  it('returns true for kebab-case whitelist entries', () => {
    expect(isRelationshipEdge('synergizes-with')).toBe(true);
    expect(isRelationshipEdge('conflicts-with')).toBe(true);
    expect(isRelationshipEdge('avoid-with')).toBe(true);
  });
  it('returns false for taxonomic edges', () => {
    expect(isRelationshipEdge('belongs-to-category')).toBe(false);
    expect(isRelationshipEdge('affects-pathway')).toBe(false);
    expect(isRelationshipEdge('has-target')).toBe(false);
  });
  it('returns false for null/undefined', () => {
    expect(isRelationshipEdge(null)).toBe(false);
    expect(isRelationshipEdge(undefined)).toBe(false);
  });
});

describe('getRelationshipLabel', () => {
  it('maps synergizes-with to user-facing copy', () => {
    expect(getRelationshipLabel('synergizes-with')).toBe('May work well together');
    expect(getRelationshipLabel('synergizeswith')).toBe('May work well together');
  });
  it('maps pairs-with to same label as synergizes-with', () => {
    expect(getRelationshipLabel('pairs-with')).toBe('May work well together');
  });
  it('maps complements', () => {
    expect(getRelationshipLabel('complements')).toBe('May support the same goal differently');
  });
  it('maps redundant-with', () => {
    expect(getRelationshipLabel('redundant-with')).toBe('May overlap');
  });
  it('maps conflicts-with and opposes-effect to Potential conflict', () => {
    expect(getRelationshipLabel('conflicts-with')).toBe('Potential conflict');
    expect(getRelationshipLabel('opposes-effect')).toBe('Potential conflict');
  });
  it('maps avoid-with to Caution signal', () => {
    expect(getRelationshipLabel('avoid-with')).toBe('Caution signal');
  });
  it('maps has-community-signal', () => {
    expect(getRelationshipLabel('has-community-signal')).toBe('Community-reported pairing');
  });
  it('maps contradicted-by', () => {
    expect(getRelationshipLabel('contradicted-by')).toBe('Contradicted by evidence');
  });
  it('returns Related compound for unknown types', () => {
    expect(getRelationshipLabel('unknown-type')).toBe('Related compound');
    expect(getRelationshipLabel(null)).toBe('Related compound');
  });
});

describe('getEvidenceTierLabel', () => {
  it('maps strong (case-insensitive)', () => {
    expect(getEvidenceTierLabel('strong')).toBe('Strong evidence');
    expect(getEvidenceTierLabel('Strong')).toBe('Strong evidence');
    expect(getEvidenceTierLabel('STRONG')).toBe('Strong evidence');
  });
  it('maps moderate', () => {
    expect(getEvidenceTierLabel('moderate')).toBe('Moderate evidence');
  });
  it('maps limited', () => {
    expect(getEvidenceTierLabel('limited')).toBe('Limited evidence');
  });
  it('maps mechanistic', () => {
    expect(getEvidenceTierLabel('mechanistic')).toBe('Mechanistic evidence');
  });
  it('maps anecdotal to community report copy without em dash', () => {
    expect(getEvidenceTierLabel('anecdotal')).toBe('Community report: not clinically verified');
  });
  it('returns Evidence level unknown for null, undefined, empty, and unrecognised', () => {
    expect(getEvidenceTierLabel(null)).toBe('Evidence level unknown');
    expect(getEvidenceTierLabel(undefined)).toBe('Evidence level unknown');
    expect(getEvidenceTierLabel('')).toBe('Evidence level unknown');
    expect(getEvidenceTierLabel('unknown')).toBe('Evidence level unknown');
    expect(getEvidenceTierLabel('insufficient')).toBe('Evidence level unknown');
  });
});

describe('getCommunitySignalLabel', () => {
  it('returns string for isolated', () => {
    expect(getCommunitySignalLabel('isolated')).toBe('Rarely reported in community');
  });
  it('returns string for recurring', () => {
    expect(getCommunitySignalLabel('recurring')).toBe('Commonly reported in community');
  });
  it('returns string for widespread', () => {
    expect(getCommunitySignalLabel('widespread')).toBe('Widely reported across communities');
  });
  it('returns null for none', () => {
    expect(getCommunitySignalLabel('none')).toBeNull();
  });
  it('returns null for null/undefined/empty', () => {
    expect(getCommunitySignalLabel(null)).toBeNull();
    expect(getCommunitySignalLabel(undefined)).toBeNull();
    expect(getCommunitySignalLabel('')).toBeNull();
  });
});
