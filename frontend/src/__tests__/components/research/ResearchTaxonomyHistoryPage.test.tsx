import ResearchTaxonomyHistoryPage from '@/app/admin/research/taxonomy/history/page';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';

const searchParamsMock = vi.hoisted(() => ({
  get: vi.fn((key: string) => key === 'entry' ? 'audit-apply' : null),
}));

vi.mock('next/navigation', () => ({
  useSearchParams: () => searchParamsMock,
}));

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <div>{title}</div>,
}));

vi.mock('@/components/ui/GlassCard', () => ({
  GlassCard: ({ children, className }: { children: React.ReactNode; className?: string }) => <div className={className}>{children}</div>,
}));

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement>) => <a href={href} {...props}>{children}</a>,
}));

describe('ResearchTaxonomyHistoryPage', () => {
  beforeEach(() => {
    searchParamsMock.get.mockImplementation((key: string) => key === 'entry' ? 'audit-apply' : null);
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({
      schemaVersion: '1.0.0',
      updatedAtUtc: '2026-05-10T12:00:00Z',
      entries: [
        {
          entryId: 'audit-save',
          action: 'save-taxonomy',
          createdAtUtc: '2026-05-10T11:00:00Z',
          taxonomyVersion: '1.0.0',
          summary: 'Saved taxonomy with 3 categories.',
          beforeTaxonomy: {
            taxonomyVersion: '1.0.0',
            updatedAtUtc: '2026-05-10T10:00:00Z',
            categories: [{ name: 'Nootropics', aliases: ['nootropic'] }],
          },
          afterTaxonomy: {
            taxonomyVersion: '1.0.0',
            updatedAtUtc: '2026-05-10T11:00:00Z',
            categories: [
              { name: 'Nootropics', aliases: ['nootropic', 'cognitive support'] },
              { name: 'Longevity', aliases: [] },
            ],
          },
        },
        {
          entryId: 'audit-apply',
          action: 'apply-migration-fixup',
          createdAtUtc: '2026-05-10T12:00:00Z',
          taxonomyVersion: '1.0.0',
          summary: 'Applied taxonomy migration fix-up to 2 files.',
          applyReceipt: { appliedAtUtc: '2026-05-10T12:00:00Z', taxonomyVersion: '1.0.0', counts: { totalFilesUpdated: 2, requestFilesUpdated: 1, taskArtifactsUpdated: 1, categoriesRewritten: 2, requestCategoriesRewritten: 1, taskItemCategoriesRewritten: 1, resolvedTaskItemCategoriesRewritten: 0 }, updatedFiles: [] },
          beforeMigrationReport: { generatedAtUtc: '2026-05-10T11:00:00Z', taxonomyVersion: '1.0.0', counts: { requestFilesScanned: 1, requestFindings: 1, taskArtifactsScanned: 1, taskItemFindings: 1, resolvedTaskItemFindings: 0, totalFindings: 2 }, deprecatedCategories: [], findings: [] },
          afterMigrationReport: { generatedAtUtc: '2026-05-10T12:00:00Z', taxonomyVersion: '1.0.0', counts: { requestFilesScanned: 1, requestFindings: 0, taskArtifactsScanned: 1, taskItemFindings: 0, resolvedTaskItemFindings: 0, totalFindings: 0 }, deprecatedCategories: [], findings: [] },
        },
      ],
    }), { status: 200 })));
  });

  afterEach(() => vi.unstubAllGlobals());

  it('renders the focused governance timeline with filters and diffs', async () => {
    render(<ResearchTaxonomyHistoryPage />);

    expect(await screen.findByText('Taxonomy Timeline')).toBeInTheDocument();
    expect(screen.getByText(/Focused on Applied taxonomy migration fix-up to 2 files/i)).toBeInTheDocument();
    expect(screen.getByText(/Migration impact Δ total -2 · requests -1 · queued -1 · resolved 0/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Export focused entry/i })).toHaveAttribute('href', expect.stringContaining('data:application/json'));
    await waitFor(() => expect(screen.queryByText('Saved taxonomy with 3 categories.')).not.toBeInTheDocument());
  });

  it('searches, filters, and exports the event stream', async () => {
    searchParamsMock.get.mockReturnValue(null);
    render(<ResearchTaxonomyHistoryPage />);

    await screen.findByText('Taxonomy Timeline');
    fireEvent.change(screen.getByPlaceholderText('Search taxonomy history'), { target: { value: 'Longevity' } });

    expect(screen.getByText('Added: Longevity')).toBeInTheDocument();
    expect(screen.queryByText('Applied taxonomy migration fix-up to 2 files.')).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Export filtered stream/i })).toHaveAttribute('href', expect.stringContaining('data:application/json'));

    fireEvent.click(screen.getByRole('button', { name: /Clear search/i }));
    fireEvent.click(screen.getByRole('button', { name: 'All Events' }));

    expect(screen.getByText('Added: Longevity')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Taxonomy Saves' }));

    expect(screen.getByText('Saved taxonomy with 3 categories.')).toBeInTheDocument();
    expect(screen.queryByText('Applied taxonomy migration fix-up to 2 files.')).not.toBeInTheDocument();
  });
});