import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import { ProviderObservationalSummary } from '@/components/protocols/ProviderObservationalSummary';
import type { Protocol } from '@/lib/types';
it('renders and copies unavailable severity without reasoning or directional labels', async () => {
  const writeText = vi.fn().mockResolvedValue(undefined);
  Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
  const protocol: Protocol = {
    id: 'p', personId: 'profile', name: 'Snapshot', version: 1, parentProtocolId: null,
    originProtocolId: null, evolvedFromRunId: null, isDraft: false, evolutionContext: '',
    isCurrentVersion: true, priorVersions: [], createdAtUtc: '2026-09-17T00:00:00Z',
    updatedAtUtc: '2026-09-17T00:00:00Z', items: [],
    stackScore: { score: 0, breakdown: { synergy: 0, redundancy: 0, conflicts: 0, evidence: 0 }, chips: [] },
    simulation: { timeline: [], insights: [] }, activeRun: null, versionDiff: null, actualComparison: null,
    interactionIntelligence: { pairs: [{ compoundA: 'Alpha', compoundB: 'Beta', severity: null }] },
  };
  render(<ProviderObservationalSummary protocol={protocol} />);
  expect(screen.getByText('Severity unavailable')).toBeInTheDocument();
  expect(screen.getByText('Alpha + Beta')).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Copy summary' }));
  await waitFor(() => expect(writeText).toHaveBeenCalledTimes(1));
  expect(writeText.mock.calls[0][0]).toContain('Severity unavailable: Alpha + Beta');
  expect(writeText.mock.calls[0][0]).not.toMatch(/undefined|Shared pathways|% confidence|Interfering/);
});
