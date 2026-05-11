import { GET, POST } from '@/app/api/research/category-taxonomy/migration-report/route';
import { mkdir, readFile, readdir, rename, unlink, writeFile } from 'node:fs/promises';

vi.mock('node:fs/promises', () => ({
  default: {},
  mkdir: vi.fn().mockResolvedValue(undefined),
  readFile: vi.fn(),
  readdir: vi.fn(),
  writeFile: vi.fn().mockResolvedValue(undefined),
  rename: vi.fn().mockResolvedValue(undefined),
  unlink: vi.fn().mockResolvedValue(undefined),
}));

describe('research category-taxonomy migration-report route', () => {
  const originalNodeEnv = process.env.NODE_ENV;
  const originalArtifactsPath = process.env.RESEARCH_ARTIFACTS_PATH;

  beforeEach(() => {
    process.env.NODE_ENV = 'test';
    process.env.RESEARCH_ARTIFACTS_PATH = 'research/pilot';
    vi.mocked(mkdir).mockClear();
    vi.mocked(readdir).mockReset();
    vi.mocked(readFile).mockReset();
    vi.mocked(writeFile).mockClear();
    vi.mocked(rename).mockClear();
    vi.mocked(unlink).mockClear();
  });

  afterEach(() => {
    if (originalNodeEnv === undefined) delete process.env.NODE_ENV;
    else process.env.NODE_ENV = originalNodeEnv;

    if (originalArtifactsPath === undefined) delete process.env.RESEARCH_ARTIFACTS_PATH;
    else process.env.RESEARCH_ARTIFACTS_PATH = originalArtifactsPath;
  });

  it('reports deprecated category usage across request and task artifacts', async () => {
    vi.mocked(readdir).mockResolvedValue([
      { isFile: () => true, name: 'research-request-1.json' },
    ] as Awaited<ReturnType<typeof readdir>>);

    vi.mocked(readFile).mockImplementation(async (filePath) => {
      const file = String(filePath).replace(/\\/g, '/');
      if (file.endsWith('research/category-taxonomy.json')) {
        return JSON.stringify({
          taxonomyVersion: '1.0.0',
          updatedAtUtc: '2026-05-10T00:00:00Z',
          categories: [
            { name: 'Nootropics', aliases: ['nootropic'] },
            { name: 'Anti-Aging', aliases: ['anti aging'], deprecated: true, replacedBy: 'Longevity' },
            { name: 'Longevity', aliases: [] },
          ],
        });
      }

      if (file.endsWith('research/research-requests/research-request-1.json')) {
        return JSON.stringify({
          requests: [{ requestId: 'req-1', compoundName: 'Epitalon', categories: ['Anti-Aging'] }],
        });
      }

      if (file.endsWith('research/pilot/research-task-queue.json')) {
        return JSON.stringify({
          items: [{ taskId: 'task-1', compoundName: 'Epitalon', categories: ['anti aging'] }],
          resolvedItems: [{ taskId: 'task-2', compoundName: 'MOTS-c', categories: ['Anti-Aging'] }],
        });
      }

      const error = Object.assign(new Error('missing'), { code: 'ENOENT' });
      throw error;
    });

    const response = await GET();
    const payload = await response.json();

    expect(response.status).toBe(200);
    expect(payload.counts).toEqual({
      requestFilesScanned: 1,
      requestFindings: 1,
      taskArtifactsScanned: 1,
      taskItemFindings: 1,
      resolvedTaskItemFindings: 1,
      totalFindings: 3,
    });
    expect(payload.deprecatedCategories).toEqual([
      {
        deprecatedCategory: 'Anti-Aging',
        replacementCategory: 'Longevity',
        findings: 3,
        requestFindings: 1,
        taskItemFindings: 1,
        resolvedTaskItemFindings: 1,
      },
    ]);
    expect(payload.findings).toEqual(expect.arrayContaining([
      expect.objectContaining({ sourceType: 'request', compoundName: 'Epitalon', matchedCategory: 'Anti-Aging' }),
      expect.objectContaining({ sourceType: 'task-item', compoundName: 'Epitalon', matchedCategory: 'anti aging' }),
      expect.objectContaining({ sourceType: 'resolved-task-item', compoundName: 'MOTS-c', matchedCategory: 'Anti-Aging' }),
    ]));
  });

  it('404s in production', async () => {
    process.env.NODE_ENV = 'production';

    const response = await GET();

    expect(response.status).toBe(404);
    expect(vi.mocked(readFile)).not.toHaveBeenCalled();
  });

  it('applies deprecated category rewrites and returns a receipt', async () => {
    vi.mocked(readdir).mockResolvedValue([
      { isFile: () => true, name: 'research-request-1.json' },
    ] as Awaited<ReturnType<typeof readdir>>);

    vi.mocked(readFile).mockImplementation(async (filePath) => {
      const file = String(filePath).replace(/\\/g, '/');
      if (file.endsWith('research/category-taxonomy.json')) {
        return JSON.stringify({
          taxonomyVersion: '1.0.0',
          updatedAtUtc: '2026-05-10T00:00:00Z',
          categories: [
            { name: 'Anti-Aging', aliases: ['anti aging'], deprecated: true, replacedBy: 'Longevity' },
            { name: 'Longevity', aliases: [] },
          ],
        });
      }

      if (file.endsWith('research/research-requests/research-request-1.json')) {
        return JSON.stringify({
          requests: [{ requestId: 'req-1', compoundName: 'Epitalon', categories: ['Anti-Aging', 'Longevity'] }],
        });
      }

      if (file.endsWith('research/pilot/research-task-queue.json')) {
        return JSON.stringify({
          items: [{ taskId: 'task-1', compoundName: 'Epitalon', categories: ['anti aging', 'Longevity'] }],
          resolvedItems: [{ taskId: 'task-2', compoundName: 'MOTS-c', categories: ['Anti-Aging'] }],
        });
      }

      const error = Object.assign(new Error('missing'), { code: 'ENOENT' });
      throw error;
    });

    const response = await POST();
    const payload = await response.json();

    expect(response.status).toBe(200);
    expect(payload.counts).toEqual({
      totalFilesUpdated: 2,
      requestFilesUpdated: 1,
      taskArtifactsUpdated: 1,
      categoriesRewritten: 3,
      requestCategoriesRewritten: 1,
      taskItemCategoriesRewritten: 1,
      resolvedTaskItemCategoriesRewritten: 1,
    });
    expect(payload.updatedFiles).toEqual(expect.arrayContaining([
      expect.objectContaining({ sourceType: 'request-file', sourcePath: 'research/research-requests/research-request-1.json', categoriesRewritten: 1, compounds: ['Epitalon'] }),
      expect.objectContaining({ sourceType: 'task-artifact', sourcePath: 'research/pilot/research-task-queue.json', categoriesRewritten: 2, compounds: ['Epitalon', 'MOTS-c'] }),
    ]));
    expect(vi.mocked(writeFile)).toHaveBeenCalledTimes(3);
    expect(vi.mocked(rename)).toHaveBeenCalledTimes(3);
    const writtenPayloads = vi.mocked(writeFile).mock.calls.map((call) => JSON.parse(String(call[1])));
    expect(writtenPayloads).toEqual(expect.arrayContaining([
      expect.objectContaining({
        requests: [expect.objectContaining({ categories: ['Longevity'] })],
      }),
      expect.objectContaining({
        items: [expect.objectContaining({ categories: ['Longevity'] })],
        resolvedItems: [expect.objectContaining({ categories: ['Longevity'] })],
      }),
      expect.objectContaining({
        entries: [expect.objectContaining({ action: 'apply-migration-fixup' })],
      }),
    ]));
  });

  it('writes an audit receipt when fix-up is applied', async () => {
    vi.mocked(readdir).mockResolvedValue([
      { isFile: () => true, name: 'research-request-1.json' },
    ] as Awaited<ReturnType<typeof readdir>>);

    vi.mocked(readFile).mockImplementation(async (filePath) => {
      const file = String(filePath).replace(/\\/g, '/');
      if (file.endsWith('research/category-taxonomy.json')) {
        return JSON.stringify({
          taxonomyVersion: '1.0.0',
          updatedAtUtc: '2026-05-10T00:00:00Z',
          categories: [
            { name: 'Anti-Aging', aliases: ['anti aging'], deprecated: true, replacedBy: 'Longevity' },
            { name: 'Longevity', aliases: [] },
          ],
        });
      }

      if (file.endsWith('research/research-requests/research-request-1.json')) {
        return JSON.stringify({
          requests: [{ requestId: 'req-1', compoundName: 'Epitalon', categories: ['Anti-Aging'] }],
        });
      }

      if (file.endsWith('research/pilot/research-task-queue.json')) {
        return JSON.stringify({ items: [], resolvedItems: [] });
      }

      if (file.endsWith('research/category-taxonomy-audit.json')) {
        return JSON.stringify({ schemaVersion: '1.0.0', updatedAtUtc: '2026-05-10T00:00:00Z', entries: [] });
      }

      const error = Object.assign(new Error('missing'), { code: 'ENOENT' });
      throw error;
    });

    const response = await POST();
    const payload = await response.json();

    expect(response.status).toBe(200);
    const writtenPayloads = vi.mocked(writeFile).mock.calls.map((call) => JSON.parse(String(call[1])));
    expect(writtenPayloads).toEqual(expect.arrayContaining([
      expect.objectContaining({
        entries: [expect.objectContaining({
          action: 'apply-migration-fixup',
          summary: 'Applied taxonomy migration fix-up to 1 files.',
          applyReceipt: expect.objectContaining({ counts: payload.counts }),
        })],
      }),
    ]));
  });
});