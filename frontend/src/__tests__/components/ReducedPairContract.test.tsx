import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { InteractionIntelligenceCard } from '@/components/protocols/InteractionIntelligenceCard';
import { OverlapResults } from '@/components/knowledge/OverlapResults';
import { StackGraph } from '@/components/protocol/StackGraph';
import { summarizeCompoundOverlap } from '@/components/tools/ToolsDecisionSurface';
import { getRelationshipCandidatesFromOverlaps } from '@/lib/onboardingIntelligence';
import { getContextTagsForOverlapFlags } from '@/lib/recommendations';
import type { CompoundRecord } from '@/lib/types';

vi.mock('@/components/recommendations/ContextualRecommendations', () => ({ ContextualRecommendations: () => null }));
const pair = { compoundA: 'Alpha', compoundB: 'Beta', severity: null };
const flag = { compoundNames: ['Alpha', 'Beta'], severity: null };
const active = { id: 'alpha', name: 'Alpha', status: 'Active' } as CompoundRecord;

describe('explicit unavailable pair severity', () => {
  it('renders a reduced interaction without directional summary or inferred strength', () => {
    render(<InteractionIntelligenceCard intelligence={{ pairs: [pair] }} />);
    expect(screen.getByText('Alpha + Beta')).toBeInTheDocument();
    expect(screen.getByText('Severity unavailable')).toBeInTheDocument();
    expect(screen.queryByText('Synergies')).not.toBeInTheDocument();
  });
  it('renders reduced overlap without empty mechanism badges', () => {
    render(<OverlapResults inputCount={2} flags={[flag]} />);
    expect(screen.getByText('Alpha × Beta')).toBeInTheDocument();
    expect(screen.getByText('Severity unavailable')).toBeInTheDocument();
    expect(screen.queryByText(/^Pathway:/)).not.toBeInTheDocument();
    expect(screen.queryByText(/^Confidence:/)).not.toBeInTheDocument();
  });
  it('does not interpret an entitled avoid sentence as measured severity', () => {
    const full = { compoundNames: ['Alpha', 'Beta'], overlapType: 'PotentialInteraction', pathwayTag: '', description: 'Avoid-with guidance directly links these compounds.', evidenceConfidence: 'Confidence 0.72' };
    expect(summarizeCompoundOverlap('checked', [full], 'Alpha', 'Beta', []).status).toBe('unknown');
    expect(summarizeCompoundOverlap('checked', [flag], 'Alpha', 'Beta', []).status).toBe('unknown');
    render(<OverlapResults inputCount={2} flags={[full]} />);
    expect(screen.getByText(full.description)).toBeInTheDocument();
    expect(screen.queryByText(/^Pathway:/)).not.toBeInTheDocument();
  });
  it('keeps onboarding detail neutral and recommendation inputs defined', () => {
    expect(getRelationshipCandidatesFromOverlaps([flag])[0].detail).toBe('Severity unavailable.');
    expect(() => getContextTagsForOverlapFlags([flag])).not.toThrow();
  });
  it('distinguishes reduced graph data from an actually empty stack', () => {
    const { rerender } = render(<StackGraph intelligence={{ pairs: [pair] }} compounds={[active]} />);
    expect(screen.getByText('Detailed graph unavailable')).toBeInTheDocument();
    expect(screen.queryByText('No active compounds')).not.toBeInTheDocument();
    rerender(<StackGraph intelligence={{ pairs: [] }} compounds={[]} />);
    expect(screen.getByText('No active compounds')).toBeInTheDocument();
  });
});
