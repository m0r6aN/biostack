import { describe, expect, it } from 'vitest';
import {
  analyzerErrorPresentation,
  formatAnalyzerError,
  formatDelta,
  formatDose,
  getScoreBand,
  unique,
} from '@/components/tools/analyzer/analyzerView';
import { ApiError } from '@/lib/api';
import type { ProtocolAnalyzerResult } from '@/lib/types';

// ── formatAnalyzerError / analyzerErrorPresentation ──────────────────────
//
// Protocol-upload remediation contract: when the analyzer API answers 400/422
// with curated validation copy (ProtocolIngestionException messages such as
// "That file is too large for the analyzer right now."), the UI must surface
// that message instead of collapsing it into generic "temporarily unavailable"
// outage framing. Synthetic placeholders ("API Error: <status>") and transport
// noise are never shown verbatim.

describe('formatAnalyzerError — safe API validation errors', () => {
  it('surfaces a 400 validation message verbatim instead of generic text', () => {
    const error = new ApiError(400, 'This PDF did not expose readable text. Try a clearer source file or a direct image scan.');

    expect(formatAnalyzerError(error, 'FileUpload')).toBe(
      'This PDF did not expose readable text. Try a clearer source file or a direct image scan.',
    );
  });

  it('surfaces a 422 validation message verbatim', () => {
    const error = new ApiError(422, 'That file is too large for the analyzer right now. Keep uploads under 12 MB.');

    expect(formatAnalyzerError(error, 'FileUpload')).toBe(
      'That file is too large for the analyzer right now. Keep uploads under 12 MB.',
    );
  });

  it('classifies server-provided 400s as validation kind', () => {
    const error = new ApiError(400, 'Protocol text is required.');

    expect(analyzerErrorPresentation(error, 'Paste')).toEqual({
      message: 'Protocol text is required.',
      kind: 'validation',
    });
  });

  it('never surfaces the synthetic API Error placeholder for 400s', () => {
    const error = new ApiError(400, 'API Error: 400 Bad Request');

    const presentation = analyzerErrorPresentation(error, 'FileUpload');
    expect(presentation.kind).toBe('service');
    expect(presentation.message).not.toContain('API Error:');
    expect(presentation.message).toBe(
      'BioStack could not analyze that input yet. Check the protocol text and try again.',
    );
  });

  it('never surfaces transport noise threaded through an ApiError-shaped message', () => {
    const error = new ApiError(400, 'Failed to fetch');

    const presentation = analyzerErrorPresentation(error, 'FileUpload');
    expect(presentation.kind).toBe('service');
    expect(presentation.message).not.toMatch(/failed to fetch/i);
  });

  it('keeps OCR service failures in camera mode on the scan-unavailable framing', () => {
    const error = new Error('OCR endpoint timed out reading text from the image');

    expect(formatAnalyzerError(error, 'CameraScan')).toBe(
      'Scan is temporarily unavailable. Upload a PDF, spreadsheet, or paste text to analyze now.',
    );
    expect(analyzerErrorPresentation(error, 'CameraScan').kind).toBe('service');
  });

  it('keeps network failures on the service-unreachable framing', () => {
    const error = new Error('network down');

    const presentation = analyzerErrorPresentation(error, 'Paste');
    expect(presentation.kind).toBe('service');
    expect(presentation.message).toBe(
      'BioStack could not reach the intelligence service. Your input is still safe. Try again in a moment.',
    );
  });

  it('classifies 404 route failures as service, not validation', () => {
    const error = new ApiError(404, 'Not Found');

    expect(analyzerErrorPresentation(error, 'FileUpload').kind).toBe('service');
  });
});

// ── getScoreBand ──────────────────────────────────────────────────────────────

describe('getScoreBand', () => {
  it('returns excellent_fit at 90', () => {
    expect(getScoreBand(90)).toBe('excellent_fit');
  });

  it('returns strong_fit at 70', () => {
    expect(getScoreBand(70)).toBe('strong_fit');
  });

  it('returns mixed_fit at 60', () => {
    expect(getScoreBand(60)).toBe('mixed_fit');
  });

  it('returns inefficient at 40', () => {
    expect(getScoreBand(40)).toBe('inefficient');
  });

  it('returns high_concern at 30', () => {
    expect(getScoreBand(30)).toBe('high_concern');
  });

  it('returns unknown when undefined', () => {
    expect(getScoreBand(undefined)).toBe('unknown');
  });

  // Boundary: exactly 85 → excellent_fit
  it('returns excellent_fit at exactly 85', () => {
    expect(getScoreBand(85)).toBe('excellent_fit');
  });

  // Boundary: 84 → strong_fit
  it('returns strong_fit at 84', () => {
    expect(getScoreBand(84)).toBe('strong_fit');
  });
});

// ── formatDelta ───────────────────────────────────────────────────────────────

describe('formatDelta', () => {
  it('prefixes positive values with +', () => {
    expect(formatDelta(5)).toBe('+5');
  });

  it('does not double-prefix negative values', () => {
    expect(formatDelta(-3)).toBe('-3');
  });

  it('returns "0" for zero', () => {
    expect(formatDelta(0)).toBe('0');
  });

  it('rounds fractional values', () => {
    expect(formatDelta(4.7)).toBe('+5');
    expect(formatDelta(-2.3)).toBe('-2');
  });
});

// ── unique ────────────────────────────────────────────────────────────────────

describe('unique', () => {
  it('removes duplicates', () => {
    expect(unique(['a', 'a', 'b'])).toEqual(['a', 'b']);
  });

  it('trims whitespace before deduplication', () => {
    expect(unique([' a', 'a ', 'b'])).toEqual(['a', 'b']);
  });

  it('removes empty strings', () => {
    expect(unique(['a', '', 'b', '  '])).toEqual(['a', 'b']);
  });

  it('returns empty array for empty input', () => {
    expect(unique([])).toEqual([]);
  });
});

// ── formatDose ────────────────────────────────────────────────────────────────

type Entry = ProtocolAnalyzerResult['protocol'][number];

function makeEntry(overrides: Partial<Entry> = {}): Entry {
  return {
    compoundName: 'BPC-157',
    dose: 500,
    unit: 'mcg',
    frequency: 'daily',
    duration: '',
    ...overrides,
  };
}

describe('formatDose', () => {
  it('returns empty string when dose is 0', () => {
    expect(formatDose(makeEntry({ dose: 0 }))).toBe('');
  });

  it('returns empty string when dose is negative', () => {
    expect(formatDose(makeEntry({ dose: -1 }))).toBe('');
  });

  it('returns dose and unit for a positive dose', () => {
    expect(formatDose(makeEntry({ dose: 500, unit: 'mcg' }))).toBe('500 mcg');
  });

  it('trims the result (no leading/trailing spaces)', () => {
    const result = formatDose(makeEntry({ dose: 100, unit: 'mg' }));
    expect(result).toBe('100 mg');
    expect(result.trim()).toBe(result);
  });
});
