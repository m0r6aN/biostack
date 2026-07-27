import { summarizeBlend } from '@/components/tools/ToolsDecisionSurface';
import type { KnowledgeEntry } from '@/lib/types';
import { describe, expect, it } from 'vitest';

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
    const result = summarizeBlend('checked', [], 'Compound A', 'Compound B', [knowledgeEntry]);

    expect(result.status).toBe('unknown');
    expect(result.reasons).toContain('No overlap findings were returned; compatibility remains unknown.');
    expect(result.reasons).toContain('Source data reports this pairing, but does not establish compatibility or safety.');
    expect(result.reasons.join(' ')).not.toMatch(/compatible|no known conflicts/i);
  });
});
