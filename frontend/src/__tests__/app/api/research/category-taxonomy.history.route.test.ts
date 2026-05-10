import { GET } from '@/app/api/research/category-taxonomy/history/route';
import { readFile } from 'node:fs/promises';

vi.mock('node:fs/promises', () => ({
  default: {},
  readFile: vi.fn(),
}));

describe('research category-taxonomy history route', () => {
  const originalNodeEnv = process.env.NODE_ENV;

  beforeEach(() => {
    process.env.NODE_ENV = 'test';
    vi.mocked(readFile).mockReset();
  });

  afterEach(() => {
    if (originalNodeEnv === undefined) delete process.env.NODE_ENV;
    else process.env.NODE_ENV = originalNodeEnv;
  });

  it('returns taxonomy audit history', async () => {
    vi.mocked(readFile).mockResolvedValue(JSON.stringify({
      schemaVersion: '1.0.0',
      updatedAtUtc: '2026-05-10T12:00:00Z',
      entries: [
        { entryId: 'audit-1', action: 'save-taxonomy', createdAtUtc: '2026-05-10T12:00:00Z', taxonomyVersion: '1.0.0', summary: 'Saved taxonomy with 4 categories.' },
      ],
    }));

    const response = await GET();
    const payload = await response.json();

    expect(response.status).toBe(200);
    expect(payload.entries).toEqual([
      expect.objectContaining({ action: 'save-taxonomy', summary: 'Saved taxonomy with 4 categories.' }),
    ]);
  });

  it('404s in production', async () => {
    process.env.NODE_ENV = 'production';

    const response = await GET();

    expect(response.status).toBe(404);
    expect(vi.mocked(readFile)).not.toHaveBeenCalled();
  });
});