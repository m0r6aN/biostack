'use client';

import { ToolsDecisionSurface } from '@/components/tools/ToolsDecisionSurface';

type CalculatorKind = 'reconstitution' | 'volume' | 'conversion';

interface PublicCalculatorExperienceProps {
  kind: CalculatorKind;
}

export function PublicCalculatorExperience({ kind }: PublicCalculatorExperienceProps) {
  const initialMode = kind === 'conversion' ? 'convert' : kind === 'reconstitution' ? 'mix' : 'dose';

  return <ToolsDecisionSurface initialMode={initialMode} compactIntro={kind !== 'reconstitution'} />;
}
