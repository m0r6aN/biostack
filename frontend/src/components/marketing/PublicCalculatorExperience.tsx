'use client';

import { ToolsDecisionSurface } from '@/components/tools/ToolsDecisionSurface';

type CalculatorKind = 'reconstitution' | 'volume' | 'conversion';

const CALCULATOR_HEADINGS: Record<CalculatorKind, string> = {
  reconstitution: 'Reconstitution calculator',
  volume: 'Volume calculator',
  conversion: 'Unit converter',
};

interface PublicCalculatorExperienceProps {
  kind: CalculatorKind;
}

export function PublicCalculatorExperience({ kind }: PublicCalculatorExperienceProps) {
  const initialMode = kind === 'conversion' ? 'convert' : kind === 'reconstitution' ? 'mix' : 'dose';

  return (
    <ToolsDecisionSurface
      initialMode={initialMode}
      compactIntro={kind !== 'reconstitution'}
      heading={CALCULATOR_HEADINGS[kind]}
    />
  );
}
