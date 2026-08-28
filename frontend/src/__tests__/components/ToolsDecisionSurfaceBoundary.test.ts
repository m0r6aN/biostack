import { COMPOUND_OVERLAP_COPY, summarizeCompoundOverlap } from '@/components/tools/ToolsDecisionSurface';
import type { KnowledgeEntry } from '@/lib/types';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

const toolsDecisionSurfaceSource = readFileSync(
  join(process.cwd(), 'src/components/tools/ToolsDecisionSurface.tsx'),
  'utf8',
);

const knowledgeEntry: KnowledgeEntry = {
  canonicalName: 'Compound A',
  aliases: [],
  classification: 'Peptide',
  regulatoryStatus: 'Research',
  mechanismSummary: 'Observed mechanism.',
  evidenceTier: 'Low',
  sourceReferences: [],
  notes: '',
  pathways: [],
  benefits: [],
  pairsWellWith: ['Compound B'],
  avoidWith: [],
  compatibleBlends: [],
  recommendedDosage: '',
  frequency: '',
  preferredTimeOfDay: '',
  weeklyDosageSchedule: [],
  drugInteractions: [],
  optimizationProtein: '',
  optimizationCarbs: '',
  optimizationSupplements: '',
  optimizationSleep: '',
  optimizationExercise: '',
};

describe('ToolsDecisionSurface public compatibility boundary', () => {
  it('treats zero overlap findings as unknown even when source data reports a pairing', () => {
    const result = summarizeCompoundOverlap('checked', [], 'Compound A', 'Compound B', [knowledgeEntry]);

    expect(result.status).toBe('unknown');
    expect(result.reasons).toContain('No overlap findings were returned; compatibility remains unknown.');
    expect(result.reasons).toContain('Source data reports this pairing, but does not establish compatibility or safety.');
    expect(result.reasons.join(' ')).not.toMatch(/compatible|no known conflicts/i);
  });

  it('uses overlap language and states the same-vial boundary explicitly', () => {
    expect(COMPOUND_OVERLAP_COPY).toEqual({
      title: 'Review compound overlap',
      helper: 'Add another compound to review known overlap, redundancy, and interaction signals across your stack.',
      idle: 'Run a check to review available interaction findings.',
      boundary: 'This does not evaluate same-vial mixing, reconstitution compatibility, or overall clinical safety.',
    });
    expect(Object.values(COMPOUND_OVERLAP_COPY).join(' ')).not.toMatch(/\bblend\b/i);
  });

  it('does not restore the ambiguous calculator wording', () => {
    const retiredCopy = [
      'Check blend safety',
      'Add another compound and check overlap, compatibility, or caution flags.',
      'Run a check to see blend findings.',
    ];

    for (const retiredLine of retiredCopy) {
      expect(toolsDecisionSurfaceSource).not.toContain(retiredLine);
    }
  });
});
