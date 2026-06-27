import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ProtocolIntelligencePanel } from '@/components/protocols/ProtocolIntelligencePanel';
import type { ProtocolIntelligenceResponse } from '@/lib/types';

describe('ProtocolIntelligencePanel', () => {
  it('renders Unknown state when no reviewed relationships exist', () => {
    render(<ProtocolIntelligencePanel intelligence={unknownResponse()} />);

    expect(screen.getAllByText('Unknown').length).toBeGreaterThan(0);
    expect(screen.getByText(/No reviewed Protocol Intelligence artifact exists/i)).toBeInTheDocument();
    expect(screen.queryByText(/confidence: high/i)).not.toBeInTheDocument();
  });

  it('renders high-risk warnings above reviewed relationship cards', () => {
    const { container } = render(<ProtocolIntelligencePanel intelligence={availableResponse()} />);

    expect(screen.getAllByText('High-risk warnings')[0]).toBeInTheDocument();
    expect(screen.getByText(/reviewed relationship: substance affects pathway/i)).toBeInTheDocument();
    const text = container.textContent ?? '';
    expect(text.indexOf('High-risk warnings')).toBeLessThan(text.indexOf('reviewed relationship'));
  });

  it('shows sourceRefs and evidence tiers visibly', () => {
    render(<ProtocolIntelligencePanel intelligence={availableResponse()} />);

    expect(screen.getByText('Evidence tier: clinical_study')).toBeInTheDocument();
    expect(screen.getByText('Source refs: 2')).toBeInTheDocument();
    expect(screen.getAllByText('Review status: approved').length).toBeGreaterThan(0);
  });

  it('shows Observer upgrade hook for gated relationship details', () => {
    render(<ProtocolIntelligencePanel intelligence={observerGatedResponse()} />);

    expect(screen.getByText(/Unlock reviewed protocol relationships and source-quality context with Operator/i)).toBeInTheDocument();
  });

  it('shows Commander upgrade hook for side-effect ambiguity on Operator', () => {
    render(<ProtocolIntelligencePanel intelligence={operatorGatedResponse()} />);

    expect(screen.getByText(/Unlock side-effect ambiguity and longitudinal Protocol Intelligence with Commander/i)).toBeInTheDocument();
  });

  it('does not render forbidden optimization language', () => {
    const { container } = render(<ProtocolIntelligencePanel intelligence={availableResponse()} />);
    const text = container.textContent?.toLowerCase() ?? '';

    for (const forbidden of [
      'optimize your dose',
      'best dose',
      'you should start',
      'you should stop',
      'switch to',
      'taper',
      'pct',
      'source this',
    ]) {
      expect(text).not.toContain(forbidden);
    }
  });
});

function unknownResponse(): ProtocolIntelligenceResponse {
  return {
    status: 'Unknown',
    phaseMap: [],
    relationships: [],
    ambiguitySignals: [],
    sourceQualityWarnings: [],
    highRiskWarnings: [],
    unknowns: ['No reviewed Protocol Intelligence artifact exists for this protocol context.'],
    safetyNotes: ['Unknown means no reviewed artifact exists.'],
    upgradeHooks: [],
  };
}

function availableResponse(): ProtocolIntelligenceResponse {
  return {
    status: 'Available',
    phaseMap: [
      {
        phase: 'active',
        label: 'Active observation window',
        evidenceTier: 'observational',
        confidence: 'low',
        sourceRefsCount: 1,
        reviewStatus: 'approved',
        userFacingBoundary: 'Observation prompt only.',
      },
    ],
    relationships: [
      {
        relationshipType: 'substance_affects_pathway',
        subject: 'Semaglutide',
        object: 'GI symptom tracking',
        evidenceTier: 'clinical_study',
        confidence: 'moderate',
        sourceRefsCount: 2,
        reviewStatus: 'approved',
        userFacingExplanation: 'Reviewed relationship for observation context.',
        userFacingBoundary: 'Discuss concerning symptoms with a qualified professional.',
      },
    ],
    ambiguitySignals: [
      {
        symptomOrOutcome: 'Nausea reported',
        onsetWindow: 'recent check-ins',
        recentChanges: ['phase changed'],
        overlapDomains: ['gi_symptoms'],
        evidenceTier: 'observational',
        confidence: 'low',
        sourceRefsCount: 1,
        reviewStatus: 'approved',
        userFacingBoundary: 'Observation prompt only.',
      },
    ],
    sourceQualityWarnings: [
      {
        subject: 'Research chemical',
        sourceClass: 'gray_market',
        blockedOutputs: ['sourcing_guidance'],
        evidenceTier: 'source_dependent',
        confidence: 'low',
        sourceRefsCount: 1,
        reviewStatus: 'approved',
        userFacingBoundary: 'Source-quality warning only.',
      },
    ],
    highRiskWarnings: [
      {
        category: 'investigational_peptides',
        requiredWarnings: ['regulatory_status'],
        blockedOutputs: ['claims_investigational_peptides_safe_or_effective'],
        evidenceTier: 'warning_first',
        confidence: 'reviewed',
        sourceRefsCount: 1,
        reviewStatus: 'approved',
        userFacingBoundary: 'Warning-first context only.',
      },
    ],
    unknowns: [],
    safetyNotes: ['Educational and observational only.'],
    upgradeHooks: [],
  };
}

function observerGatedResponse(): ProtocolIntelligenceResponse {
  return {
    ...unknownResponse(),
    upgradeHooks: [
      {
        requiredTier: 'Operator',
        featureCode: 'reviewed_relationship_graph',
        message: 'Unlock reviewed protocol relationships and source-quality context with Operator.',
      },
    ],
  };
}

function operatorGatedResponse(): ProtocolIntelligenceResponse {
  return {
    ...unknownResponse(),
    status: 'Available',
    relationships: availableResponse().relationships,
    unknowns: [],
    upgradeHooks: [
      {
        requiredTier: 'Commander',
        featureCode: 'side_effect_ambiguity_detector',
        message: 'Unlock side-effect ambiguity and longitudinal Protocol Intelligence with Commander.',
      },
    ],
  };
}
