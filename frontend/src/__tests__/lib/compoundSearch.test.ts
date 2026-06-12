import { getCompoundSearchSuggestions, resolveCompoundSearchCommit } from '@/lib/compoundSearch';
import { describe, expect, it } from 'vitest';

const compounds = ['BPC-157', 'Semaglutide', 'Retatrutide'];

describe('compound search helpers', () => {
  it('finds recognized compound suggestions from partial text', () => {
    expect(getCompoundSearchSuggestions('sema', compounds)).toEqual(['Semaglutide']);
  });

  it('commits recognized compounds using canonical casing', () => {
    expect(resolveCompoundSearchCommit('semaglutide', compounds)).toBe('Semaglutide');
  });

  it('keeps custom submitted text when no recognized compound exists', () => {
    expect(resolveCompoundSearchCommit('custom peptide', compounds)).toBe('custom peptide');
  });

  it('does not turn prepended input artifacts into recognized searches', () => {
    expect(getCompoundSearchSuggestions('semaBPC-157', compounds)).toEqual([]);
    expect(resolveCompoundSearchCommit('semaBPC-157', compounds)).toBe('semaBPC-157');
  });
});
