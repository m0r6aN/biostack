import { beforeEach, describe, expect, it } from 'vitest';
import {
  ANONYMOUS_TOOL_PAYLOAD_KEY,
  deleteAnonymousToolArtifact,
  readAnonymousToolPayload,
  saveAnonymousToolArtifact,
} from '@/lib/anonymousTools';

describe('anonymous tool artifacts', () => {
  beforeEach(() => {
    window.localStorage.removeItem(ANONYMOUS_TOOL_PAYLOAD_KEY);
  });

  it('saves, re-saves without duplicating identical content, and deletes artifacts', () => {
    const first = saveAnonymousToolArtifact({
      calculatorType: 'dose',
      substances: ['BPC-157'],
      inputs: { powderAmount: 5, desiredDose: 250 },
      outputs: { primaryAnswer: 'Draw to 10 units on a 1 mL insulin syringe' },
      reconstitutionInstructions: ['Clean vial tops with alcohol.'],
      storageInstructions: ['Store reconstituted solution in the refrigerator.'],
      compatibilityFindings: [],
    });

    expect(first.savedCalculations).toHaveLength(1);
    const artifactId = first.savedCalculations[0].id;

    const second = saveAnonymousToolArtifact({
      calculatorType: 'dose',
      substances: ['BPC-157'],
      inputs: { powderAmount: 5, desiredDose: 250 },
      outputs: { primaryAnswer: 'Draw to 10 units on a 1 mL insulin syringe' },
      reconstitutionInstructions: ['Clean vial tops with alcohol.'],
      storageInstructions: ['Store reconstituted solution in the refrigerator.'],
      compatibilityFindings: [],
    });

    expect(second.savedCalculations).toHaveLength(1);
    expect(second.savedCalculations[0].id).toBe(artifactId);

    deleteAnonymousToolArtifact(artifactId);
    expect(readAnonymousToolPayload()?.savedCalculations).toHaveLength(0);
  });
});
