import { SaveToBioStackPanel } from '@/components/research/SaveToBioStackPanel';
import type { PromotionPreview, StagedTranscriptCandidateReview } from '@/lib/types';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

// ── Mocks ─────────────────────────────────────────────────────────────────

const fetchMock = vi.fn();

vi.mock('@/lib/apiBase', () => ({ getApiBaseUrl: () => 'http://test' }));

vi.mock('@/components/ui/GlassCard', () => ({
  GlassCard: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}));

vi.mock('@/components/ui/Button', () => ({
  Button: ({ children, ...props }: React.ButtonHTMLAttributes<HTMLButtonElement>) => (
    <button {...props}>{children}</button>
  ),
}));

// ── Test data ─────────────────────────────────────────────────────────────

const ARTIFACT_ID = 'artifact-001';

const PREVIEW_URL =
  `http://test/api/v1/admin/staged-transcript-candidate-reviews/${ARTIFACT_ID}/promotion-preview`;
const EXECUTE_URL =
  `http://test/api/v1/admin/staged-transcript-candidate-reviews/${ARTIFACT_ID}/execute-promotion`;

function makeReview(
  overrides: Partial<StagedTranscriptCandidateReview> = {}
): StagedTranscriptCandidateReview {
  return {
    artifactId: ARTIFACT_ID,
    canonicality: 'canonical',
    reviewState: 'review_approved_for_promotion',
    sourceType: 'youtube',
    sourceUrl: 'https://youtube.com/watch?v=test',
    provider: 'test-provider',
    isDeterministicFixture: false,
    segmentCount: 5,
    segmentSnapshotSignature: 'sig-abc',
    sourceMetadata: {},
    createdAtUtc: '2024-01-01T00:00:00Z',
    updatedAtUtc: '2024-01-01T00:00:00Z',
    targetCanonicalName: 'BPC-157',
    promotedKnowledgeEntryId: null,
    promotedAtUtc: null,
    ...overrides,
  };
}

function makePreview(overrides: Partial<PromotionPreview> = {}): PromotionPreview {
  return {
    artifactId: ARTIFACT_ID,
    canPromote: true,
    reviewState: 'review_approved_for_promotion',
    targetAssigned: true,
    targetCanonicalName: 'BPC-157',
    resolvedTargetKnowledgeEntryId: 'ke-001',
    alreadyPromoted: false,
    promotedKnowledgeEntryId: null,
    evidenceGate: {
      passed: true,
      tier: 'moderate',
      citationCount: 3,
      mechanismSummaryPresent: true,
      failureReasons: [],
    },
    blockingReasons: [],
    wouldWrite: false,
    ...overrides,
  };
}

function okJson(body: unknown) {
  return { ok: true, json: async () => body };
}

function errResponse(status: number, text = '') {
  return { ok: false, status, text: async () => text };
}

// ── Tests ─────────────────────────────────────────────────────────────────

describe('SaveToBioStackPanel', () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
  });

  // 1. Renders save affordance for an eligible review
  it('renders Save to BioStack button for an approved, unpromoted review', () => {
    render(<SaveToBioStackPanel artifactId={ARTIFACT_ID} review={makeReview()} />);

    expect(screen.getByRole('button', { name: 'Save to BioStack' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /execute save/i })).not.toBeInTheDocument();
  });

  // 2. Calls promotion-preview endpoint before any execute call
  it('calls promotion-preview before executing promotion', async () => {
    fetchMock.mockResolvedValueOnce(okJson(makePreview()));

    render(<SaveToBioStackPanel artifactId={ARTIFACT_ID} review={makeReview()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Save to BioStack' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        PREVIEW_URL,
        expect.objectContaining({ method: 'POST' })
      );
    });

    const executeCalls = fetchMock.mock.calls.filter(
      call => typeof call[0] === 'string' && (call[0] as string).includes('execute-promotion')
    );
    expect(executeCalls).toHaveLength(0);
  });

  // 3. Displays canPromote=true preview with target and evidence gate details
  it('shows Ready to save state with target name and Passed evidence gate', async () => {
    fetchMock.mockResolvedValueOnce(okJson(makePreview()));

    render(<SaveToBioStackPanel artifactId={ARTIFACT_ID} review={makeReview()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Save to BioStack' }));

    await waitFor(() => {
      expect(screen.getByText('Ready to save')).toBeInTheDocument();
    });

    expect(screen.getByText('BPC-157')).toBeInTheDocument();
    expect(screen.getByText('Passed')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Execute Save to BioStack' })).toBeInTheDocument();
  });

  // 4. Blocks execution when canPromote=false
  it('shows Cannot save yet and hides execute button when canPromote is false', async () => {
    fetchMock.mockResolvedValueOnce(
      okJson(makePreview({ canPromote: false, blockingReasons: ['Target not resolved'] }))
    );

    render(<SaveToBioStackPanel artifactId={ARTIFACT_ID} review={makeReview()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Save to BioStack' }));

    await waitFor(() => {
      expect(screen.getByText('Cannot save yet')).toBeInTheDocument();
    });

    expect(screen.queryByRole('button', { name: /execute save/i })).not.toBeInTheDocument();
  });

  // 5. Displays evidence gate failure reasons inline
  it('displays evidence gate failure reasons when gate is failed', async () => {
    fetchMock.mockResolvedValueOnce(
      okJson(
        makePreview({
          canPromote: false,
          evidenceGate: {
            passed: false,
            tier: null,
            citationCount: 0,
            mechanismSummaryPresent: false,
            failureReasons: ['Not enough citations', 'Mechanism summary absent'],
          },
        })
      )
    );

    render(<SaveToBioStackPanel artifactId={ARTIFACT_ID} review={makeReview()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Save to BioStack' }));

    await waitFor(() => {
      expect(screen.getByText('Not enough citations')).toBeInTheDocument();
    });

    expect(screen.getByText('Mechanism summary absent')).toBeInTheDocument();
    expect(screen.getByText('Failed')).toBeInTheDocument();
  });

  // 6. Displays "Not assigned" when target is missing
  it('displays Not assigned when targetAssigned is false', async () => {
    fetchMock.mockResolvedValueOnce(
      okJson(
        makePreview({
          canPromote: false,
          targetAssigned: false,
          targetCanonicalName: null,
          resolvedTargetKnowledgeEntryId: null,
        })
      )
    );

    render(<SaveToBioStackPanel artifactId={ARTIFACT_ID} review={makeReview()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Save to BioStack' }));

    await waitFor(() => {
      expect(screen.getByText('Not assigned')).toBeInTheDocument();
    });
  });

  // 7. Shows pending review blocking message when reviewState is pending
  it('shows pending review message and no save button for pending_review state', () => {
    render(
      <SaveToBioStackPanel
        artifactId={ARTIFACT_ID}
        review={makeReview({ reviewState: 'pending_review' })}
      />
    );

    expect(
      screen.getByText('Pending review — not yet eligible to save.')
    ).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /save to biostack/i })).not.toBeInTheDocument();
  });

  // 8. Shows already-promoted state on initial render — no duplicate save confusion
  it('shows Already saved to BioStack without a save button when already promoted', () => {
    render(
      <SaveToBioStackPanel
        artifactId={ARTIFACT_ID}
        review={makeReview({
          promotedKnowledgeEntryId: 'ke-123',
          promotedAtUtc: '2024-01-15T10:00:00Z',
        })}
      />
    );

    expect(screen.getByText('Already saved to BioStack')).toBeInTheDocument();
    expect(screen.getByText('ke-123')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /save/i })).not.toBeInTheDocument();
  });

  // 9. Executes promotion only after preview passes — correct call order
  it('only calls execute-promotion after preview passes and user confirms', async () => {
    fetchMock
      .mockResolvedValueOnce(okJson(makePreview({ canPromote: true })))
      .mockResolvedValueOnce(okJson(makeReview({ promotedKnowledgeEntryId: 'ke-new' })));

    render(<SaveToBioStackPanel artifactId={ARTIFACT_ID} review={makeReview()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Save to BioStack' }));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Execute Save to BioStack' })).toBeInTheDocument();
    });

    // Only preview called at this point
    expect(fetchMock).toHaveBeenCalledTimes(1);

    fireEvent.click(screen.getByRole('button', { name: 'Execute Save to BioStack' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        EXECUTE_URL,
        expect.objectContaining({ method: 'POST' })
      );
    });

    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  // 10. Shows success state after promotion completes
  it('shows Saved to BioStack with knowledge entry ID after successful promotion', async () => {
    const successReview = makeReview({
      promotedKnowledgeEntryId: 'ke-new-456',
      promotedAtUtc: '2024-06-01T12:00:00Z',
    });

    fetchMock
      .mockResolvedValueOnce(okJson(makePreview()))
      .mockResolvedValueOnce(okJson(successReview));

    render(<SaveToBioStackPanel artifactId={ARTIFACT_ID} review={makeReview()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Save to BioStack' }));

    await waitFor(() =>
      screen.getByRole('button', { name: 'Execute Save to BioStack' })
    );

    fireEvent.click(screen.getByRole('button', { name: 'Execute Save to BioStack' }));

    await waitFor(() => {
      expect(screen.getByText('Saved to BioStack')).toBeInTheDocument();
    });

    expect(screen.getByText('ke-new-456')).toBeInTheDocument();
  });

  // 11. Handles API error (non-ok response) — shows message and Try again resets to idle
  it('shows error message on failed preview and Try again resets to idle', async () => {
    fetchMock.mockResolvedValueOnce(errResponse(500, 'Internal Server Error'));

    render(<SaveToBioStackPanel artifactId={ARTIFACT_ID} review={makeReview()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Save to BioStack' }));

    await waitFor(() => {
      expect(screen.getByText(/Preview failed: 500/)).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Try again' }));

    expect(screen.getByRole('button', { name: 'Save to BioStack' })).toBeInTheDocument();
  });

  // 12. Confirms execute-promotion is never called when preview is blocked
  it('never calls execute-promotion when preview returns canPromote=false', async () => {
    fetchMock.mockResolvedValueOnce(
      okJson(makePreview({ canPromote: false, blockingReasons: ['Evidence gate failed'] }))
    );

    render(<SaveToBioStackPanel artifactId={ARTIFACT_ID} review={makeReview()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Save to BioStack' }));

    await waitFor(() => {
      expect(screen.getByText('Cannot save yet')).toBeInTheDocument();
    });

    // No execute button rendered
    expect(screen.queryByRole('button', { name: /execute/i })).not.toBeInTheDocument();

    // fetch was called exactly once (preview only) — execute-promotion never reached
    const executeCalls = fetchMock.mock.calls.filter(
      call => typeof call[0] === 'string' && (call[0] as string).includes('execute-promotion')
    );
    expect(executeCalls).toHaveLength(0);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });
});
