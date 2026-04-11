import {
    getRecommendationSectionCopy,
    getRecommendationsForKnowledgeEntry,
    getRecommendationsForOverlapFlags,
    getRecommendationsForTags,
} from '@/lib/recommendations';
import { describe, expect, it } from 'vitest';

describe('recommendations helpers', () => {
  it('returns recovery-context examples for repair-oriented compounds', () => {
    const recommendations = getRecommendationsForKnowledgeEntry({
      canonicalName: 'BPC-157',
      aliases: [],
      classification: 'Peptide',
      regulatoryStatus: 'Research use',
      mechanismSummary: '',
      evidenceTier: 'Moderate',
      sourceReferences: [],
      notes: '',
      pathways: ['tissue-repair'],
      benefits: ['Recovery support'],
      pairsWellWith: [],
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
    });

    expect(recommendations.map((item) => item.displayName)).toContain('Collagen peptides');
    expect(recommendations.map((item) => item.displayName)).toContain('Creatine');
  });

  it('returns mitochondrial-context examples for energy-oriented compounds', () => {
    const recommendations = getRecommendationsForKnowledgeEntry({
      canonicalName: 'NAD+',
      aliases: [],
      classification: 'Coenzyme',
      regulatoryStatus: 'Supplement',
      mechanismSummary: '',
      evidenceTier: 'Moderate',
      sourceReferences: [],
      notes: '',
      pathways: ['cellular-energy', 'mitochondrial-function'],
      benefits: ['Energy support'],
      pairsWellWith: ['MOTS-C'],
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
    });

    expect(recommendations.map((item) => item.displayName)).toContain('CoQ10');
    expect(recommendations.map((item) => item.displayName)).toContain('Creatine');
  });

  it('returns nothing when there is no clear contextual reason', () => {
    const recommendations = getRecommendationsForKnowledgeEntry({
      canonicalName: 'Unknown compound',
      aliases: [],
      classification: 'Other',
      regulatoryStatus: '',
      mechanismSummary: '',
      evidenceTier: '',
      sourceReferences: [],
      notes: '',
      pathways: ['unclear-pathway'],
      benefits: [],
      pairsWellWith: [],
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
    });

    expect(recommendations).toEqual([]);
  });

  it('returns contextual examples for overlap flags when the pathway implies recovery support', () => {
    const recommendations = getRecommendationsForOverlapFlags([
      {
        compoundNames: ['BPC-157', 'TB-500'],
        overlapType: 'Potential redundancy',
        pathwayTag: 'tissue-repair',
        description: 'Educational reference only.',
        evidenceConfidence: 'Medium',
      },
    ]);

    expect(recommendations.map((item) => item.displayName)).toContain('Collagen peptides');
  });

  it('returns surface-specific copy for knowledge-search energy contexts', () => {
    const copy = getRecommendationSectionCopy('knowledge-search', ['energy', 'mitochondrial']);

    expect(copy.title).toBe('Common additions');
    expect(copy.description).toContain('mitochondrial-support');
  });

  it('diversifies example sources across a recommendation set', () => {
    const recommendations = getRecommendationsForTags(['recovery', 'energy'], 3, 'compound-detail');

    const sourceNames = recommendations.map((item) => item.sourceName);

    expect(new Set(sourceNames).size).toBe(sourceNames.length);
    expect(sourceNames).toEqual(expect.arrayContaining(['Amazon', 'Vitacost', 'iHerb']));
  });

  it('rotates the primary source by surface for the same recommendation context', () => {
    const compoundDetail = getRecommendationsForKnowledgeEntry(
      {
        canonicalName: 'NAD+',
        aliases: [],
        classification: 'Coenzyme',
        regulatoryStatus: 'Supplement',
        mechanismSummary: '',
        evidenceTier: 'Moderate',
        sourceReferences: [],
        notes: '',
        pathways: ['cellular-energy', 'mitochondrial-function'],
        benefits: ['Energy support'],
        pairsWellWith: ['MOTS-C'],
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
      },
      3,
      'compound-detail'
    );
    const knowledgeSearch = getRecommendationsForKnowledgeEntry(
      {
        canonicalName: 'NAD+',
        aliases: [],
        classification: 'Coenzyme',
        regulatoryStatus: 'Supplement',
        mechanismSummary: '',
        evidenceTier: 'Moderate',
        sourceReferences: [],
        notes: '',
        pathways: ['cellular-energy', 'mitochondrial-function'],
        benefits: ['Energy support'],
        pairsWellWith: ['MOTS-C'],
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
      },
      3,
      'knowledge-search'
    );

    expect(compoundDetail[0].displayName).toBe('CoQ10');
    expect(knowledgeSearch[0].displayName).toBe('CoQ10');
    expect(compoundDetail[0].sourceName).toBe('Vitacost');
    expect(knowledgeSearch[0].sourceName).toBe('Amazon');
  });
});