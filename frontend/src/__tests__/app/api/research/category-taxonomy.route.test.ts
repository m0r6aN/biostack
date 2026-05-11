import { PUT } from '@/app/api/research/category-taxonomy/route';
import { mkdir, readFile, rename, unlink, writeFile } from 'node:fs/promises';
import path from 'node:path';

vi.mock('node:fs/promises', () => ({
  default: {},
  mkdir: vi.fn().mockResolvedValue(undefined),
  readFile: vi.fn().mockRejectedValue(Object.assign(new Error('missing'), { code: 'ENOENT' })),
  writeFile: vi.fn().mockResolvedValue(undefined),
  rename: vi.fn().mockResolvedValue(undefined),
  unlink: vi.fn().mockResolvedValue(undefined),
}));

function request(body: unknown) {
  return new Request('http://localhost/api/research/category-taxonomy', { method: 'PUT', body: JSON.stringify(body) });
}

describe('research category-taxonomy route', () => {
  const originalNodeEnv = process.env.NODE_ENV;

  beforeEach(() => {
    process.env.NODE_ENV = 'test';
    vi.mocked(mkdir).mockClear();
    vi.mocked(readFile).mockReset().mockRejectedValue(Object.assign(new Error('missing'), { code: 'ENOENT' }));
    vi.mocked(writeFile).mockClear();
    vi.mocked(rename).mockClear();
    vi.mocked(unlink).mockClear();
  });

  afterEach(() => {
    if (originalNodeEnv === undefined) delete process.env.NODE_ENV;
    else process.env.NODE_ENV = originalNodeEnv;
  });

  it('saves a sanitized taxonomy artifact', async () => {
    const response = await PUT(request({
      categories: [
        { name: 'Nootropics', aliases: 'nootropic, nootropics, Nootropics' },
        { name: 'Cognitive Support', aliases: 'cognitive support', deprecated: true, replacedBy: 'Nootropics' },
        { name: 'Nootropics', aliases: ['duplicate'] },
        { name: 'Longevity', aliases: ['longevity'] },
        { name: 'Anti-Aging', aliases: ['anti-aging', 'anti aging', 'Longevity'], deprecated: true, replacedBy: 'Longevity' },
      ],
    }));
    const payload = await response.json();

    expect(response.status).toBe(200);
    expect(payload.taxonomyVersion).toBe('1.0.0');
    expect(payload.categories).toEqual([
      { name: 'Nootropics', aliases: ['nootropic'] },
      { name: 'Cognitive Support', aliases: [], deprecated: true, replacedBy: 'Nootropics' },
      { name: 'Longevity', aliases: [] },
      { name: 'Anti-Aging', aliases: [], deprecated: true, replacedBy: 'Longevity' },
    ]);
    expect(vi.mocked(mkdir)).toHaveBeenCalledWith(expect.stringContaining(`${path.sep}research`), { recursive: true });
    expect(vi.mocked(rename)).toHaveBeenCalledTimes(2);

    const writtenPayloads = vi.mocked(writeFile).mock.calls.map((call) => JSON.parse(String(call[1])));
    expect(writtenPayloads).toEqual(expect.arrayContaining([
      expect.objectContaining({ categories: payload.categories }),
      expect.objectContaining({
        entries: [expect.objectContaining({ action: 'save-taxonomy', summary: 'Saved taxonomy with 4 categories.' })],
      }),
    ]));
  });

  it('requires at least one valid category', async () => {
    const response = await PUT(request({ categories: [{ name: '   ', aliases: '' }] }));

    expect(response.status).toBe(400);
    expect(await response.json()).toEqual({ error: 'At least one valid category is required.' });
    expect(vi.mocked(writeFile)).not.toHaveBeenCalled();
  });

  it('drops replacement canon when an entry is not deprecated', async () => {
    const response = await PUT(request({ categories: [{ name: 'Performance', aliases: 'performance', deprecated: false, replacedBy: 'Recovery' }] }));
    const payload = await response.json();

    expect(response.status).toBe(200);
    expect(payload.categories).toEqual([{ name: 'Performance', aliases: [] }]);
  });

  it('404s in production', async () => {
    process.env.NODE_ENV = 'production';

    const response = await PUT(request({ categories: [{ name: 'Nootropics', aliases: '' }] }));

    expect(response.status).toBe(404);
    expect(vi.mocked(writeFile)).not.toHaveBeenCalled();
  });
});