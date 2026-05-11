import ResearchTaxonomyPage from '@/app/admin/research/taxonomy/page';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';

const loaderMocks = vi.hoisted(() => ({
  fetchResearchCategoryTaxonomy: vi.fn().mockResolvedValue({
    taxonomyVersion: '1.0.0',
    updatedAtUtc: '2026-05-10T00:00:00Z',
    categories: [
      { name: 'Nootropics', aliases: ['nootropic', 'cognitive support'] },
      { name: 'Anti-Aging', aliases: ['anti-aging'], deprecated: true, replacedBy: 'Longevity' },
      { name: 'Longevity', aliases: [] },
    ],
  }),
}));

vi.mock('@/lib/apiBase', () => ({ getApiBaseUrl: () => 'http://localhost' }));

vi.mock('@/lib/research/loader', () => ({
  fetchResearchCategoryTaxonomy: () => loaderMocks.fetchResearchCategoryTaxonomy(),
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

describe('ResearchTaxonomyPage', () => {
  beforeEach(() => {
    loaderMocks.fetchResearchCategoryTaxonomy.mockClear();
    vi.stubGlobal('fetch', vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'dev' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        generatedAtUtc: '2026-05-10T12:00:00Z',
        taxonomyVersion: '1.0.0',
        counts: {
          requestFilesScanned: 1,
          requestFindings: 1,
          taskArtifactsScanned: 1,
          taskItemFindings: 1,
          resolvedTaskItemFindings: 0,
          totalFindings: 2,
        },
        deprecatedCategories: [
          { deprecatedCategory: 'Anti-Aging', replacementCategory: 'Longevity', findings: 2, requestFindings: 1, taskItemFindings: 1, resolvedTaskItemFindings: 0 },
        ],
        findings: [
          { sourceType: 'request', sourcePath: 'research/research-requests/request-1.json', compoundName: 'Epitalon', matchedCategory: 'Anti-Aging', deprecatedCategory: 'Anti-Aging', replacementCategory: 'Longevity', requestId: 'req-1' },
          { sourceType: 'task-item', sourcePath: 'research/pilot/research-task-queue.json', compoundName: 'Epitalon', matchedCategory: 'anti aging', deprecatedCategory: 'Anti-Aging', replacementCategory: 'Longevity', taskId: 'task-1' },
        ],
      }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        schemaVersion: '1.0.0',
        updatedAtUtc: '2026-05-10T12:00:00Z',
        entries: [
          {
            entryId: 'audit-1',
            action: 'save-taxonomy',
            createdAtUtc: '2026-05-10T12:00:00Z',
            taxonomyVersion: '1.0.0',
            summary: 'Saved taxonomy with 3 categories.',
            beforeTaxonomy: {
              taxonomyVersion: '1.0.0',
              updatedAtUtc: '2026-05-10T11:00:00Z',
              categories: [
                { name: 'Nootropics', aliases: ['nootropic'] },
                { name: 'Longevity', aliases: [] },
              ],
            },
            afterTaxonomy: {
              taxonomyVersion: '1.0.0',
              updatedAtUtc: '2026-05-10T12:00:00Z',
              categories: [
                { name: 'Nootropics', aliases: ['nootropic', 'cognitive support'] },
                { name: 'Anti-Aging', aliases: ['anti-aging'], deprecated: true, replacedBy: 'Longevity' },
                { name: 'Longevity', aliases: [] },
              ],
            },
          },
        ],
      }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        taxonomyVersion: '1.0.0',
        updatedAtUtc: '2026-05-11T00:00:00Z',
        categories: [
          { name: 'Nootropics', aliases: ['nootropic', 'cognitive support'] },
          { name: 'Anti-Aging', aliases: ['anti-aging'], deprecated: true, replacedBy: 'Longevity' },
          { name: 'Longevity', aliases: [] },
          { name: 'Recovery', aliases: ['repair'] },
        ],
      }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        generatedAtUtc: '2026-05-11T12:00:00Z',
        taxonomyVersion: '1.0.0',
        counts: {
          requestFilesScanned: 1,
          requestFindings: 0,
          taskArtifactsScanned: 1,
          taskItemFindings: 0,
          resolvedTaskItemFindings: 0,
          totalFindings: 0,
        },
        deprecatedCategories: [],
        findings: [],
      }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        schemaVersion: '1.0.0',
        updatedAtUtc: '2026-05-11T12:00:00Z',
        entries: [
          { entryId: 'audit-2', action: 'save-taxonomy', createdAtUtc: '2026-05-11T12:00:00Z', taxonomyVersion: '1.0.0', summary: 'Saved taxonomy with 4 categories.' },
        ],
      }), { status: 200 })));
  });

  afterEach(() => vi.unstubAllGlobals());

  it('renders existing taxonomy categories', async () => {
    render(<ResearchTaxonomyPage />);

    expect(await screen.findByText('Category Taxonomy')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Nootropics')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Anti-Aging')).toBeInTheDocument();
    expect(screen.getAllByDisplayValue('Longevity').length).toBeGreaterThan(0);
    expect(screen.getByText('Canonical research categories')).toBeInTheDocument();
    expect(screen.getAllByText('Deprecated').length).toBeGreaterThan(0);
    expect(screen.getByText('Migration impact preview')).toBeInTheDocument();
    expect(screen.getByText('Anti-Aging → Longevity')).toBeInTheDocument();
    expect(screen.getByText(/research\/research-requests\/request-1\.json/i)).toBeInTheDocument();
    expect(screen.getByText('Governance audit history')).toBeInTheDocument();
    expect(screen.getByText('Saved taxonomy with 3 categories.')).toBeInTheDocument();
    expect(screen.getByText('Added: Anti-Aging')).toBeInTheDocument();
    expect(screen.getByText('Aliases changed: Nootropics')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Open full timeline/i })).toHaveAttribute('href', '/admin/research/taxonomy/history');
    expect(screen.getByRole('link', { name: /Open in timeline/i })).toHaveAttribute('href', '/admin/research/taxonomy/history?entry=audit-1');
  });

  it('adds a category and saves the taxonomy with deprecations', async () => {
    const fetchMock = vi.mocked(fetch);

    render(<ResearchTaxonomyPage />);

    await screen.findByDisplayValue('Nootropics');
    fireEvent.click(screen.getAllByRole('checkbox')[0]);
    fireEvent.change(screen.getAllByPlaceholderText('Replacement canonical category')[0], { target: { value: 'Longevity' } });
    fireEvent.click(screen.getByRole('button', { name: /Add category/i }));

    const nameInputs = screen.getAllByPlaceholderText('Canonical category name');
    const aliasInputs = screen.getAllByPlaceholderText('Comma-separated aliases');
    const replacementInputs = screen.getAllByPlaceholderText('Replacement canonical category');
    fireEvent.change(nameInputs[nameInputs.length - 1], { target: { value: 'Recovery' } });
    fireEvent.change(aliasInputs[aliasInputs.length - 1], { target: { value: 'repair' } });
    expect(replacementInputs[replacementInputs.length - 1]).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: /Save taxonomy/i }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/research/category-taxonomy', expect.objectContaining({ method: 'PUT' })));
    expect(fetchMock.mock.calls.filter(([url]) => url === '/api/research/category-taxonomy/migration-report').length).toBe(2);
    expect(fetchMock.mock.calls.filter(([url]) => url === '/api/research/category-taxonomy/history').length).toBe(2);
    expect(fetchMock).toHaveBeenCalledWith('/api/research/category-taxonomy', expect.objectContaining({
      body: JSON.stringify({
        categories: [
          { name: 'Nootropics', aliases: 'nootropic, cognitive support', deprecated: true, replacedBy: 'Longevity' },
          { name: 'Anti-Aging', aliases: 'anti-aging', deprecated: true, replacedBy: 'Longevity' },
          { name: 'Longevity', aliases: '', deprecated: false, replacedBy: '' },
          { name: 'Recovery', aliases: 'repair', deprecated: false, replacedBy: '' },
        ],
      }),
    }));
    expect(await screen.findByText(/Saved taxonomy 1.0.0 with 4 categories/i)).toBeInTheDocument();
    expect(await screen.findByText(/No current research files reference deprecated taxonomy categories/i)).toBeInTheDocument();
    expect(await screen.findByText('Saved taxonomy with 4 categories.')).toBeInTheDocument();
  });

  it('applies migration fix-up and renders the receipt', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'dev' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        generatedAtUtc: '2026-05-10T12:00:00Z',
        taxonomyVersion: '1.0.0',
        counts: {
          requestFilesScanned: 1,
          requestFindings: 1,
          taskArtifactsScanned: 1,
          taskItemFindings: 1,
          resolvedTaskItemFindings: 0,
          totalFindings: 2,
        },
        deprecatedCategories: [
          { deprecatedCategory: 'Anti-Aging', replacementCategory: 'Longevity', findings: 2, requestFindings: 1, taskItemFindings: 1, resolvedTaskItemFindings: 0 },
        ],
        findings: [
          { sourceType: 'request', sourcePath: 'research/research-requests/request-1.json', compoundName: 'Epitalon', matchedCategory: 'Anti-Aging', deprecatedCategory: 'Anti-Aging', replacementCategory: 'Longevity', requestId: 'req-1' },
          { sourceType: 'task-item', sourcePath: 'research/pilot/research-task-queue.json', compoundName: 'Epitalon', matchedCategory: 'anti aging', deprecatedCategory: 'Anti-Aging', replacementCategory: 'Longevity', taskId: 'task-1' },
        ],
      }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        schemaVersion: '1.0.0',
        updatedAtUtc: '2026-05-10T12:00:00Z',
        entries: [],
      }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        appliedAtUtc: '2026-05-10T12:30:00Z',
        taxonomyVersion: '1.0.0',
        counts: {
          totalFilesUpdated: 2,
          requestFilesUpdated: 1,
          taskArtifactsUpdated: 1,
          categoriesRewritten: 2,
          requestCategoriesRewritten: 1,
          taskItemCategoriesRewritten: 1,
          resolvedTaskItemCategoriesRewritten: 0,
        },
        updatedFiles: [
          { sourceType: 'request-file', sourcePath: 'research/research-requests/request-1.json', categoriesRewritten: 1, compounds: ['Epitalon'], rewrites: [] },
          { sourceType: 'task-artifact', sourcePath: 'research/pilot/research-task-queue.json', categoriesRewritten: 1, compounds: ['Epitalon'], rewrites: [] },
        ],
      }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        generatedAtUtc: '2026-05-10T12:31:00Z',
        taxonomyVersion: '1.0.0',
        counts: {
          requestFilesScanned: 1,
          requestFindings: 0,
          taskArtifactsScanned: 1,
          taskItemFindings: 0,
          resolvedTaskItemFindings: 0,
          totalFindings: 0,
        },
        deprecatedCategories: [],
        findings: [],
      }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        schemaVersion: '1.0.0',
        updatedAtUtc: '2026-05-10T12:31:00Z',
        entries: [
          {
            entryId: 'audit-3',
            action: 'apply-migration-fixup',
            createdAtUtc: '2026-05-10T12:31:00Z',
            taxonomyVersion: '1.0.0',
            summary: 'Applied taxonomy migration fix-up to 2 files.',
            applyReceipt: { counts: { totalFilesUpdated: 2, categoriesRewritten: 2 } },
            beforeMigrationReport: {
              generatedAtUtc: '2026-05-10T12:00:00Z',
              taxonomyVersion: '1.0.0',
              counts: { requestFilesScanned: 1, requestFindings: 1, taskArtifactsScanned: 1, taskItemFindings: 1, resolvedTaskItemFindings: 0, totalFindings: 2 },
              deprecatedCategories: [],
              findings: [],
            },
            afterMigrationReport: {
              generatedAtUtc: '2026-05-10T12:31:00Z',
              taxonomyVersion: '1.0.0',
              counts: { requestFilesScanned: 1, requestFindings: 0, taskArtifactsScanned: 1, taskItemFindings: 0, resolvedTaskItemFindings: 0, totalFindings: 0 },
              deprecatedCategories: [],
              findings: [],
            },
          },
        ],
      }), { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);

    render(<ResearchTaxonomyPage />);

    await screen.findByText('Migration impact preview');
    fireEvent.click(screen.getByRole('button', { name: /Apply migration fix-up/i }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/research/category-taxonomy/migration-report', { method: 'POST' }));
    expect(await screen.findByText(/Applied taxonomy migration fix-up to 2 files and rewrote 2 category references/i)).toBeInTheDocument();
    expect(await screen.findByText('Last migration receipt')).toBeInTheDocument();
    expect(await screen.findByText('Applied taxonomy migration fix-up to 2 files.')).toBeInTheDocument();
    expect(await screen.findByText(/Migration impact Δ total -2 · requests -1 · queued -1 · resolved 0/i)).toBeInTheDocument();
    expect(screen.getByText('research/research-requests/request-1.json')).toBeInTheDocument();
    expect(screen.getByText('research/pilot/research-task-queue.json')).toBeInTheDocument();
  });
});